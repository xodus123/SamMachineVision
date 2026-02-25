using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Arithmetic;

[NodeInfo("Image Blend", NodeCategories.Arithmetic, Description = "Alpha blend two images together")]
public class ImageBlendNode : BaseNode
{
    private InputPort<Mat> _image1Input = null!;
    private InputPort<Mat> _image2Input = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private NodeProperty _alpha = null!;

    protected override void Setup()
    {
        _image1Input = AddInput<Mat>("Image1");
        _image2Input = AddInput<Mat>("Image2");
        _resultOutput = AddOutput<Mat>("Result");
        _alpha = AddDoubleProperty("Alpha", "Alpha", 0.5, 0.0, 1.0, "Blend weight for Image1 (Image2 weight = 1 - Alpha)");
    }

    public override void Process()
    {
        try
        {
            var image1 = GetInputValue(_image1Input);
            var image2 = GetInputValue(_image2Input);

            if (image1 == null || image1.Empty() || image2 == null || image2.Empty())
            {
                Error = "Both input images required";
                return;
            }

            var alpha = _alpha.GetValue<double>();

            // Resize Image2 to match Image1 if sizes differ
            Mat img2 = image2;
            bool needDispose = false;
            if (image1.Size() != image2.Size() || image1.Type() != image2.Type())
            {
                img2 = new Mat();
                Cv2.Resize(image2, img2, image1.Size());
                if (img2.Type() != image1.Type())
                {
                    img2.ConvertTo(img2, image1.Type());
                }
                needDispose = true;
            }

            var result = new Mat();
            Cv2.AddWeighted(image1, alpha, img2, 1.0 - alpha, 0, result);

            if (needDispose) img2.Dispose();

            SetOutputValue(_resultOutput, result);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Image Blend error: {ex.Message}";
        }
    }
}
