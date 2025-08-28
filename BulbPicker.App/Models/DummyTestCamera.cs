using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace BulbPicker.App.Models
{
    // TODO: 새로운 Image Composition 로직을 반영하기. (토요일에 만든 구 로직을 신 로직으로 대체하기)
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
        }
    }
}
