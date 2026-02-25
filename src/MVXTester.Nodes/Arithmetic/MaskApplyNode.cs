using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Arithmetic;

[NodeInfo("Mask Apply", NodeCategories.Arithmetic, Description = "Apply a mask to an image using bitwise AND")]
public class MaskApplyNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private InputPort<Mat> _maskInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _invert = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _maskInput = AddInput<Mat>("Mask");
        _resultOutput = AddOutput<Mat>("Result");
        _invert = AddBoolProperty("Invert", "Invert Mask", false, "Invert the mask before applying");
    }

    public override void Process()
    {
        try
        {
            var image = GetInputValue(_imageInput);
            var mask = GetInputValue(_maskInput);

            if (image == null || image.Empty())
            {
                Error = "No input image";
                return;
            }

            if (mask == null || mask.Empty())
            {
                Error = "No mask image";
                return;
            }

            var invert = _invert.GetValue<bool>();

            // Ensure mask is single channel
            Mat grayMask = mask;
            bool needDispose = false;
            if (mask.Channels() > 1)
            {
                grayMask = new Mat();
                Cv2.CvtColor(mask, grayMask, ColorConversionCodes.BGR2GRAY);
                needDispose = true;
            }

            // Invert mask if requested
            Mat appliedMask = grayMask;
            bool needDisposeMask = false;
            if (invert)
            {
                appliedMask = new Mat();
                Cv2.BitwiseNot(grayMask, appliedMask);
                needDisposeMask = true;
            }

            var result = new Mat();
            Cv2.BitwiseAnd(image, image, result, appliedMask);

            if (needDisposeMask) appliedMask.Dispose();
            if (needDispose) grayMask.Dispose();

            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Mask Apply error: {ex.Message}";
        }
    }
}
