using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;

namespace BulbPicker.App.ViewModels
{
    class CamerasViewModel : ViewModelBase
    {
        public List<BaslerCamera> Cameras { get; set; } = new ();

        public CamerasViewModel()
        {
            Cameras.Add(new BaslerCamera("12345"));
            Cameras.Add(new BaslerCamera("098765"));
        }
    }
}
