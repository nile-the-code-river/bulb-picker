using BulbPicker.App.Infrastructures;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

// TODO: socket도 꼭 두 개여야 하나 한번 확인
// TODO: 전반적으로 log 넣기
namespace BulbPicker.App.Models
{
    public enum RobotArmState
    {
        Disconnected,
        Connected,
        ServoOn,
        Running
    }

    public class RobotArm : INotifyPropertyChanged
    {
        public string IP { get; set; }
        
        public int RobotArmPort { get; set; }
        public IPEndPoint RobotArmIPEndPoint { get; set; }
        public Socket RobotArmSocket { get; set; }

        public int ProgramPort { get; set; }
        public IPEndPoint ProgramIPEndPoint { get; set; }
        public Socket ProgramSocket;
        
        public RobotArmState State { get; private set; }

        public RelayCommand PowerButtonCommand => new RelayCommand(execute => Power());
        public RelayCommand ServoOnCommand => new RelayCommand(execute => ServoOn(), canExecute => State == RobotArmState.Connected);
        public RelayCommand RunCommand => new RelayCommand(execute => Run(), canExecute => State == RobotArmState.ServoOn);

        public event PropertyChangedEventHandler? PropertyChanged;

        public RobotArm(string ip, int robotArmPort, int programPort)
        {
            IP = ip;
            RobotArmPort = robotArmPort;
            ProgramPort = programPort;

            SetUpConnectionConfiguration();
        }

        private void SetUpConnectionConfiguration ()
        {
            // Robot Arm : connecting, sending coordinates
            RobotArmIPEndPoint = new IPEndPoint(IPAddress.Parse(IP), RobotArmPort);
            RobotArmSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Program : Controlling robot arms with command lines like 'SO', 'RN'
            ProgramIPEndPoint = new IPEndPoint(IPAddress.Parse(IP), ProgramPort);
            ProgramSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        private void Power()
        {
            if(State == RobotArmState.Connected) Disconnect();
            else Connect();
        }

        private void Connect()
        {
            try
            {
                RobotArmSocket.Connect(RobotArmIPEndPoint);
                State = RobotArmState.Connected;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void Disconnect()
        {
            try
            {
                RobotArmSocket.Close();
                ProgramSocket.Close();
                State = RobotArmState.Disconnected;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ServoOn()
        {
            try
            {
                ProgramSocket.Send(Encoding.ASCII.GetBytes("SO\r"));
                State = RobotArmState.ServoOn;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void Run()
        {
            try
            {
                ProgramSocket.Send(Encoding.ASCII.GetBytes("RN\r"));
                State = RobotArmState.Running;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void SendPickUpPoint()
        {
            string test = "";
            RobotArmSocket.Send(Encoding.ASCII.GetBytes(test));
        }

    }
}
