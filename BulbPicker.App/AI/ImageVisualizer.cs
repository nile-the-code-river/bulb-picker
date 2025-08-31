using OpenCvSharp.Extensions;
using OpenCvSharp;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace WindowsFormsApp1
{
    public static class ImageVisualizer
    {
        public static Bitmap DrawBoxes(
    Mat srcMat,
    List<(float x1, float y1, float x2, float y2, float conf, float cls)> boxes)
        {
            if (srcMat == null || srcMat.Empty())
                throw new ArgumentException("srcMat 为空");

            // 1) 确保是 3 通道 BGR（否则 Graphics 无法在 8bpp 上绘制）
            Mat bgr = srcMat;
            bool needRelease = false;
            if (srcMat.Type() == MatType.CV_8UC1)
            {
                bgr = new Mat();
                Cv2.CvtColor(srcMat, bgr, ColorConversionCodes.GRAY2BGR);
                needRelease = true;
            }

            // 2) Mat -> Bitmap(24bppRgb)
            Bitmap bmp = BitmapConverter.ToBitmap(bgr);
            if (needRelease) bgr.Dispose();

            // 3) 复用你原来的绘制逻辑
            Bitmap result = new Bitmap(bmp);     // 拷贝一份用于绘制
            bmp.Dispose();

            using (Graphics g = Graphics.FromImage(result))
            using (Pen pen = new Pen(Color.Red, 2))
            using (Font font = new Font("Arial", 10))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Brush centerBrush = Brushes.Green;
                Brush textBrush = Brushes.Blue;
                int radius = 4;

                // 两条参考横线（按需修改）
                int yLine1 = 330;
                int yLine2 = 500;
                g.DrawLine(Pens.Yellow, 0, yLine1, result.Width - 1, yLine1);
                g.DrawLine(Pens.Yellow, 0, yLine2, result.Width - 1, yLine2);

                foreach (var (x1, y1, x2, y2, conf, cls) in boxes)
                {
                    int w = (int)(x2 - x1);
                    int h = (int)(y2 - y1);
                    Rectangle rect = new Rectangle((int)x1, (int)y1, w, h);
                    g.DrawRectangle(pen, rect);

                    float cx = (x1 + x2) / 2f;
                    float cy = (y1 + y2) / 2f;
                    float wid_value = (x2 - x1);
                    float height_value = (y2 - y1);
                    float z_value = Math.Min(wid_value, height_value);

                    g.FillEllipse(centerBrush, cx - radius, cy - radius, radius * 2, radius * 2);

                    string coordText = $"({cx:F0}, {cy:F0},  {z_value:F0})";
                    g.DrawString(coordText, font, textBrush, cx + radius + 2, cy - radius - 10);

                    string label = $"Conf: {conf:F2} Cls: {cls}";
                    g.DrawString(label, font, Brushes.Red, rect.X, rect.Y - 15);
                }
            }
            return result;   // Bitmap，可直接赋给 pictureBox.Image
        }
    }
}
