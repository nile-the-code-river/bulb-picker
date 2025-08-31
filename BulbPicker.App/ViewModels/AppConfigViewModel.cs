using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using BulbPicker.App.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace BulbPicker.App.ViewModels
{
    class AppConfigViewModel
    {
        public RelayCommand SaveOffsetsCommand => new RelayCommand(execute => SaveOffsets());

        public ObservableCollection<DisplayedOffset> DisplayedOffsets = new ObservableCollection<DisplayedOffset>();

        public AppConfigViewModel()
        {
            // loop through 'RobotArmService.Instance.RobotArms' and get offsets from it
            RobotArmService.Instance.RobotArms.CollectionChanged += RobotArms_CollectionChanged;
        }

        private void RobotArms_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //DisplayedOffsets.Add(e.NewItems[0]);
            MessageBox.Show("rb chged");
        }

        private void SaveOffsets()
        {
            // write to config json via Config Service

            LogService.Instance.AddLog(new Log("New offsets saved to the config file.", LogType.SettingFileUpdated));
            LogService.Instance.AddLog(new Log("TODO: for loop thr scaras and write down offsets", LogType.FOR_TEST));
        }
    }
}
