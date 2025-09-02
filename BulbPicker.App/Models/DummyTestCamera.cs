using BulbPicker.App.Services;
using System.Drawing;
using System.IO;
using System.Windows;

namespace BulbPicker.App.Models
{
    public class DummyTestCamera : BaslerCamera
    {
        public DummyTestCamera(string alias, BaslerCameraPosition position) : base(alias, "Testing", position) { }

        private Bitmap FetchBitmapFromLocalDirectory(int fileName)
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Test", Alias);

            if (!Directory.Exists(folder))
            {
                MessageBox.Show($"{Alias} : Image source fodler does not exist for dummy camera test.");
                return null;
            }

            string bmpFile = Path.Combine(folder, $"{fileName}.bmp");

            if (!File.Exists(bmpFile))
            {
                MessageBox.Show($"{Alias} : Image file '{fileName}.bmp' not found in {folder}.");
                return null;
            }

            using (var retrievedBitmap = Image.FromFile(bmpFile))
            {
                return new Bitmap(retrievedBitmap);
            }
        }

        public void SimulateImageGrabbedEvent()
        {
            var bitmap = FetchBitmapFromLocalDirectory(TestIndexManager.Instance.DummyCameraImageIndex);

            ProcessBitmap(bitmap);

            bitmap.Dispose();
        }
    }
}
