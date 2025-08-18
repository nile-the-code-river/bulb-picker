using Basler.Pylon;
using BulbPicker.App.Models;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;

namespace BulbPicker.App.Services
{
    // TODO: 감지된 카메라 없을 때의 예외처리는 나중에
    public class CameraService
    {
        // Singleton
        public static CameraService Instance { get; } = new CameraService();
        private CameraService() { }

        // vars
        public ObservableCollection<BaslerCamera> Cameras { get; set; } = new();
        
        public bool IsGrabbing { get; set; }
        private DispatcherTimer captureTimer = null;

        // funcs
        private void InitializeCaptureTimer()
        {
            captureTimer = new DispatcherTimer();
            captureTimer.Interval = TimeSpan.FromSeconds(1);
            captureTimer.Tick += CaptureTimer_Tick;
        }

        private void CaptureTimer_Tick(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public async Task FindCamerasAsync ()
        {
            var cameras = await Task.Run(() =>
            {
                List<BaslerCamera> camerasFound = new();
                foreach (ICameraInfo camInfo in CameraFinder.Enumerate())
                {
                    camerasFound.Add(new BaslerCamera { Camera = new Camera(camInfo) });
                }
                return camerasFound;
            });

            cameras.ForEach(Cameras.Add);

            MessageBox.Show("Camera Finding Sequence Ended");
        }


        private void GrabImage()
        {


            // when tick, (1sec)
            // ImageBefore = ImageAfter
            // ImageAfter = newImage
        }
    }
}
