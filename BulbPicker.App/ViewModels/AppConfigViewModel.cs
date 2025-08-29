using BulbPicker.App.Models;
using BulbPicker.App.Services;
using System.Collections.ObjectModel;

namespace BulbPicker.App.ViewModels
{
    // 0831 TODO : Implement
    class AppConfigViewModel
    {
        public ObservableCollection<RobotArm> RobotArms => RobotArmService.Instance.RobotArms;

        // 0831 TODO : Write to config file, so that input from the user can be saved and be retrieved next time the app is opened
        // 0831 TODO : add 'Save Button' to UI and implement the function
    }
}
