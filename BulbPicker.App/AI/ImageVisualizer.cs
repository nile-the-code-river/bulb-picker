using System.Drawing;

namespace WindowsFormsApp1
{
    public static class ImageVisualizer
    {
        public static Bitmap DrawBoxes(
            Bitmap bmp,
            List<(float x1, float y1, float x2, float y2, float conf, float cls)> boxes)
        {
            Bitmap result = new Bitmap(bmp);

            using (Graphics g = Graphics.FromImage(result))
            {
                Pen pen = new Pen(Color.Red, 2);
                Brush centerBrush = Brushes.Green;
                int radius = 4;
                Font font = new Font("Arial", 10);
                Brush textBrush = Brushes.Blue;

                foreach (var (x1, y1, x2, y2, conf, cls) in boxes)
                {
                    int width = (int)(x2 - x1);
                    int height = (int)(y2 - y1);
                    Rectangle rect = new Rectangle((int)x1, (int)y1, width, height);
                    g.DrawRectangle(pen, rect);

                    float centerX = (x1 + x2) / 2;
                    float centerY = (y1 + y2) / 2;

                    g.FillEllipse(centerBrush, centerX - radius, centerY - radius, radius * 2, radius * 2);

                    string coordText = $"({centerX:F0}, {centerY:F0})";
                    g.DrawString(coordText, font, textBrush, centerX + radius + 2, centerY - radius - 10);

                    string label = $"Conf: {conf:F2} Cls: {cls}";
                    g.DrawString(label, font, Brushes.Red, rect.X, rect.Y - 15);
                }
            }

            return result;
        }
    }
}
