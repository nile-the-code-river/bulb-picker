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

    // 0830 TODO: LOCK elements used in multiple threads
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

        // 0830 TODO 1st : Refactor
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
                        CompositeImage_FactoryVerTest(row);


                        // 0830 TODO 1st : 여기에 관련 로직 차례 차례 넣어야 할 듯

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


        // 0830 TODO 1: 지워도 될 듯. 지워도 문제 없이 돌아가면 지우기
        private static Bitmap Snapshot(Bitmap src)
        {
            using (var ms = new MemoryStream())
            {
                src.Save(ms, ImageFormat.Bmp);
                ms.Position = 0;
                return new Bitmap(ms);
            }
        }

        // 0830 TODO 1st : Refactor
        private int _rowCount = 0;
        private void CompositeImage_FactoryVerTest(CompositeImageRowBuffer rowImages)
        {

            if (_rowCount == 0)
            {
                _compositImageRowBuffer = rowImages;
                _rowCount++;
                return;
            }

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


            // SEND TO SCARA LOGIC
            // sep to another method
            for (int i = 0; i < boxesValue.Count; i++)
            {
                //if (boxesValue[i].Y_Center <= 78 || boxesValue[i].Y_Center > 266)
                if (boxesValue[i].Y_Center <= 68 || boxesValue[i].Y_Center > 240)
                {
                    LogService.Instance.AddLog(new Log($"skipped (y: {boxesValue[i].Y_Center})", LogType.FOR_TEST));
                    continue;
                }

                bool isForOutside = boxesValue[i].X_Center < 320;
                RobotArmPosition firstRowPosition = isForOutside ? RobotArmPosition.FirstRowOutside : RobotArmPosition.FirstRowInside;


                float yValue = boxesValue[i].Y_Center;
                float xValue = boxesValue[i].X_Center;
                //float zValue = (boxesValue[0].y2 - boxesValue[0].y1) / 2.54f;

                float scaraXValue = 0f;
                float scaraYValue = 0f;

                int testXOffSet = 30;

                if (isForOutside) // SCARA 1
                {
                    // 
                    scaraXValue = (yValue) - 121 + testXOffSet;
                    scaraYValue = (xValue) - 837 + 0;
                }else
                {
                    scaraXValue = (yValue) - 71 + 0;
                    scaraYValue = (xValue) - 1003 + 0;

                }


                var zTestValue = Math.Min(boxesValue[i].x2 - boxesValue[i].x1, boxesValue[i].y2 - boxesValue[i].y1);

                float scaraZValue = (zTestValue) + 55 + 0;
                // Big bulb
                //float scaraZValue = (zTestValue) + 42 + 0;

                string pickUpPoint = "1," + scaraXValue.ToString("0.000") + "," + scaraYValue.ToString("0.000") + "," + (scaraZValue).ToString("0.000") + ",1,0,0\r";


                var firstRowRobotArm = RobotArmService.Instance.RobotArms.Where(x => x.Position == firstRowPosition).FirstOrDefault();
                firstRowRobotArm.SendPickUpPoint(pickUpPoint);

                //
                string testPositionStr = isForOutside ? "OUTSIDE (1)" : "INSIDE (2)";
                LogService.Instance.AddLog(new Log($"Coordinates SENT to {testPositionStr} \nx: {scaraXValue}, y:{scaraYValue}, z:{scaraZValue}", LogType.FOR_TEST));
            }

            // TEST
            var resultImage = ImageVisualizer.DrawBoxes(resized, boxesValue);
            FileSaveService.SaveBitmapTo(resultImage, FolderName.BoundingBoxImage, TestIndexManager.Instance.CombinedImageIndex.ToString());

            // after operation is completed
            _compositImageRowBuffer = rowImages;
            _rowCount++;
            TestIndexManager.Instance.IncrementCombinedImageIndex();
        }

        public Bitmap Combine2x2Images(Bitmap outsideAfter, Bitmap insideAfter, Bitmap outsideBefore, Bitmap insideBefore)
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

            FileSaveService.SaveBitmapTo(combinedBitmap, FolderName.ImageComposition, TestIndexManager.Instance.CombinedImageIndex.ToString());

            return combinedBitmap;
        }
    }
}
