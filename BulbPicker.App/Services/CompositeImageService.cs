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
    public class CompositeImageFragment
    {
        public Bitmap Image { get; init; }
        public BaslerCameraPosition CameraPosition { get; init; }
        public CompositeImageFragment(Bitmap image, BaslerCameraPosition cameraPosition)
        {
            Image = image;
            CameraPosition = cameraPosition;
        }
    }

    // 이게 꼭 있어야 하나 싶긴 한데 일단 급하니 유지하고 사용한다.
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

        private readonly ObservableCollection<CompositeImageFragment> _firstRowCompositeImageQuque;
        private DispatcherTimer _firstRowClearTimer;


        private CompositeImageService ( )
        {
            _dispatcher = Application.Current.Dispatcher;

            _firstRowCompositeImageQuque = new ObservableCollection<CompositeImageFragment>();
            _firstRowCompositeImageQuque.CollectionChanged += FirstRowImagesToCombine_CollectionChanged;
        }

        public void AddToCompositionQueue(CompositeImageFragment fragment)
        {
            _dispatcher.BeginInvoke(() =>
            {
                _firstRowCompositeImageQuque.Add(fragment);
            }, DispatcherPriority.Background);
        }

        // TODO 0831: Ensure Thread Safety
        private void FirstRowImagesToCombine_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    // combine the two
                    if (_firstRowCompositeImageQuque.Count == 2)
                    {
                        Bitmap outside = null;
                        Bitmap inside = null;

                        CompositeImageFragment firstFragment = _firstRowCompositeImageQuque[0];
                        CompositeImageFragment secondFragment = _firstRowCompositeImageQuque[1];

                        outside = firstFragment.CameraPosition == BaslerCameraPosition.Outisde ? firstFragment.Image : secondFragment.Image;
                        inside = firstFragment.CameraPosition == BaslerCameraPosition.Inside ? firstFragment.Image : secondFragment.Image;

                        if(outside == null || inside == null)
                        {
                            MessageBox.Show("Unexpected Error Occurred in CompositeImageService.");
                            return;
                        }

                        CompositeImageRowBuffer row = new CompositeImageRowBuffer(outside, inside);
                        FireBulbPickingSequence(row);
                    }
                    // clear queue items
                    else if (_firstRowCompositeImageQuque.Count == 1)
                    {
                        _firstRowClearTimer?.Stop();
                        // 500ms
                        _firstRowClearTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                        _firstRowClearTimer.Tick += (_, __) =>
                        {
                            _firstRowClearTimer.Stop();
                            _firstRowCompositeImageQuque.Clear();
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

        // TODO 0830 1st : Make this async
        private void FireBulbPickingSequence(CompositeImageRowBuffer rowImages)
        {
            // first ever row of images
            if (_compositImageRowBuffer == null)
            {
                _compositImageRowBuffer = rowImages;
                return;
            }

            // COMBINE
            var combinedBitmap = Combine2x2Images(rowImages.Outside, rowImages.Inside, _compositImageRowBuffer.Outside, _compositImageRowBuffer.Inside);

            // AI
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "YoloModel", "best_640.onnx");
            var model = new Yolov11Onnx(modelPath);
            // TODO 0831: 일단은 resize 여기서 함... 바꿔야함..ㅎㅎ
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
            _compositImageRowBuffer.Inside.Dispose();
            _compositImageRowBuffer.Outside.Dispose();
            
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
