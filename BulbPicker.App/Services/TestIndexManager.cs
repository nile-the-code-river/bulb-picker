using System.Diagnostics;

namespace BulbPicker.App.Services
{
    public class TestIndexManager
    {
        private static readonly TestIndexManager _instance = new TestIndexManager();
        public static TestIndexManager Instance => _instance;
        public DateTime ManagedDateTime;
        public int ManagedIndex1 { get; set; }
        public void IncrementManagedIndex1() => ManagedIndex1++;

        public Stopwatch _testStopWatch = new Stopwatch();

        private TestIndexManager()
        {
            ManagedDateTime = DateTime.Now;
        }

        public void StartTestStopwatch()
        {
            _testStopWatch.Start();
        }
        
        public void StopTestStopwatch()
        {
            _testStopWatch.Stop();
        }

        public void LogTestStopwatchNow()
        {
            long elapsedMilliseconds = _testStopWatch.ElapsedMilliseconds;
            LogService.Instance.AddLog(new Models.Log($"Stopwatch: {elapsedMilliseconds}ms", Models.LogType.ImageCombined));
        }

        public string GetStopwatchMilliSecondsNow() => $"{_testStopWatch.ElapsedMilliseconds}";
    }
}
