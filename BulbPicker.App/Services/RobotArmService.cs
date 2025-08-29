using BulbPicker.App.Models;
using System.Collections.ObjectModel;

namespace BulbPicker.App.Services
{
    public class RobotArmService
    {
        private static readonly RobotArmService _instance = new RobotArmService();
        public static RobotArmService Instance => _instance;

        private const int ROBOT_ARM_PORT = 8011;
        private const int PROGRAM_PORT = 1000;

        private ObservableCollection<RobotArm> _robotArms = new ObservableCollection<RobotArm>();
        public ObservableCollection<RobotArm> RobotArms
        {
            get => _robotArms;
            set => _robotArms = value;
        }

        private RobotArmService()
        {
            // TODO: Alias 없애기 걍
            RobotArms.Add(new RobotArm("1st Outside", "192.168.0.11", ROBOT_ARM_PORT, PROGRAM_PORT, RobotArmPosition.FirstRowOutside));
            RobotArms.Add(new RobotArm("1st Inside", "192.168.0.12", ROBOT_ARM_PORT, PROGRAM_PORT, RobotArmPosition.FirstRowInside));
            RobotArms.Add(new RobotArm("2nd Outside", "192.168.0.13", ROBOT_ARM_PORT, PROGRAM_PORT, RobotArmPosition.SecondRowOutside));
            RobotArms.Add(new RobotArm("2nd Inside", "192.168.0.14", ROBOT_ARM_PORT, PROGRAM_PORT, RobotArmPosition.SecondRowInside));
        }
    }
}
