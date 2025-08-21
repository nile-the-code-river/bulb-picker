using BulbPicker.App.Models;
using System.Collections.ObjectModel;

namespace BulbPicker.App.Services
{
    public sealed class LogService
    {
        private static readonly LogService _instance = new LogService();
        public static LogService Instance => _instance;
        private LogService() { }

        private ObservableCollection<Log> _logs = new ObservableCollection<Log>();
        public ObservableCollection<Log> Logs
        {
            get => _logs;
            set => _logs = value;
        }

        public void AddLog(Log log) => Logs.Add(log);
    }
}
