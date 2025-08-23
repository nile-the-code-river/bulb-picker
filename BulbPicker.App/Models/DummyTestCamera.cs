using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BulbPicker.App.Models
{
    public class DummyTestCamera : BaslerCamera
    {
        public DummyTestCamera(string alias) : base(alias, null) { }

        public void FetchBitmapFromLocalDirectory(int fileName)
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Test", Alias);

            if (!Directory.Exists(folder))
            {
                MessageBox.Show($"{Alias} : Image source fodler does not exist for dummy camera test.");
                return;
            }


            string bmpFile = Path.Combine(folder, $"{fileName}.bmp");

            if (!File.Exists(bmpFile))
            {
                MessageBox.Show($"{Alias} : Image file '{fileName}.bmp' not found in {folder}.");
                return;
            }


            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(bmpFile, UriKind.Absolute);
                bi.EndInit();
                bi.Freeze();

                ReceivedBitmapSource = bi;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Alias} : Failed to load {fileName}.bmp\n{ex.Message}");
                ReceivedBitmapSource = null;
            }
        }
    }
}
