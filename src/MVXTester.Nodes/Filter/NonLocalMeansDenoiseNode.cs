using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Filter;

[NodeInfo("Non-Local Means Denoise", NodeCategories.Filter, Description = "Advanced denoising using Non-Local Means algorithm")]
public class NonLocalMeansDenoiseNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _h = null!;
    private NodeProperty _hColor = null!;
    private NodeProperty _templateWindowSize = null!;
    private NodeProperty _searchWindowSize = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _resultOutput = AddOutput<Mat>("Result");
        _h = AddFloatProperty("H", "Filter Strength (H)", 10f, 1f, 100f, "Filter strength for luminance component");
        _hColor = AddFloatProperty("HColor", "Color Filter Strength", 10f, 1f, 100f, "Filter strength for color components (color images only)");
        _templateWindowSize = AddIntProperty("TemplateWindowSize", "Template Window Size", 7, 3, 21, "Size of template patch (odd number)");
        _searchWindowSize = AddIntProperty("SearchWindowSize", "Search Window Size", 21, 3, 51, "Size of area where search is performed (odd number)");
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

            var h = _h.GetValue<float>();
            var hColor = _hColor.GetValue<float>();
            var templateWindowSize = _templateWindowSize.GetValue<int>();
            var searchWindowSize = _searchWindowSize.GetValue<int>();

            // Ensure odd window sizes
            if (templateWindowSize % 2 == 0) templateWindowSize++;
            if (searchWindowSize % 2 == 0) searchWindowSize++;

            var result = new Mat();
            if (image.Channels() >= 3)
            {
                Cv2.FastNlMeansDenoisingColored(image, result, h, hColor, templateWindowSize, searchWindowSize);
            }
            else
            {
                Cv2.FastNlMeansDenoising(image, result, h, templateWindowSize, searchWindowSize);
            }

            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Non-Local Means Denoise error: {ex.Message}";
        }
    }
}
