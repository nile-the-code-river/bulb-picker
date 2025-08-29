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

        public ObservableCollection<RobotArm> RobotArms { get; init; }

        private RobotArmService()
        {
            // LATER TODO : get data from config file & initialize
            RobotArms = new ObservableCollection<RobotArm>()
            {
                new RobotArm("SCARA 1", "192.168.0.11", ROBOT_ARM_PORT, PROGRAM_PORT, RobotArmPosition.FirstRowOutside),
                new RobotArm("SCARA 2", "192.168.0.12", ROBOT_ARM_PORT, PROGRAM_PORT, RobotArmPosition.FirstRowInside),
                new RobotArm("SCARA 3", "192.168.0.13", ROBOT_ARM_PORT, PROGRAM_PORT, RobotArmPosition.SecondRowOutside),
                new RobotArm("SCARA 4", "192.168.0.14", ROBOT_ARM_PORT, PROGRAM_PORT, RobotArmPosition.SecondRowInside)
            };
        }
    }
}
