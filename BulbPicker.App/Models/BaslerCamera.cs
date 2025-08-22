using Basler.Pylon;
using BulbPicker.App.Infrastructures;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BulbPicker.App.Models
{
    public class BaslerCamera : INotifyPropertyChanged
    {
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

        public BaslerCamera(string alias, string serialNumber)
        {
            Alias = alias;
            SerialNumber = serialNumber;

            SetUpCamera();
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

        private void StreamGrabber_GrabStopped(object? sender, GrabStopEventArgs e)
        {
            // empty for now
        }

        private void StreamGrabber_ImageGrabbed(object? sender, ImageGrabbedEventArgs e)
        {
            // empty for now
        }

        private void StreamGrabber_GrabStarted(object? sender, EventArgs e)
        {
            // empty for now
        }

        private void Camera_CameraOpened(object? sender, EventArgs e)
        {
            // empty for now
        }

        private void Camera_CameraClosed(object? sender, EventArgs e)
        {
            Camera.Close();
            Camera.Dispose();
        }

        // DEPRECATED
        public async void TakeOneShot()
        {
            if (Camera == null)
            {
                MessageBox.Show("Camera is null");
                return;
            }

            try
            {
                var grabResult = GrabShotResult();

                using (grabResult)
                {
                    if (grabResult.GrabSucceeded)
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
                }
            }
            catch (Exception e)
            {
                if(Camera.IsOpen)
                    Camera.Close();

                MessageBox.Show("Exception! {0}" + e.Message);
            }
        }

        private IGrabResult GrabShotResult()
        {
            Camera.CameraOpened += Configuration.AcquireSingleFrame;

            Camera.Open();

            // 
            if (Camera.StreamGrabber.IsGrabbing) MessageBox.Show("Already Grabbing");

            Camera.StreamGrabber.Start();
            IGrabResult grabResult = Camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
            Camera.StreamGrabber.Stop();

            Camera.Close();

            return grabResult;
        }
    }
}
