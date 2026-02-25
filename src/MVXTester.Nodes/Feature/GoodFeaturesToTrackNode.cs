using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Feature;

[NodeInfo("Good Features To Track", NodeCategories.Feature, Description = "Shi-Tomasi corner detection (Good Features to Track)")]
public class GoodFeaturesToTrackNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private OutputPort<Point[]> _cornersOutput = null!;
    private NodeProperty _maxCorners = null!;
    private NodeProperty _qualityLevel = null!;
    private NodeProperty _minDistance = null!;
    private NodeProperty _blockSize = null!;
    private NodeProperty _useHarris = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _resultOutput = AddOutput<Mat>("Result");
        _cornersOutput = AddOutput<Point[]>("Corners");
        _maxCorners = AddIntProperty("MaxCorners", "Max Corners", 100, 1, 10000, "Maximum number of corners to return");
        _qualityLevel = AddDoubleProperty("QualityLevel", "Quality Level", 0.01, 0.001, 1.0, "Minimal accepted quality of corners");
        _minDistance = AddDoubleProperty("MinDistance", "Min Distance", 10.0, 1.0, 1000.0, "Minimum possible Euclidean distance between corners");
        _blockSize = AddIntProperty("BlockSize", "Block Size", 3, 3, 31, "Size of averaging block for computing derivative covariance matrix");
        _useHarris = AddBoolProperty("UseHarris", "Use Harris", false, "Use Harris detector instead of Shi-Tomasi");
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

            var maxCorners = _maxCorners.GetValue<int>();
            var qualityLevel = _qualityLevel.GetValue<double>();
            var minDistance = _minDistance.GetValue<double>();
            var blockSize = _blockSize.GetValue<int>();
            var useHarris = _useHarris.GetValue<bool>();

            // Convert to grayscale if needed
            Mat gray = image;
            bool needDispose = false;
            if (image.Channels() > 1)
            {
                gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                needDispose = true;
            }

            var corners2f = Cv2.GoodFeaturesToTrack(gray, maxCorners, qualityLevel, minDistance,
                null, blockSize, useHarris, 0.04);
            if (needDispose) gray.Dispose();

            // Convert Point2f[] to Point[]
            var corners = corners2f.Select(p => new Point((int)p.X, (int)p.Y)).ToArray();

            // Draw corners on result
            var result = image.Clone();
            if (result.Channels() == 1)
                Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

            foreach (var corner in corners)
            {
                Cv2.Circle(result, corner, 5, new Scalar(0, 255, 0), 2);
            }

            SetOutputValue(_resultOutput, result);
            SetOutputValue(_cornersOutput, corners);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Good Features To Track error: {ex.Message}";
        }
    }
}
