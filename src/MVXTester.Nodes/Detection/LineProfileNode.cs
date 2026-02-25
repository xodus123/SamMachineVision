using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Detection;

[NodeInfo("Line Profile", NodeCategories.Detection, Description = "Measure pixel intensity along a line")]
public class LineProfileNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<double[]> _profileOutput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _x1 = null!;
    private NodeProperty _y1 = null!;
    private NodeProperty _x2 = null!;
    private NodeProperty _y2 = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _profileOutput = AddOutput<double[]>("Profile");
        _resultOutput = AddOutput<Mat>("Result");
        _x1 = AddIntProperty("X1", "X1", 0, 0, 10000, "Start X coordinate");
        _y1 = AddIntProperty("Y1", "Y1", 0, 0, 10000, "Start Y coordinate");
        _x2 = AddIntProperty("X2", "X2", 100, 0, 10000, "End X coordinate");
        _y2 = AddIntProperty("Y2", "Y2", 100, 0, 10000, "End Y coordinate");
    }

    public override void Process()
    {
        try
        {
            var image = GetInputValue(_imageInput);
            if (image == null || image.Empty())
            {
                Error = "No input image";
                return;
            }

            var x1 = _x1.GetValue<int>();
            var y1 = _y1.GetValue<int>();
            var x2 = _x2.GetValue<int>();
            var y2 = _y2.GetValue<int>();

            // Convert to grayscale for intensity sampling
            Mat gray = image;
            bool needDispose = false;
            if (image.Channels() > 1)
            {
                gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                needDispose = true;
            }

            // Sample pixels along the line using Bresenham-like steps
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int numSamples = Math.Max(dx, dy) + 1;
            if (numSamples < 2) numSamples = 2;

            var profile = new double[numSamples];
            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / (numSamples - 1);
                int px = (int)(x1 + t * (x2 - x1));
                int py = (int)(y1 + t * (y2 - y1));

                // Clamp to image bounds
                px = Math.Clamp(px, 0, gray.Width - 1);
                py = Math.Clamp(py, 0, gray.Height - 1);

                profile[i] = gray.Get<byte>(py, px);
            }

            if (needDispose) gray.Dispose();

            // Draw result image
            var result = image.Clone();
            if (result.Channels() == 1)
                Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

            // Draw the measurement line
            Cv2.Line(result, new Point(x1, y1), new Point(x2, y2), new Scalar(0, 255, 0), 1);
            Cv2.Circle(result, new Point(x1, y1), 3, new Scalar(0, 0, 255), -1);
            Cv2.Circle(result, new Point(x2, y2), 3, new Scalar(255, 0, 0), -1);

            // Draw a small graph of the profile at the bottom
            int graphHeight = Math.Min(60, result.Height / 4);
            int graphWidth = result.Width;
            if (graphHeight > 10 && profile.Length > 1)
            {
                int graphTop = result.Height - graphHeight;

                // Semi-transparent background
                using var overlay = result.Clone();
                Cv2.Rectangle(overlay, new Rect(0, graphTop, graphWidth, graphHeight), new Scalar(0, 0, 0), -1);
                Cv2.AddWeighted(overlay, 0.5, result, 0.5, 0, result);

                // Draw profile curve
                for (int i = 1; i < profile.Length; i++)
                {
                    int px1 = (int)((double)(i - 1) / (profile.Length - 1) * (graphWidth - 1));
                    int px2 = (int)((double)i / (profile.Length - 1) * (graphWidth - 1));
                    int py1 = graphTop + graphHeight - 1 - (int)(profile[i - 1] / 255.0 * (graphHeight - 2));
                    int py2 = graphTop + graphHeight - 1 - (int)(profile[i] / 255.0 * (graphHeight - 2));
                    Cv2.Line(result, new Point(px1, py1), new Point(px2, py2), new Scalar(0, 255, 0), 1);
                }
            }

            SetOutputValue(_profileOutput, profile);
            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Line Profile error: {ex.Message}";
        }
    }
}
