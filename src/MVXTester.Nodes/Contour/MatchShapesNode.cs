using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Contour;

[NodeInfo("Match Shapes", NodeCategories.Contour, Description = "Compare contours using shape matching")]
public class MatchShapesNode : BaseNode
{
    private InputPort<Point[][]> _contours1Input = null!;
    private InputPort<Point[][]> _contours2Input = null!;
    private OutputPort<double[]> _similaritiesOutput = null!;
    private NodeProperty _method = null!;

    protected override void Setup()
    {
        _contours1Input = AddInput<Point[][]>("Contours1");
        _contours2Input = AddInput<Point[][]>("Contours2");
        _similaritiesOutput = AddOutput<double[]>("Similarities");
        _method = AddEnumProperty("Method", "Match Method", ShapeMatchModes.I1, "Shape matching method");
    }

    public override void Process()
    {
        try
        {
            var contours1 = GetInputValue(_contours1Input);
            var contours2 = GetInputValue(_contours2Input);

            if (contours1 == null || contours1.Length == 0)
            {
                Error = "No Contours1 input";
                return;
            }

            if (contours2 == null || contours2.Length == 0)
            {
                Error = "No Contours2 input";
                return;
            }

            var method = _method.GetValue<ShapeMatchModes>();
            var reference = contours2[0];

            var similarities = contours1.Select(c =>
                Cv2.MatchShapes(c, reference, method)
            ).ToArray();

            SetOutputValue(_similaritiesOutput, similarities);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Match Shapes error: {ex.Message}";
        }
    }
}
