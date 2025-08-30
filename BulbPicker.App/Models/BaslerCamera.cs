using Basler.Pylon;
using BulbPicker.App.Infrastructures;
using BulbPicker.App.Services;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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

        protected BaslerCamera(string alias, string serialNumber, BaslerCameraPosition position)
        {
            Alias = alias;
            SerialNumber = serialNumber;
            Position = position;
        }

        async public static Task<BaslerCamera> CreateAsync(string alias, string serialNumber, BaslerCameraPosition position)
        {
            var camera = new BaslerCamera(alias, serialNumber, position);

            if (camera.SerialNumber != null) await camera.SetUpCameraAsync();

            return camera;
        }

        async private Task SetUpCameraAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    Camera = new Camera(SerialNumber);

                    Camera.CameraOpened += Configuration.AcquireContinuous;
                    Camera.CameraOpened += Camera_CameraOpened;

                    Camera.CameraClosed += Camera_CameraClosed;

                    Camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;

                    Camera.Open();
                });
            }
            catch (Exception e)
            {
                MessageBox.Show(SerialNumber + " " + e.Message);
                SerialNumber = "NOT FOUND";
            }
        }

        public void DisplayImageGrabbed(BitmapSource source)
        {
            // SAVE IMAGE : use FileSaveService instead
            //SaveGrabbedImageToTestFolder(bitmap, TestIndexManager.Instance.GetStopwatchMilliSecondsNow());

            Application.Current.Dispatcher.Invoke(() =>
            {
                ReceivedBitmapSource = source;
            }, DispatcherPriority.DataBind);
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

        private void StreamGrabber_ImageGrabbed(object? sender, ImageGrabbedEventArgs e)
        {
            IGrabResult grabResult = e.GrabResult;

            using (grabResult)
            {
                if (grabResult.GrabSucceeded)
                {
                    Bitmap bitmap = RetrieveBitmapFromGrabResult(grabResult);

                    ProcessBitmap(bitmap);

                    FileSaveService.SaveBitmapTo(bitmap, FolderName.SingleImageGrabbed, $"{grabResult.Timestamp}_{Position}");
                    TestIndexManager.Instance.IncrementSingleImageGrabbedIndex();

                    bitmap.Dispose();
                }
                else
                {
                    LogService.Instance.AddLog(new Log($"An image was not grabbed succesfully. This could be due to the camera or the environment.\nwidth: {grabResult.Width}, height:{grabResult.Height}", LogType.ERROR));

                    Bitmap errorBitmap = null;
                    try
                    {
                        errorBitmap = RetrieveBitmapFromGrabResult(grabResult);
                        FileSaveService.SaveBitmapTo(errorBitmap, FolderName.ERROR, grabResult.Timestamp.ToString());
                    }
                    catch (Exception err)
                    {
                        LogService.Instance.AddLog(new Log($"Could not save the failed image. {err.Message}", LogType.ERROR));
                    }
                    finally
                    {
                        errorBitmap?.Dispose();
                    }
                }
            }
        }

        private Bitmap RetrieveBitmapFromGrabResult(IGrabResult grabResult)
        {
            // TODO later: optimize https://chatgpt.com/c/68b1ad0c-7208-8325-9040-ea499392f20e
            Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppArgb);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            _pixelConverter.OutputPixelFormat = PixelType.BGRA8packed;
            IntPtr ptrBmp = bmpData.Scan0;
            _pixelConverter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult);
            bitmap.UnlockBits(bmpData);

            return bitmap;
        }

        // TODO later: find a better name
        protected void ProcessBitmap(Bitmap bitmap)
        {
            // should (1) show image on UI & (2) send to composition queue to enable test cameras to work as same
            var image = BitmapManager.BitmapToImageSource(bitmap);
            DisplayImageGrabbed(image);

            // TODO: 이미지 합성 크기 다시 정하면 구현하기
            //Bitmap resized = new Bitmap(bitmap, new System.Drawing.Size(640, 640));
            // Reminder: this solved 'this bitmap is used in elsewhere' problem 
            //var bmpForQueue = (Bitmap)resized.Clone();

            var clone = (Bitmap)bitmap.Clone();

            // hand in ownership
            CompositeImageFragment fragment = new CompositeImageFragment(clone, Position);
            CompositeImageService.Instance.AddToCompositionQueue(fragment);
        }

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
