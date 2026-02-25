using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Filter;

public enum InpaintType
{
    NavierStokes,
    Telea
}

[NodeInfo("Inpaint", NodeCategories.Filter, Description = "Image inpainting to repair damaged areas")]
public class InpaintNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private InputPort<Mat> _maskInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _radius = null!;
    private NodeProperty _method = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _maskInput = AddInput<Mat>("Mask");
        _resultOutput = AddOutput<Mat>("Result");
        _radius = AddDoubleProperty("Radius", "Inpaint Radius", 3.0, 1.0, 100.0, "Radius of a circular neighborhood of each point inpainted");
        _method = AddEnumProperty("Method", "Method", InpaintType.Telea, "Inpainting method");
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

            var radius = _radius.GetValue<double>();
            var method = _method.GetValue<InpaintType>();

            var cvMethod = method switch
            {
                InpaintType.NavierStokes => InpaintMethod.NS,
                InpaintType.Telea => InpaintMethod.Telea,
                _ => InpaintMethod.Telea
            };

            // Ensure mask is single channel
            Mat grayMask = mask;
            bool needDispose = false;
            if (mask.Channels() > 1)
            {
                grayMask = new Mat();
                Cv2.CvtColor(mask, grayMask, ColorConversionCodes.BGR2GRAY);
                needDispose = true;
            }

            var result = new Mat();
            Cv2.Inpaint(image, grayMask, result, radius, cvMethod);

            if (needDispose) grayMask.Dispose();

            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Inpaint error: {ex.Message}";
        }
    }
}
