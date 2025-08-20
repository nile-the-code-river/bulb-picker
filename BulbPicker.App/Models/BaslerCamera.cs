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
        public byte Index { get; set; } = 0;

        private Camera _camera;
        public Camera Camera
        {
            get => _camera;
            set
            {
                _camera = value;
                CameraNumber = value?.CameraInfo?.GetValueOrDefault("SerialNumber", "null");
            }
        }
        public string? CameraNumber { get; private set; }

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

        // TODO: Refine
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
