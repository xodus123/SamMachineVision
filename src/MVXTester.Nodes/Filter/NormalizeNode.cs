using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Filter;

public enum NormalizeType
{
    MinMax,
    Inf,
    L1,
    L2
}

[NodeInfo("Normalize", NodeCategories.Filter, Description = "Normalize image intensity range")]
public class NormalizeNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _alpha = null!;
    private NodeProperty _beta = null!;
    private NodeProperty _normType = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _resultOutput = AddOutput<Mat>("Result");
        _alpha = AddDoubleProperty("Alpha", "Alpha", 0.0, 0.0, 255.0, "Lower range boundary (for NORM_MINMAX)");
        _beta = AddDoubleProperty("Beta", "Beta", 255.0, 0.0, 255.0, "Upper range boundary (for NORM_MINMAX)");
        _normType = AddEnumProperty("NormType", "Norm Type", NormalizeType.MinMax, "Normalization type");
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

            var alpha = _alpha.GetValue<double>();
            var beta = _beta.GetValue<double>();
            var normType = _normType.GetValue<NormalizeType>();

            var cvNormType = normType switch
            {
                NormalizeType.MinMax => NormTypes.MinMax,
                NormalizeType.Inf => NormTypes.INF,
                NormalizeType.L1 => NormTypes.L1,
                NormalizeType.L2 => NormTypes.L2,
                _ => NormTypes.MinMax
            };

            var result = new Mat();
            Cv2.Normalize(image, result, alpha, beta, cvNormType);

            // Convert to 8-bit for display if needed
            if (result.Type() != image.Type())
            {
                result.ConvertTo(result, image.Type());
            }

            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Normalize error: {ex.Message}";
        }
    }
}
