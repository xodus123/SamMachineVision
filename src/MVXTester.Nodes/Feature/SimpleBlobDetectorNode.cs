using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Feature;

[NodeInfo("Simple Blob Detector", NodeCategories.Feature, Description = "Detect blobs using SimpleBlobDetector")]
public class SimpleBlobDetectorNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private OutputPort<Point[]> _centersOutput = null!;
    private OutputPort<double[]> _sizesOutput = null!;
    private NodeProperty _minThreshold = null!;
    private NodeProperty _maxThreshold = null!;
    private NodeProperty _filterByArea = null!;
    private NodeProperty _minArea = null!;
    private NodeProperty _maxArea = null!;
    private NodeProperty _filterByCircularity = null!;
    private NodeProperty _minCircularity = null!;
    private NodeProperty _filterByConvexity = null!;
    private NodeProperty _minConvexity = null!;
    private NodeProperty _filterByInertia = null!;
    private NodeProperty _minInertiaRatio = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _resultOutput = AddOutput<Mat>("Result");
        _centersOutput = AddOutput<Point[]>("Centers");
        _sizesOutput = AddOutput<double[]>("Sizes");
        _minThreshold = AddIntProperty("MinThreshold", "Min Threshold", 50, 0, 255, "Minimum threshold for binarization");
        _maxThreshold = AddIntProperty("MaxThreshold", "Max Threshold", 220, 0, 255, "Maximum threshold for binarization");
        _filterByArea = AddBoolProperty("FilterByArea", "Filter By Area", true, "Filter blobs by area");
        _minArea = AddDoubleProperty("MinArea", "Min Area", 100.0, 0.0, 100000.0, "Minimum blob area");
        _maxArea = AddDoubleProperty("MaxArea", "Max Area", 50000.0, 0.0, 1000000.0, "Maximum blob area");
        _filterByCircularity = AddBoolProperty("FilterByCircularity", "Filter By Circularity", false, "Filter blobs by circularity");
        _minCircularity = AddDoubleProperty("MinCircularity", "Min Circularity", 0.1, 0.0, 1.0, "Minimum circularity");
        _filterByConvexity = AddBoolProperty("FilterByConvexity", "Filter By Convexity", false, "Filter blobs by convexity");
        _minConvexity = AddDoubleProperty("MinConvexity", "Min Convexity", 0.5, 0.0, 1.0, "Minimum convexity");
        _filterByInertia = AddBoolProperty("FilterByInertia", "Filter By Inertia", false, "Filter blobs by inertia ratio");
        _minInertiaRatio = AddDoubleProperty("MinInertiaRatio", "Min Inertia Ratio", 0.1, 0.0, 1.0, "Minimum inertia ratio");
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

            var blobParams = new SimpleBlobDetector.Params
            {
                MinThreshold = _minThreshold.GetValue<int>(),
                MaxThreshold = _maxThreshold.GetValue<int>(),
                FilterByArea = _filterByArea.GetValue<bool>(),
                MinArea = (float)_minArea.GetValue<double>(),
                MaxArea = (float)_maxArea.GetValue<double>(),
                FilterByCircularity = _filterByCircularity.GetValue<bool>(),
                MinCircularity = (float)_minCircularity.GetValue<double>(),
                FilterByConvexity = _filterByConvexity.GetValue<bool>(),
                MinConvexity = (float)_minConvexity.GetValue<double>(),
                FilterByInertia = _filterByInertia.GetValue<bool>(),
                MinInertiaRatio = (float)_minInertiaRatio.GetValue<double>()
            };

            using var detector = SimpleBlobDetector.Create(blobParams);
            var keypoints = detector.Detect(image);

            var centers = keypoints.Select(kp => new Point((int)kp.Pt.X, (int)kp.Pt.Y)).ToArray();
            var sizes = keypoints.Select(kp => (double)kp.Size).ToArray();

            var result = new Mat();
            Cv2.DrawKeypoints(image, keypoints, result, Scalar.All(-1), DrawMatchesFlags.DrawRichKeypoints);

            SetOutputValue(_resultOutput, result);
            SetOutputValue(_centersOutput, centers);
            SetOutputValue(_sizesOutput, sizes);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Simple Blob Detector error: {ex.Message}";
        }
    }
}
