using BulbPicker.App.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WindowsFormsApp1;

namespace BulbPicker.App.Services
{
    public class ImageToCompositeQuqueItem
    {
        public Bitmap Image { get; set; }
        public BaslerCameraPosition CameraPosition { get; set; }
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

    // TODO: LOCK elements used in multiple threads
    public class CompositeImageService
    {
        private static readonly CompositeImageService _instance = new CompositeImageService();
        public static CompositeImageService Instance => _instance;

        private readonly Dispatcher _dispatcher;

        // TODO: 쓸려고 일단 만들었는데 쓸 거면 쓰기
        public ObservableCollection<BitmapSource> TestImages { get; private set; } = new ObservableCollection<BitmapSource>();


        // TODO: OLD LOGIC. DELETE
        private CompositeImage _compositeImageBuffer = new CompositeImage();



        private readonly ObservableCollection<ImageToCompositeQuqueItem> _firstRowImageToCompositeQuque;
        // TODO: _secondRowImageToCompositeQueue

        private CompositeImageRowBuffer _compositImageRowBuffer = null;


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

        // TODO: DONE BY HERE__________________________________
        private DispatcherTimer _firstRowClearTimer;
        private void FirstRowImagesToCombine_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    LogService.Instance.AddLog(new Log($"Added ", LogType.Connected));

                    if (_firstRowImageToCompositeQuque.Count == 2)
                    {
                        // if 2 items -> shoot to CompositeImages
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
                    }
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
                case NotifyCollectionChangedAction.Remove:
                    LogService.Instance.AddLog(new Log($"count: {_firstRowImageToCompositeQuque.Count}", LogType.Connected));
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }


        // TODO 1: 지워도 될 듯. 지워도 문제 없이 돌아가면 지우기
        private static Bitmap Snapshot(Bitmap src)
        {
            using (var ms = new MemoryStream())
            {
                src.Save(ms, ImageFormat.Bmp);
                ms.Position = 0;
                return new Bitmap(ms);
            }
        }

        // TODO: 이름 바꾸고 메인 로직으로 만들기
        private int _rowCount = 0;
        private void CompositeImage_FactoryVerTest(CompositeImageRowBuffer rowImages)
        {
            if(_rowCount == 0)
            {
                _compositImageRowBuffer = rowImages;
                _rowCount++;
                return;
            }

            using var outsideAfter = Snapshot(rowImages.Outside);
            using var insideAfter = Snapshot(rowImages.Inside);
            using var outsideBefore = Snapshot(_compositImageRowBuffer.Outside);
            using var insideBefore = Snapshot(_compositImageRowBuffer.Inside);

            var combinedBitmap = CombineImages(outsideAfter, insideAfter, outsideBefore, insideBefore);

            string modelName = "best_640";
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "YoloModel", $"{modelName}.onnx");
            var model = new Yolov11Onnx(modelPath);
            // TODO: 일단은 resize 여기서 함... 바꿔야함..ㅎㅎ
            Bitmap resized = new Bitmap(combinedBitmap, new System.Drawing.Size(640, 640));
            var boxesValue = model.PredictBoxes(resized);

            foreach(var boxValue in boxesValue)
            {
                if(boxValue.Y_Center <= 140 || boxValue.Y_Center > 366)
                {
                    LogService.Instance.AddLog(new Log("skipped", LogType.FOR_TEST));
                    continue;
                }

                // TODO: send coordinates to SCARAs
                LogService.Instance.AddLog(new Log($"x: {boxValue.X_Center}, y:{boxValue.Y_Center}", LogType.FOR_TEST));
            }

            // Draw boudning box
            var resultImage = ImageVisualizer.DrawBoxes(resized, boxesValue);

            // Test: Save Images with Boudning Box
            string saveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_test-result", "bouding-boxes", _testFolderName);
            Directory.CreateDirectory(saveDir);
            string savePath = Path.Combine(saveDir, $"{_testCombinedImageIndex}.bmp");
            resultImage.Save(savePath, ImageFormat.Bmp);

            // after operation is completed
            _compositImageRowBuffer = rowImages;
            _rowCount++;
        }

        // TODO: OLD LOGIC. DELETE
        public void ReceiveBitmapGrabbed(int newIndex, BaslerCameraPosition position, Bitmap newBitmapSource)
        {
            // 
            _compositeImageBuffer.AddImage(newIndex, position, newBitmapSource);

            if (_compositeImageBuffer.IsAllFragmentCollected)
            {
                var combinedBitmap = CombineImages(_compositeImageBuffer.OutsideAfter.Bitmap, _compositeImageBuffer.InsideAfter.Bitmap, _compositeImageBuffer.OutsideBefore.Bitmap, _compositeImageBuffer.InsideBefore.Bitmap);

                // TODO: send to AI ASYNCHRONOUSLY
                // TEST
                AddToTestImages(combinedBitmap);
            }
        }

        // TODO: OLD LOGIC. DELETE
        private void AddToTestImages(Bitmap bitmap)
        {
            // WARN: DUPLICATE LOGIC (BitmapToImageSource exists in BaslerCamera.cs)
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                bitmapimage.Freeze();

                TestImages.Add(bitmapimage);
            }
        }

        // TODO: 정돈하기
        private string _testFolderName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        private int _testCombinedImageIndex = 0;

        public Bitmap CombineImages(Bitmap outsideAfter, Bitmap insideAfter, Bitmap outsideBefore, Bitmap insideBefore)
        {
            int width = outsideAfter.Width;
            int height = insideAfter.Height;

            int tempArg1 = 230;
            int tempArg2 = 600;

            Bitmap combinedBitmap = new Bitmap(width * 2, height * 2);
            using (Graphics g = Graphics.FromImage(combinedBitmap))
            {
                g.Clear(Color.White);
                g.DrawImage(outsideAfter, 0, 0, width, height);
                g.DrawImage(insideAfter, width - tempArg1, 0, width, height);
                g.DrawImage(outsideBefore, 0, height - tempArg2, width, height);
                g.DrawImage(insideBefore, width - tempArg1, height - tempArg2, width, height);
            }

            string saveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_test-result", "image-composition", _testFolderName);
            Directory.CreateDirectory(saveDir);

            string savePath = Path.Combine(saveDir, $"{_testCombinedImageIndex}.bmp");
            combinedBitmap.Save(savePath, ImageFormat.Bmp);

            _testCombinedImageIndex++;

            // WARN: TODO: DISPOSE BITMAP

            string logMsg = $"Image Combined when ManagedIndex is {GrabbedImageIndexManager.Instance.ManagedImageIndex}";
            LogService.Instance.AddLog(new Log(logMsg, LogType.ImageCombined));

            return combinedBitmap;
        }
    }
}
