using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Contour;

[NodeInfo("Fit Ellipse", NodeCategories.Contour, Description = "Fit ellipses to contours (minimum 5 points)")]
public class FitEllipseNode : BaseNode
{
    private InputPort<Point[][]> _contoursInput = null!;
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _minPoints = null!;
    private NodeProperty _drawThickness = null!;

    protected override void Setup()
    {
        _contoursInput = AddInput<Point[][]>("Contours");
        _imageInput = AddInput<Mat>("Image");
        _resultOutput = AddOutput<Mat>("Result");
        _minPoints = AddIntProperty("MinPoints", "Min Points", 5, 5, 100, "Minimum number of points required to fit ellipse");
        _drawThickness = AddIntProperty("DrawThickness", "Draw Thickness", 2, 1, 10, "Thickness of drawn ellipses");
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

            var minPoints = _minPoints.GetValue<int>();
            var drawThickness = _drawThickness.GetValue<int>();

            var result = image.Clone();
            if (result.Channels() == 1)
                Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

            var color = new Scalar(0, 255, 0); // Green

            foreach (var contour in contours)
            {
                if (contour.Length < minPoints) continue;

                var ellipse = Cv2.FitEllipse(contour);
                Cv2.Ellipse(result, ellipse, color, drawThickness);
            }

            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Fit Ellipse error: {ex.Message}";
        }
    }
}
