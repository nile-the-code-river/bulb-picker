using BulbPicker.App.Models;
using System.Collections.ObjectModel;

namespace BulbPicker.App.ViewModels
{
    class RobotArmsViewModel
    {
        // TODO: get from config file
        // TODO: also when the offset changed in ui by users
        public int OffsetX = 0;
        public int OffsetY = 0;
        public int OffsetZ = 0;

        private const int ROBOT_ARM_PORT = 8011;
        private const int PROGRAM_PORT = 1000;

        private ObservableCollection<RobotArm> _robotArms = new ObservableCollection<RobotArm>();
        public ObservableCollection<RobotArm> RobotArms
        {
            get => _robotArms;
            set => _robotArms = value;
        }

        public RobotArmsViewModel()
        {
            // TODO: get from config file
            RobotArms.Add(new RobotArm("1st Outside", "192.168.0.11", ROBOT_ARM_PORT, PROGRAM_PORT));
            RobotArms.Add(new RobotArm("1st Inside", "192.168.0.12", ROBOT_ARM_PORT, PROGRAM_PORT));
            RobotArms.Add(new RobotArm("2nd Outside", "192.168.0.13", ROBOT_ARM_PORT, PROGRAM_PORT));
            RobotArms.Add(new RobotArm("2nd Inside", "192.168.0.14", ROBOT_ARM_PORT, PROGRAM_PORT));
        }

    }
}
