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

            // GPT
            // 1) System.Drawing.Bitmap 으로 직접 로드(파일 잠금 해제용 클론)
            using var loaded = (Bitmap)Image.FromFile(bmpFile); // 잠금 발생
            using var bmp = new Bitmap(loaded);                  // 스트림/파일과 분리된 복제본

            // 2) 조합 서비스로 전달 (소유권: 여기서 유지 → 전달 후 필요시 따로 Clone 하거나, 서비스 쪽에서 Clone)
            SendBitmapForComposition(bmp); // 서비스가 즉시 쓰고 끝나면 여기서 dispose, 지속 보관이면 서비스가 Clone해서 보관

            // 3) WPF 표시가 필요하면 BitmapSource 로 변환
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
