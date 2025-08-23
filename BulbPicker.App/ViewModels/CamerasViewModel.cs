using Basler.Pylon;
using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;

namespace BulbPicker.App.ViewModels
{
    class CamerasViewModel : ViewModelBase, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private int _testingDummyCount = 1; 
        public bool IsTestingDummyCamera { get; private set; } = false;
        private bool _isTestingInProgress = false;
        public bool IsTestingInProgress
        {
            get => _isTestingInProgress;
            private set
            {
                _isTestingInProgress = value;
                OnPropertyChanged(nameof(IsTestingInProgress));   // notify UI
            }
        }
        public RelayCommand RunDummyTestCommand => new RelayCommand(execute => RunDummyTest());


        private ObservableCollection<BaslerCamera> _cameras = new ObservableCollection<BaslerCamera>();
        public ObservableCollection<BaslerCamera> Cameras
        {
            get => _cameras;
            set => _cameras = value;
        }

        public CamerasViewModel()
        {
            // TEST for any env with dummy cameras
            SetUpDummyTest();

            // TEST for real env (the factory) with working cameras
            // SetUpFirstRowTest();
            // SetUpSecondRowTest();
        }

        public void SetUpDummyTest()
        {
            IsTestingDummyCamera = true;

            // using DummyTestCamera.cs
            Cameras.Add(new DummyTestCamera("Outside_1st"));
            Cameras.Add(new DummyTestCamera("Inside_1st"));
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

        // WARN: TODO: make sure this is conducted in another thread
        private void _dummyTestShotTimer_Tick(object? sender, EventArgs e)
        {
            foreach (var camera in Cameras)
            {
                if(camera is DummyTestCamera dummyCamera)
                {
                    dummyCamera.FetchBitmapFromLocalDirectory(_testingDummyCount);
                }
            }

            _testingDummyCount++;
        }

        public void SetUpFirstRowTest()
        {
            Cameras.Add(new BaslerCamera("1st Outside", "40007011"));
            Cameras.Add(new BaslerCamera("1st Inside", "40012243"));
        }

        public void SetUpSecondRowTest()
        {
            Cameras.Add(new BaslerCamera("2nd Outside", "40058520"));
            Cameras.Add(new BaslerCamera("2nd Inside", "21914827"));
        }
    }
}
