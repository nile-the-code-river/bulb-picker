using System.Diagnostics;

namespace BulbPicker.App.Services
{
    // TEST: 일단 Test Purpose로 유지해둠
    public class TestIndexManager
    {
        private static readonly TestIndexManager _instance = new TestIndexManager();
        public static TestIndexManager Instance => _instance;


        public string ManagedDateTimeStr { get; set; }
        public int ManagedIndex1 { get; set; }
        public void IncrementManagedIndex1() => ManagedIndex1++;



        public Stopwatch _testStopWatch = new Stopwatch();

        private TestIndexManager()
        {
            ManagedDateTimeStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
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
            LogService.Instance.AddLog(new Models.Log($"Stopwatch: {GetStopwatchMilliSecondsNow()}ms", Models.LogType.ImageCombined));
        }

        public string GetStopwatchMilliSecondsNow() => $"{_testStopWatch.ElapsedMilliseconds}";
    }
}
