using BulbPicker.App.AI;
using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
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
    public class CompositeImageService : ObservableObject
    {
        private static readonly CompositeImageService _instance = new CompositeImageService();
        public static CompositeImageService Instance => _instance;


        private readonly Dispatcher _dispatcher;

        private CompositeImageRowBuffer _compositImageRowBuffer = null;

        private readonly ObservableCollection<CompositeImageFragment> _firstRowCompositeImageQuque;
        private DispatcherTimer _firstRowClearTimer;

        private BitmapSource _firstRowCompositeImageSource;
        public BitmapSource FirstRowCompositeImageSource
        {
            get => _firstRowCompositeImageSource;
            private set
            {
                _firstRowCompositeImageSource = value;
                OnPropertyChanged(nameof(FirstRowCompositeImageSource));
            }
        }

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

                        // DO NOT use for loop. Explicitly specify index like it does now. (ex. _firstRowCompositeImageQuque[0])
                        // Items might be added while in this function because camera runs on multi threads.
                        CompositeImageFragment firstFragment = _firstRowCompositeImageQuque[0];
                        CompositeImageFragment secondFragment = _firstRowCompositeImageQuque[1];

                        outside = firstFragment.CameraPosition == BaslerCameraPosition.Outisde ? firstFragment.Image : secondFragment.Image;
                        inside = firstFragment.CameraPosition == BaslerCameraPosition.Inside ? firstFragment.Image : secondFragment.Image;

                        if(outside == null || inside == null)
                        {
                            // Images might be both outside or both inside in this case, which can happen due to the camera(s)' instability.
                            // or another thread might have operated _firstRowCompositeImageQuque.Clear() just before this line was called, which is also due to the camera(s)' instability.
                            MessageBox.Show("Unexpected Error Occurred in CompositeImageService.");
                            return;
                        }

                        // Clone bitmap becuase it should dispose all bitmaps in _firstRowCompositeImageQuque before Clear().
                        // Calling Dispose() after successful image composition might lead to potential GDI lick for SOME bitmaps if the camera(s) is/are unstable.
                        Bitmap outsideClone = (Bitmap)outside.Clone();
                        Bitmap insideClone = (Bitmap)inside.Clone();
                        
                        CompositeImageRowBuffer row = new CompositeImageRowBuffer(outsideClone, insideClone);
                        // should be async
                        FireBulbPickingSequence(row);

                        outside.Dispose();
                        inside.Dispose();
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

                            foreach (var item in _firstRowCompositeImageQuque)
                            {
                                item.Image.Dispose();
                            }

                            _firstRowCompositeImageQuque.Clear();
                        };
                        _firstRowClearTimer.Start();
                    }
                    else
                    {
                        MessageBox.Show($"Unexpected Number of Items in _firstRowImageToCompositeQuque.");
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
            //var combinedBitmap = Combine2x2Images(rowImages.Outside, rowImages.Inside, _compositImageRowBuffer.Outside, _compositImageRowBuffer.Inside);

            OpenCvSharp.Mat combinedMat = ImageCombiner.Combine2x2WithScale(rowImages.Outside, rowImages.Inside, _compositImageRowBuffer.Outside, _compositImageRowBuffer.Inside);

            // 0831
            //string modelName = "best_640";
            // 0901＿１
            //string modelName = "new_best_640";
            // 0901_2
            //string modelName = "best_gray_final6";
            // 0902_1
            string modelName = "latest_best";

            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "YoloModel", $"{modelName}.onnx");
            var model = new Yolov11Onnx(modelPath);
            var boxesValue = model.PredictBoxes(combinedMat);

            // Define the pick up point and send it to corresponding robot arm
            for (int i = 0; i < boxesValue.Count; i++)
            {
                var tempBoxValue = boxesValue[i];

                BulbBoundingBox boundingBox = new BulbBoundingBox(tempBoxValue.x1, tempBoxValue.y1, tempBoxValue.x2, tempBoxValue.y2, tempBoxValue.X_Center, tempBoxValue.Y_Center);

                var correspondingRobotArm = DecideRobotArm(boundingBox);

                BulbPickUpPoint pickUpPoint = GetBulbPickUpPoint(boundingBox, correspondingRobotArm);

                // detected bulb is out of the safe area
                if (pickUpPoint == null) continue;

                var firstRowRobotArm = RobotArmService.Instance.RobotArms.Where(x => x.Position == pickUpPoint.CorrespondingRobotArm).FirstOrDefault();

                if (firstRowRobotArm == null) MessageBox.Show("ERROR: There is no such robot arm");

                firstRowRobotArm.SendPickUpPoint(pickUpPoint);
            }

            var resultImage = ImageVisualizer.DrawBoxes(combinedMat, boxesValue);
            FileSaveService.SaveBitmapTo(resultImage, FolderName.BoundingBoxImage, modelName + "__" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            // TODO: Cut areas out of the safe

            // bounding box 사진을 내보내야 하나, 정사각형으로 하나 safe area만 보이게 하나, 합성만 된 사진을 보내야하나 고민이지만 일단 합성만 된 사진 보여줌
            var displayImage = BitmapManager.BitmapToImageSource(resultImage);
            _dispatcher.Invoke(() =>
            {
                FirstRowCompositeImageSource = displayImage;
            }, DispatcherPriority.DataBind);


            // after operation is completed
            _compositImageRowBuffer.Inside.Dispose();
            _compositImageRowBuffer.Outside.Dispose();
            
            _compositImageRowBuffer = rowImages;

            TestIndexManager.Instance.IncrementCombinedImageIndex();
        }

        //private Bitmap Combine2x2Images(Bitmap outsideAfter, Bitmap insideAfter, Bitmap outsideBefore, Bitmap insideBefore)
        //{
        //    int singleWidth = outsideAfter.Width;
        //    int singleHeight = insideAfter.Height;

        //    int X_offset = 115;
        //    int Y_offset = 310;
        //    int Padding = singleWidth - X_offset - singleHeight + Y_offset;
        //    int new_width = singleWidth - X_offset;
        //    int new_height = singleHeight - Y_offset;

        //    Bitmap CombinedImage = new Bitmap(new_width * 2, (new_height + Padding) * 2);

        //    using (Graphics g = Graphics.FromImage(CombinedImage))
        //    {

        //        g.Clear(Color.Black);
        //        Rectangle srcRectOA = new Rectangle(0, 0, new_width, new_height);
        //        Rectangle srcRectIA = new Rectangle(X_offset, 0, new_width, new_height);
        //        Rectangle srcRectOB = new Rectangle(0, Y_offset, new_width, new_height);
        //        Rectangle srcRectIB = new Rectangle(X_offset, Y_offset, new_width, new_height);
        //        Rectangle destRectOA = new Rectangle(0, Padding, new_width, new_height);
        //        Rectangle destRectIA = new Rectangle(new_width, Padding, new_width, new_height);
        //        Rectangle destRectOB = new Rectangle(0, new_height + Padding, new_width, new_height);
        //        Rectangle destRectIB = new Rectangle(new_width, new_height + Padding, new_width, new_height);

        //        g.DrawImage(outsideAfter, destRectOA, srcRectOA, GraphicsUnit.Pixel);
        //        g.DrawImage(insideAfter, destRectIA, srcRectIA, GraphicsUnit.Pixel);
        //        g.DrawImage(outsideBefore, destRectOB, srcRectOB, GraphicsUnit.Pixel);
        //        g.DrawImage(insideBefore, destRectIB, srcRectIB, GraphicsUnit.Pixel);
        //    }
        //    //FileSaveService.SaveBitmapTo(combinedBitmap, FolderName.ImageComposition, TestIndexManager.Instance.CombinedImageIndex.ToString());

        //    return CombinedImage;
        //}

        // temp fix
        private float _paddingOffset = 95.47f;

        private RobotArm DecideRobotArm(BulbBoundingBox boundingBox)
        {
            RobotArmPosition correspondingRobotArmPosition =
                boundingBox.XCenter < 320 ? RobotArmPosition.FirstRowOutside : RobotArmPosition.FirstRowInside;

            var firstRowRobotArm = RobotArmService.Instance.RobotArms.Where(x => x.Position == correspondingRobotArmPosition).FirstOrDefault();

            if (firstRowRobotArm == null) MessageBox.Show("ERROR: There is no such robot arm");

            return firstRowRobotArm;
        }

        /// <returns>Null if bulb should not be picked up (out of 'safe area')</returns>
        private BulbPickUpPoint? GetBulbPickUpPoint(BulbBoundingBox boundingBox, RobotArm robotArm)
        {
            // out of safe area
            //if (boxesValue[i].Y_Center <= 78 || boxesValue[i].Y_Center > 266)
            if (boundingBox.YCenter <= 65 + _paddingOffset || boundingBox.YCenter > 252 + _paddingOffset) // 160.5 , 347.5
            {
                //LogService.Instance.AddLog(new Log($"skipped (y: {boundingBox.YCenter})", LogType.FOR_TEST));
                return null;
            }

            BulbPickUpPoint pickUpPoint = new BulbPickUpPoint();

            float manualRobotArmOffset_X = 30 - _paddingOffset;
            float manualRobotArmOffset_Y = 0;
            float manualRobotArmOffset_Z = 55;

            // retrieve default robot arm offset X & Y
            switch (robotArm.Position)
            {
                case RobotArmPosition.FirstRowOutside:
                    manualRobotArmOffset_X += -121;
                    manualRobotArmOffset_Y += -837;
                    break;
                case RobotArmPosition.FirstRowInside:
                    manualRobotArmOffset_X += -71;
                    manualRobotArmOffset_Y += -1003;
                    break;
                case RobotArmPosition.SecondRowOutside:
                case RobotArmPosition.SecondRowInside:
                default:
                    MessageBox.Show("Unexpected Corresponding Robot Arm Position");
                    break;
            }

            // x is set using YCenter, and y is set using XCenter
            float finalX = boundingBox.YCenter + robotArm.Offsets.X + manualRobotArmOffset_X;
            float finalY = boundingBox.XCenter + robotArm.Offsets.Y + manualRobotArmOffset_Y;
            // shortest line(s) of the bounding box
            float finalZ = Math.Min(boundingBox.X2 - boundingBox.X1, boundingBox.Y2 - boundingBox.Y1) + robotArm.Offsets.Z + manualRobotArmOffset_Z;

            //LogService.Instance.AddLog(new Log($"finalX = {boundingBox.YCenter} + {robotArm.Offsets.X} + {manualRobotArmOffset_X}"
            //                                    + $"\nfinalY = {boundingBox.XCenter} + {robotArm.Offsets.Y} + {manualRobotArmOffset_Y}"
            //                                    + $"\nfinalZ = {Math.Min(boundingBox.X2 - boundingBox.X1, boundingBox.Y2 - boundingBox.Y1)} + {robotArm.Offsets.Z} + {manualRobotArmOffset_Z}", LogType.FOR_TEST));

            pickUpPoint.SetX(finalX);
            pickUpPoint.SetY(finalY);
            pickUpPoint.SetZ(finalZ);
            pickUpPoint.SetCorrespondingRobotArm(robotArm.Position);

            return pickUpPoint;
        }
    }
}
