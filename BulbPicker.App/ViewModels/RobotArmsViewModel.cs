using BulbPicker.App.Models;
using BulbPicker.App.Services;
using System.Collections.ObjectModel;

namespace BulbPicker.App.ViewModels
{
    class RobotArmsViewModel
    {
        public ObservableCollection<RobotArm> RobotArms => RobotArmService.Instance.RobotArms;
    }
}
