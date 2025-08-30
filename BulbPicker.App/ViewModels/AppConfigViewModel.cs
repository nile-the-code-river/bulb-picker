using BulbPicker.App.Models;
using BulbPicker.App.Services;
using System.Collections.ObjectModel;

namespace BulbPicker.App.ViewModels
{
    // TODO 0831 : Implement
    class AppConfigViewModel
    {
        public ObservableCollection<RobotArm> RobotArms => RobotArmService.Instance.RobotArms;

        // TODO 0831 : Write to config file, so that input from the user can be saved and be retrieved next time the app is opened
        // TODO 0831 : add 'Save Button' to UI and implement the function
    }
}
