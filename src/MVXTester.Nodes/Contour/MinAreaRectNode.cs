using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Contour;

[NodeInfo("Min Area Rect", NodeCategories.Contour, Description = "Find minimum area rotated rectangles for contours")]
public class MinAreaRectNode : BaseNode
{
    private InputPort<Point[][]> _contoursInput = null!;
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _drawThickness = null!;

    protected override void Setup()
    {
        _contoursInput = AddInput<Point[][]>("Contours");
        _imageInput = AddInput<Mat>("Image");
        _resultOutput = AddOutput<Mat>("Result");
        _drawThickness = AddIntProperty("DrawThickness", "Draw Thickness", 2, 1, 10, "Thickness of drawn rectangles");
    }

    public override void Process()
    {
        try
        {
            var contours = GetInputValue(_contoursInput);
            if (contours == null || contours.Length == 0)
            {
                Error = "No contours input";
                return;
            }

            var image = GetInputValue(_imageInput);
            if (image == null || image.Empty())
            {
                Error = "No input image";
                return;
            }

            var drawThickness = _drawThickness.GetValue<int>();

            var result = image.Clone();
            if (result.Channels() == 1)
                Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

            var color = new Scalar(0, 255, 0); // Green

            foreach (var contour in contours)
            {
                if (contour.Length < 2) continue;

                var box = Cv2.MinAreaRect(contour);
                var points = box.Points().Select(p => new Point((int)p.X, (int)p.Y)).ToArray();
                Cv2.Polylines(result, new[] { points }, true, color, drawThickness);
            }

            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Min Area Rect error: {ex.Message}";
        }
    }
}
