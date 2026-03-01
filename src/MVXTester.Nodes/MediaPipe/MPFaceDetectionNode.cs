using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace MVXTester.Nodes.MediaPipe;

/// <summary>
/// Detects faces using MediaPipe BlazeFace short-range model.
/// Outputs bounding boxes, scores, and annotated result image.
/// </summary>
[NodeInfo("MP Face Detection", NodeCategories.MediaPipe,
    Description = "Detect faces using MediaPipe BlazeFace model")]
public class MPFaceDetectionNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private OutputPort<Rect[]> _facesOutput = null!;
    private OutputPort<double[]> _scoresOutput = null!;
    private OutputPort<int> _countOutput = null!;

    private NodeProperty _confidence = null!;
    private NodeProperty _maxDetections = null!;

    private const string ModelFile = "face_detection_short_range.onnx";
    private const int InputSize = 128;

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");

        _resultOutput = AddOutput<Mat>("Result");
        _facesOutput = AddOutput<Rect[]>("Faces");
        _scoresOutput = AddOutput<double[]>("Scores");
        _countOutput = AddOutput<int>("Count");

        _confidence = AddDoubleProperty("Confidence", "Confidence", 0.5, 0.0, 1.0, "Minimum detection confidence");
        _maxDetections = AddIntProperty("MaxDetections", "Max Detections", 10, 1, 100, "Maximum number of detections");
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

            var session = MediaPipeHelper.GetSession(ModelFile);
            var threshold = (float)_confidence.GetValue<double>();
            var maxDet = _maxDetections.GetValue<int>();

            // Preprocess: resize to 128x128, RGB, [0,1]
            var inputData = MediaPipeHelper.PreprocessImageNHWC(image, InputSize, InputSize);
            var inputName = session.InputNames[0];
            var inputs = new List<NamedOnnxValue>
            {
                MediaPipeHelper.CreateTensor(inputName, inputData, new[] { 1, InputSize, InputSize, 3 })
            };

            // Run inference
            using var results = session.Run(inputs);
            var outputs = results.ToList();

            // Parse outputs: regressors [1, 896, 16] and classificators [1, 896, 1]
            var regressors = outputs[0].AsTensor<float>();
            var classificators = outputs[1].AsTensor<float>();

            var anchors = MediaPipeHelper.GetBlazeFaceAnchors();
            int numAnchors = anchors.GetLength(0);

            float scaleX = image.Width;
            float scaleY = image.Height;

            var detections = new List<(Rect Box, float Score)>();

            for (int i = 0; i < numAnchors; i++)
            {
                float score = MediaPipeHelper.Sigmoid(classificators[0, i, 0]);
                if (score < threshold) continue;

                float anchorCx = anchors[i, 0];
                float anchorCy = anchors[i, 1];

                // Decode box: center offset + size
                float cx = anchorCx + regressors[0, i, 0] / InputSize;
                float cy = anchorCy + regressors[0, i, 1] / InputSize;
                float w = regressors[0, i, 2] / InputSize;
                float h = regressors[0, i, 3] / InputSize;

                // Convert to pixel coordinates
                int x1 = (int)((cx - w / 2) * scaleX);
                int y1 = (int)((cy - h / 2) * scaleY);
                int bw = (int)(w * scaleX);
                int bh = (int)(h * scaleY);

                // Clamp to image bounds
                x1 = Math.Max(0, Math.Min(x1, image.Width - 1));
                y1 = Math.Max(0, Math.Min(y1, image.Height - 1));
                bw = Math.Max(1, Math.Min(bw, image.Width - x1));
                bh = Math.Max(1, Math.Min(bh, image.Height - y1));

                detections.Add((new Rect(x1, y1, bw, bh), score));
            }

            // Apply NMS
            var nmsResults = MediaPipeHelper.NonMaxSuppression(detections, 0.3f);
            if (nmsResults.Count > maxDet)
                nmsResults = nmsResults.Take(maxDet).ToList();

            // Build output arrays
            var faceList = new List<Rect>();
            var scoreList = new List<double>();

            var result = image.Clone();
            if (result.Channels() == 1)
                Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

            for (int i = 0; i < nmsResults.Count; i++)
            {
                var (box, s, _) = nmsResults[i];
                faceList.Add(box);
                scoreList.Add(s);

                MediaPipeHelper.DrawDetectionBox(result, box, $"Face", s,
                    new Scalar(0, 255, 0), 2);
            }

            SetOutputValue(_resultOutput, result);
            SetOutputValue(_facesOutput, faceList.ToArray());
            SetOutputValue(_scoresOutput, scoreList.ToArray());
            SetOutputValue(_countOutput, faceList.Count);
            SetPreview(result);
            Error = null;
        }
        catch (FileNotFoundException ex)
        {
            Error = ex.Message;
        }
        catch (Exception ex)
        {
            Error = $"MP Face Detection error: {ex.Message}";
        }
    }
}
