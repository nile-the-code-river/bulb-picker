using Basler.Pylon;
using BulbPicker.App.Infrastructures;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;

//private PixelDataConverter converter = new PixelDataConverter();
//public string CameraNumber { get; init; }
//public bool IsGrabbing { get; set; }
//public int AxisOffsetX { get; set; }
//public int AxisOffsetY { get; set; }

namespace BulbPicker.App.Models
{
    public class BaslerCamera : INotifyPropertyChanged
    {
        public byte Index { get; set; } = 0;

        private Camera _camera;
        public Camera Camera
        {
            get => _camera;
            set
            {
                _camera = value;
                IPAddress = value?.CameraInfo?.GetValueOrDefault("IpAddress", "0");
                value.Open();
            }
        }
        public string? IPAddress { get; private set; } = "000.000.000.000";

        private BitmapSource _oneShotImage;
        public BitmapSource OneShotImage
        {
            get => _oneShotImage;
            set
            {
                _oneShotImage = value;
                OnPropertyChanged(nameof(OneShotImage));
            }
        }

        public RelayCommand TestCommand => new RelayCommand(execute => TakeOneShot(), canExecute => Camera != null );

        // PropertyChanged
        // TODO: Separate
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void Test()
        {
            MessageBox.Show("clicked");
        }

        // TODO: Refine
        public async void TakeOneShot()
        {
            try
            {
                // 매번 새로운 캡쳐를 위해 StreamGrabber를 다시 시작
                if (Camera.StreamGrabber.IsGrabbing) Camera.StreamGrabber.Stop();
                Camera.StreamGrabber.Start();
                
                IGrabResult grabResult = Camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);

                if (grabResult == null || !grabResult.GrabSucceeded)
                {
                    Camera.StreamGrabber.Stop();
                    return;
                }

                if (grabResult != null && grabResult.GrabSucceeded)
                {
                    byte[] buffer = grabResult.PixelData as byte[];
                    BitmapSource bitmap = BitmapSource.Create(
                        grabResult.Width,
                        grabResult.Height,
                        96, 96,
                        PixelFormats.Gray8,
                        null,
                        buffer,
                        grabResult.Width);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OneShotImage = bitmap;
                    });
                }

                grabResult?.Dispose();
                Camera.StreamGrabber.Stop(); // 캡쳐 후 정지

            }
            catch (Exception e)
            {
                if(Camera.IsOpen) Camera.Close();
                MessageBox.Show("Exception: {0}" + e.Message);
            }
        }

        // TODO: Refine
        //private IGrabResult StartGrab()
        //{
        //    Camera.CameraOpened += Configuration.AcquireSingleFrame;

        //    Camera.Open();

        //    Camera.StreamGrabber.Start();
        //    IGrabResult grabResult = Camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
        //    Camera.StreamGrabber.Stop();
        //    Camera.Close();

        //    return grabResult;
        //}
    }
}
