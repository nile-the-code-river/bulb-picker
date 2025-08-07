using Basler.Pylon;
using BulbPicker.App.Infrastructures;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulbPicker.App.ViewModels
{
    class BaslerCamera
    {
        public Camera Camera { get; set; }
        //private PixelDataConverter converter = new PixelDataConverter();
        public byte Index { get; set; }
        public string IPAddress { get; set; }
        //public bool IsGrabbing { get; set; }
        //public int AxisOffsetX { get; set; }
        //public int AxisOffsetY { get; set; }
        public Bitmap ImageBefore { get; set; }
        public Bitmap ImageAfter { get; set; }

        public BaslerCamera()
        {
            
        }

    }

    class CamerasViewModel : ViewModelBase
    {
        public List<BaslerCamera> BaslerCameras { get; set; }

    }
}
