using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace BulbPicker.App.Services
{
    // TODO later: textbox 값은 수정했는데 save 버튼 안 눌렀을 경우 'Changes Not Saved' 등 문구 떠 있게 하는 기능 만들기 (bool)
    class ConfigService : ObservableObject
    {
        private static readonly ConfigService _instance = new ConfigService();
        public static ConfigService Instance => _instance;


        private ObservableCollection<RobotArmOffsets> _displayedOffsets = new ObservableCollection<RobotArmOffsets>();
        public ObservableCollection<RobotArmOffsets> DisplayedOffsets
        {
            get => _displayedOffsets;
            set
            {
                _displayedOffsets = value;
                OnPropertyChanged(nameof(DisplayedOffsets));
            }
        }

        private ConfigService( )
        {
            // TODO later: make this async
            ApplyConfigSettings();
        }


        private void ApplyConfigSettings()
        {
            // config json에서 읽은 만큼

            // read from config json
            SetUpRobotArmDefaultSettings();
            SetUpRobotArmOffsets();
            SetUpInitialDisplayedRobotArmOffset();

        }

        private void SetUpRobotArmDefaultSettings()
        {
            // read from config json and set up robot arms
            // set up offsets as well
        }

        // called when...
        // (1) initializing robot arms when the app starts
        // (2) user modifies offsets & save them
        private void SetUpRobotArmOffsets()
        {
            // call 'SetUpOffsets' inside robotarm
        }

        private void SetUpInitialDisplayedRobotArmOffset()
        {
            // test
            DisplayedOffsets.Add(new RobotArmOffsets());
            DisplayedOffsets.Add(new RobotArmOffsets());
        }

        public void UpdateRobotArmOffsets()
        {
            // update config file
            // call SetUpRobotArmOffsets()
            
            
            LogService.Instance.AddLog(new Log("New offsets saved to the config file.", LogType.SettingFileUpdated));
        }
    }
}
