using BulbPicker.App.Models;

namespace BulbPicker.App.ViewModels
{
    class AppConfigViewModel
    {
        public List<RobotArm> RobotArms { get; set; } = new List<RobotArm>();
        //const string TEST_IP = "000.000.000.000";
        //public List<RobotArm> RobotArms { get; set; }
        //    = new List<RobotArm>() { new RobotArm(TEST_IP), new RobotArm(TEST_IP), new RobotArm(TEST_IP), new RobotArm(TEST_IP) };
    }
}
