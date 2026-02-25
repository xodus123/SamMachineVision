using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Detection;

[NodeInfo("Min Max Loc", NodeCategories.Detection, Description = "Find min/max pixel locations and values")]
public class MinMaxLocNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<double> _minValOutput = null!;
    private OutputPort<double> _maxValOutput = null!;
    private OutputPort<Point> _minLocOutput = null!;
    private OutputPort<Point> _maxLocOutput = null!;
    private OutputPort<Mat> _resultOutput = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _minValOutput = AddOutput<double>("MinVal");
        _maxValOutput = AddOutput<double>("MaxVal");
        _minLocOutput = AddOutput<Point>("MinLoc");
        _maxLocOutput = AddOutput<Point>("MaxLoc");
        _resultOutput = AddOutput<Mat>("Result");
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

            // Convert to grayscale if needed
            Mat gray = image;
            bool needDispose = false;
            if (image.Channels() > 1)
            {
                gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                needDispose = true;
            }

            Cv2.MinMaxLoc(gray, out double minVal, out double maxVal, out Point minLoc, out Point maxLoc);

            if (needDispose) gray.Dispose();

            // Draw markers on result
            var result = image.Clone();
            if (result.Channels() == 1)
                Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

            // Min location - blue marker
            Cv2.Circle(result, minLoc, 8, new Scalar(255, 0, 0), 2);
            Cv2.DrawMarker(result, minLoc, new Scalar(255, 0, 0), MarkerTypes.Cross, 16, 2);
            Cv2.PutText(result, $"Min:{minVal:F0}", new Point(minLoc.X + 10, minLoc.Y - 5),
                HersheyFonts.HersheySimplex, 0.4, new Scalar(255, 0, 0), 1);

            // Max location - red marker
            Cv2.Circle(result, maxLoc, 8, new Scalar(0, 0, 255), 2);
            Cv2.DrawMarker(result, maxLoc, new Scalar(0, 0, 255), MarkerTypes.Cross, 16, 2);
            Cv2.PutText(result, $"Max:{maxVal:F0}", new Point(maxLoc.X + 10, maxLoc.Y - 5),
                HersheyFonts.HersheySimplex, 0.4, new Scalar(0, 0, 255), 1);

            SetOutputValue(_minValOutput, minVal);
            SetOutputValue(_maxValOutput, maxVal);
            SetOutputValue(_minLocOutput, minLoc);
            SetOutputValue(_maxLocOutput, maxLoc);
            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Min Max Loc error: {ex.Message}";
        }
    }
}
