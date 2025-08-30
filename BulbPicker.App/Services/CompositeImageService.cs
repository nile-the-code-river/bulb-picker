using BulbPicker.App.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using WindowsFormsApp1;

namespace BulbPicker.App.Services
{
    public class ImageToCompositeQuqueItem
    {
        public Bitmap Image { get; init; }
        public BaslerCameraPosition CameraPosition { get; init; }
        public ImageToCompositeQuqueItem(Bitmap image, BaslerCameraPosition cameraPosition)
        {
            Image = image;
            CameraPosition = cameraPosition;
        }
    }

    public class CompositeImageRowBuffer
    {
        public Bitmap Outside { get; set; }
        public Bitmap Inside { get; set; }
        public CompositeImageRowBuffer(Bitmap outside, Bitmap inside)
        {
            Outside = outside;
            Inside = inside;
        }
    }

    // TODO 0830: LOCK elements used in multiple threads
    public class CompositeImageService
    {
        private static readonly CompositeImageService _instance = new CompositeImageService();
        public static CompositeImageService Instance => _instance;


        private readonly Dispatcher _dispatcher;

        private CompositeImageRowBuffer _compositImageRowBuffer = null;

        private readonly ObservableCollection<ImageToCompositeQuqueItem> _firstRowImageToCompositeQuque;
        private DispatcherTimer _firstRowClearTimer;


        private CompositeImageService ( )
        {
            _dispatcher = Application.Current.Dispatcher;

            _firstRowImageToCompositeQuque = new ObservableCollection<ImageToCompositeQuqueItem>();
            _firstRowImageToCompositeQuque.CollectionChanged += FirstRowImagesToCombine_CollectionChanged;
        }

        public void AddToCompositionQueue(ImageToCompositeQuqueItem queueItem)
        {
            _dispatcher.BeginInvoke(() =>
            {
                _firstRowImageToCompositeQuque.Add(queueItem);
            }, DispatcherPriority.Background);
        }

        // TODO 0830 1st : Refactor - 조금 걸릴 듯
        private void FirstRowImagesToCombine_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    // combine the two
                    if (_firstRowImageToCompositeQuque.Count == 2)
                    {
                        Bitmap outside = null;
                        Bitmap inside = null;

                        ImageToCompositeQuqueItem formerItem = _firstRowImageToCompositeQuque.FirstOrDefault();
                        ImageToCompositeQuqueItem newItem = e.NewItems[0] as ImageToCompositeQuqueItem;

                        outside = formerItem.CameraPosition == BaslerCameraPosition.Outisde ? formerItem.Image : newItem.Image;
                        inside = formerItem.CameraPosition == BaslerCameraPosition.Inside ? formerItem.Image : newItem.Image;

                        if(outside == null || inside == null)
                        {
                            MessageBox.Show("Unexpected Error Occurred in CompositeImageService.");
                            return;
                        }

                        CompositeImageRowBuffer row = new CompositeImageRowBuffer(new Bitmap(outside), new Bitmap(inside));
                        FireBulbPickingSequence(row);
                    }
                    // fire deleting sequence
                    else if (_firstRowImageToCompositeQuque.Count == 1)
                    {
                        _firstRowClearTimer?.Stop();
                        _firstRowClearTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                        _firstRowClearTimer.Tick += (_, __) =>
                        {
                            _firstRowClearTimer.Stop();
                            _firstRowImageToCompositeQuque.Clear();
                        };
                        _firstRowClearTimer.Start();
                    }
                    else
                    {
                        MessageBox.Show($"Unexpected Number of Items in _firstRowImageToCompositeQuque. Stopwatch is now {TestIndexManager.Instance.GetStopwatchMilliSecondsNow()}");
                    }
                break;
            }
        }


        // TODO 0830 1st: 지워도 될 듯. 지워도 문제 없이 돌아가면 지우기
        private static Bitmap Snapshot(Bitmap src)
        {
            using (var ms = new MemoryStream())
            {
                src.Save(ms, ImageFormat.Bmp);
                ms.Position = 0;
                return new Bitmap(ms);
            }
        }

        // TODO 0830 1st : Make this async
        private void FireBulbPickingSequence(CompositeImageRowBuffer rowImages)
        {
            // first ever row of images
            if (_compositImageRowBuffer == null)
            {
                _compositImageRowBuffer = rowImages;
                return;
            }


            // TODO 0830 : 안 쓸 수 있는 기능 전부 다 쓰지 말기
            using var outsideAfter = Snapshot(rowImages.Outside);
            using var insideAfter = Snapshot(rowImages.Inside);
            using var outsideBefore = Snapshot(_compositImageRowBuffer.Outside);
            using var insideBefore = Snapshot(_compositImageRowBuffer.Inside);

            // COMBINE
            var combinedBitmap = Combine2x2Images(outsideAfter, insideAfter, outsideBefore, insideBefore);

            // AI
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "YoloModel", "best_640.onnx");
            var model = new Yolov11Onnx(modelPath);
            // TODO: 일단은 resize 여기서 함... 바꿔야함..ㅎㅎ
            Bitmap resized = new Bitmap(combinedBitmap, new System.Drawing.Size(640, 640));
            var boxesValue = model.PredictBoxes(resized);


            // Define the pick up point and send it to corresponding robot arm
            for (int i = 0; i < boxesValue.Count; i++)
            {
                var tempBoxValue = boxesValue[i];

                BulbPickUpPoint pickUpPoint = GetBulbPickUpPoint(new BulbBoundingBox(tempBoxValue.x1, tempBoxValue.y1, tempBoxValue.x2, tempBoxValue.y2, tempBoxValue.X_Center, tempBoxValue.Y_Center));

                // detected bulb is out of the safe area
                if (pickUpPoint == null) continue;

                var firstRowRobotArm = RobotArmService.Instance.RobotArms.Where(x => x.Position == pickUpPoint.CorrespondingRobotArm).FirstOrDefault();

                if (firstRowRobotArm == null) MessageBox.Show("ERROR: There is no such robot arm");

                firstRowRobotArm.SendPickUpPoint(pickUpPoint);
            }

            // TEST
            var resultImage = ImageVisualizer.DrawBoxes(resized, boxesValue);
            FileSaveService.SaveBitmapTo(resultImage, FolderName.BoundingBoxImage, TestIndexManager.Instance.CombinedImageIndex.ToString());

            // after operation is completed
            _compositImageRowBuffer = rowImages;
            TestIndexManager.Instance.IncrementCombinedImageIndex();
        }

        private Bitmap Combine2x2Images(Bitmap outsideAfter, Bitmap insideAfter, Bitmap outsideBefore, Bitmap insideBefore)
        {
            int width = outsideAfter.Width;
            int height = insideAfter.Height;

            int widthArg1 = 230;
            int heightArg2 = 300;
            int padding = 1458;

            Bitmap combinedBitmap = new Bitmap(width * 2 - widthArg1, height * 2 + padding);
            using (Graphics g = Graphics.FromImage(combinedBitmap))
            {
                g.Clear(Color.Black);
                g.DrawImage(outsideBefore, 0, height - heightArg2, width, height);
                g.DrawImage(insideBefore, width - widthArg1, height - heightArg2, width, height);
                g.DrawImage(outsideAfter, 0, 0, width, height - heightArg2);
                g.DrawImage(insideAfter, width - widthArg1, 0, width, height - heightArg2);
            }

            //FileSaveService.SaveBitmapTo(combinedBitmap, FolderName.ImageComposition, TestIndexManager.Instance.CombinedImageIndex.ToString());

            return combinedBitmap;
        }

        /// <returns>Null if bulb should not be picked up (out of 'safe area')</returns>
        private BulbPickUpPoint? GetBulbPickUpPoint(BulbBoundingBox boundingBox)
        {
            // out of safe area
            //if (boxesValue[i].Y_Center <= 78 || boxesValue[i].Y_Center > 266)
            if (boundingBox.YCenter <= 68 || boundingBox.YCenter > 240)
            {
                LogService.Instance.AddLog(new Log($"skipped (y: {boundingBox.YCenter})", LogType.FOR_TEST));
                return null;
            }


            BulbPickUpPoint pickUpPoint = new BulbPickUpPoint();

            float defaultRobotArmOffset_X = 0;
            float defaultRobotArmOffset_Y = 0;
            float defaultRobotArmOffset_Z = 55;

            float manualRobotArmOffset_X = 30;
            float manualRobotArmOffset_Y = 0;
            float manualRobotArmOffset_Z = 0;


            RobotArmPosition correspondingRobotArmPosition =
                boundingBox.XCenter < 320 ? RobotArmPosition.FirstRowOutside : RobotArmPosition.FirstRowInside;

            // retrieve default robot arm offset X & Y
            switch (correspondingRobotArmPosition)
            {
                case RobotArmPosition.FirstRowOutside:
                    defaultRobotArmOffset_X = -121;
                    defaultRobotArmOffset_Y = -837;
                    break;
                case RobotArmPosition.FirstRowInside:
                    defaultRobotArmOffset_X = -71;
                    defaultRobotArmOffset_Y = -1003;
                    break;
                case RobotArmPosition.SecondRowOutside:
                case RobotArmPosition.SecondRowInside:
                default:
                    MessageBox.Show("Unexpected Corresponding Robot Arm Position");
                    break;
            }

            // x is set using YCenter, and y is set using XCenter
            pickUpPoint.SetX(boundingBox.YCenter + defaultRobotArmOffset_X + manualRobotArmOffset_X);
            pickUpPoint.SetY(boundingBox.XCenter + defaultRobotArmOffset_Y + manualRobotArmOffset_Y);
            // shortest line(s) of the bounding box
            pickUpPoint.SetZ(Math.Min(boundingBox.X2 - boundingBox.X1, boundingBox.Y2 - boundingBox.Y1) + defaultRobotArmOffset_Z + manualRobotArmOffset_Z);
            pickUpPoint.SetCorrespondingRobotArm(correspondingRobotArmPosition);

            return pickUpPoint;
        }
    }
}
