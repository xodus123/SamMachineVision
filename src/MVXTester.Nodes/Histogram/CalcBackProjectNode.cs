using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Histogram;

[NodeInfo("Calc Back Project", NodeCategories.Histogram, Description = "Histogram back-projection for object detection")]
public class CalcBackProjectNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private InputPort<Mat> _targetInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _histBins = null!;
    private NodeProperty _rangeMin = null!;
    private NodeProperty _rangeMax = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _targetInput = AddInput<Mat>("TargetRegion");
        _resultOutput = AddOutput<Mat>("Result");
        _histBins = AddIntProperty("HistBins", "Hist Bins", 180, 1, 256, "Number of histogram bins");
        _rangeMin = AddIntProperty("RangeMin", "Range Min", 0, 0, 255, "Minimum range value");
        _rangeMax = AddIntProperty("RangeMax", "Range Max", 180, 0, 255, "Maximum range value");
    }

    public override void Process()
    {
        try
        {
            var image = GetInputValue(_imageInput);
            var target = GetInputValue(_targetInput);

            if (image == null || image.Empty())
            {
                Error = "No input image";
                return;
            }
            if (target == null || target.Empty())
            {
                Error = "No target region image";
                return;
            }

            var histBins = _histBins.GetValue<int>();
            var rangeMin = _rangeMin.GetValue<int>();
            var rangeMax = _rangeMax.GetValue<int>();

            // Convert both images to HSV
            using var imageHsv = new Mat();
            using var targetHsv = new Mat();
            Cv2.CvtColor(image, imageHsv, ColorConversionCodes.BGR2HSV);
            Cv2.CvtColor(target, targetHsv, ColorConversionCodes.BGR2HSV);

            // Compute histogram of target's Hue channel
            var ranges = new[] { new Rangef(rangeMin, rangeMax) };
            using var targetHist = new Mat();
            Cv2.CalcHist(new[] { targetHsv }, new[] { 0 }, null, targetHist, 1, new[] { histBins }, ranges);
            Cv2.Normalize(targetHist, targetHist, 0, 255, NormTypes.MinMax);

            // Back-project onto image
            var result = new Mat();
            Cv2.CalcBackProject(new[] { imageHsv }, new[] { 0 }, targetHist, result, ranges);

            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Calc Back Project error: {ex.Message}";
        }
    }
}
