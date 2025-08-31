using BulbPicker.App.Infrastructures;
using BulbPicker.App.Models;
using BulbPicker.App.Services;

namespace BulbPicker.App.ViewModels
{
    class AppConfigViewModel
    {
        public AsyncCommand SaveOffsetsCommand => new AsyncCommand(async () => await ConfigService.Instance.UpdateRobotArmOffsetsAsync());
    }
}
