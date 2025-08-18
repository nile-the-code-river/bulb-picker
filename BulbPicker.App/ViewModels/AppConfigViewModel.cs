using BulbPicker.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulbPicker.App.ViewModels
{
    class AppConfigViewModel
    {
        const string TEST_IP = "000.000.000.000";
        public List<RobotArm> RobotArms { get; set; }
            = new List<RobotArm>() { new RobotArm(TEST_IP), new RobotArm(TEST_IP), new RobotArm(TEST_IP), new RobotArm(TEST_IP) };
    }
}
