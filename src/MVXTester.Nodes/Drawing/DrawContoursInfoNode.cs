using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Drawing;

[NodeInfo("Draw Contours Info", NodeCategories.Drawing, Description = "Draw contours with labels showing center positions and area")]
public class DrawContoursInfoNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private InputPort<Point[][]> _contoursInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _showCenter = null!;
    private NodeProperty _showArea = null!;
    private NodeProperty _showIndex = null!;
    private NodeProperty _minArea = null!;
    private NodeProperty _fontScale = null!;
    private NodeProperty _thickness = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _contoursInput = AddInput<Point[][]>("Contours");
        _resultOutput = AddOutput<Mat>("Result");
        _showCenter = AddBoolProperty("ShowCenter", "Show Center", true, "Draw center dot on each contour");
        _showArea = AddBoolProperty("ShowArea", "Show Area", true, "Show area text for each contour");
        _showIndex = AddBoolProperty("ShowIndex", "Show Index", true, "Show contour index number");
        _minArea = AddDoubleProperty("MinArea", "Min Area", 0, 0, 1000000, "Minimum contour area to display");
        _fontScale = AddDoubleProperty("FontScale", "Font Scale", 0.4, 0.1, 5.0, "Font scale for labels");
        _thickness = AddIntProperty("Thickness", "Thickness", 1, 1, 10, "Line thickness");
    }

    public override void Process()
    {
        try
        {
            var image = GetInputValue(_imageInput);
            var contours = GetInputValue(_contoursInput);

            if (image == null || image.Empty())
            {
                Error = "No input image";
                return;
            }

            if (contours == null || contours.Length == 0)
            {
                SetOutputValue(_resultOutput, image.Clone());
                SetPreview(image);
                Error = null;
                return;
            }

            var showCenter = _showCenter.GetValue<bool>();
            var showArea = _showArea.GetValue<bool>();
            var showIndex = _showIndex.GetValue<bool>();
            var minArea = _minArea.GetValue<double>();
            var fontScale = _fontScale.GetValue<double>();
            var thickness = _thickness.GetValue<int>();

            var result = image.Clone();
            if (result.Channels() == 1)
                Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

            // Predefined colors for different contours
            var colors = new[]
            {
                new Scalar(0, 255, 0),
                new Scalar(255, 0, 0),
                new Scalar(0, 0, 255),
                new Scalar(255, 255, 0),
                new Scalar(0, 255, 255),
                new Scalar(255, 0, 255),
                new Scalar(128, 255, 0),
                new Scalar(0, 128, 255)
            };

            for (int i = 0; i < contours.Length; i++)
            {
                var area = Cv2.ContourArea(contours[i]);
                if (area < minArea)
                    continue;

                var color = colors[i % colors.Length];
                Cv2.DrawContours(result, contours, i, color, thickness);

                var moments = Cv2.Moments(contours[i]);
                if (moments.M00 > 0)
                {
                    int cx = (int)(moments.M10 / moments.M00);
                    int cy = (int)(moments.M01 / moments.M00);

                    if (showCenter)
                    {
                        Cv2.Circle(result, new Point(cx, cy), 3, color, -1);
                    }

                    int textY = cy - 5;

                    if (showIndex)
                    {
                        Cv2.PutText(result, $"#{i}", new Point(cx + 5, textY),
                            HersheyFonts.HersheySimplex, fontScale, color, thickness);
                        textY -= (int)(15 * fontScale / 0.4);
                    }

                    if (showArea)
                    {
                        Cv2.PutText(result, $"A:{area:F0}", new Point(cx + 5, textY),
                            HersheyFonts.HersheySimplex, fontScale, color, thickness);
                        textY -= (int)(15 * fontScale / 0.4);
                    }

                    if (showCenter)
                    {
                        Cv2.PutText(result, $"({cx},{cy})", new Point(cx + 5, textY),
                            HersheyFonts.HersheySimplex, fontScale, color, thickness);
                    }
                }
            }

            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Draw Contours Info error: {ex.Message}";
        }
    }
}
