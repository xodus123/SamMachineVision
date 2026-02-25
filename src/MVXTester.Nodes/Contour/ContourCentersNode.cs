using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Contour;

[NodeInfo("Contour Centers", NodeCategories.Contour, Description = "Find centroids of contours using moments")]
public class ContourCentersNode : BaseNode
{
    private InputPort<Point[][]> _contoursInput = null!;
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<Point[]> _centersOutput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _minArea = null!;
    private NodeProperty _drawRadius = null!;

    protected override void Setup()
    {
        _contoursInput = AddInput<Point[][]>("Contours");
        _imageInput = AddInput<Mat>("Image");
        _centersOutput = AddOutput<Point[]>("Centers");
        _resultOutput = AddOutput<Mat>("Result");
        _minArea = AddDoubleProperty("MinArea", "Min Area", 0, 0, 1000000, "Minimum contour area to include");
        _drawRadius = AddIntProperty("DrawRadius", "Draw Radius", 5, 1, 50, "Radius of drawn center points");
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

            var minArea = _minArea.GetValue<double>();
            var drawRadius = _drawRadius.GetValue<int>();

            var centersList = new List<Point>();

            foreach (var contour in contours)
            {
                var area = Cv2.ContourArea(contour);
                if (area < minArea) continue;

                var moments = Cv2.Moments(contour);
                if (moments.M00 == 0) continue;

                int cx = (int)(moments.M10 / moments.M00);
                int cy = (int)(moments.M01 / moments.M00);
                centersList.Add(new Point(cx, cy));
            }

            var centers = centersList.ToArray();
            SetOutputValue(_centersOutput, centers);

            var image = GetInputValue(_imageInput);
            if (image != null && !image.Empty())
            {
                var result = image.Clone();
                if (result.Channels() == 1)
                    Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

                foreach (var center in centers)
                {
                    Cv2.Circle(result, center, drawRadius, new Scalar(0, 0, 255), -1);
                }

                SetOutputValue(_resultOutput, result);
                SetPreview(result);
            }

            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Contour Centers error: {ex.Message}";
        }
    }
}
