using BulbPicker.App.Models;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace BulbPicker.App.Services
{
    // TODO: LOCK elements used in multiple threads
    public class CompositeImageService
    {
        private static readonly CompositeImageService _instance = new CompositeImageService();
        public static CompositeImageService Instance => _instance;
        private CompositeImageService() { }

        // collected images for test
        public ObservableCollection<BitmapSource> TestImages { get; private set; } = new ObservableCollection<BitmapSource>();
        // 아이템 늘어나면 set (add_) 될 때 save image 하기

        private CompositeImage _compositeImageBuffer = new CompositeImage();

        public void ReceiveBitmapGrabbed(int newIndex, BaslerCameraPosition position, Bitmap newBitmapSource)
        {
            _compositeImageBuffer.AddImage(newIndex, position, newBitmapSource);

            if (_compositeImageBuffer.IsAllFragmentCollected)
            {
                var combinedBitmap = CompositeImages(_compositeImageBuffer);

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

        public Bitmap CompositeImages(CompositeImage ci)
        {
            int width = ci.OutsideAfter.Bitmap.Width;
            int height = ci.InsideAfter.Bitmap.Height;

            int tempArg1 = 230;
            int tempArg2 = 600;

            Bitmap combinedBitmap = new Bitmap(width * 2, height * 2);
            using (Graphics g = Graphics.FromImage(combinedBitmap))
            {
                g.Clear(Color.White);
                g.DrawImage(ci.OutsideAfter.Bitmap, 0, 0, width, height);
                g.DrawImage(ci.InsideAfter.Bitmap, width - tempArg1, 0, width, height);
                g.DrawImage(ci.OutsideBefore.Bitmap, 0, height - tempArg2, width, height);
                g.DrawImage(ci.InsideBefore.Bitmap, width - tempArg1, height - tempArg2, width, height);
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
