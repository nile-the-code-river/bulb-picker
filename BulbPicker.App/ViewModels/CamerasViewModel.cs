using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using BulbPicker.App.Services;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace BulbPicker.App.ViewModels
{
    class CamerasViewModel : ViewModelBase
    {
        public bool IsTestingDummyCamera { get; private set; } = false;
        private bool _isTestingInProgress = false;
        public bool IsTestingInProgress
        {
            get => _isTestingInProgress;
            private set
            {
                _isTestingInProgress = value;
                OnPropertyChanged(nameof(IsTestingInProgress));
            }
        }

        private ObservableCollection<BaslerCamera> _cameras = new ObservableCollection<BaslerCamera>();
        public ObservableCollection<BaslerCamera> Cameras
        {
            get => _cameras;
            set => _cameras = value;
        }

        public CamerasViewModel()
        {
            // TEST for real env (the factory) with working cameras
            SetUpFirstRowTest();
            // SetUpSecondRowTest();

            // TEST for any env with dummy cameras
            //SetUpDummyTest();

        }

        #region For Test Env (when there is no real cameras)
        public RelayCommand RunDummyTestCommand => new RelayCommand(execute => RunDummyTest());

        public void SetUpDummyTest()
        {
            IsTestingDummyCamera = true;

            // using DummyTestCamera.cs
            Cameras.Add(new DummyTestCamera("Outside_1st", BaslerCameraPosition.Outisde));
            Cameras.Add(new DummyTestCamera("Inside_1st", BaslerCameraPosition.Inside));
        }

        private DispatcherTimer _dummyTestShotTimer;
        private void RunDummyTest()
        {
            if(!IsTestingInProgress)
            {
                InitializeDummtTestTimer();
            }
            else
            {
                if(_dummyTestShotTimer != null) _dummyTestShotTimer.Stop();
            }

            IsTestingInProgress = !IsTestingInProgress;
        }

        private void InitializeDummtTestTimer()
        {
            _dummyTestShotTimer = new DispatcherTimer();
            _dummyTestShotTimer.Interval = TimeSpan.FromSeconds(1);
            _dummyTestShotTimer.Tick += _dummyTestShotTimer_Tick;
            _dummyTestShotTimer.Start();
        }

        // TODO: Replace with NEW LOGIC
        private void _dummyTestShotTimer_Tick(object? sender, EventArgs e)
        {
            foreach (var camera in Cameras)
            {
                if(camera is DummyTestCamera dummyCamera)
                {
                    dummyCamera.FetchBitmapFromLocalDirectory(GrabbedImageIndexManager.Instance.ManagedImageIndex);
                }
            }

            GrabbedImageIndexManager.Instance.Increment();
        }

        public void SetUpFirstRowTest()
        {
            Cameras.Add(new BaslerCamera("1st Outside", "40007011", BaslerCameraPosition.Outisde));
            Cameras.Add(new BaslerCamera("1st Inside", "40012243", BaslerCameraPosition.Inside));
        }

        public void SetUpSecondRowTest()
        {
            Cameras.Add(new BaslerCamera("2nd Outside", "40058520", BaslerCameraPosition.Outisde));
            Cameras.Add(new BaslerCamera("2nd Inside", "21914827", BaslerCameraPosition.Inside));
        }
        #endregion
    }
}
