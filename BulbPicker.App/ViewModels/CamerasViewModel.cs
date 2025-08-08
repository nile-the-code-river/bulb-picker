using Basler.Pylon;
using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using BulbPicker.App.Services;
using System.Collections.ObjectModel;

namespace BulbPicker.App.ViewModels
{
    class CamerasViewModel : ViewModelBase
    {

        private readonly CameraService _cameraService;
        public ObservableCollection<BaslerCamera> Cameras => _cameraService.Cameras;
        // -> INotifyPropertyChanged
        public bool IsLoading => Cameras.Count == 0;

        // for design preview's sake
        public CamerasViewModel() : this(App.CameraService)
        {
            _cameraService.Cameras.Add(new BaslerCamera());
            _cameraService.Cameras.Add(new BaslerCamera());
        }

        public CamerasViewModel(CameraService cameraService)
        {
            _cameraService = cameraService;
        }
    }
}
