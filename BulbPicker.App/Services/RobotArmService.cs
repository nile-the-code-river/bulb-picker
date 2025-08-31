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

        public ObservableCollection<RobotArm> RobotArms { get; private set; } = new ObservableCollection<RobotArm>();

        private RobotArmService()
        {
            // LATER TODO : get data from config file & initialize
            RobotArms.Add(new RobotArm("SCARA 1", "192.168.0.11", ROBOT_ARM_PORT, PROGRAM_PORT, RobotArmPosition.FirstRowOutside));
            RobotArms.Add(new RobotArm("SCARA 2", "192.168.0.12", ROBOT_ARM_PORT, PROGRAM_PORT, RobotArmPosition.FirstRowInside));
        }
    }
}
