using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulbPicker.App.Models
{
    enum RobotArmState
    {
        On,
        Off,
        Running
    }

    class RobotArm
    {
        public string IPAddress { get; set; }
        public string Port { get; set; }
        public RobotArmState State { get; private set; }

        // Socket
        // write down commands keywords like SO, RN, RS,ERR

        public RobotArm(string ip)
        {
            IPAddress = ip;
        }

        public void Connect()
        {
            // socket.connect()
            State = RobotArmState.On;
        }

        public void Disconnect()
        {
            State = RobotArmState.Off;
        }

        public void Run()
        {
            State = RobotArmState.Running;
            //
        }
    }
}
