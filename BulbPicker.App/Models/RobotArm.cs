using BulbPicker.App.Infrastructures;
using BulbPicker.App.Services;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

// TODO: 에러 핸들링, 예외 처리, Conn -> Discon -> Conn 때 문제 등 해결하기
namespace BulbPicker.App.Models
{
    public enum RobotArmState
    {
        Disconnected,
        Connected,
        ServoOn,
        Running
    }

    public enum RobotArmPosition
    {
        FirstRowOutside,
        FirstRowInside,
        SecondRowOutside,
        SecondRowInside
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

        public RobotArmPosition Position { get; private set; }

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

        public RelayCommand TestCommand => new RelayCommand(execute => TestRobotArmMove());

        public event PropertyChangedEventHandler? PropertyChanged;

        public RobotArm(string alias, string ip, int robotArmPort, int programPort, RobotArmPosition position)
        {
            Alias = alias;
            IP = ip;
            RobotArmPort = robotArmPort;
            ProgramPort = programPort;
            Position = position;
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
            catch (Exception e)
            {
                MessageBox.Show($"로봇팔과의 연결을 실패했습니다. 프로그램 또는/및 로봇팔을 재시작 해 주세요.\n{e.Message}");
            }
        }

        private void SafeClose(Socket? socket)
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
            catch (Exception e)
            {
                MessageBox.Show($"로봇팔에게 'ServoOn'을 요청했으나 문제가 발생했습니다. 프로그램 또는/및 로봇팔을 재시작 해 주세요.\n{e.Message}");
            }
        }

        private void Run()
        {
            try
            {
                ProgramSocket.Send(Encoding.ASCII.GetBytes("RN\r"));
                State = RobotArmState.Running;
                LogService.Instance.AddLog(new Log($"{IP} now running", LogType.RobotArmCommunication));
            }
            catch (Exception e)
            {
                MessageBox.Show($"로봇팔에게 'Run'을 요청했으나 문제가 발생했습니다. 프로그램 또는/및 로봇팔을 재시작 해 주세요.\n{e.Message}");
            }
        }

        // TODO: Implement. 로봇팔에게 좌표 보내는 메소드
        public void SendPickUpPoint(string pickUpPoint)
        {
            RobotArmSocket.Send(Encoding.ASCII.GetBytes(pickUpPoint));
        }

        private void TestRobotArmMove()
        {
            string testCoordinates = "1," + (116.1641).ToString("0.000") + "," + (-690.9336).ToString("0.000") + "," + (139.1408).ToString("0.000") + ",1,0,0\r";
            RobotArmSocket.Send(Encoding.ASCII.GetBytes(testCoordinates));
            LogService.Instance.AddLog(new Log($"{testCoordinates} sent to {IP}", LogType.RobotArmPointsSent));
        }
    }
}
