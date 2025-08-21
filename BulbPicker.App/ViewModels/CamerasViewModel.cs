using Basler.Pylon;
using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace BulbPicker.App.ViewModels
{
    class CamerasViewModel : ViewModelBase
    {
        private ObservableCollection<BaslerCamera> _cameras = new ObservableCollection<BaslerCamera>();
        public ObservableCollection<BaslerCamera> Cameras
        {
            get => _cameras;
            set
            {
                _cameras = value;
            }
        }
        
        // 공통 타이머
        private DispatcherTimer _shotTimer;

        public CamerasViewModel()
        {
            TestDummy();

            //Test();
            //InitializeTimer();
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

        public void TestDummy()
        {
            Cameras.Add(new BaslerCamera("Dummy", null));
        }

        public void Test()
        {
            Cameras.Add(new BaslerCamera("1st Outside", new Camera("")));
            Cameras.Add(new BaslerCamera("1st Inside", new Camera("")));
            Cameras.Add(new BaslerCamera("2nd Outside", new Camera("40058520")));
            Cameras.Add(new BaslerCamera("2nd Inside", new Camera("21914827")));
        }
    }
}
