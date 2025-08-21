using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using BulbPicker.App.Services;
using System.Collections.ObjectModel;

namespace BulbPicker.App.ViewModels
{
    class LogsViewModel
    {
        public ObservableCollection<Log> Logs => LogService.Instance.Logs;
    }
}
