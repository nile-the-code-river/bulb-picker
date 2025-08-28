using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
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

        public List<(float x1, float y1, float x2, float y2, float X_Center, float Y_Center)> PredictBoxes(Bitmap bmpInput640x640)
        {
            var input = new DenseTensor<float>(new[] { 1, 3, 640, 640 });

            for (int y = 0; y < 640; y++)
            {
                for (int x = 0; x < 640; x++)
                {
                    Color pixel = bmpInput640x640.GetPixel(x, y);
                    input[0, 0, y, x] = pixel.R / 255f;
                    input[0, 1, y, x] = pixel.G / 255f;
                    input[0, 2, y, x] = pixel.B / 255f;
                }
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("images", input)
            };

            using (var results = _session.Run(inputs))
            {
                var output = results.First().AsEnumerable<float>().ToArray();

                var boxes = new List<(float x1, float y1, float x2, float y2, float X_Center, float Y_Center)>();

                for (int i = 0; i < output.Length; i++)
                {

                    float x1 = output[i * 6];
                    float y1 = output[i * 6 + 1];
                    float x2 = output[i * 6 + 2];
                    float y2 = output[i * 6 + 3];
                    float conf = output[i * 6 + 4];
                    float cls = output[i * 6 + 5];
                    float X_Center = (x1 + x2) / 2;
                    float Y_Center = (y1 + y2) / 2;
                    if (cls == 1)
                        continue;

                    if (conf == 0f)
                        break;

                    boxes.Add((x1, y1, x2, y2, X_Center, Y_Center));
                }
                return boxes;
            }
        }
    }
}
