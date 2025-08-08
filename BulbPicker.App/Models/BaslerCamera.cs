using Basler.Pylon;
using System.ComponentModel;
using System.Drawing;

namespace BulbPicker.App.Models
{
    public class BaslerCamera : INotifyPropertyChanged
    {
        private Camera _camera;
        public Camera Camera
        {
            get => _camera;
            set
            {
                _camera = value;
                IPAddress = value?.CameraInfo?.GetValueOrDefault("IpAddress", "0");
            }
        }
        public string? IPAddress { get; private set; }
        //private PixelDataConverter converter = new PixelDataConverter();
        //public string CameraNumber { get; init; }
        //public bool IsGrabbing { get; set; }
        //public int AxisOffsetX { get; set; }
        //public int AxisOffsetY { get; set; }
        public Bitmap ImageBefore { get; set; }
        public Bitmap ImageAfter { get; set; }

        public BaslerCamera()
        {
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
