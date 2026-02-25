using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Detection;

public enum ConnectivityType
{
    Four = 4,
    Eight = 8
}

[NodeInfo("Connected Components", NodeCategories.Detection, Description = "Connected component labeling with statistics")]
public class ConnectedComponentsNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<Mat> _labelsOutput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private OutputPort<int> _countOutput = null!;
    private NodeProperty _connectivity = null!;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _labelsOutput = AddOutput<Mat>("Labels");
        _resultOutput = AddOutput<Mat>("Result");
        _countOutput = AddOutput<int>("Count");
        _connectivity = AddEnumProperty("Connectivity", "Connectivity", ConnectivityType.Eight, "Pixel connectivity (4 or 8)");
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

            var connectivity = (int)_connectivity.GetValue<ConnectivityType>();

            // Ensure single channel binary input
            Mat binary = image;
            bool needDispose = false;
            if (image.Channels() > 1)
            {
                binary = new Mat();
                Cv2.CvtColor(image, binary, ColorConversionCodes.BGR2GRAY);
                needDispose = true;
            }

            var labels = new Mat();
            var stats = new Mat();
            var centroids = new Mat();
            var pixelConn = connectivity == 4 ? PixelConnectivity.Connectivity4 : PixelConnectivity.Connectivity8;
            int count = Cv2.ConnectedComponentsWithStats(binary, labels, stats, centroids, pixelConn);
            if (needDispose) binary.Dispose();

            // Create colored result - assign a random color to each component
            var result = new Mat(image.Rows, image.Cols, MatType.CV_8UC3, Scalar.All(0));
            var random = new Random(42);
            var colors = new Scalar[count];
            colors[0] = new Scalar(0, 0, 0); // background is black
            for (int i = 1; i < count; i++)
            {
                colors[i] = new Scalar(random.Next(50, 256), random.Next(50, 256), random.Next(50, 256));
            }

            for (int y = 0; y < labels.Rows; y++)
            {
                for (int x = 0; x < labels.Cols; x++)
                {
                    int label = labels.At<int>(y, x);
                    if (label > 0)
                    {
                        result.Set(y, x, new Vec3b(
                            (byte)colors[label].Val0,
                            (byte)colors[label].Val1,
                            (byte)colors[label].Val2));
                    }
                }
            }

            stats.Dispose();
            centroids.Dispose();

            SetOutputValue(_labelsOutput, labels);
            SetOutputValue(_resultOutput, result);
            SetOutputValue(_countOutput, count - 1); // Exclude background
            SetPreview(result);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Connected Components error: {ex.Message}";
        }
    }
}
