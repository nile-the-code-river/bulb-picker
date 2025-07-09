using Basler.Pylon;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace BulbPicker.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Camera camera = null;
        public MainWindow()
        {
            InitializeComponent();
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
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (camera == null) return;

            try
            {
                camera.Open();

                camera.StreamGrabber.Start();
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

                camera.StreamGrabber.Stop();
                camera.Close();

                result.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Button_Click_1(object sender, RoutedEventArgs e) => Close();
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                List<ICameraInfo> allCameras = CameraFinder.Enumerate();
                var camera1 = allCameras.FirstOrDefault();
                if (camera1 == null)
                {
                    MessageBox.Show("No camera found");
                }
                else
                {
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
                    MessageBox.Show("Camera opened successfully");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Camera initialization failed: {ex.Message}");
            }
        }
    }
}