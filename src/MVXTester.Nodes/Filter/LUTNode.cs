using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Filter;

[NodeInfo("LUT", NodeCategories.Filter, Description = "Apply lookup table transformation with gamma, brightness, and contrast")]
public class LUTNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _gamma = null!;
    private NodeProperty _brightness = null!;
    private NodeProperty _contrast = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _resultOutput = AddOutput<Mat>("Result");
        _gamma = AddDoubleProperty("Gamma", "Gamma", 1.0, 0.1, 10.0, "Gamma correction value");
        _brightness = AddIntProperty("Brightness", "Brightness", 0, -255, 255, "Brightness adjustment");
        _contrast = AddDoubleProperty("Contrast", "Contrast", 1.0, 0.1, 5.0, "Contrast multiplier");
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

            var gamma = _gamma.GetValue<double>();
            var brightness = _brightness.GetValue<int>();
            var contrast = _contrast.GetValue<double>();

            // Build LUT
            var lutData = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                // Apply gamma
                double value = Math.Pow(i / 255.0, 1.0 / gamma) * 255.0;
                // Apply contrast
                value = ((value - 128.0) * contrast) + 128.0;
                // Apply brightness
                value += brightness;
                // Clamp to [0, 255]
                lutData[i] = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(value)));
            }

            using var lut = Mat.FromArray(lutData);
            var result = new Mat();
            Cv2.LUT(image, lut, result);

            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"LUT error: {ex.Message}";
        }
    }
}
