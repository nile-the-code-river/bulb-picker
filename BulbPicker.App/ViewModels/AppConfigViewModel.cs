using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using BulbPicker.App.Services;

namespace BulbPicker.App.ViewModels
{
    class AppConfigViewModel
    {
        public RelayCommand SaveOffsetsCommand => new RelayCommand(execute => SaveOffsets());

        private void SaveOffsets()
        {
            ConfigService.Instance.UpdateRobotArmOffsets();

            // TODO later: 글자 등 invalid 값 입력 시 warning
            // TODO later
            //LogService.Instance.AddLog(new Log("TODO: for loop thr scaras and write down offsets", LogType.FOR_TEST));
        }
    }
}
