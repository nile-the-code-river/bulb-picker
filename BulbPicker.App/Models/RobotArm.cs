using BulbPicker.App.Infrastructures;
using BulbPicker.App.Services;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;

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
        public string Alias { get; set; } = "Alias Ex.";
        public string IP { get; set; } = "IP Ex.";

        public int RobotArmPort { get; set; } = 0;
        public IPEndPoint RobotArmIPEndPoint { get; set; }
        public Socket RobotArmSocket { get; set; }

        public int ProgramPort { get; set; } = 0;
        public IPEndPoint ProgramIPEndPoint { get; set; }
        public Socket ProgramSocket;
        
        private RobotArmState _state;
        public RobotArmState State
        {
            get => _state;
            private set
            {
                _state = value;
            }
        }

        public RelayCommand PowerButtonCommand => new RelayCommand(execute => Power());
        public RelayCommand ServoOnCommand => new RelayCommand(execute => ServoOn(), canExecute => State == RobotArmState.Connected);
        public RelayCommand RunCommand => new RelayCommand(execute => Run(), canExecute => State == RobotArmState.ServoOn);

        public RelayCommand TestCommand => new RelayCommand(execute => TestMove());

        public event PropertyChangedEventHandler? PropertyChanged;

        public RobotArm(string alias, string ip, int robotArmPort, int programPort)
        {
            Alias = alias;
            IP = ip;
            RobotArmPort = robotArmPort;
            ProgramPort = programPort;
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
            if(State == RobotArmState.Disconnected) Connect();
            else Disconnect();
        }

        private void Connect()
        {
            try
            {
                SetUpConnectionConfiguration();

                RobotArmSocket.Connect(RobotArmIPEndPoint);
                ProgramSocket.Connect(ProgramIPEndPoint);
                State = RobotArmState.Connected;
                LogService.Instance.AddLog(new Log($"{IP} Connected", LogType.Connected));
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static void SafeClose(Socket? socket)
        {
            if (socket == null) return;
            try { if (socket.Connected) socket.Shutdown(SocketShutdown.Both); } catch { }
            try { socket.Close(); } catch { }
            try { socket.Dispose(); } catch { }
        }

        private void Disconnect()
        {
            SafeClose(RobotArmSocket);
            SafeClose(ProgramSocket);

            RobotArmSocket = null;
            ProgramSocket = null;

            State = RobotArmState.Disconnected;
            LogService.Instance.AddLog(new Log($"{IP} Disconnected", LogType.Disconnected));
        }

        private void ServoOn()
        {
            try
            {
                ProgramSocket.Send(Encoding.ASCII.GetBytes("SO\r"));
                State = RobotArmState.ServoOn;
                LogService.Instance.AddLog(new Log($"{IP} ServoOn", LogType.RobotArmCommunication));
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
                // TODO: add a property here "Alias" as well as in BaslerCamera
                LogService.Instance.AddLog(new Log($"{IP} now running", LogType.RobotArmCommunication));
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

        private void TestMove()
        {
            string test = "1,200.410,-500.000,147.616,1,0,0\r";
            test = "1," + (116.1641).ToString("0.000") + "," + (-690.9336).ToString("0.000") + "," + (139.1408).ToString("0.000") + ",1,0,0\r";
            RobotArmSocket.Send(Encoding.ASCII.GetBytes(test));
            LogService.Instance.AddLog(new Log($"{test} sent to {IP}", LogType.RobotArmPointsSent));
        }
    }
}
