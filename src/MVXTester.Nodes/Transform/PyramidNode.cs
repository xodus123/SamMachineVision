using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Transform;

public enum PyramidDirection
{
    Up,
    Down
}

[NodeInfo("Pyramid", NodeCategories.Transform, Description = "Image pyramid operations (upscale or downscale)")]
public class PyramidNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _direction = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _resultOutput = AddOutput<Mat>("Result");
        _direction = AddEnumProperty("Direction", "Direction", PyramidDirection.Down, "Pyramid direction (Up = enlarge, Down = shrink)");
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

            var direction = _direction.GetValue<PyramidDirection>();

            var result = new Mat();
            if (direction == PyramidDirection.Up)
            {
                Cv2.PyrUp(image, result);
            }
            else
            {
                Cv2.PyrDown(image, result);
            }

            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Pyramid error: {ex.Message}";
        }
    }
}
