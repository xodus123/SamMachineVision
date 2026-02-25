using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Segmentation;

[NodeInfo("Watershed", NodeCategories.Segmentation, Description = "Watershed segmentation algorithm")]
public class WatershedNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private InputPort<Mat> _markersInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _distThreshold = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _markersInput = AddInput<Mat>("Markers");
        _resultOutput = AddOutput<Mat>("Result");
        _distThreshold = AddDoubleProperty("DistThreshold", "Distance Threshold", 0.5, 0.0, 1.0, "Threshold for distance transform when auto-generating markers");
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

            var distThreshold = _distThreshold.GetValue<double>();

            // Ensure image is 3-channel BGR (required by Watershed)
            Mat bgr = image;
            bool needDisposeBgr = false;
            if (image.Channels() == 1)
            {
                bgr = new Mat();
                Cv2.CvtColor(image, bgr, ColorConversionCodes.GRAY2BGR);
                needDisposeBgr = true;
            }

            var markersInput = GetInputValue(_markersInput);
            var markers = new Mat();

            if (markersInput != null && !markersInput.Empty())
            {
                // Use provided markers
                if (markersInput.Type() != MatType.CV_32SC1)
                    markersInput.ConvertTo(markers, MatType.CV_32SC1);
                else
                    markersInput.CopyTo(markers);
            }
            else
            {
                // Auto-generate markers using distance transform + threshold
                Mat gray;
                bool needDisposeGray = false;
                if (image.Channels() > 1)
                {
                    gray = new Mat();
                    Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                    needDisposeGray = true;
                }
                else
                {
                    gray = image;
                }

                var binary = new Mat();
                Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                if (needDisposeGray) gray.Dispose();

                // Distance transform
                var dist = new Mat();
                Cv2.DistanceTransform(binary, dist, DistanceTypes.L2, DistanceTransformMasks.Mask5);
                binary.Dispose();

                // Threshold distance transform
                Cv2.MinMaxLoc(dist, out _, out double maxDist);
                var distBinary = new Mat();
                Cv2.Threshold(dist, distBinary, maxDist * distThreshold, 255, ThresholdTypes.Binary);
                dist.Dispose();

                // Convert to CV_8U for connected components
                var distU8 = new Mat();
                distBinary.ConvertTo(distU8, MatType.CV_8UC1);
                distBinary.Dispose();

                // Find markers using connected components
                Cv2.ConnectedComponents(distU8, markers);
                distU8.Dispose();

                // Increment all labels by 1 so background becomes 1, not 0
                // Watershed treats 0 as unknown
                Cv2.Add(markers, new Scalar(1), markers);
            }

            // Apply watershed
            Cv2.Watershed(bgr, markers);

            // Color each segment
            var result = new Mat(image.Rows, image.Cols, MatType.CV_8UC3, Scalar.All(0));
            var random = new Random(42);

            // Find unique label range
            Cv2.MinMaxLoc(markers, out double minLabel, out double maxLabel);
            int numLabels = (int)maxLabel + 1;
            var colors = new Scalar[Math.Max(numLabels, 1)];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Scalar(random.Next(50, 256), random.Next(50, 256), random.Next(50, 256));
            }

            for (int y = 0; y < markers.Rows; y++)
            {
                for (int x = 0; x < markers.Cols; x++)
                {
                    int label = markers.At<int>(y, x);
                    if (label == -1)
                    {
                        // Boundary - draw in white
                        result.Set(y, x, new Vec3b(255, 255, 255));
                    }
                    else if (label > 0 && label < numLabels)
                    {
                        result.Set(y, x, new Vec3b(
                            (byte)colors[label].Val0,
                            (byte)colors[label].Val1,
                            (byte)colors[label].Val2));
                    }
                }
            }

            markers.Dispose();
            if (needDisposeBgr) bgr.Dispose();

            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Watershed error: {ex.Message}";
        }
    }
}
