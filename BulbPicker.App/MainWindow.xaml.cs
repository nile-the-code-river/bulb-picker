using Basler.Pylon;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BulbPicker.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Camera camera = null;
        private DispatcherTimer captureTimer = null;
        private readonly object bufferLock = new object();
        private Queue<BitmapSource> imageBuffer = new Queue<BitmapSource>();
        private const int MAX_BUFFER_SIZE = 10; // 최대 10장까지 버퍼링

        public MainWindow()
        {
            InitializeComponent();
            InitializeCaptureTimer();
        }

        private void InitializeCaptureTimer()
        {
            captureTimer = new DispatcherTimer();
            captureTimer.Interval = TimeSpan.FromSeconds(1); // 1초마다
            captureTimer.Tick += CaptureTimer_Tick;
        }

        private void CaptureTimer_Tick(object sender, EventArgs e)
        {
            AutoOneShot();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else DragMove();
        }


        // 수동 캡쳐 (기존 기능 유지)
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (camera == null) return;
            try
            {
                if (!camera.IsOpen)
                {
                    camera.Open();
                }

                if (!camera.StreamGrabber.IsGrabbing)
                {
                    camera.StreamGrabber.Start();
                }

                IGrabResult result = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);

                if (result.GrabSucceeded)
                {
                    byte[] buffer = result.PixelData as byte[];
                    int stride = result.Width;
                    BitmapSource bitmap = BitmapSource.Create(
                        result.Width,
                        result.Height,
                        96, 96,
                        PixelFormats.Gray8,
                        null,
                        buffer,
                        stride);
                    CameraImage.Source = bitmap;
                }

                result.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                // UI 업데이트 - 연결 중 표시
                var button = sender as Button;
                if (button != null)
                {
                    button.Content = "연결 중...";
                    button.IsEnabled = false;
                }

                await Task.Run(() =>
                {
                    List<ICameraInfo> allCameras = CameraFinder.Enumerate();
                    var camera1 = allCameras.FirstOrDefault();
                    if (camera1 == null)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show("No camera found"));
                        return;
                    }

                    camera = new Camera(camera1);
                    camera.CameraOpened += Configuration.AcquireSingleFrame;
                    camera.Open();

                    // 혹시 grabbing 중이면 멈추기
                    if (camera.StreamGrabber.IsGrabbing)
                    {
                        camera.StreamGrabber.Stop();
                    }

                    //camera.Parameters[PLCamera.BslScalingFactor].SetValue(2.5);
                    //camera.Parameters[PLCamera.TriggerMode].SetValue(PLCamera.TriggerMode.Off);
                });

                MessageBox.Show("Camera opened successfully");

                // 카메라 연결 완료 후 자동 캡쳐 시작
                StartAutoCapture();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Camera initialization failed: {ex.Message}");
            }
            finally
            {
                // UI 복원
                var button = sender as Button;
                if (button != null)
                {
                    button.Content = "Connect"; // 원래 버튼 텍스트로 변경
                    button.IsEnabled = true;
                }
            }
        }
        private async void AutoOneShot()
        {
            if (camera == null || !camera.IsOpen) return;

            try
            {
                // 매번 새로운 캡쳐를 위해 StreamGrabber를 다시 시작
                if (camera.StreamGrabber.IsGrabbing)
                {
                    camera.StreamGrabber.Stop();
                }

                camera.StreamGrabber.Start();
                IGrabResult result = camera.StreamGrabber.RetrieveResult(2000, TimeoutHandling.Return);

                if (result != null && result.GrabSucceeded)
                {
                    byte[] buffer = result.PixelData as byte[];
                    int stride = result.Width;
                    BitmapSource bitmap = BitmapSource.Create(
                        result.Width,
                        result.Height,
                        96, 96,
                        PixelFormats.Gray8,
                        null,
                        buffer,
                        stride);

                    // UI 업데이트 (메인 스레드에서)
                    Dispatcher.Invoke(() =>
                    {
                        CameraImage.Source = bitmap;

                        // 버퍼에 이미지 추가
                        lock (bufferLock)
                        {
                            imageBuffer.Enqueue(bitmap);

                            // 버퍼 크기 제한
                            if (imageBuffer.Count > MAX_BUFFER_SIZE)
                            {
                                imageBuffer.Dequeue();
                            }
                        }
                    });
                }

                result?.Dispose();
                camera.StreamGrabber.Stop(); // 캡쳐 후 정지
            }
            catch (Exception ex)
            {
                // 에러 발생 시 타이머 정지
                captureTimer?.Stop();
                Dispatcher.Invoke(() => MessageBox.Show($"Auto capture error: {ex.Message}"));
            }
        }

        private void StartAutoCapture()
        {
            if (camera != null && camera.IsOpen)
            {
                try
                {
                    // 타이머 시작 (StreamGrabber는 AutoOneShot에서 매번 제어)
                    captureTimer.Start();

                    MessageBox.Show("Auto capture started (1 frame per second)");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to start auto capture: {ex.Message}");
                }
            }
        }
        private void StopAutoCapture()
        {
            captureTimer?.Stop();

            if (camera != null && camera.StreamGrabber.IsGrabbing)
            {
                camera.StreamGrabber.Stop();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        protected override void OnClosed(EventArgs e)
        {
            StopAutoCapture();

            if (camera != null)
            {
                if (camera.IsOpen)
                {
                    camera.Close();
                }
                camera.Dispose();
            }

            base.OnClosed(e);
        }
    }
}