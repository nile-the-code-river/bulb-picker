using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Drawing;

namespace WindowsFormsApp1
{
    public class Yolov11Onnx
    {
        private InferenceSession _session;

        public Yolov11Onnx(string modelPath)
        {
            _session = new InferenceSession(modelPath);
        }
        // using Opencv method to prediction

        public List<(float x1, float y1, float x2, float y2, float X_Center, float Y_Center)>
    PredictBoxes(Mat bgr640)
        {
            if (bgr640.Empty() || bgr640.Width != 640 || bgr640.Height != 640 || bgr640.Type() != MatType.CV_8UC3)
                throw new ArgumentException("需要 640x640、CV_8UC3 的 Mat (BGR)");

            // Mat(BGR)->Tensor(NCHW, RGB, [0,1])
            var input = new DenseTensor<float>(new[] { 1, 3, 640, 640 });
            var idx = bgr640.GetGenericIndexer<Vec3b>();
            for (int y = 0; y < 640; y++)
                for (int x = 0; x < 640; x++)
                {
                    var p = idx[y, x];             // B,G,R
                    input[0, 0, y, x] = p.Item2 / 255f; // R
                    input[0, 1, y, x] = p.Item1 / 255f; // G
                    input[0, 2, y, x] = p.Item0 / 255f; // B
                }

            var inputs = new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor("images", input)
    };

            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = null;
            try
            {
                results = _session.Run(inputs);
                var first = results.First();
                float[] output = first.AsEnumerable<float>().ToArray();

                var boxes = new List<(float, float, float, float, float, float)>();
                for (int i = 0; i + 5 < output.Length; i++)
                {
                    float x1 = output[i * 6 + 0], y1 = output[i * 6 + 1],
                          x2 = output[i * 6 + 2], y2 = output[i * 6 + 3],
                          conf = output[i * 6 + 4], cls = output[i * 6 + 5];
                    if (conf == 0f) break;
                    if (cls == 1f) continue;

                    float cx = (x1 + x2) / 2f, cy = (y1 + y2) / 2f;
                    boxes.Add((x1, y1, x2, y2, cx, cy));
                }
                return boxes;
            }
            finally
            {
                if (results != null) results.Dispose();

                // 兼容不同 ORT 版本：只有在实现了 IDisposable 时才释放
                foreach (var v in inputs)
                {
                    var d = v as IDisposable;
                    if (d != null) d.Dispose();
                }
            }
        }


        //public List<(float x1, float y1, float x2, float y2, float X_Center, float Y_Center)>
        //    PredictBoxes(Bitmap bmpInput640x640)
        //{
        //    var input = new DenseTensor<float>(new[] { 1, 3, 640, 640 });

        //    for (int y = 0; y < 640; y++)
        //    {
        //        for (int x = 0; x < 640; x++)
        //        {
        //            Color pixel = bmpInput640x640.GetPixel(x, y);
        //            input[0, 0, y, x] = pixel.R / 255f;
        //            input[0, 1, y, x] = pixel.G / 255f;
        //            input[0, 2, y, x] = pixel.B / 255f;
        //        }
        //    }

        //    var inputs = new List<NamedOnnxValue>
        //    {
        //        NamedOnnxValue.CreateFromTensor("images", input)
        //    };

        //    using (var results = _session.Run(inputs))
        //    {
        //        var output = results.First().AsEnumerable<float>().ToArray();

        //        var boxes = new List<(float x1, float y1, float x2, float y2, float X_Center, float Y_Center)>();

        //        for (int i = 0; i < output.Length; i++)
        //        {

        //            float x1 = output[i * 6];
        //            float y1 = output[i * 6 + 1];
        //            float x2 = output[i * 6 + 2];
        //            float y2 = output[i * 6 + 3];
        //            float conf = output[i * 6 + 4];
        //            float cls = output[i * 6 + 5];
        //            float X_Center = (x1 + x2) / 2;
        //            float Y_Center = (y1 + y2) / 2;
        //            if (cls == 1)
        //                continue;

        //            if (conf == 0f)
        //                break;

        //            boxes.Add((x1, y1, x2, y2, conf, cls));
        //        }
        //        return boxes;
        //    }
    }
}
