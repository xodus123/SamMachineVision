using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Detection;

[NodeInfo("Template Match Multi", NodeCategories.Detection, Description = "Find multiple template matches in image using NMS")]
public class TemplateMatchMultiNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private InputPort<Mat> _templateInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private OutputPort<Point[]> _matchesOutput = null!;
    private OutputPort<int> _countOutput = null!;
    private NodeProperty _method = null!;
    private NodeProperty _matchThreshold = null!;
    private NodeProperty _maxMatches = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _templateInput = AddInput<Mat>("Template");
        _resultOutput = AddOutput<Mat>("Result");
        _matchesOutput = AddOutput<Point[]>("Matches");
        _countOutput = AddOutput<int>("Count");
        _method = AddEnumProperty("Method", "Method", TemplateMatchModes.CCoeffNormed, "Matching method");
        _matchThreshold = AddDoubleProperty("MatchThreshold", "Match Threshold", 0.8, 0.0, 1.0, "Minimum match score threshold");
        _maxMatches = AddIntProperty("MaxMatches", "Max Matches", 100, 1, 1000, "Maximum number of matches to return");
    }

    public override void Process()
    {
        try
        {
            var image = GetInputValue(_imageInput);
            var template = GetInputValue(_templateInput);

            if (image == null || image.Empty())
            {
                Error = "No input image";
                return;
            }
            if (template == null || template.Empty())
            {
                Error = "No template image";
                return;
            }

            var method = _method.GetValue<TemplateMatchModes>();
            var matchThreshold = _matchThreshold.GetValue<double>();
            var maxMatches = _maxMatches.GetValue<int>();

            var matchResult = new Mat();
            Cv2.MatchTemplate(image, template, matchResult, method);

            // Find all matches above threshold using NMS approach
            var matches = new List<Point>();
            int tw = template.Width;
            int th = template.Height;

            for (int i = 0; i < maxMatches; i++)
            {
                Cv2.MinMaxLoc(matchResult, out double minVal, out double maxVal, out Point minLoc, out Point maxLoc);

                // For methods where minimum means best match
                bool minIsBest = method == TemplateMatchModes.SqDiff || method == TemplateMatchModes.SqDiffNormed;
                double bestVal = minIsBest ? minVal : maxVal;
                Point bestLoc = minIsBest ? minLoc : maxLoc;

                // Check threshold
                if (minIsBest)
                {
                    if (bestVal > (1.0 - matchThreshold))
                        break;
                }
                else
                {
                    if (bestVal < matchThreshold)
                        break;
                }

                matches.Add(bestLoc);

                // Suppress the found region by flooding it with a value that won't be picked again
                int x1 = Math.Max(0, bestLoc.X - tw / 2);
                int y1 = Math.Max(0, bestLoc.Y - th / 2);
                int x2 = Math.Min(matchResult.Cols, bestLoc.X + tw / 2 + 1);
                int y2 = Math.Min(matchResult.Rows, bestLoc.Y + th / 2 + 1);
                var roi = new Rect(x1, y1, x2 - x1, y2 - y1);
                matchResult[roi].SetTo(minIsBest ? new Scalar(1.0) : new Scalar(0.0));
            }

            matchResult.Dispose();

            // Draw match rectangles on result
            var result = image.Clone();
            if (result.Channels() == 1)
                Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

            foreach (var match in matches)
            {
                var rect = new Rect(match.X, match.Y, tw, th);
                Cv2.Rectangle(result, rect, new Scalar(0, 255, 0), 2);
            }

            var matchArray = matches.ToArray();

            SetOutputValue(_resultOutput, result);
            SetOutputValue(_matchesOutput, matchArray);
            SetOutputValue(_countOutput, matchArray.Length);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Template Match Multi error: {ex.Message}";
        }
    }
}
