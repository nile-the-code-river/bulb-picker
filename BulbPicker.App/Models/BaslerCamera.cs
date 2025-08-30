using Basler.Pylon;
using BulbPicker.App.Infrastructures;
using BulbPicker.App.Services;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BulbPicker.App.Models
{
    public enum BaslerCameraPosition
    {
        Outisde,
        Inside
    }

    public class BaslerCamera : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly PixelDataConverter _pixelConverter = new PixelDataConverter();

        public string Alias { get; init; }

        private Camera _camera;
        public Camera Camera
        {
            get => _camera;
            private set => _camera = value;
        }

        public string SerialNumber { get; private set; }
        public BaslerCameraPosition Position { get; init; }

        private BitmapSource _receivedBitmapsource;
        public BitmapSource ReceivedBitmapSource
        {
            get => _receivedBitmapsource;
            private set
            {
                _receivedBitmapsource = value;
                OnPropertyChanged(nameof(ReceivedBitmapSource));
            }
        }

        public RelayCommand TestCommand => new RelayCommand(execute => Run(), canExecute => Camera != null );

        public BaslerCamera(string alias, string serialNumber, BaslerCameraPosition position)
        {
            Alias = alias;
            SerialNumber = serialNumber;
            Position = position;

            // If SerialNumber is null, the app is testing for dummy (there is no real camera)
            if (SerialNumber == null) SerialNumber = "Testing Dummy";
            else SetUpCamera();
        }

        public void DisplayImageGrabbed(BitmapSource source)
        {
            // SAVE IMAGE : use FileSaveService instead
            //SaveGrabbedImageToTestFolder(bitmap, TestIndexManager.Instance.GetStopwatchMilliSecondsNow());

            Application.Current.Dispatcher.Invoke(() =>
            {
                ReceivedBitmapSource = source;
            }, System.Windows.Threading.DispatcherPriority.DataBind);
        }

        // TODO 0830 : make this async
        private void SetUpCamera()
        {
            try
            {
                Camera = new Camera(SerialNumber);

                Camera.CameraOpened += Configuration.AcquireContinuous;
                Camera.CameraOpened += Camera_CameraOpened;

                Camera.CameraClosed += Camera_CameraClosed;

                Camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;

                Camera.Open();
            }
            catch (Exception e)
            {
                MessageBox.Show(SerialNumber + " " + e.Message);
                SerialNumber = "NOT FOUND";
            }
        }

        // TODO: can make it better (copy pfs 'once' and set its type as 'content')
        private void Camera_CameraOpened(object? sender, EventArgs e)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\.."));
                string filePath = Path.Combine(projectRoot, "Assets", "Config_Temp", "camera-profile.pfs");

                Camera.Parameters.Load(filePath, ParameterPath.CameraDevice);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        // TODO 0830: Bitmap Clone 하지 말고 그냥 보낸 뒤 Dispose 하지 말기. Bitmap Dispose 관련 테스트 여러 번, 여러 개 하기.
        private void StreamGrabber_ImageGrabbed(object? sender, ImageGrabbedEventArgs e)
        {
            IGrabResult grabResult = e.GrabResult;

            using (grabResult)
            {
                if (grabResult.GrabSucceeded)
                {
                    // TODO: optimize https://chatgpt.com/c/68b1ad0c-7208-8325-9040-ea499392f20e
                    // TODO: separate into another method
                    Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppArgb);
                    BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                    _pixelConverter.OutputPixelFormat = PixelType.BGRA8packed;
                    IntPtr ptrBmp = bmpData.Scan0;
                    _pixelConverter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult);
                    bitmap.UnlockBits(bmpData);

                    // TODO: 이미지 합성 크기 다시 정하면 구현하기
                    //Bitmap resized = new Bitmap(bitmap, new System.Drawing.Size(640, 640));
                    // Reminder: this solved 'this bitmap is used in elsewhere' problem 
                    //var bmpForQueue = (Bitmap)resized.Clone();
                    var bmpForQueue = (Bitmap)bitmap.Clone();
                    AddToCompositionQueue(bmpForQueue);

                    var source = BitmapManager.BitmapToImageSource(bitmap);
                    DisplayImageGrabbed(source);

                    bitmap.Dispose();
                }
                else
                {
                    MessageBox.Show("GrabResult was not grabbed succesfully");
                }
            }
        }

        public void AddToCompositionQueue(Bitmap bitmap)
            => CompositeImageService.Instance.AddToCompositionQueue(new ImageToCompositeQuqueItem(bitmap, Position));


        // TODO 0831
        private void Camera_CameraClosed(object? sender, EventArgs e)
        {
            // TODO: 이미 Close 됐을 거 같은데?
            // TODO: 이거 App 꺼질 때도 다 해야 함. 로봇 팔 Dispose (Close) 관련도 다 처리해서 안전하게 만들어야 함
            Camera.Close();
            Camera.Dispose();
        }

        private void Run()
        {
            if (!Camera.StreamGrabber.IsGrabbing)
            {
                Camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            }
            else
            {
                MessageBox.Show("이미 카메라를 실행하고 있습니다.");
            }
        }
    }
}
