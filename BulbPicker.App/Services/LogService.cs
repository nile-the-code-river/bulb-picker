using BulbPicker.App.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace BulbPicker.App.Services
{
    public sealed class LogService
    {
        private static readonly LogService _instance = new LogService();
        public static LogService Instance => _instance;

        private readonly Dispatcher _dispatcher;

        private LogService( )
        {
            _dispatcher = Application.Current.Dispatcher;
        }

        private ObservableCollection<Log> _logs = new ObservableCollection<Log>();
        public ObservableCollection<Log> Logs
        {
            get => _logs;
            set => _logs = value;
        }

        public void AddLog(Log log)
        {
            _dispatcher.Invoke(() =>
            {
                Logs.Add(log);
            });
        }
    }
}
