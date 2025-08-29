using System.ComponentModel;
using System.Diagnostics;

namespace BulbPicker.App.Services
{
    public sealed class TestIndexManager : INotifyPropertyChanged
    {
        private static readonly TestIndexManager _instance = new TestIndexManager();
        public static TestIndexManager Instance => _instance;
        private TestIndexManager()
        { ManagedDateTimeStr = DateTime.Now.ToString("yyyyMMdd_HHmmss"); }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        //
        public string ManagedDateTimeStr { get; init; }

        //
        public int DummyCameraImageIndex { get; private set; }
        public void IncrementDummyCameraImageIndex() => DummyCameraImageIndex++;

        //
        private int _combinedImageIndex = 0;
        public int CombinedImageIndex
        {
            get => _combinedImageIndex;
            private set
            {
                _combinedImageIndex = value;
                OnPropertyChanged(nameof(CombinedImageIndex));
            }
        }
        public void IncrementCombinedImageIndex() => CombinedImageIndex++;


        //
        private Stopwatch _testStopWatch = new Stopwatch();
        public void StartTestStopwatch() => _testStopWatch.Start();
        public void StopTestStopwatch() => _testStopWatch.Stop();
        public string GetStopwatchMilliSecondsNow() => $"{_testStopWatch.ElapsedMilliseconds}";
    }
}
