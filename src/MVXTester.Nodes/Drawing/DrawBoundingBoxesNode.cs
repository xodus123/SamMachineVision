using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Drawing;

[NodeInfo("Draw Bounding Boxes", NodeCategories.Drawing, Description = "Draw bounding rectangles from Rect array")]
public class DrawBoundingBoxesNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private InputPort<Rect[]> _rectsInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _colorR = null!;
    private NodeProperty _colorG = null!;
    private NodeProperty _colorB = null!;
    private NodeProperty _thickness = null!;
    private NodeProperty _showLabel = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _rectsInput = AddInput<Rect[]>("Rects");
        _resultOutput = AddOutput<Mat>("Result");
        _colorR = AddIntProperty("ColorR", "Color R", 0, 0, 255, "Red component");
        _colorG = AddIntProperty("ColorG", "Color G", 255, 0, 255, "Green component");
        _colorB = AddIntProperty("ColorB", "Color B", 0, 0, 255, "Blue component");
        _thickness = AddIntProperty("Thickness", "Thickness", 2, 1, 10, "Line thickness");
        _showLabel = AddBoolProperty("ShowLabel", "Show Label", true, "Show dimension labels on each rectangle");
    }

    public override void Process()
    {
        try
        {
            var image = GetInputValue(_imageInput);
            var rects = GetInputValue(_rectsInput);

            if (image == null || image.Empty())
            {
                Error = "No input image";
                return;
            }

            if (rects == null || rects.Length == 0)
            {
                SetOutputValue(_resultOutput, image.Clone());
                SetPreview(image);
                Error = null;
                return;
            }

            var r = _colorR.GetValue<int>();
            var g = _colorG.GetValue<int>();
            var b = _colorB.GetValue<int>();
            var thickness = _thickness.GetValue<int>();
            var showLabel = _showLabel.GetValue<bool>();

            var result = image.Clone();
            if (result.Channels() == 1)
                Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

            var color = new Scalar(b, g, r);

            foreach (var rect in rects)
            {
                Cv2.Rectangle(result, rect, color, thickness);

                if (showLabel)
                {
                    var label = $"{rect.Width}x{rect.Height}";
                    Cv2.PutText(result, label, new Point(rect.X, rect.Y - 5),
                        HersheyFonts.HersheySimplex, 0.4, color, 1);
                }
            }

            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Draw Bounding Boxes error: {ex.Message}";
        }
    }
}
