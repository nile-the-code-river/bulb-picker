using Basler.Pylon;
using BulbPicker.App.Models;
using System.Collections.ObjectModel;

namespace BulbPicker.App.Services
{
    public class CameraService
    {
        // Singleton
        public static CameraService Instance { get; } = new CameraService();
        private CameraService() { }

        // vars
        public ObservableCollection<BaslerCamera> Cameras { get; set; } = new();

        // funcs
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
        }
    }
}
