using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Segmentation;

[NodeInfo("GrabCut", NodeCategories.Segmentation, Description = "GrabCut foreground segmentation")]
public class GrabCutNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private OutputPort<Mat> _maskOutput = null!;
    private NodeProperty _rectX = null!;
    private NodeProperty _rectY = null!;
    private NodeProperty _rectW = null!;
    private NodeProperty _rectH = null!;
    private NodeProperty _iterations = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _resultOutput = AddOutput<Mat>("Result");
        _maskOutput = AddOutput<Mat>("Mask");
        _rectX = AddIntProperty("RectX", "Rect X", 10, 0, 10000, "ROI rectangle X position");
        _rectY = AddIntProperty("RectY", "Rect Y", 10, 0, 10000, "ROI rectangle Y position");
        _rectW = AddIntProperty("RectW", "Rect Width", 200, 1, 10000, "ROI rectangle width");
        _rectH = AddIntProperty("RectH", "Rect Height", 200, 1, 10000, "ROI rectangle height");
        _iterations = AddIntProperty("Iterations", "Iterations", 5, 1, 50, "Number of GrabCut iterations");
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

            if (image.Channels() != 3)
            {
                Error = "GrabCut requires a 3-channel (BGR) image";
                return;
            }

            var rect = new Rect(
                _rectX.GetValue<int>(),
                _rectY.GetValue<int>(),
                _rectW.GetValue<int>(),
                _rectH.GetValue<int>()
            );
            var iterations = _iterations.GetValue<int>();

            // Clamp rect to image bounds
            rect.X = Math.Max(0, Math.Min(rect.X, image.Width - 2));
            rect.Y = Math.Max(0, Math.Min(rect.Y, image.Height - 2));
            rect.Width = Math.Min(rect.Width, image.Width - rect.X);
            rect.Height = Math.Min(rect.Height, image.Height - rect.Y);

            var mask = new Mat(image.Size(), MatType.CV_8UC1, Scalar.All(0));
            var bgModel = new Mat();
            var fgModel = new Mat();

            Cv2.GrabCut(image, mask, rect, bgModel, fgModel, iterations, GrabCutModes.InitWithRect);

            bgModel.Dispose();
            fgModel.Dispose();

            // Create binary mask (foreground = 255)
            // GC_PR_FGD = 3, GC_FGD = 1
            var fgMask = new Mat();
            Cv2.Compare(mask, new Scalar(3), fgMask, CmpType.EQ);

            var definiteFg = new Mat();
            Cv2.Compare(mask, new Scalar(1), definiteFg, CmpType.EQ);
            Cv2.BitwiseOr(fgMask, definiteFg, fgMask);

            mask.Dispose();
            definiteFg.Dispose();

            // Apply mask to image to get foreground result
            var result = new Mat(image.Size(), image.Type(), Scalar.All(0));
            image.CopyTo(result, fgMask);

            SetOutputValue(_resultOutput, result);
            SetOutputValue(_maskOutput, fgMask);
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"GrabCut error: {ex.Message}";
        }
    }
}
