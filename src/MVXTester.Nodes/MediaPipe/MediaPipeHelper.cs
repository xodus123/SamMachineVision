using System.Collections.Concurrent;
using OpenCvSharp;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace MVXTester.Nodes.MediaPipe;

/// <summary>
/// Shared helper for MediaPipe ONNX model inference.
/// Provides model path resolution, session caching, image preprocessing,
/// anchor generation, post-processing, and visualization utilities.
/// </summary>
public static class MediaPipeHelper
{
    #region Session Management

    private static readonly ConcurrentDictionary<string, InferenceSession> _sessionCache = new();

    /// <summary>
    /// Resolve model file path by searching known directories.
    /// </summary>
    public static string? ResolveModelPath(string modelFileName)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        if (Path.IsPathRooted(modelFileName) && File.Exists(modelFileName))
            return modelFileName;

        var path = Path.Combine(baseDir, "Models", "MediaPipe", modelFileName);
        if (File.Exists(path)) return path;

        path = Path.Combine(baseDir, modelFileName);
        if (File.Exists(path)) return path;

        path = Path.Combine(baseDir, "data", modelFileName);
        if (File.Exists(path)) return path;

        return null;
    }

    /// <summary>
    /// Get or create a cached ONNX InferenceSession for the given model file.
    /// </summary>
    public static InferenceSession GetSession(string modelFileName)
    {
        var modelPath = ResolveModelPath(modelFileName)
            ?? throw new FileNotFoundException(
                $"MediaPipe model not found: {modelFileName}. " +
                $"Place the model file in Models/MediaPipe/ folder.");

        return _sessionCache.GetOrAdd(modelPath, path =>
        {
            var opts = new SessionOptions();
            opts.InterOpNumThreads = 1;
            opts.IntraOpNumThreads = Math.Max(1, Environment.ProcessorCount / 2);
            opts.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            return new InferenceSession(path, opts);
        });
    }

    /// <summary>
    /// Dispose all cached sessions.
    /// </summary>
    public static void DisposeAll()
    {
        foreach (var session in _sessionCache.Values)
            session.Dispose();
        _sessionCache.Clear();
    }

    #endregion

    #region Image Preprocessing

    /// <summary>
    /// Preprocess Mat to NHWC float tensor [1, H, W, 3], RGB, normalized to [0,1].
    /// </summary>
    public static float[] PreprocessImageNHWC(Mat input, int width, int height)
    {
        using var resized = new Mat();
        Cv2.Resize(input, resized, new Size(width, height));

        using var rgb = new Mat();
        if (resized.Channels() == 1)
            Cv2.CvtColor(resized, rgb, ColorConversionCodes.GRAY2RGB);
        else
            Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);

        var data = new float[1 * height * width * 3];
        var indexer = rgb.GetGenericIndexer<Vec3b>();
        int idx = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = indexer[y, x];
                data[idx++] = pixel.Item0 / 255.0f; // R
                data[idx++] = pixel.Item1 / 255.0f; // G
                data[idx++] = pixel.Item2 / 255.0f; // B
            }
        }
        return data;
    }

    /// <summary>
    /// Preprocess Mat to NCHW float tensor [1, 3, H, W], RGB, normalized to [0,1].
    /// </summary>
    public static float[] PreprocessImageNCHW(Mat input, int width, int height)
    {
        using var resized = new Mat();
        Cv2.Resize(input, resized, new Size(width, height));

        using var rgb = new Mat();
        if (resized.Channels() == 1)
            Cv2.CvtColor(resized, rgb, ColorConversionCodes.GRAY2RGB);
        else
            Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);

        var data = new float[1 * 3 * height * width];
        var indexer = rgb.GetGenericIndexer<Vec3b>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = indexer[y, x];
                data[0 * height * width + y * width + x] = pixel.Item0 / 255.0f; // R
                data[1 * height * width + y * width + x] = pixel.Item1 / 255.0f; // G
                data[2 * height * width + y * width + x] = pixel.Item2 / 255.0f; // B
            }
        }
        return data;
    }

    /// <summary>
    /// Create a named ONNX tensor value from float array with given dimensions.
    /// </summary>
    public static NamedOnnxValue CreateTensor(string name, float[] data, int[] dims)
    {
        var tensor = new DenseTensor<float>(data, dims);
        return NamedOnnxValue.CreateFromTensor(name, tensor);
    }

    #endregion

    #region Post-Processing Utilities

    public static float Sigmoid(float x) => 1.0f / (1.0f + MathF.Exp(-x));

    /// <summary>
    /// Get flat float array from tensor regardless of shape (2D, 3D, etc.).
    /// </summary>
    public static float[] GetFlatArray(Tensor<float> tensor)
    {
        if (tensor is DenseTensor<float> dense)
            return dense.Buffer.ToArray();
        var arr = new float[tensor.Length];
        int i = 0;
        foreach (var v in tensor)
            arr[i++] = v;
        return arr;
    }

    /// <summary>
    /// Detect if landmark coordinates are in normalized [0,1] space or pixel [0,inputSize] space.
    /// Samples first few valid coordinates and checks if all are below threshold.
    /// </summary>
    public static bool IsNormalizedCoordinates(float[] data, int numLandmarks, int stride, float threshold = 2.0f)
    {
        for (int i = 0; i < Math.Min(10, numLandmarks); i++)
        {
            int idx = i * stride;
            if (idx + 1 >= data.Length) break;
            float absX = Math.Abs(data[idx]);
            float absY = Math.Abs(data[idx + 1]);
            if (absX > threshold || absY > threshold)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Compute Intersection over Union between two rectangles.
    /// </summary>
    public static float IoU(Rect a, Rect b)
    {
        var intersection = a & b; // OpenCvSharp Rect intersection
        if (intersection.Width <= 0 || intersection.Height <= 0) return 0;
        float intersectionArea = intersection.Width * intersection.Height;
        float unionArea = a.Width * a.Height + b.Width * b.Height - intersectionArea;
        return unionArea > 0 ? intersectionArea / unionArea : 0;
    }

    /// <summary>
    /// Apply Non-Maximum Suppression to detection results.
    /// </summary>
    public static List<(Rect Box, float Score, int Index)> NonMaxSuppression(
        List<(Rect Box, float Score)> detections, float iouThreshold = 0.3f)
    {
        var sorted = detections
            .Select((d, i) => (d.Box, d.Score, Index: i))
            .OrderByDescending(d => d.Score)
            .ToList();

        var result = new List<(Rect Box, float Score, int Index)>();

        while (sorted.Count > 0)
        {
            var best = sorted[0];
            result.Add(best);
            sorted.RemoveAt(0);
            sorted.RemoveAll(d => IoU(best.Box, d.Box) > iouThreshold);
        }

        return result;
    }

    #endregion

    #region Anchor Generation

    private static float[,]? _blazeFaceAnchors;
    private static float[,]? _palmDetectionAnchors;

    /// <summary>
    /// Generate 896 SSD anchors for BlazeFace short-range model (128x128 input).
    /// </summary>
    public static float[,] GetBlazeFaceAnchors()
    {
        if (_blazeFaceAnchors != null) return _blazeFaceAnchors;

        var anchors = new List<float[]>();
        int inputSize = 128;

        // Layer configs: [stride, numAnchors]
        (int stride, int count)[] layers = { (8, 2), (16, 6) };

        foreach (var (stride, count) in layers)
        {
            int gridSize = inputSize / stride;
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    float cx = (x + 0.5f) / gridSize;
                    float cy = (y + 0.5f) / gridSize;
                    for (int k = 0; k < count; k++)
                    {
                        anchors.Add(new[] { cx, cy });
                    }
                }
            }
        }

        _blazeFaceAnchors = new float[anchors.Count, 2];
        for (int i = 0; i < anchors.Count; i++)
        {
            _blazeFaceAnchors[i, 0] = anchors[i][0];
            _blazeFaceAnchors[i, 1] = anchors[i][1];
        }
        return _blazeFaceAnchors;
    }

    /// <summary>
    /// Generate 2016 SSD anchors for palm detection model (192x192 input).
    /// </summary>
    public static float[,] GetPalmDetectionAnchors()
    {
        if (_palmDetectionAnchors != null) return _palmDetectionAnchors;

        var anchors = new List<float[]>();
        int inputSize = 192;

        (int stride, int count)[] layers = { (8, 2), (16, 6) };

        foreach (var (stride, count) in layers)
        {
            int gridSize = inputSize / stride;
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    float cx = (x + 0.5f) / gridSize;
                    float cy = (y + 0.5f) / gridSize;
                    for (int k = 0; k < count; k++)
                    {
                        anchors.Add(new[] { cx, cy });
                    }
                }
            }
        }

        _palmDetectionAnchors = new float[anchors.Count, 2];
        for (int i = 0; i < anchors.Count; i++)
        {
            _palmDetectionAnchors[i, 0] = anchors[i][0];
            _palmDetectionAnchors[i, 1] = anchors[i][1];
        }
        return _palmDetectionAnchors;
    }

    #endregion

    #region Drawing Helpers

    /// <summary>
    /// Draw landmarks and their connections on an image.
    /// </summary>
    public static void DrawLandmarks(Mat image, Point[] landmarks, int[][] connections,
        Scalar color, int thickness = 1, int radius = 2)
    {
        // Draw connections
        foreach (var conn in connections)
        {
            if (conn.Length < 2) continue;
            int i0 = conn[0], i1 = conn[1];
            if (i0 >= 0 && i0 < landmarks.Length && i1 >= 0 && i1 < landmarks.Length)
            {
                Cv2.Line(image, landmarks[i0], landmarks[i1], color, thickness, LineTypes.AntiAlias);
            }
        }

        // Draw landmark points
        foreach (var pt in landmarks)
        {
            Cv2.Circle(image, pt, radius, color, -1, LineTypes.AntiAlias);
        }
    }

    /// <summary>
    /// Draw a detection bounding box with label and score.
    /// </summary>
    public static void DrawDetectionBox(Mat image, Rect box, string label,
        float score, Scalar color, int thickness = 2)
    {
        Cv2.Rectangle(image, box, color, thickness);

        var text = $"{label} {score:P0}";
        var textSize = Cv2.GetTextSize(text, HersheyFonts.HersheySimplex, 0.5, 1, out int baseline);
        var textBg = new Rect(box.X, box.Y - textSize.Height - baseline - 4,
            textSize.Width + 4, textSize.Height + baseline + 4);

        if (textBg.Y < 0) textBg.Y = box.Y;
        Cv2.Rectangle(image, textBg, color, -1);
        Cv2.PutText(image, text, new Point(textBg.X + 2, textBg.Y + textSize.Height + 2),
            HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 255), 1);
    }

    #endregion

    #region Connection Definitions

    /// <summary>Hand skeleton connections (21 landmarks).</summary>
    public static readonly int[][] HandConnections =
    {
        // Thumb
        new[] {0, 1}, new[] {1, 2}, new[] {2, 3}, new[] {3, 4},
        // Index finger
        new[] {0, 5}, new[] {5, 6}, new[] {6, 7}, new[] {7, 8},
        // Middle finger
        new[] {0, 9}, new[] {9, 10}, new[] {10, 11}, new[] {11, 12},
        // Ring finger
        new[] {0, 13}, new[] {13, 14}, new[] {14, 15}, new[] {15, 16},
        // Pinky
        new[] {0, 17}, new[] {17, 18}, new[] {18, 19}, new[] {19, 20},
        // Palm base
        new[] {5, 9}, new[] {9, 13}, new[] {13, 17}
    };

    /// <summary>Pose skeleton connections (33 landmarks).</summary>
    public static readonly int[][] PoseConnections =
    {
        // Face
        new[] {0, 1}, new[] {1, 2}, new[] {2, 3}, new[] {3, 7},
        new[] {0, 4}, new[] {4, 5}, new[] {5, 6}, new[] {6, 8},
        new[] {9, 10},
        // Torso
        new[] {11, 12}, new[] {11, 23}, new[] {12, 24}, new[] {23, 24},
        // Left arm
        new[] {11, 13}, new[] {13, 15}, new[] {15, 17}, new[] {15, 19}, new[] {15, 21}, new[] {17, 19},
        // Right arm
        new[] {12, 14}, new[] {14, 16}, new[] {16, 18}, new[] {16, 20}, new[] {16, 22}, new[] {18, 20},
        // Left leg
        new[] {23, 25}, new[] {25, 27}, new[] {27, 29}, new[] {27, 31}, new[] {29, 31},
        // Right leg
        new[] {24, 26}, new[] {26, 28}, new[] {28, 30}, new[] {28, 32}, new[] {30, 32}
    };

    /// <summary>Face mesh tessellation contour connections (subset for visualization).</summary>
    public static readonly int[][] FaceMeshContours =
    {
        // Face oval
        new[]{10,338}, new[]{338,297}, new[]{297,332}, new[]{332,284}, new[]{284,251}, new[]{251,389},
        new[]{389,356}, new[]{356,454}, new[]{454,323}, new[]{323,361}, new[]{361,288}, new[]{288,397},
        new[]{397,365}, new[]{365,379}, new[]{379,378}, new[]{378,400}, new[]{400,377}, new[]{377,152},
        new[]{152,148}, new[]{148,176}, new[]{176,149}, new[]{149,150}, new[]{150,136}, new[]{136,172},
        new[]{172,58}, new[]{58,132}, new[]{132,93}, new[]{93,234}, new[]{234,127}, new[]{127,162},
        new[]{162,21}, new[]{21,54}, new[]{54,103}, new[]{103,67}, new[]{67,109}, new[]{109,10},
        // Lips outer
        new[]{61,146}, new[]{146,91}, new[]{91,181}, new[]{181,84}, new[]{84,17}, new[]{17,314},
        new[]{314,405}, new[]{405,321}, new[]{321,375}, new[]{375,291}, new[]{291,61},
        // Lips inner
        new[]{78,95}, new[]{95,88}, new[]{88,178}, new[]{178,87}, new[]{87,14}, new[]{14,317},
        new[]{317,402}, new[]{402,318}, new[]{318,324}, new[]{324,308}, new[]{308,78},
        // Left eye
        new[]{33,7}, new[]{7,163}, new[]{163,144}, new[]{144,145}, new[]{145,153}, new[]{153,154},
        new[]{154,155}, new[]{155,133}, new[]{133,173}, new[]{173,157}, new[]{157,158}, new[]{158,159},
        new[]{159,160}, new[]{160,161}, new[]{161,246}, new[]{246,33},
        // Right eye
        new[]{263,249}, new[]{249,390}, new[]{390,373}, new[]{373,374}, new[]{374,380}, new[]{380,381},
        new[]{381,382}, new[]{382,362}, new[]{362,398}, new[]{398,384}, new[]{384,385}, new[]{385,386},
        new[]{386,387}, new[]{387,388}, new[]{388,466}, new[]{466,263},
        // Left eyebrow
        new[]{46,53}, new[]{53,52}, new[]{52,65}, new[]{65,55}, new[]{70,63}, new[]{63,105},
        new[]{105,66}, new[]{66,107},
        // Right eyebrow
        new[]{276,283}, new[]{283,282}, new[]{282,295}, new[]{295,285}, new[]{300,293}, new[]{293,334},
        new[]{334,296}, new[]{296,336}
    };

    #endregion

    #region COCO Labels

    /// <summary>COCO 80-class label map (with non-contiguous IDs).</summary>
    public static readonly Dictionary<int, string> CocoLabels = new()
    {
        {1,"person"},{2,"bicycle"},{3,"car"},{4,"motorcycle"},{5,"airplane"},
        {6,"bus"},{7,"train"},{8,"truck"},{9,"boat"},{10,"traffic light"},
        {11,"fire hydrant"},{13,"stop sign"},{14,"parking meter"},{15,"bench"},
        {16,"bird"},{17,"cat"},{18,"dog"},{19,"horse"},{20,"sheep"},
        {21,"cow"},{22,"elephant"},{23,"bear"},{24,"zebra"},{25,"giraffe"},
        {27,"backpack"},{28,"umbrella"},{31,"handbag"},{32,"tie"},{33,"suitcase"},
        {34,"frisbee"},{35,"skis"},{36,"snowboard"},{37,"sports ball"},{38,"kite"},
        {39,"baseball bat"},{40,"baseball glove"},{41,"skateboard"},{42,"surfboard"},
        {43,"tennis racket"},{44,"bottle"},{46,"wine glass"},{47,"cup"},{48,"fork"},
        {49,"knife"},{50,"spoon"},{51,"bowl"},{52,"banana"},{53,"apple"},
        {54,"sandwich"},{55,"orange"},{56,"broccoli"},{57,"carrot"},{58,"hot dog"},
        {59,"pizza"},{60,"donut"},{61,"cake"},{62,"chair"},{63,"couch"},
        {64,"potted plant"},{65,"bed"},{67,"dining table"},{70,"toilet"},{72,"tv"},
        {73,"laptop"},{74,"mouse"},{75,"remote"},{76,"keyboard"},{77,"cell phone"},
        {78,"microwave"},{79,"oven"},{80,"toaster"},{81,"sink"},{82,"refrigerator"},
        {84,"book"},{85,"clock"},{86,"vase"},{87,"scissors"},{88,"teddy bear"},
        {89,"hair drier"},{90,"toothbrush"}
    };

    /// <summary>
    /// Get COCO label name by class ID. Returns "class_N" for unknown IDs.
    /// Also handles 0-based contiguous indexing (some models use 0-79 instead of 1-90).
    /// </summary>
    public static string GetCocoLabel(int classId)
    {
        if (CocoLabels.TryGetValue(classId, out var label))
            return label;
        if (CocoLabels.TryGetValue(classId + 1, out label))
            return label;
        return $"class_{classId}";
    }

    #endregion
}
