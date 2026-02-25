using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Transform;

public enum DistanceType
{
    L1,
    L2,
    C
}

public enum DistanceMaskSize
{
    Three = 3,
    Five = 5,
    Precise = 0
}

[NodeInfo("Distance Transform", NodeCategories.Transform, Description = "Distance transform of a binary image")]
public class DistanceTransformNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _distanceType = null!;
    private NodeProperty _maskSize = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _resultOutput = AddOutput<Mat>("Result");
        _distanceType = AddEnumProperty("DistanceType", "Distance Type", DistanceType.L2, "Distance type for transform");
        _maskSize = AddEnumProperty("MaskSize", "Mask Size", DistanceMaskSize.Five, "Size of the distance transform mask");
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

            var distType = _distanceType.GetValue<DistanceType>();
            var maskSize = _maskSize.GetValue<DistanceMaskSize>();

            var cvDistType = distType switch
            {
                DistanceType.L1 => DistanceTypes.L1,
                DistanceType.L2 => DistanceTypes.L2,
                DistanceType.C => DistanceTypes.C,
                _ => DistanceTypes.L2
            };

            var cvMaskSize = maskSize switch
            {
                DistanceMaskSize.Three => DistanceTransformMasks.Mask3,
                DistanceMaskSize.Five => DistanceTransformMasks.Mask5,
                DistanceMaskSize.Precise => DistanceTransformMasks.Precise,
                _ => DistanceTransformMasks.Mask5
            };

            // Ensure grayscale input
            Mat gray = image;
            bool needDispose = false;
            if (image.Channels() > 1)
            {
                gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                needDispose = true;
            }

            using var distResult = new Mat();
            Cv2.DistanceTransform(gray, distResult, cvDistType, cvMaskSize);

            if (needDispose) gray.Dispose();

            // Normalize to 0-255 for visualization
            var result = new Mat();
            Cv2.Normalize(distResult, result, 0, 255, NormTypes.MinMax);
            result.ConvertTo(result, MatType.CV_8U);

            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Distance Transform error: {ex.Message}";
        }
    }
}
