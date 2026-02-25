using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Detection;

[NodeInfo("Pixel Count", NodeCategories.Detection, Description = "Count non-zero or thresholded pixels")]
public class PixelCountNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<int> _countOutput = null!;
    private OutputPort<double> _ratioOutput = null!;
    private NodeProperty _useThreshold = null!;
    private NodeProperty _thresholdValue = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _countOutput = AddOutput<int>("Count");
        _ratioOutput = AddOutput<double>("Ratio");
        _useThreshold = AddBoolProperty("UseThreshold", "Use Threshold", false, "Apply threshold before counting");
        _thresholdValue = AddIntProperty("ThresholdValue", "Threshold Value", 128, 0, 255, "Threshold value");
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

            var useThreshold = _useThreshold.GetValue<bool>();
            var thresholdValue = _thresholdValue.GetValue<int>();

            // Convert to grayscale if needed
            Mat gray = image;
            bool needDispose = false;
            if (image.Channels() > 1)
            {
                gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                needDispose = true;
            }

            Mat countMat = gray;
            bool needDisposeCount = false;

            if (useThreshold)
            {
                countMat = new Mat();
                Cv2.Threshold(gray, countMat, thresholdValue, 255, ThresholdTypes.Binary);
                needDisposeCount = true;
            }

            int count = Cv2.CountNonZero(countMat);
            int totalPixels = image.Width * image.Height;
            double ratio = totalPixels > 0 ? (double)count / totalPixels : 0.0;

            if (needDisposeCount) countMat.Dispose();
            if (needDispose) gray.Dispose();

            SetOutputValue(_countOutput, count);
            SetOutputValue(_ratioOutput, ratio);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Pixel Count error: {ex.Message}";
        }
    }
}
