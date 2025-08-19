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
        private DispatcherTimer captureTimer = null;
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

        public CamerasViewModel()
        {
            Test();
        }
        public void Test()
        {
            Cameras.Add(new BaslerCamera() { Camera = new Camera("21914827") });
        }
    }
}
