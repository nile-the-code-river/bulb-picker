using BulbPicker.App.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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


        // collected images for test
        public ObservableCollection<BitmapSource> TestImages { get; private set; } = new ObservableCollection<BitmapSource>();
        // TODO: 아이템 늘어나면 set (add_) 될 때 save image 하기

        // TODO: 정리하기. 좀 없애기.
        private CompositeImage _compositeImageBuffer = new CompositeImage();

        private readonly ObservableCollection<ImageToCompositeQuqueItem> _firstRowImageToCompositeQuque;

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

        private DispatcherTimer _firstRowClearTimer;
        private void FirstRowImagesToCombine_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (string newItem in e.NewItems)
                    {
                        LogService.Instance.AddLog(new Log($"Added: {newItem}", LogType.Connected));
                    }

                    // TODO
                    if(_firstRowImageToCompositeQuque.Count == 2)
                    {
                        // if 2 items -> shoot to CompositeImages
                        Bitmap outside = null;
                        Bitmap inside = null;

                        foreach (ImageToCompositeQuqueItem newItem in e.NewItems)
                        {
                            if(newItem.CameraPosition == BaslerCameraPosition.Outisde)
                            {
                                outside = newItem.Image;
                            }
                            else
                            {
                                inside = newItem.Image;
                            }
                        }

                        if(outside == null || inside == null)
                        {
                            MessageBox.Show("Unexpected Error Occurred in CompositeImageService.");
                            return;
                        }

                        CompositeImageRowBuffer row = new CompositeImageRowBuffer(outside, inside);
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
                        MessageBox.Show("Unexpected Number of Items in _firstRowImageToCompositeQuque");
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    LogService.Instance.AddLog(new Log($"count: {_firstRowImageToCompositeQuque.Count}", LogType.Connected));
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        private int _rowCount = 0;
        private void CompositeImage_FactoryVerTest(CompositeImageRowBuffer rowImages)
        {
            if(_rowCount == 0)
            {
                _compositImageRowBuffer = rowImages;
                return;
            }

            var combinedBitmap = CombineImages(rowImages.Outside, rowImages.Inside, _compositImageRowBuffer.Outside, _compositImageRowBuffer.Inside);
            _compositImageRowBuffer = rowImages;
            
            _rowCount++;
        }

        public void ReceiveBitmapGrabbed(int newIndex, BaslerCameraPosition position, Bitmap newBitmapSource)
        {
            // 
            _compositeImageBuffer.AddImage(newIndex, position, newBitmapSource);

            if (_compositeImageBuffer.IsAllFragmentCollected)
            {
                var combinedBitmap = CombineImages(_compositeImageBuffer.OutsideAfter.Bitmap, _compositeImageBuffer.InsideAfter.Bitmap, _compositeImageBuffer.OutsideBefore.Bitmap, _compositeImageBuffer.InsideBefore.Bitmap);

                // TODO: send to AI ASYNCHRONOUSLY

                AddToTestImages(combinedBitmap);
            }
        }

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

        private string _testFolderName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        private int _testCombinedImageIndex = 0;

        public Bitmap CombineImages(Bitmap outsideAfter, Bitmap insideAfter, Bitmap outsideBefore, Bitmap insideBefore) // CompositeImage ci
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
