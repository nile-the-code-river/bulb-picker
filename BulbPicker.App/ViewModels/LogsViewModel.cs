using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using BulbPicker.App.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace BulbPicker.App.ViewModels
{
    // TODO later : Implement Color Converter (by Level)
    class LogsViewModel
    {
        public ObservableCollection<Log> Logs => LogService.Instance.Logs;

        public AsyncCommand DownloadLogsCommand => new AsyncCommand(async () => await DownloadLogsAsync());
        private async Task DownloadLogsAsync()
        {
            if (Logs == null || Logs.Count == 0) return;

            await FileSaveService.SaveLogsAsync(Logs);
        }

        #region Test Logging
        public RelayCommand AddLog_Connected
            => new RelayCommand(execute =>
            {
                int i= 0;
                while (i < 100)
                {
                    LogService.Instance.AddLog(new Log("BULK_TEST", LogType.Connected));
                    i++;
                }
            });
        //public RelayCommand AddLog_Connected
        //    => new RelayCommand(execute => LogService.Instance.AddLog(new Log("TEST_Connected", LogType.Connected)));

        public RelayCommand AddLog_Disconnected
            => new RelayCommand(execute => LogService.Instance.AddLog(new Log("TEST_Discon asdsad ecated TESd T_Disconneasdeted TEST_Di assconnectedasd aasdasd asd TEST_asdDisconnected TEST_Disconnected", LogType.Disconnected)));

        public RelayCommand AddLog_RobotArmPointSent
            => new RelayCommand(execute => LogService.Instance.AddLog(new Log("TEST_RobotArmPointSent", LogType.RobotArmPointsSent)));

        public RelayCommand AddLog_RobotArmCommunication
            => new RelayCommand(execute => LogService.Instance.AddLog(new Log("TEST_RobotArmCommunication", LogType.RobotArmCommunication)));
        
        public RelayCommand AddLog_SettingFileUpdated
            => new RelayCommand(execute => LogService.Instance.AddLog(new Log("TEST_SettingFileUpdated", LogType.SettingFileUpdated)));

        public RelayCommand AddLog_ImageCombined
            => new RelayCommand(execute => LogService.Instance.AddLog(new Log("TEST_Image Combined", LogType.ImageCombined)));
        #endregion
    }
}
