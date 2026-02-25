using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Contour;

[NodeInfo("Moments", NodeCategories.Contour, Description = "Compute image moments for each contour")]
public class MomentsNode : BaseNode
{
    private InputPort<Point[][]> _contoursInput = null!;
    private OutputPort<double[]> _areasOutput = null!;
    private OutputPort<double[]> _centerXOutput = null!;
    private OutputPort<double[]> _centerYOutput = null!;

    protected override void Setup()
    {
        _contoursInput = AddInput<Point[][]>("Contours");
        _areasOutput = AddOutput<double[]>("Areas");
        _centerXOutput = AddOutput<double[]>("CenterX");
        _centerYOutput = AddOutput<double[]>("CenterY");
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

            var areas = new double[contours.Length];
            var centerX = new double[contours.Length];
            var centerY = new double[contours.Length];

            for (int i = 0; i < contours.Length; i++)
            {
                var moments = Cv2.Moments(contours[i]);
                areas[i] = moments.M00;

                if (moments.M00 != 0)
                {
                    centerX[i] = moments.M10 / moments.M00;
                    centerY[i] = moments.M01 / moments.M00;
                }
                else
                {
                    centerX[i] = 0;
                    centerY[i] = 0;
                }
            }

            SetOutputValue(_areasOutput, areas);
            SetOutputValue(_centerXOutput, centerX);
            SetOutputValue(_centerYOutput, centerY);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Moments error: {ex.Message}";
        }
    }
}
