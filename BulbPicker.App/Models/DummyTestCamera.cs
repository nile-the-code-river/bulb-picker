using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace BulbPicker.App.Models
{
    public class DummyTestCamera : BaslerCamera
    {
        public DummyTestCamera(string alias, BaslerCameraPosition position) : base(alias, null, position) { }

        //GPT
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

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

            using var loaded = (Bitmap)Image.FromFile(bmpFile);
            using var bmp = new Bitmap(loaded);

            // IMPT: send bitmap for composition
            SendBitmapForComposition(bmp);

            var hBmp = bmp.GetHbitmap();
            try
            {
                var src = Imaging.CreateBitmapSourceFromHBitmap(
                    hBmp, IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                src.Freeze();
                ReceivedBitmapSource = src;
            }
            finally
            {
                // GDI 핸들 릭 방지
                DeleteObject(hBmp);
            }

            return;

            try
            {
                // For Display test
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(bmpFile, UriKind.Absolute);
                bi.EndInit();
                bi.Freeze();

                ReceivedBitmapSource = bi;


                // For Composition Test
                using var fs = File.OpenRead(bmpFile);
                using var temp = new Bitmap(fs);
                var bmp2 = new Bitmap(temp);
                try
                {
                    SendBitmapForComposition(bmp2);
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    bmp2.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Alias} : Failed to load {fileName}.bmp\n{ex.Message}");
                ReceivedBitmapSource = null;
            }
        }
    }
}
