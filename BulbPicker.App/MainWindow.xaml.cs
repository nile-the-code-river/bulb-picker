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

        private async void CameraConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.Content = "Connecting...";
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

                    if (camera.StreamGrabber.IsGrabbing) camera.StreamGrabber.Stop();
                });

                StartAutoCapture();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Camera initialization failed: {ex.Message}");
            }
            finally
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.Content = "Connected";
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

                    Dispatcher.Invoke(() =>
                    {
                        //CameraImage.Source = bitmap;
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
                    captureTimer.Start();
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

        //------------------------------------------------------------------------------------------
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