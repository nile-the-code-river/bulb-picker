using BulbPicker.App.Models;
using System.Collections.ObjectModel;
using System.Windows;
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

        public void ReceiveBitmapSourceGrabbed(int newIndex, BaslerCameraPosition position, BitmapSource newBitmapSource)
        {
            _compositeImageBuffer.AddImage(newIndex, position, newBitmapSource);

            if (_compositeImageBuffer.IsAllFragmentCollected)
            {
                var combinedBitmapSource = CompositeImages(_compositeImageBuffer);
                // TODO: send to AI ASYNCHRONOUSLY

                TestImages.Add(combinedBitmapSource);
            }
        }

        public BitmapSource CompositeImages(CompositeImage ci)
        {
            // TODO: composition logic goes here

            string logMsg = $"Image Combined when ManagedIndex is {GrabbedImageIndexManager.Instance.ManagedImageIndex}";
            LogService.Instance.AddLog(new Log(logMsg, LogType.ImageCombined));

            return null;
        }
    }
}
