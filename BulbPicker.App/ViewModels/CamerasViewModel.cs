using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using BulbPicker.App.Services;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;

namespace BulbPicker.App.ViewModels
{
    class CamerasViewModel : ObservableObject
    {
        private ObservableCollection<BaslerCamera> _cameras = new ObservableCollection<BaslerCamera>();
        public ObservableCollection<BaslerCamera> Cameras
        {
            get => _cameras;
            private set => _cameras = value;
        }

        private bool _loadingCameras = true;
        public bool LoadingCameras
        {
            get => _loadingCameras;
            private set
            {
                _loadingCameras = value;
                OnPropertyChanged(nameof(LoadingCameras));
            }
        }

        public async void OnLoadedAsync()
        {
            await SetUpCamerasAsync();
        }

        public async Task SetUpCamerasAsync()
        {
            //
            bool isTestingWithDummies = false;
            if (isTestingWithDummies)
            {
                SetUpDummyTest();
                return;
            }

            var cam1 = await BaslerCamera.CreateAsync("Camera 1", "40007011", BaslerCameraPosition.Outisde);
            var cam2 = await BaslerCamera.CreateAsync("Camera 2", "40012243", BaslerCameraPosition.Inside);
            Cameras.Add(cam1);
            Cameras.Add(cam2);

            // only testing 1st row of cameras
            LoadingCameras = false;
            return;

            var cam3 = await BaslerCamera.CreateAsync("Camera 3", "40058520", BaslerCameraPosition.Outisde);
            var cam4 = await BaslerCamera.CreateAsync("Camera 4", "21914827", BaslerCameraPosition.Inside);
            Cameras.Add(cam3);
            Cameras.Add(cam4);
        }

        #region For Test Env (when there is no real cameras)
        public RelayCommand RunDummyTestCommand => new RelayCommand(execute => RunDummyTest());

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

        private void _dummyTestShotTimer_Tick(object? sender, EventArgs e)
        {
            foreach (var camera in Cameras)
            {
                if (camera is DummyTestCamera dummyCamera)
                {
                    ((DummyTestCamera)camera).SimulateImageGrabbedEvent();
                }
            }
            
            TestIndexManager.Instance.IncrementDummyCameraImageIndex();
        }
        #endregion
    }
}
