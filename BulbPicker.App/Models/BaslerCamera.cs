using Basler.Pylon;
using BulbPicker.App.Infrastructures;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BulbPicker.App.Models
{
    public class BaslerCamera : INotifyPropertyChanged
    {
        private readonly PixelDataConverter _pixelConverter = new PixelDataConverter();

        public string Alias { get; set; }

        private Camera _camera;
        public Camera Camera
        {
            get => _camera;
            set
            {
                _camera = value;
            }
        }
        public string SerialNumber { get; private set; }

        // TODO: change the name into TakenImage or sth

        private BitmapSource _receivedBitmapsource;
        public BitmapSource ReceivedBitmapSource
        {
            get => _receivedBitmapsource;
            set
            {
                _receivedBitmapsource = value;
                OnPropertyChanged(nameof(ReceivedBitmapSource));
            }
        }

        public RelayCommand TestCommand => new RelayCommand(execute => Run(), canExecute => Camera != null );

        // PropertyChanged
        // TODO: Separate
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public BaslerCamera(string alias, string serialNumber)
        {
            Alias = alias;
            SerialNumber = serialNumber;

            // If SerialNumber is null, it is testing camera dummy
            if (SerialNumber == null) SerialNumber = "Testing Dummy";
            else SetUpCamera();
        }

        // TODO: make this async
        private void SetUpCamera()
        {
            Camera = new Camera(SerialNumber);

            Camera.CameraOpened += Configuration.AcquireContinuous;
            Camera.CameraOpened += Camera_CameraOpened;

            Camera.CameraClosed += Camera_CameraClosed;

            Camera.StreamGrabber.GrabStarted += StreamGrabber_GrabStarted;
            Camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;
            Camera.StreamGrabber.GrabStopped += StreamGrabber_GrabStopped;

            Camera.Open();
        }

        private void RunDummyImageProcess()
        {
            new Task(() =>
            {
                // 1초마다 특정 폴더에서 이미지 가져와서 OneShotImage에 넣기
            });
        }


        private void Camera_CameraOpened(object? sender, EventArgs e)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                // bin\Debug\netX → 프로젝트 루트로 올라가기
                string projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\.."));
                string filePath = Path.Combine(projectRoot, "Assets", "Config_Temp", "camera-profile.pfs");

                Camera.Parameters.Load(filePath, ParameterPath.CameraDevice);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void StreamGrabber_GrabStopped(object? sender, GrabStopEventArgs e)
        {
            // empty for now
        }


        private string _sessionDir = null;
        private int _imageIndex = 0;
        private readonly object _saveLock = new object();


        private void SaveBitmapToSession(Bitmap bmp)
        {
            if (_sessionDir == null)
            {
                string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CapturedImages");
                Directory.CreateDirectory(baseDir);
                string sessionName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + Alias;
                _sessionDir = Path.Combine(baseDir, sessionName);
                Directory.CreateDirectory(_sessionDir);
                _imageIndex = 0;
            }

            int idx = System.Threading.Interlocked.Increment(ref _imageIndex) - 1;
            string fullPath = Path.Combine(_sessionDir, $"frame_{idx:D5}.bmp");

            // 원본과 메모리 연결을 끊기 위해 반드시 클론 후 저장
            using var clone = bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            lock (_saveLock)
            {
                clone.Save(fullPath, System.Drawing.Imaging.ImageFormat.Bmp);
            }
        }

        private void StreamGrabber_ImageGrabbed(object? sender, ImageGrabbedEventArgs e)
        {

            IGrabResult grabResult = e.GrabResult;

            using (grabResult)
            {
                if (grabResult.GrabSucceeded)
                {
                    Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                    // Lock the bits of the bitmap.
                    BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                    // Place the pointer to the buffer of the bitmap.
                    _pixelConverter.OutputPixelFormat = PixelType.BGRA8packed;
                    IntPtr ptrBmp = bmpData.Scan0;
                    _pixelConverter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult);
                    bitmap.UnlockBits(bmpData);


                    // image saving for test
                    SaveBitmapToSession(bitmap);


                    var source = BitmapToImageSource(bitmap);
                    bitmap.Dispose();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ReceivedBitmapSource = source;
                    }, System.Windows.Threading.DispatcherPriority.DataBind);
                }
            }
        }

        private void StreamGrabber_GrabStarted(object? sender, EventArgs e)
        {
            // empty for now
        }

        private void Camera_CameraClosed(object? sender, EventArgs e)
        {
            Camera.Close();
            Camera.Dispose();
        }

        private void Run()
        {
            Camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            // 
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                bitmapimage.Freeze();

                return bitmapimage;
            }
        }

        //// DEPRECATED
        //public async void FireOneShot()
        //{
        //    if (Camera == null)
        //    {
        //        MessageBox.Show("Camera is null");
        //        return;
        //    }

        //    try
        //    {
        //        var grabResult = GrabShotResult();

        //        using (grabResult)
        //        {
        //            if (grabResult.GrabSucceeded)
        //            {
        //                Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
        //                // Lock the bits of the bitmap.
        //                BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
        //                // Place the pointer to the buffer of the bitmap.
        //                _pixelConverter.OutputPixelFormat = PixelType.BGRA8packed;
        //                IntPtr ptrBmp = bmpData.Scan0;
        //                _pixelConverter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult);
        //                bitmap.UnlockBits(bmpData);

        //                OneShotImage = bitmap;


        //                // TODO: 전에 있던 이미지 데이터 저장하는 기능 구현해야 함
        //                //Bitmap bitmapOld = pictureBox.Image as Bitmap;
        //                //pictureBox.Image = bitmap;
        //                //if (bitmapOld != null)
        //                //{
        //                //    // Dispose the bitmap.
        //                //    bitmapOld.Dispose();
        //                //}



        //                //byte[] src = grabResult.PixelData as byte[];

        //                //if(src == null || src.Length == 0)
        //                //{
        //                //    MessageBox.Show("Buffer Error");
        //                //}

        //                //var copy = new byte[src.Length];
        //                //// Thread Safe
        //                //Buffer.BlockCopy(src, 0, copy, 0, src.Length);

        //                //// breaks here
        //                //Application.Current.Dispatcher.Invoke(() =>
        //                //{
        //                //    var bmp = BitmapSource.Create(
        //                //        grabResult.Width, grabResult.Height, 96, 96,
        //                //        PixelFormats.Gray8, null, copy, grabResult.Width);
        //                //    OneShotImage = bmp;
        //                //});

        //                //var conv = new PixelDataConverter { OutputPixelFormat = PixelType.BGRA8packed };
        //                //int w = grabResult.Width, h = grabResult.Height, stride = w * 4, size = stride * h;
        //                //var managed = new byte[size];
        //                //conv.Convert(managed, size, grabResult);

        //                //Application.Current.Dispatcher.Invoke(() =>
        //                //{
        //                //    var wb = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        //                //    wb.WritePixels(new Int32Rect(0, 0, w, h), managed, stride, 0);
        //                //    wb.Freeze();
        //                //    OneShotImage = wb;
        //                //});

        //                //byte[] buffer = grabResult.PixelData as byte[];
        //                //BitmapSource bitmap = BitmapSource.Create(
        //                //    grabResult.Width,
        //                //    grabResult.Height,
        //                //    96, 96,
        //                //    PixelFormats.Gray8,
        //                //    null,
        //                //    buffer,
        //                //    grabResult.Width);



        //                //bitmap.Freeze();

        //                // //breaks here
        //                //Application.Current.Dispatcher.Invoke(() =>
        //                //{
        //                //    OneShotImage = bitmap;
        //                //});
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        if(Camera.IsOpen)
        //            Camera.Close();

        //        MessageBox.Show("Exception! {0}" + e.Message);
        //    }
        //}

        //private IGrabResult GrabShotResult()
        //{
        //    Camera.CameraOpened += Configuration.AcquireSingleFrame;

        //    Camera.Open();

        //    // 
        //    if (Camera.StreamGrabber.IsGrabbing) MessageBox.Show("Already Grabbing");

        //    Camera.StreamGrabber.Start();
        //    IGrabResult grabResult = Camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
        //    Camera.StreamGrabber.Stop();

        //    Camera.Close();

        //    return grabResult;
        //}
    }
}
