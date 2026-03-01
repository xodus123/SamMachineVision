using System.Text;
using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace MVXTester.Nodes.MediaPipe;

/// <summary>
/// Detects objects using SSD MobileNet V2 model trained on COCO dataset (80 classes).
/// Auto-detects input format (NCHW/NHWC) and output format from model metadata.
/// Debug mode shows actual model output tensor info for troubleshooting.
/// </summary>
[NodeInfo("MP Object Detection", NodeCategories.MediaPipe,
    Description = "Detect objects using SSD MobileNet V2 (COCO 80 classes)")]
public class MPObjectDetectionNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<Mat> _resultOutput = null!;
    private OutputPort<Rect[]> _boxesOutput = null!;
    private OutputPort<string[]> _labelsOutput = null!;
    private OutputPort<double[]> _scoresOutput = null!;
    private OutputPort<int> _countOutput = null!;

    private NodeProperty _confidence = null!;
    private NodeProperty _maxDetections = null!;
    private NodeProperty _inputRange = null!;
    private NodeProperty _debug = null!;

    private const string ModelFile = "ssd_mobilenet_v2.onnx";
    private const int InputSize = 300;

    private static readonly Scalar[] ClassColors =
    {
        new(0, 255, 0), new(255, 0, 0), new(0, 0, 255), new(255, 255, 0),
        new(255, 0, 255), new(0, 255, 255), new(128, 255, 0), new(255, 128, 0),
        new(0, 128, 255), new(128, 0, 255), new(255, 0, 128), new(0, 255, 128)
    };

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");

        _resultOutput = AddOutput<Mat>("Result");
        _boxesOutput = AddOutput<Rect[]>("BoundingBoxes");
        _labelsOutput = AddOutput<string[]>("Labels");
        _scoresOutput = AddOutput<double[]>("Scores");
        _countOutput = AddOutput<int>("Count");

        _confidence = AddDoubleProperty("Confidence", "Confidence", 0.5, 0.0, 1.0, "Minimum detection confidence");
        _maxDetections = AddIntProperty("MaxDetections", "Max Detections", 20, 1, 100, "Maximum number of detections");
        _inputRange = AddEnumProperty("InputRange", "Input Range", InputNormMode.Uint8, "Input normalization: Uint8=[0,255], Float01=[0,1], Signed=[-1,1]");
        _debug = AddBoolProperty("Debug", "Debug Info", false, "Show model output tensor info in text preview");
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
            var showDebug = _debug.GetValue<bool>();

            // Auto-detect input layout (NCHW vs NHWC)
            var inputName = session.InputNames[0];
            var inputShape = session.InputMetadata[inputName].Dimensions;
            bool isNCHW = inputShape.Length >= 4 && inputShape[1] == 3;

            // Normalization: Uint8=[0,255], Float01=[0,1], Signed=[-1,1]
            var normMode = _inputRange.GetValue<InputNormMode>();
            float scale = normMode switch
            {
                InputNormMode.Float01 => 1.0f / 255.0f,
                InputNormMode.Signed => 2.0f / 255.0f,
                _ => 1.0f // Uint8: keep [0,255]
            };
            float offset = normMode switch
            {
                InputNormMode.Signed => -1.0f,
                _ => 0f
            };

            float[] inputData;
            int[] tensorShape;
            if (isNCHW)
            {
                inputData = MediaPipeHelper.PreprocessImageNCHW(image, InputSize, InputSize, scale, offset);
                tensorShape = new[] { 1, 3, InputSize, InputSize };
            }
            else
            {
                inputData = MediaPipeHelper.PreprocessImageNHWC(image, InputSize, InputSize, scale, offset);
                tensorShape = new[] { 1, InputSize, InputSize, 3 };
            }

            var inputs = new List<NamedOnnxValue>
            {
                MediaPipeHelper.CreateTensor(inputName, inputData, tensorShape)
            };

            using var results = session.Run(inputs);
            var outputs = results.ToList();

            var result = image.Clone();
            if (result.Channels() == 1)
                Cv2.CvtColor(result, result, ColorConversionCodes.GRAY2BGR);

            var boxList = new List<Rect>();
            var labelList = new List<string>();
            var scoreList = new List<double>();

            // Debug: collect output tensor info
            var debugSb = showDebug ? new StringBuilder() : null;

            // Collect all output tensors with metadata
            var tensorInfos = new List<(string Name, float[] Flat, int[] Dims, int Index)>();
            for (int i = 0; i < outputs.Count; i++)
            {
                var tensor = outputs[i].AsTensor<float>();
                var flat = MediaPipeHelper.GetFlatArray(tensor);
                var dims = tensor.Dimensions.ToArray();
                tensorInfos.Add((outputs[i].Name, flat, dims, i));

                if (debugSb != null)
                {
                    debugSb.AppendLine($"[{i}] \"{outputs[i].Name}\"");
                    debugSb.AppendLine($"    shape=[{string.Join(",", dims)}] len={flat.Length}");
                    var sample = flat.Take(8).Select(v => v.ToString("F3"));
                    debugSb.AppendLine($"    vals=[{string.Join(", ", sample)}]");
                    if (flat.Length > 0)
                    {
                        float min = flat.Min(), max = flat.Max();
                        debugSb.AppendLine($"    range=[{min:F3} ~ {max:F3}]");
                    }
                }
            }

            // Identify outputs using robust matching
            float[]? boxFlat = null, classFlat = null, scoreFlat = null, numDetFlat = null;
            int[]? boxDims = null;
            string matchMethod = "none";

            // Strategy 1: Name-based matching with disambiguation
            foreach (var info in tensorInfos)
            {
                var name = info.Name.ToLowerInvariant();

                // "num_detections" or similar - must check first (shortest tensor, length=1)
                if (name.Contains("num") && info.Flat.Length <= 10)
                {
                    numDetFlat = info.Flat;
                }
                // "detection_boxes" - contains "box" but not "score" or "class"
                else if (name.Contains("box") && !name.Contains("score") && !name.Contains("class"))
                {
                    boxFlat = info.Flat;
                    boxDims = info.Dims;
                }
                // "detection_scores" - contains "score" but not "class"
                // Also match "multiclass_scores" here as scores
                else if (name.Contains("score") && !name.Contains("class"))
                {
                    scoreFlat = info.Flat;
                }
                // "detection_classes" - contains "class" but not "score"
                else if ((name.Contains("class") || name.Contains("label")) && !name.Contains("score"))
                {
                    classFlat = info.Flat;
                }
            }

            // Handle ambiguous names like "detection_multiclass_scores" (contains both "class" and "score")
            // These are score tensors, not class tensors
            if (scoreFlat == null)
            {
                foreach (var info in tensorInfos)
                {
                    var name = info.Name.ToLowerInvariant();
                    if (name.Contains("score") && info.Flat != boxFlat && info.Flat != numDetFlat)
                    {
                        scoreFlat = info.Flat;
                        break;
                    }
                }
            }

            if (boxFlat != null)
                matchMethod = "name";

            // Strategy 2: Shape-based identification (if name matching incomplete)
            if (boxFlat == null && outputs.Count >= 4)
            {
                matchMethod = "shape";

                // Identify by shape characteristics:
                // boxes: last dim = 4 (e.g., [1,N,4])
                // classes: 1D-ish, integer values (e.g., [1,N])
                // scores: 1D-ish, values in [0,1] (e.g., [1,N])
                // num_detections: scalar or [1]

                foreach (var info in tensorInfos)
                {
                    var dims = info.Dims;
                    var flat = info.Flat;

                    // num_detections: very short tensor (length 1-2)
                    if (flat.Length <= 2 && numDetFlat == null)
                    {
                        numDetFlat = flat;
                        continue;
                    }

                    // boxes: last dimension is 4
                    if (dims.Length >= 2 && dims[^1] == 4 && boxFlat == null)
                    {
                        boxFlat = flat;
                        boxDims = dims;
                        continue;
                    }
                }

                // Remaining two tensors: distinguish classes vs scores by value range
                var remaining = tensorInfos
                    .Where(t => t.Flat != boxFlat && t.Flat != numDetFlat)
                    .ToList();

                if (remaining.Count >= 2)
                {
                    // Scores: all values in [0, 1]; Classes: integer-like values (often > 1)
                    var t0 = remaining[0];
                    var t1 = remaining[1];

                    bool t0IsScore = IsScoreLike(t0.Flat);
                    bool t1IsScore = IsScoreLike(t1.Flat);

                    if (t0IsScore && !t1IsScore)
                    {
                        scoreFlat = t0.Flat;
                        classFlat = t1.Flat;
                    }
                    else if (t1IsScore && !t0IsScore)
                    {
                        scoreFlat = t1.Flat;
                        classFlat = t0.Flat;
                    }
                    else
                    {
                        // Both look like scores or both like classes - use TF standard order
                        // TF order: boxes[0], classes[1], scores[2], num[3]
                        // Sort by original index and assign classes first, scores second
                        var sorted = remaining.OrderBy(r => r.Index).ToList();
                        classFlat = sorted[0].Flat;
                        scoreFlat = sorted[1].Flat;
                    }
                }
                else if (remaining.Count == 1)
                {
                    // Only one remaining - assume it's scores
                    scoreFlat = remaining[0].Flat;
                }
            }

            // Strategy 3: For 4-output models, try standard TF output order as last resort
            if (boxFlat == null && outputs.Count >= 4)
            {
                matchMethod = "order-tf";
                // TF Object Detection API order: boxes, classes, scores, num_detections
                boxFlat = tensorInfos[0].Flat;
                boxDims = tensorInfos[0].Dims;
                classFlat = tensorInfos[1].Flat;
                scoreFlat = tensorInfos[2].Flat;
                numDetFlat = tensorInfos[3].Flat;
            }

            if (debugSb != null)
            {
                debugSb.AppendLine($"\nMatch: {matchMethod}");
                debugSb.AppendLine($"box={boxFlat?.Length}, class={classFlat?.Length}, score={scoreFlat?.Length}, num={numDetFlat?.Length}");
            }

            if (boxFlat != null && classFlat != null && scoreFlat != null)
            {
                int numDet = numDetFlat != null && numDetFlat.Length > 0
                    ? Math.Min((int)numDetFlat[0], maxDet)
                    : Math.Min(scoreFlat.Length, maxDet);

                // Determine box values per detection (typically 4)
                int boxStride = numDet > 0 ? boxFlat.Length / numDet : 4;
                if (boxStride < 4) boxStride = 4;

                // Detect if boxes are normalized [0,1] or pixel [0, inputSize/imgSize]
                bool boxNormalized = true;
                for (int i = 0; i < Math.Min(numDet * boxStride, boxFlat.Length); i++)
                {
                    if (Math.Abs(boxFlat[i]) > 2.0f) { boxNormalized = false; break; }
                }

                // Detect box order: TF SSD outputs [y_min, x_min, y_max, x_max] normalized
                // Heuristic: if boxes are normalized and box shape has last dim 4, assume TF format
                // This is the standard for TF Object Detection API models
                bool isTfBoxOrder = boxNormalized && boxDims != null &&
                    boxDims.Length >= 2 && boxDims[^1] == 4;

                if (debugSb != null)
                {
                    debugSb.AppendLine($"numDet={numDet} stride={boxStride} norm={boxNormalized} tfOrder={isTfBoxOrder}");
                }

                for (int i = 0; i < numDet; i++)
                {
                    if (i >= scoreFlat.Length) break;
                    float score = scoreFlat[i];
                    if (score < threshold) continue;

                    int bIdx = i * boxStride;
                    if (bIdx + 3 >= boxFlat.Length) break;

                    float v0 = boxFlat[bIdx + 0];
                    float v1 = boxFlat[bIdx + 1];
                    float v2 = boxFlat[bIdx + 2];
                    float v3 = boxFlat[bIdx + 3];

                    float xMin, yMin, xMax, yMax;
                    if (isTfBoxOrder)
                    {
                        // TF SSD: [y_min, x_min, y_max, x_max]
                        yMin = v0; xMin = v1; yMax = v2; xMax = v3;
                    }
                    else
                    {
                        // Standard: [x_min, y_min, x_max, y_max]
                        xMin = v0; yMin = v1; xMax = v2; yMax = v3;
                    }

                    int x1, y1, w, h;
                    if (boxNormalized)
                    {
                        x1 = (int)(xMin * image.Width);
                        y1 = (int)(yMin * image.Height);
                        w = (int)((xMax - xMin) * image.Width);
                        h = (int)((yMax - yMin) * image.Height);
                    }
                    else
                    {
                        x1 = (int)(xMin / InputSize * image.Width);
                        y1 = (int)(yMin / InputSize * image.Height);
                        w = (int)((xMax - xMin) / InputSize * image.Width);
                        h = (int)((yMax - yMin) / InputSize * image.Height);
                    }

                    x1 = Math.Max(0, Math.Min(x1, image.Width - 1));
                    y1 = Math.Max(0, Math.Min(y1, image.Height - 1));
                    w = Math.Max(1, Math.Min(w, image.Width - x1));
                    h = Math.Max(1, Math.Min(h, image.Height - y1));

                    int classId = i < classFlat.Length ? (int)classFlat[i] : 0;
                    string label = MediaPipeHelper.GetCocoLabel(classId);

                    boxList.Add(new Rect(x1, y1, w, h));
                    labelList.Add(label);
                    scoreList.Add(score);

                    if (debugSb != null && boxList.Count <= 5)
                    {
                        debugSb.AppendLine($"det[{i}]: cls={classId}({label}) s={score:F3} box=[{v0:F3},{v1:F3},{v2:F3},{v3:F3}]");
                    }
                }
            }
            else if (outputs.Count >= 1)
            {
                // Single-tensor output: [1, 1, N, 7] or [1, N, 7]
                ParseSingleOutput(outputs[0], image.Width, image.Height, threshold, maxDet,
                    boxList, labelList, scoreList, debugSb);
            }

            // Draw
            for (int i = 0; i < boxList.Count; i++)
            {
                var color = ClassColors[i % ClassColors.Length];
                MediaPipeHelper.DrawDetectionBox(result, boxList[i], labelList[i],
                    (float)scoreList[i], color, 2);
            }

            Cv2.PutText(result, $"Objects: {boxList.Count}", new Point(10, 25),
                HersheyFonts.HersheySimplex, 0.7, new Scalar(0, 255, 0), 2);

            SetOutputValue(_resultOutput, result);
            SetOutputValue(_boxesOutput, boxList.ToArray());
            SetOutputValue(_labelsOutput, labelList.ToArray());
            SetOutputValue(_scoresOutput, scoreList.ToArray());
            SetOutputValue(_countOutput, boxList.Count);
            SetPreview(result);

            if (debugSb != null)
                SetTextPreview(debugSb.ToString());
            else
                SetTextPreview(null);

            Error = null;
        }
        catch (FileNotFoundException ex)
        {
            Error = ex.Message;
        }
        catch (Exception ex)
        {
            Error = $"MP Object Detection error: {ex.Message}";
        }
    }

    /// <summary>
    /// Check if a tensor looks like confidence scores (all values in [0, 1] range).
    /// </summary>
    private static bool IsScoreLike(float[] flat)
    {
        int sampleCount = Math.Min(flat.Length, 100);
        for (int i = 0; i < sampleCount; i++)
        {
            if (flat[i] < -0.01f || flat[i] > 1.01f)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Parse single-tensor output [1, 1, N, 7] or [1, N, 7].
    /// Each row: [batch_id, class_id, score, x1, y1, x2, y2]
    /// </summary>
    private static void ParseSingleOutput(DisposableNamedOnnxValue output,
        int imgWidth, int imgHeight, float threshold, int maxDet,
        List<Rect> boxes, List<string> labels, List<double> scores,
        StringBuilder? debugSb)
    {
        var flat = MediaPipeHelper.GetFlatArray(output.AsTensor<float>());
        var dims = output.AsTensor<float>().Dimensions.ToArray();

        // Detect stride (values per detection)
        int stride = 7;
        int numDet;

        if (dims.Length == 4 && dims[3] == 7)
        {
            // [1, 1, N, 7]
            numDet = dims[2];
        }
        else if (dims.Length == 3 && dims[2] == 7)
        {
            // [1, N, 7]
            numDet = dims[1];
        }
        else if (dims.Length == 2 && dims[1] == 7)
        {
            // [N, 7]
            numDet = dims[0];
        }
        else
        {
            // Unknown format - try to infer
            numDet = flat.Length / stride;
        }

        if (debugSb != null)
            debugSb.AppendLine($"SingleOutput: dims=[{string.Join(",", dims)}] stride={stride} numDet={numDet}");

        for (int i = 0; i < numDet && boxes.Count < maxDet; i++)
        {
            int baseIdx = i * stride;
            if (baseIdx + 6 >= flat.Length) break;

            int classId = (int)flat[baseIdx + 1];
            float score = flat[baseIdx + 2];
            if (score < threshold) continue;

            float x1Norm = flat[baseIdx + 3];
            float y1Norm = flat[baseIdx + 4];
            float x2Norm = flat[baseIdx + 5];
            float y2Norm = flat[baseIdx + 6];

            // Auto-detect if normalized or pixel coords
            bool isNorm = x1Norm <= 1.1f && y1Norm <= 1.1f && x2Norm <= 1.1f && y2Norm <= 1.1f;

            int x1, y1, w, h;
            if (isNorm)
            {
                x1 = (int)(x1Norm * imgWidth);
                y1 = (int)(y1Norm * imgHeight);
                w = (int)((x2Norm - x1Norm) * imgWidth);
                h = (int)((y2Norm - y1Norm) * imgHeight);
            }
            else
            {
                x1 = (int)(x1Norm / InputSize * imgWidth);
                y1 = (int)(y1Norm / InputSize * imgHeight);
                w = (int)((x2Norm - x1Norm) / InputSize * imgWidth);
                h = (int)((y2Norm - y1Norm) / InputSize * imgHeight);
            }

            x1 = Math.Max(0, Math.Min(x1, imgWidth - 1));
            y1 = Math.Max(0, Math.Min(y1, imgHeight - 1));
            w = Math.Max(1, Math.Min(w, imgWidth - x1));
            h = Math.Max(1, Math.Min(h, imgHeight - y1));

            string label = MediaPipeHelper.GetCocoLabel(classId);
            boxes.Add(new Rect(x1, y1, w, h));
            labels.Add(label);
            scores.Add(score);

            if (debugSb != null && boxes.Count <= 5)
            {
                debugSb.AppendLine($"det[{i}]: cls={classId}({label}) s={score:F3} box=[{x1Norm:F3},{y1Norm:F3},{x2Norm:F3},{y2Norm:F3}]");
            }
        }
    }
}

/// <summary>
/// Input normalization mode for object detection models.
/// </summary>
public enum InputNormMode
{
    /// <summary>[0, 255] - TF SSD models with built-in preprocessing</summary>
    Uint8,
    /// <summary>[0, 1] - Models expecting normalized float</summary>
    Float01,
    /// <summary>[-1, 1] - MobileNet-style normalization</summary>
    Signed
}
