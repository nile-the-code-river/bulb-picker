using BulbPicker.App.Models;

namespace BulbPicker.App.ViewModels
{
    class RobotArmsViewModel
    {
        // TODO: get from config file
        public int ScaraOffsetX = 0;
        public int ScaraOffsetY = 0;
        public int ScaraOffsetZ = 0;

        const string TEST_IP = "192.168.0.13";
        const int TEST_SYS_PORT = 1000;
        const int TEST_PORT = 8011;
        public List<RobotArm> RobotArms { get; set; }
            = new List<RobotArm>() { new RobotArm(TEST_IP, TEST_SYS_PORT, TEST_PORT) };


    }
}
