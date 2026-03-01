using System.Collections;
using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Value;

/// <summary>
/// Print node for debugging: displays any data type as formatted text on the preview.
/// Supports Mat, arrays, points, rects, scalars, and all primitive types.
/// </summary>
[NodeInfo("Print", NodeCategories.Value,
    Description = "Display any value as formatted text in preview")]
public class PrintNode : BaseNode
{
    private InputPort<object> _input0 = null!;
    private InputPort<object> _input1 = null!;
    private InputPort<object> _input2 = null!;
    private InputPort<object> _input3 = null!;
    private OutputPort<string> _textOutput = null!;

    private NodeProperty _fontSize = null!;
    private NodeProperty _bgColor = null!;
    private NodeProperty _maxItems = null!;
    private NodeProperty _showType = null!;
    private NodeProperty _label0 = null!;
    private NodeProperty _label1 = null!;
    private NodeProperty _label2 = null!;
    private NodeProperty _label3 = null!;

    private const int PreviewWidth = 480;
    private const int MinPreviewHeight = 120;
    private const int MaxPreviewHeight = 800;
    private const int Padding = 12;

    protected override void Setup()
    {
        _input0 = AddInput<object>("In 0");
        _input1 = AddInput<object>("In 1");
        _input2 = AddInput<object>("In 2");
        _input3 = AddInput<object>("In 3");

        _textOutput = AddOutput<string>("Text");

        _fontSize = AddDoubleProperty("FontSize", "Font Size", 0.45, 0.2, 2.0, "Text font scale");
        _bgColor = AddEnumProperty("BgColor", "Background", PrintBgColor.Dark, "Background color");
        _maxItems = AddIntProperty("MaxItems", "Max Items", 20, 1, 200, "Maximum array items to display");
        _showType = AddBoolProperty("ShowType", "Show Type", true, "Show data type name");
        _label0 = AddStringProperty("Label0", "Label 0", "", "Custom label for Input 0");
        _label1 = AddStringProperty("Label1", "Label 1", "", "Custom label for Input 1");
        _label2 = AddStringProperty("Label2", "Label 2", "", "Custom label for Input 2");
        _label3 = AddStringProperty("Label3", "Label 3", "", "Custom label for Input 3");
    }

    public override void Process()
    {
        try
        {
            var inputs = new[]
            {
                (Value: GetInputValue(_input0), Label: _label0.GetValue<string>(), Name: "In 0"),
                (Value: GetInputValue(_input1), Label: _label1.GetValue<string>(), Name: "In 1"),
                (Value: GetInputValue(_input2), Label: _label2.GetValue<string>(), Name: "In 2"),
                (Value: GetInputValue(_input3), Label: _label3.GetValue<string>(), Name: "In 3"),
            };

            var fontSize = (float)_fontSize.GetValue<double>();
            var bgMode = _bgColor.GetValue<PrintBgColor>();
            var maxItems = _maxItems.GetValue<int>();
            var showType = _showType.GetValue<bool>();

            // Build text lines
            var lines = new List<(string Text, Scalar Color)>();
            bool hasAnyInput = false;

            foreach (var (value, label, name) in inputs)
            {
                if (value == null && !IsPortConnected(name)) continue;
                hasAnyInput = true;

                var displayLabel = string.IsNullOrEmpty(label) ? name : label;
                FormatValue(lines, displayLabel, value, showType, maxItems, fontSize);
                lines.Add(("", default)); // blank line separator
            }

            if (!hasAnyInput)
            {
                lines.Add(("No input connected", new Scalar(128, 128, 128)));
            }

            // Remove trailing blank line
            while (lines.Count > 0 && string.IsNullOrEmpty(lines[^1].Text))
                lines.RemoveAt(lines.Count - 1);

            // Build full text for output port
            var fullText = string.Join(Environment.NewLine,
                lines.Where(l => !string.IsNullOrEmpty(l.Text)).Select(l => l.Text));
            SetOutputValue(_textOutput, fullText);

            // Render preview image
            RenderPreview(lines, fontSize, bgMode);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Print error: {ex.Message}";
        }
    }

    private bool IsPortConnected(string portName)
    {
        return Inputs.Any(p => p.Name == portName && p.IsConnected);
    }

    private void RenderPreview(List<(string Text, Scalar Color)> lines, float fontSize, PrintBgColor bgMode)
    {
        var font = HersheyFonts.HersheySimplex;
        int thickness = fontSize >= 0.6 ? 2 : 1;

        // Calculate line height
        var sampleSize = Cv2.GetTextSize("Ay", font, fontSize, thickness, out int baseline);
        int lineHeight = sampleSize.Height + baseline + 6;

        // Calculate required height
        int textHeight = lines.Count * lineHeight + Padding * 2;
        int height = Math.Clamp(textHeight, MinPreviewHeight, MaxPreviewHeight);

        // Background color
        Scalar bgScalar = bgMode switch
        {
            PrintBgColor.Dark => new Scalar(30, 30, 30),
            PrintBgColor.Light => new Scalar(240, 240, 240),
            PrintBgColor.Blue => new Scalar(60, 30, 20),
            _ => new Scalar(30, 30, 30)
        };

        Scalar defaultTextColor = bgMode == PrintBgColor.Light
            ? new Scalar(30, 30, 30)
            : new Scalar(220, 220, 220);

        using var preview = new Mat(height, PreviewWidth, MatType.CV_8UC3, bgScalar);

        int y = Padding + sampleSize.Height;
        foreach (var (text, color) in lines)
        {
            if (y > height - Padding) break;

            if (string.IsNullOrEmpty(text))
            {
                y += lineHeight / 2; // half-height for blank lines
                continue;
            }

            var drawColor = color == default ? defaultTextColor : color;

            // Truncate long lines
            var drawText = text;
            if (drawText.Length > 80)
                drawText = drawText[..77] + "...";

            Cv2.PutText(preview, drawText, new Point(Padding, y),
                font, fontSize, drawColor, thickness, LineTypes.AntiAlias);
            y += lineHeight;
        }

        SetPreview(preview);
    }

    private static void FormatValue(List<(string Text, Scalar Color)> lines,
        string label, object? value, bool showType, int maxItems, float fontSize)
    {
        var headerColor = new Scalar(100, 200, 255);   // Orange-yellow
        var typeColor = new Scalar(180, 180, 100);      // Teal
        var valueColor = new Scalar(150, 255, 150);     // Green
        var dimColor = new Scalar(140, 140, 140);       // Gray
        var numColor = new Scalar(180, 220, 255);       // Light peach

        if (value == null)
        {
            lines.Add(($"[{label}] null", dimColor));
            return;
        }

        var type = value.GetType();
        var typeName = GetFriendlyTypeName(type);

        // Header line
        if (showType)
            lines.Add(($"[{label}] ({typeName})", headerColor));
        else
            lines.Add(($"[{label}]", headerColor));

        // Format based on type
        switch (value)
        {
            case Mat mat:
                FormatMat(lines, mat, valueColor, dimColor);
                break;

            case Point[] points:
                FormatArray(lines, "Point", points.Length,
                    i => $"  [{i}] ({points[i].X}, {points[i].Y})", maxItems, numColor, dimColor);
                break;

            case Point2f[] points2f:
                FormatArray(lines, "Point2f", points2f.Length,
                    i => $"  [{i}] ({points2f[i].X:F2}, {points2f[i].Y:F2})", maxItems, numColor, dimColor);
                break;

            case Rect[] rects:
                FormatArray(lines, "Rect", rects.Length,
                    i => $"  [{i}] X={rects[i].X} Y={rects[i].Y} W={rects[i].Width} H={rects[i].Height}",
                    maxItems, numColor, dimColor);
                break;

            case double[] doubles:
                FormatArray(lines, "double", doubles.Length,
                    i => $"  [{i}] {doubles[i]:F4}", maxItems, numColor, dimColor);
                break;

            case float[] floats:
                FormatArray(lines, "float", floats.Length,
                    i => $"  [{i}] {floats[i]:F4}", maxItems, numColor, dimColor);
                break;

            case int[] ints:
                FormatArray(lines, "int", ints.Length,
                    i => $"  [{i}] {ints[i]}", maxItems, numColor, dimColor);
                break;

            case string[] strings:
                FormatArray(lines, "string", strings.Length,
                    i => $"  [{i}] \"{strings[i]}\"", maxItems, valueColor, dimColor);
                break;

            case byte[] bytes:
                lines.Add(($"  Length: {bytes.Length}", valueColor));
                if (bytes.Length > 0)
                {
                    var preview = string.Join(" ",
                        bytes.Take(Math.Min(16, bytes.Length)).Select(b => b.ToString("X2")));
                    if (bytes.Length > 16) preview += " ...";
                    lines.Add(($"  {preview}", numColor));
                }
                break;

            case bool boolVal:
                lines.Add(($"  {boolVal}", boolVal ? new Scalar(100, 255, 100) : new Scalar(100, 100, 255)));
                break;

            case int intVal:
                lines.Add(($"  {intVal}", numColor));
                break;

            case long longVal:
                lines.Add(($"  {longVal}", numColor));
                break;

            case float floatVal:
                lines.Add(($"  {floatVal:F4}", numColor));
                break;

            case double doubleVal:
                lines.Add(($"  {doubleVal:F6}", numColor));
                break;

            case string strVal:
                // Multi-line string support
                var strLines = strVal.Split('\n');
                foreach (var sl in strLines.Take(maxItems))
                    lines.Add(($"  {sl.TrimEnd('\r')}", valueColor));
                if (strLines.Length > maxItems)
                    lines.Add(($"  ... ({strLines.Length - maxItems} more lines)", dimColor));
                break;

            case Point pt:
                lines.Add(($"  ({pt.X}, {pt.Y})", numColor));
                break;

            case Point2f pt2f:
                lines.Add(($"  ({pt2f.X:F2}, {pt2f.Y:F2})", numColor));
                break;

            case Rect rect:
                lines.Add(($"  X={rect.X} Y={rect.Y} W={rect.Width} H={rect.Height}", numColor));
                break;

            case Size size:
                lines.Add(($"  {size.Width} x {size.Height}", numColor));
                break;

            case Scalar scalar:
                lines.Add(($"  ({scalar.Val0:F1}, {scalar.Val1:F1}, {scalar.Val2:F1}, {scalar.Val3:F1})", numColor));
                break;

            case IList list:
                FormatArray(lines, "item", list.Count,
                    i => $"  [{i}] {list[i]}", maxItems, valueColor, dimColor);
                break;

            default:
                // Fallback: ToString
                var str = value.ToString() ?? "";
                var fallbackLines = str.Split('\n');
                foreach (var fl in fallbackLines.Take(maxItems))
                    lines.Add(($"  {fl.TrimEnd('\r')}", valueColor));
                break;
        }
    }

    private static void FormatMat(List<(string Text, Scalar Color)> lines, Mat mat,
        Scalar valueColor, Scalar dimColor)
    {
        if (mat.Empty())
        {
            lines.Add(("  (empty)", dimColor));
            return;
        }

        lines.Add(($"  Size: {mat.Width} x {mat.Height}", valueColor));
        lines.Add(($"  Channels: {mat.Channels()}", valueColor));
        lines.Add(($"  Type: {mat.Type()}", valueColor));
        lines.Add(($"  Depth: {mat.Depth()}", valueColor));

        // Show pixel value at center
        int cx = mat.Width / 2, cy = mat.Height / 2;
        try
        {
            if (mat.Channels() == 1)
            {
                var val = mat.At<byte>(cy, cx);
                lines.Add(($"  Center[{cx},{cy}]: {val}", dimColor));
            }
            else if (mat.Channels() == 3)
            {
                var val = mat.At<Vec3b>(cy, cx);
                lines.Add(($"  Center[{cx},{cy}]: B={val.Item0} G={val.Item1} R={val.Item2}", dimColor));
            }
        }
        catch { /* ignore pixel access errors */ }
    }

    private static void FormatArray(List<(string Text, Scalar Color)> lines,
        string elementName, int count, Func<int, string> formatter,
        int maxItems, Scalar itemColor, Scalar dimColor)
    {
        lines.Add(($"  Count: {count}", itemColor));
        int show = Math.Min(count, maxItems);
        for (int i = 0; i < show; i++)
        {
            try
            {
                lines.Add((formatter(i), itemColor));
            }
            catch
            {
                lines.Add(($"  [{i}] (error)", dimColor));
            }
        }
        if (count > maxItems)
            lines.Add(($"  ... ({count - maxItems} more {elementName}s)", dimColor));
    }

    private static string GetFriendlyTypeName(Type type)
    {
        if (type == typeof(int)) return "int";
        if (type == typeof(long)) return "long";
        if (type == typeof(float)) return "float";
        if (type == typeof(double)) return "double";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(string)) return "string";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(Mat)) return "Mat";
        if (type == typeof(Point)) return "Point";
        if (type == typeof(Point2f)) return "Point2f";
        if (type == typeof(Rect)) return "Rect";
        if (type == typeof(Size)) return "Size";
        if (type == typeof(Scalar)) return "Scalar";
        if (type == typeof(int[])) return "int[]";
        if (type == typeof(float[])) return "float[]";
        if (type == typeof(double[])) return "double[]";
        if (type == typeof(string[])) return "string[]";
        if (type == typeof(byte[])) return "byte[]";
        if (type == typeof(Point[])) return "Point[]";
        if (type == typeof(Point2f[])) return "Point2f[]";
        if (type == typeof(Rect[])) return "Rect[]";
        if (type.IsArray) return $"{type.GetElementType()?.Name}[]";
        if (type.IsGenericType)
        {
            var genArgs = string.Join(", ", type.GetGenericArguments().Select(t => t.Name));
            return $"{type.Name.Split('`')[0]}<{genArgs}>";
        }
        return type.Name;
    }
}

/// <summary>
/// Background color option for Print node preview.
/// </summary>
public enum PrintBgColor
{
    Dark,
    Light,
    Blue
}
