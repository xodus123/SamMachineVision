using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Contour;

[NodeInfo("Contour Filter", NodeCategories.Contour, Description = "Filter contours by area and/or perimeter range")]
public class ContourFilterNode : BaseNode
{
    private InputPort<Point[][]> _contoursInput = null!;
    private OutputPort<Point[][]> _filteredOutput = null!;
    private OutputPort<int> _countOutput = null!;
    private NodeProperty _minArea = null!;
    private NodeProperty _maxArea = null!;
    private NodeProperty _minPerimeter = null!;
    private NodeProperty _maxPerimeter = null!;

    protected override void Setup()
    {
        _contoursInput = AddInput<Point[][]>("Contours");
        _filteredOutput = AddOutput<Point[][]>("Filtered");
        _countOutput = AddOutput<int>("Count");
        _minArea = AddDoubleProperty("MinArea", "Min Area", 100, 0, 1000000, "Minimum contour area");
        _maxArea = AddDoubleProperty("MaxArea", "Max Area", 10000000, 0, 10000000, "Maximum contour area");
        _minPerimeter = AddDoubleProperty("MinPerimeter", "Min Perimeter", 0, 0, 100000, "Minimum contour perimeter");
        _maxPerimeter = AddDoubleProperty("MaxPerimeter", "Max Perimeter", 1000000, 0, 1000000, "Maximum contour perimeter");
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
            var maxArea = _maxArea.GetValue<double>();
            var minPerimeter = _minPerimeter.GetValue<double>();
            var maxPerimeter = _maxPerimeter.GetValue<double>();

            var filtered = contours.Where(c =>
            {
                var area = Cv2.ContourArea(c);
                var perimeter = Cv2.ArcLength(c, true);
                return area >= minArea && area <= maxArea &&
                       perimeter >= minPerimeter && perimeter <= maxPerimeter;
            }).ToArray();

            SetOutputValue(_filteredOutput, filtered);
            SetOutputValue(_countOutput, filtered.Length);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Contour Filter error: {ex.Message}";
        }
    }
}
