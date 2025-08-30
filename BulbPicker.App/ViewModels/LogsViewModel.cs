using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using BulbPicker.App.Services;
using System.Collections.ObjectModel;

namespace BulbPicker.App.ViewModels
{
    // TODO 0831 : Implement Color Converter (by Level)
    class LogsViewModel
    {
        public ObservableCollection<Log> Logs => LogService.Instance.Logs;

        public RelayCommand AddLog_Connected
            => new RelayCommand(execute => LogService.Instance.AddLog(new Log("TEST_Connected", LogType.Connected)));

        public RelayCommand AddLog_Disconnected
            => new RelayCommand(execute => LogService.Instance.AddLog(new Log("TEST_Disconnected", LogType.Disconnected)));

        public RelayCommand AddLog_RobotArmPointSent
            => new RelayCommand(execute => LogService.Instance.AddLog(new Log("TEST_RobotArmPointSent", LogType.RobotArmPointsSent)));

        public RelayCommand AddLog_RobotArmCommunication
            => new RelayCommand(execute => LogService.Instance.AddLog(new Log("TEST_RobotArmCommunication", LogType.RobotArmCommunication)));
        
        public RelayCommand AddLog_SettingFileUpdated
            => new RelayCommand(execute => LogService.Instance.AddLog(new Log("TEST_SettingFileUpdated", LogType.SettingFileUpdated)));

        public RelayCommand AddLog_ImageCombined
            => new RelayCommand(execute => LogService.Instance.AddLog(new Log("TEST_Image Combined", LogType.ImageCombined)));
    }
}
