using System.Collections;
using System.Drawing;
using System.Text;
using System.Diagnostics;
using Basler.Pylon;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using System.Net;
using System.Net.Sockets;
using Microsoft.Win32;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace MVINSP_LINESCAN
{
    public partial class frmInspMain
    {
        #region Pylon
        //네 개의 카메라 객체를 정의합니다.
        private Camera camera = null;

        // PixelDataConverter는 픽셀 데이터를 처리하고 변환하는 데 사용되는 객체입니다. 각 카메라로부터 얻은 이미지를 처리하거나 포맷을 변환할 때 사용됩니다. 여기서는 세 개의 변환기
        private PixelDataConverter converter2 = new PixelDataConverter();

        //Stopwatch 객체는 시간 측정에 사용됩니다.카메라의 성능을 측정하거나, 이미지 캡처의 타이밍을 기록할 때 사용 각각 독립적인 시간 측정
        private Stopwatch stopWatch = new Stopwatch();

        //Bitmap 객체는 이미지를 저장하는 데 사용됩니다. 여기서는 가로 2590, 세로 2048 크기의 8비트 색상 인덱스를 가진 비트맵 이미지(m_bitmap)를 생성합니다
        private Bitmap m_bitmap = new Bitmap(2590, 2048, PixelFormat.Format8bppIndexed);
        private Bitmap m_CacheBitmap = null;  //캐시 비트맵으로 아직 초기화하지 않았으므로 null 처리
        #endregion

        #region Euresys
        EWorldShape EWorldShape1 = new EWorldShape(); // EWorldShape instance
        EImageBW8 imgSrc = new EImageBW8(); // EImageBW8 instance

        EROIBW8 roiSrc = new EROIBW8();
        EROIBW8 roiManual = new EROIBW8();
        EFrameShape EFrameShape1 = new EFrameShape(); // EFrameShape instance
        ELineGauge ELineGaugeFindPosition = new ELineGauge();
        EPointGauge EPointGaugeFindPosition = new EPointGauge();
        EMatcher EMatcherFindPosition = new EMatcher();
        #endregion

        #region Inspection Results
        ArrayList arrFrameShapes = new ArrayList();
        ArrayList arrToolResults = new ArrayList();
        ArrayList arrIsToolActivate = new ArrayList();
        ArrayList arrRepeatResult = new ArrayList();
        #endregion

        #region vars
        // 여러 카메라 시스템을 동시에 제어, 성능 측정, 이미지 데이터를 처리하는 데 필요한 변수 선언
        private frmLoading fLoading;  //로딩 화면을 표시하는 폼 객체, 이미지 처리 중이거나 데이터를 로드하는 동안 사용자에게 대기 상태를 보여주는데 사용

        // System.Diagnostics.Stopwatch 클래스를 사용하여 시간을 측정하는 타이머
        private System.Diagnostics.Stopwatch swInspProc; //검사 프로세스의 시간을 측정하는데 타이머 객체
        private System.Diagnostics.Stopwatch swTotalProc; //전체 프로세스 시간을 측정하는 객체

        //각 카메라에 보여주는 이미지를 저장하는 배열 객체
        private ArrayList arrImages = new ArrayList();

        //각 카메라에서 처리된 프레임 수를 기록하는 변수들
        private int n_FrameCount = 0;  //첫번쨰 카메라의 처리된 프레임 수. 일단 0으로 초기화
        private int TOTAL_FRAME_COUNT = 1;  //첫번쨰 카메라의 처리된 총 프레임수

        // 각 카메라의 해상도를 조정하는 변수. 너비와 높이를 일단 0으로 초기화
        private int nResolutionWidth = 0;
        private int nResolutionHeight = 0;


        // ROI(관심영역)의 좌표와 크기를 나타내는 변수들. ROI는 이미지에서 처리할 특정 영역을 나타냄
        private int nDMCodeROI_X = 0;  //첫번째 카메라의 관심영역 x좌표
        private int nDMCodeROI_Y = 0; //첫번째 카메라의 관심영역 y좌표
        private int nDMCodeROI_WIDTH = 0;  //첫번쨰 카메라의 관심영역 너비
        private int nDMCodeROI_HEIGHT = 0; //첫번째 카메라의 관심영역 높이

        private int n_defaultPictureBoxWidth = 0;
        private int n_defaultPictureBoxHeight = 0;

        private bool b_isOneShot = false;

        Point pPositionOfFirstClickScroll;

        OpenFileDialog ofdCurrentFile;
        OpenFileDialog ofdMatchingFile;

        OpenFileDialog ofdToolPreset;

        private float fMatchMinScore = 0.60f;
        private float fMatchingAngle = 1.00f;
        private int nMatchingCount = 1;

        /* COMM Preset */
        private string strPortName = "";
        private string strBaudRate = "";

        /* TCPIP SCARA Controller IP Endpoint Preset */
        IPEndPoint ipepSCARA1, ipepSCARA2, ipepSCARA3, ipepSCARA4;
        IPEndPoint ipepSCARA1CMD, ipepSCARA2CMD, ipepSCARA3CMD, ipepSCARA4CMD;
        Socket clientSCARA1, clientSCARA2, clientSCARA3, clientSCARA4;
        Socket clientSCARA1CMD, clientSCARA2CMD, clientSCARA3CMD, clientSCARA4CMD;
        string SCARA1_IP = "192.168.0.11";
        int SCARA1_SYS_PORT = 1000;
        int SCARA1_PORT = 8011;
        bool bFlagSCARA1 = false;
        string strMsgSCARA1 = "2,321.064,-503.605,227.472,1,0,0,221.064,-403.605,200.472,2,0,0\r";
        float fScaraXOffset = 0.000f;
        #endregion

        // 1: config settings & initializing
        public frmInspMain() { EnableButtons(false, false); GetSystemConfig(); GetToolPreset(); UpdateDataGrid(); }

        // 2 connect to the camera
        private void frmInspMain_Load(object sender, EventArgs e)
        {
            camera = new Camera("40007011");
            camera.CameraOpened += Configuration.AcquireContinuous;
            camera.ConnectionLost += OnConnectionLost;
            camera.CameraOpened += OnCameraOpened;
            camera.CameraClosed += OnCameraClosed;
            camera.StreamGrabber.GrabStarted += OnGrabStarted;
            camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;
            camera.StreamGrabber.GrabStopped += OnGrabStopped;
            camera.Open();

            camera2 = new Camera("40012243"); camera2.Open();

            fScaraXOffset = float.Parse(txtXOffset.Text.Trim()); // x y z

            Calibrate();
        }

        private void frmInspMain_FormClosing(object sender, FormClosingEventArgs ev) { clientSCARA1.Close(); camera2.Close(); camera2.Dispose(); }

        // 3: set scara & connect to it & endlessly receive? data from it
        private void ScaraConnect1()
        {
            ipepSCARA1 = new IPEndPoint(IPAddress.Parse(SCARA1_IP), SCARA1_PORT);
            clientSCARA1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSCARA1.Connect(ipepSCARA1);

            new Task(() =>
            {
                while (true)
                {
                    var binary = new Byte[4096];
                    clientSCARA1.Receive(binary);
                }
            }).Start();
        }

        // 4: scara disconnect
        private void ScaraDisconnect1() { clientSCARA1.Close(); }

        // 5: scara something
        private void ScaraSO1() { clientSCARA1CMD.Send(Encoding.ASCII.GetBytes("SO\r")); }

        // 6: scara run
        private void ScaraRN1() { clientSCARA1CMD.Send(Encoding.ASCII.GetBytes("RN\r")); }

        // 7: scara something
        private void ScaraRSERR1() { clientSCARA1CMD.Send(Encoding.ASCII.GetBytes("RS,ERR\r"));  }

        // 8: add log
        private void AddListResult(string msg) { lstResults.Items.Add(msg); }

        #region Support Functions

        // 9: ~~config settings for system & tool (labeling related?)~~
        [DllImport("kernel32")] public static extern int GetPrivateProfileString(string section, string key, string default1, StringBuilder result, int size, string path);
        [DllImport("kernel32")] private static extern long WritePrivateProfileString(string section, string key, string val, string path);
        [DllImport("kernel32")] private static extern uint GetPrivateProfileSection(string IpAppName, byte[] IpPairValues, uint nSize, string IpFileName);
        public string[] GetIniValue(string Section, string path)
        {
            byte[] ba = new byte[5000];
            uint Flag = GetPrivateProfileSection(Section, ba, 5000, path);
            return Encoding.Default.GetString(ba).Split(new char[1] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void GetSystemConfig()
        {
            StringBuilder result = new StringBuilder(255);
            GetPrivateProfileString("DEFAULT PRESET", "FILE_NAME", "none", result, 255, Application.StartupPath + "\\CONFIG.INI");
            ofdToolPreset = new OpenFileDialog();
            ofdToolPreset.FileName = result.ToString();
        }

        private void GetToolPreset()
        {
            StringBuilder result = new StringBuilder(255);
            GetPrivateProfileString("MATCHING", "FILE_NAME", "none", result, 255, ofdToolPreset.FileName);
            ofdMatchingFile = new OpenFileDialog();
            ofdMatchingFile.FileName = result.ToString();
            PatternMatchingLearn();

            GetPrivateProfileString("DMCODE_ROI_INFO", "HEIGHT", "none", result, 255, ofdToolPreset.FileName);
            nDMCodeROI_HEIGHT = int.Parse(result.ToString());
        }

        // 10: check if it can grab images or something
        private void EnableButtons(bool canGrab, bool canStop)
        {
            toolStripButtonAutoInspStart.Enabled = canGrab;
            toolStripButtonOneShot.Enabled = canGrab && camera.Parameters[PLCamera.AcquisitionMode].CanSetValue("SingleFrame");
            toolStripButtonStop.Enabled = canStop;
        }
        #endregion

        #region Vision Calibration/Inspection/Result/Display
        private void Calibrate()
        {
            EWorldShape1.SetSensorSize(nResolutionWidth, nResolutionHeight);
            imgSrc.Load(Application.StartupPath + "\\FRAME_BUFFER\\ACQ.bmp");
            EWorldShape1.Process(imgSrc, true);

            EWorldShape1.AddLandmark((float.Parse(gridCalibration["gridCalibrationPixelY", j].Value.ToString().Trim());
            EWorldShape1.AutoCalibrateLandmarks(false);
        }

        private void PatternMatchingLearn() { EImageBW8 imgTmp = new EImageBW8(); imgTmp.Load(ofdMatchingFile.FileName); EMatcherFindPosition.LearnPattern(imgTmp); }

        [HandleProcessCorruptedStateExceptions]
        private bool FindInitialPosition() { EMatcherFindPosition.MaxAngle = float.Parse("10.0"); EMatcherFindPosition.Match(imgSrc); }

        // AI 쪽에서 로봇팔에 coord 주면 필요없는 기능
        // 11: send the detection coordinates to SCARA
        [HandleProcessCorruptedStateExceptions]
        private void DoInspection()
        {
            imgSrc.Load(Application.StartupPath + "\\FRAME_BUFFER\\ACQ.bmp");
            FindInitialPosition();

            EFrameShape efs = new EFrameShape();
            efs.Attach(EWorldShape1);
            lstResults.Items.Add(strMsgSCARA1);

            //strMsgSCARA1 = "2,321.064,-503.605,227.472,1,0,0,221.064,-403.605,200.472,2,0,0\r";
            clientSCARA1.Send(Encoding.ASCII.GetBytes(strMsgSCARA1));

            arrFrameShapes.Add(efs);

            ((EFrameShape)obj).Process(imgSrc, true); // for
            EWorldShape1.Process(imgSrc, true);

            Redraw(pictureBox.CreateGraphics());
        }

        private void Redraw(Graphics g) { roiSrc.Attach(imgSrc); imgSrc.Draw(g, scale); EFrameShape1.SetZoom(scale); EFrameShape1.Draw(g, EDrawingMode.Actual); }
        #endregion



        #region Pylon Support Functions
        private void Stop() => camera.StreamGrabber.Stop();
        private void OneShot() => camera.StreamGrabber.Start(1, GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
        private void ContinuousShot() => camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
        #endregion



        #region Pylon Callback Functions
        private void OnConnectionLost(Object sender, EventArgs e) { BeginInvoke(new EventHandler<EventArgs>(OnConnectionLost), sender, e); DestroyCamera(); }
        private void OnCameraOpened(Object sender, EventArgs e) { BeginInvoke(new EventHandler<EventArgs>(OnCameraOpened), sender, e); camera.Parameters.Load(Application.StartupPath + "\\camera1_profile.pfs", ParameterPath.CameraDevice); EnableButtons(true, false); }
        private void OnCameraClosed(Object sender, EventArgs e) { BeginInvoke(new EventHandler<EventArgs>(OnCameraClosed), sender, e); EnableButtons(false, false); }
        private void OnGrabStarted(Object sender, EventArgs e) { BeginInvoke(new EventHandler<EventArgs>(OnGrabStarted), sender, e); EnableButtons(false, true); }
        private void OnImageGrabbed(Object sender, ImageGrabbedEventArgs e)
        {
            BeginInvoke(new EventHandler<ImageGrabbedEventArgs>(OnImageGrabbed), sender, e.Clone());

            IGrabResult grabResult = e.GrabResult;
            Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);
            pictureBox.Image = bitmap;

            e.DisposeGrabResultIfClone();
        }
        private void OnGrabStopped(Object sender, GrabStopEventArgs e) { BeginInvoke(new EventHandler<GrabStopEventArgs>(OnGrabStopped), sender, e); EnableButtons(true, false); }
        #endregion



        #region Event Handler
        private void btnSCARA1Connect_Click(object sender, EventArgs e) { ScaraDisconnect1(); ScaraConnect1(); }
        private void toolStripButtonOneShot_Click(object sender, EventArgs e) => OneShot();
        private void toolStripButtonAutoInspStart_Click(object sender, EventArgs e) { n_FrameCount = 0; ContinuousShot(); }
        private void toolStripButtonContinuousShot_Click(object sender, EventArgs e) => ContinuousShot();
        private void toolStripButtonStop_Click(object sender, EventArgs e) => Stop();
        private void btnSampleInspect_Click(object sender, EventArgs e) => DoInspection();
        private void pictureBox_Paint(object sender, PaintEventArgs e) => Redraw(e.Graphics);
        private void pictureBox_Resize(object sender, EventArgs e) => Redraw(pictureBox.CreateGraphics());
        private void btnConfirmMatching_Click(object sender, EventArgs e) => PatternMatchingLearn();
        private void tsbZoomIn_Click(object sender, EventArgs e) { pictureBox.Width = (int)(pictureBox.Width + (n_defaultPictureBoxWidth * 0.25)); pictureBox.Height = (int)(pictureBox.Height + (n_defaultPictureBoxHeight * 0.25)); }
        private void tsbZoomOut_Click(object sender, EventArgs e) { pictureBox.Width = (int)(pictureBox.Width - (n_defaultPictureBoxWidth * 0.25)); pictureBox.Height = (int)(pictureBox.Height - (n_defaultPictureBoxHeight * 0.25)); }
       
        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            // 십자선
            int nHoriDefault = 24;  int nVertiDefault = 0;  int nGridPitch = 4;
            tsslblAbsPosition.Text = "커서위치 픽셀계좌표 : X [" + pictureBox.PointToClient(new Point(Control.MousePosition.X, Control.MousePosition.Y)).X * (100 / float.Parse(tstxtZoomRatio.Text.Trim())) + "], Y [" + pictureBox.PointToClient(new Point(Control.MousePosition.X, Control.MousePosition.Y)).Y * (100 / float.Parse(tstxtZoomRatio.Text.Trim())) + "]";
        }

        private void gridCalibration_CellEnter(object sender, DataGridViewCellEventArgs e) => gridCalibration[e.ColumnIndex, e.RowIndex].Value = "0.00";
        private void gridCalibration_CellLeave(object sender, DataGridViewCellEventArgs e) => float.Parse(gridCalibration[e.ColumnIndex, e.RowIndex].Value.ToString().Trim());
        private void gridCalibration_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e) => gridCalibration["gridCalibrationNo", e.RowIndex - 1].Value = e.RowIndex.ToString();
        private void btnInspection_Click(object sender, EventArgs e) => DoInspection();
        #endregion



        #region GDI & Bitmap Support
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern int DeleteDC(IntPtr hdc);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern int BitBlt(IntPtr hdcDst, int xDst, int yDst, int w, int h, IntPtr hdcSrc, int xSrc, int ySrc, int rop);
        static int SRCCOPY = 0x00CC0020;

        private void btnApply1_Click(object sender, EventArgs e) => strMsgSCARA1 = txtMsg1.Text.Trim();
        private void btnSO1_Click(object sender, EventArgs e) => ScaraSO1();
        private void btnRN1_Click(object sender, EventArgs e) => ScaraRN1();
        private void btnRSERR1_Click(object sender, EventArgs e) => ScaraRSERR1();
        private void btnXOffset_Click(object sender, EventArgs e) => fScaraXOffset = float.Parse(txtXOffset.Text.Trim());



        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO bmi, uint Usage, out IntPtr bits, IntPtr hSection, uint dwOffset);
        static uint BI_RGB = 0;
        static uint DIB_RGB_COLORS = 0;
        private object converter;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public uint biSize;
            public int biWidth, biHeight;
            public short biPlanes, biBitCount;
            public uint biCompression, biSizeImage;
            public int biXPelsPerMeter, biYPelsPerMeter;
            public uint biClrUsed, biClrImportant;

            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 256)]
            public uint[] cols;
        }

        static uint MAKERGB(int r, int g, int b) => ((uint)(b & 255)) | ((uint)((r & 255) << 8)) | ((uint)((g & 255) << 16));

        // OpenCV로 대체 가능 ~~~~
        static System.Drawing.Bitmap CopyToBpp(System.Drawing.Bitmap b, int bpp)
        {
            IntPtr hbm = b.GetHbitmap();

            BITMAPINFO bmi = new BITMAPINFO();

            bmi.biSize = 40; bmi.biWidth = w; bmi.biHeight = h; bmi.biPlanes = 1; bmi.biBitCount = (short)bpp; bmi.biCompression = BI_RGB; bmi.biSizeImage = (uint)(((w + 7) & 0xFFFFFFF8) * h / 8); bmi.biXPelsPerMeter = 1000000; bmi.biYPelsPerMeter = 1000000;

            // Now for the colour table.
            uint ncols = (uint)1 << bpp;
            bmi.biClrUsed = ncols;
            bmi.biClrImportant = ncols;
            bmi.cols = new uint[256];
            if (bpp == 1) { bmi.cols[0] = MAKERGB(0, 0, 0); bmi.cols[1] = MAKERGB(255, 255, 255); }
            else { for (int i = 0; i < ncols; i++) bmi.cols[i] = MAKERGB(i, i, i); }

            IntPtr bits0; IntPtr hbm0 = CreateDIBSection(IntPtr.Zero, ref bmi, DIB_RGB_COLORS, out bits0, IntPtr.Zero, 0);
            IntPtr sdc = GetDC(IntPtr.Zero);
            IntPtr hdc = CreateCompatibleDC(sdc); SelectObject(hdc, hbm);
            IntPtr hdc0 = CreateCompatibleDC(sdc); SelectObject(hdc0, hbm0);

            BitBlt(hdc0, 0, 0, w, h, hdc, 0, 0, SRCCOPY);

            System.Drawing.Bitmap b0 = System.Drawing.Bitmap.FromHbitmap(hbm0);

            DeleteDC(hdc);
            DeleteDC(hdc0);
            ReleaseDC(IntPtr.Zero, sdc);
            DeleteObject(hbm);
            DeleteObject(hbm0);

            return b0;
        }
        #endregion
    }
}
