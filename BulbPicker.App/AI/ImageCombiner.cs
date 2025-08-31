using OpenCvSharp.Extensions;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulbPicker.App.AI
{

    public static class ImageCombiner
    {

        //public static Bitmap ImageCombiner2x2(Bitmap OutsideAfter, Bitmap OutsideBefore, Bitmap InsideAfter, Bitmap InsideBefore)
        //{



        //    var new_bitmap = new Bitmap(640, 640);
        //    return new_bitmap;
        //}


        public static Mat Combine2x2WithScale(
            Bitmap ImgOutsideAfter, Bitmap ImgInsideAfter, Bitmap ImgOutsideBefore, Bitmap ImgInsideBefore  // , Bitmap img5, Bitmap img6
            )
        {
            if (ImgOutsideAfter == null || ImgInsideAfter == null || ImgOutsideBefore == null || ImgInsideBefore == null)
                throw new ArgumentNullException("One or more input images are null.");
            int singleWidth = ImgOutsideAfter.Width;
            int singleHeight = ImgInsideAfter.Height;

            int X_offset = 115;
            int Y_offset = 310;
            int Padding = 0; // singleWidth - X_offset - singleHeight + Y_offset;
            int new_width = singleWidth - X_offset;
            int new_height = singleHeight - Y_offset;

            Bitmap CombinedImage = new Bitmap(new_width * 2, (new_height + Padding) * 2, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(CombinedImage))
            {

                g.Clear(Color.White);
                Rectangle srcRectOA = new Rectangle(0, 0, new_width, new_height);
                Rectangle srcRectIA = new Rectangle(X_offset, 0, new_width, new_height);
                Rectangle srcRectOB = new Rectangle(0, Y_offset, new_width, new_height);
                Rectangle srcRectIB = new Rectangle(X_offset, Y_offset, new_width, new_height);
                Rectangle destRectOA = new Rectangle(0, Padding, new_width, new_height);
                Rectangle destRectIA = new Rectangle(new_width, Padding, new_width, new_height);
                Rectangle destRectOB = new Rectangle(0, new_height + Padding, new_width, new_height);
                Rectangle destRectIB = new Rectangle(new_width, new_height + Padding, new_width, new_height);

                g.DrawImage(ImgOutsideAfter, destRectOA, srcRectOA, GraphicsUnit.Pixel);
                g.DrawImage(ImgInsideAfter, destRectIA, srcRectIA, GraphicsUnit.Pixel);
                g.DrawImage(ImgOutsideBefore, destRectOB, srcRectOB, GraphicsUnit.Pixel);
                g.DrawImage(ImgInsideBefore, destRectIB, srcRectIB, GraphicsUnit.Pixel);


                //g.DrawImage(ImgOutsideAfter, 0, 0, singleWidth, singleHeight);
                //g.DrawImage(ImgInsideAfter, singleWidth-220, 0, singleWidth, singleHeight); // singleWidth-220
                //g.DrawImage(ImgOutsideBefore, 0, singleHeight, singleWidth, singleHeight); // singleHeight-612
                //g.DrawImage(ImgInsideBefore, singleWidth, singleHeight, singleWidth, singleHeight); // singleWidth-220, singleHeight-612
                //g.DrawImage(ImgOutsideAfter, 0, 0, singleWidth, singleHeight);
                //g.DrawImage(ImgInsideAfter, singleWidth, 0, singleWidth, singleHeight); // singleWidth-220
                //g.DrawImage(img5, 0, singleHeight * 2, singleWidth, singleHeight);
                //g.DrawImage(img6, singleWidth, singleHeight * 2, singleWidth, singleHeight);

            }
            //string path = @"D:\c#\combined.png";                  // 建议用 .png
            //CombinedImage.Save(path, ImageFormat.Png);

            Mat mat = BitmapConverter.ToMat(CombinedImage);

            // === 一次性等比缩放 + letterbox 到 640×640（黑边=0） ===
            const int target = 640;
            double s = System.Math.Min(target / (double)mat.Width, target / (double)mat.Height);
            int newW = (int)System.Math.Round(mat.Width * s);
            int newH = (int)System.Math.Round(mat.Height * s);
            if (newW < 1) newW = 1;     // 防御
            if (newH < 1) newH = 1;

            // 下采样用 AREA
            Mat resized = new Mat();
            Cv2.Resize(mat, resized, new OpenCvSharp.Size(newW, newH), 0, 0, InterpolationFlags.Area);

            // letterbox：把 resized 贴到 640×640 的中央
            Mat input640 = new Mat(new OpenCvSharp.Size(target, target), MatType.CV_8UC3, Scalar.All(0));
            int dx = (target - newW) / 2;
            int dy = (target - newH) / 2;
            Rect roi = new Rect(dx, dy, newW, newH);
            using (Mat inputView = new Mat(input640, roi))
            {
                resized.CopyTo(inputView);
            }

            // （可选）如果模型需要 RGB 而不是 BGR：
            //Mat input640_rgb = new Mat();
            //Cv2.CvtColor(input640, input640_rgb, ColorConversionCodes.BGR2RGB);

            // （可选）保存确认
            //Cv2.ImWrite(@"D:\c#\combined_640.png", input640);          // BGR
            // Cv2.ImWrite(@"D:\c#\combined_640_rgb.png", input640_rgb);

            // 用完注意释放（或包在 using 块里）
            mat.Dispose();
            resized.Dispose();
            //input640.Dispose();
            //input640_rgb.Dispose();

            return input640;          // 返回 BGR Mat（让调用方负责 Dispose）
        }
    }
}
