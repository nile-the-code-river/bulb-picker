using Basler.Pylon;
using System.Drawing;

namespace BulbPicker.App.Models
{
    class BaslerCamera
    {
        public Camera Camera { get; init; }
        //private PixelDataConverter converter = new PixelDataConverter();
        public string CameraNumber { get; init; }
        //public bool IsGrabbing { get; set; }
        //public int AxisOffsetX { get; set; }
        //public int AxisOffsetY { get; set; }
        public Bitmap ImageBefore { get; set; }
        public Bitmap ImageAfter { get; set; }

        public BaslerCamera(string cameraNumber)
        {
            CameraNumber = cameraNumber;
            //Camera = new Camera(cameraNumber);
            // add events
        }

    }
}
