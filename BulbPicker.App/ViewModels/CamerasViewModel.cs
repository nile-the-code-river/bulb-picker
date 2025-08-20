using Basler.Pylon;
using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using BulbPicker.App.Services;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace BulbPicker.App.ViewModels
{
    class CamerasViewModel : ViewModelBase
    {
        //private ObservableCollection<BaslerCamera> _cameras;
        public ObservableCollection<BaslerCamera> Cameras
        {
            get; set;
        } = new ObservableCollection<BaslerCamera>();
        
        // 공통 타이머
        private DispatcherTimer _shotTimer;

        public CamerasViewModel()
        {
            Test();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _shotTimer = new DispatcherTimer();
            _shotTimer.Interval = TimeSpan.FromSeconds(1);
            _shotTimer.Tick += _shotTimer_Tick;
            _shotTimer.Start();
        }

        private void _shotTimer_Tick(object? sender, EventArgs e)
        {
            foreach (var camera in Cameras)
            {
                camera.TakeOneShot();
            }
        }

        public void Test()
        {
            Cameras.Add(new BaslerCamera() { Camera = new Camera("40058520") });
            Cameras.Add(new BaslerCamera() { Camera = new Camera("21914827") });
        }
    }
}
