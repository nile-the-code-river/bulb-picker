using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using BulbPicker.App.Services;

namespace BulbPicker.App.ViewModels
{
    class AppConfigViewModel
    {
        //public AsyncCommand SaveOffsetsCommand => new AsyncCommand(async () => await ConfigService.Instance.UpdateRobotArmOffsetsAsync());
        public AsyncCommand SaveOffsetsCommand => new AsyncCommand(async () => { });

        private void SaveOffsets()
        {
            // TODO later: 글자 등 invalid 값 입력 시 warning
            // TODO later:for loop thr scaras and write down saved offsets in log
        }
    }
}
