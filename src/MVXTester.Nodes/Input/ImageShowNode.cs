using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Input;

[NodeInfo("Image Show", NodeCategories.Input, Description = "Display image in OpenCV window (mouse/keyboard events sent to Event nodes via RuntimeEventBus)")]
public class ImageShowNode : BaseNode
{
    private InputPort<Mat> _imageInput = null!;
    private NodeProperty _windowName = null!;
    private string _activeWindowName = "Image";

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _windowName = AddStringProperty("WindowName", "Window Name", "Image", "Display window name");
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

            // Only show OpenCV window in runtime mode (F5 Execute)
            if (IsRuntimeMode)
            {
                _activeWindowName = _windowName.GetValue<string>();
                if (string.IsNullOrWhiteSpace(_activeWindowName))
                    _activeWindowName = "Image";

                // Delegate to shared display manager (single HighGUI thread for all windows)
                ImageShowManager.ShowImage(_activeWindowName, image, OnOpenCvMouseCallback);
            }

            SetPreview(image);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Image Show error: {ex.Message}";
        }
    }

    /// <summary>
    /// Called by OpenCV on the shared display thread when mouse events occur on this window.
    /// Publishes to RuntimeEventBus so MouseEventNode / MouseRoiNode can receive them.
    /// </summary>
    private void OnOpenCvMouseCallback(MouseEventTypes eventType, int x, int y, MouseEventFlags flags, IntPtr userdata)
    {
        var mapped = MapMouseEventType(eventType);
        if (mapped == null) return;

        RuntimeEventBus.RaiseMouseEvent(new MouseEventData
        {
            EventType = mapped.Value,
            X = x,
            Y = y,
            Button = GetButton(eventType)
        });
    }

    private static MouseEventType? MapMouseEventType(MouseEventTypes cvEvent)
    {
        return cvEvent switch
        {
            MouseEventTypes.MouseMove => MouseEventType.Move,
            MouseEventTypes.LButtonDown => MouseEventType.LeftDown,
            MouseEventTypes.LButtonUp => MouseEventType.LeftUp,
            MouseEventTypes.RButtonDown => MouseEventType.RightDown,
            MouseEventTypes.RButtonUp => MouseEventType.RightUp,
            MouseEventTypes.MButtonDown => MouseEventType.MiddleDown,
            MouseEventTypes.MButtonUp => MouseEventType.MiddleUp,
            MouseEventTypes.MouseWheel => MouseEventType.Wheel,
            _ => null
        };
    }

    private static int GetButton(MouseEventTypes cvEvent)
    {
        return cvEvent switch
        {
            MouseEventTypes.LButtonDown or MouseEventTypes.LButtonUp => 0,
            MouseEventTypes.MButtonDown or MouseEventTypes.MButtonUp => 1,
            MouseEventTypes.RButtonDown or MouseEventTypes.RButtonUp => 2,
            _ => -1
        };
    }

    public override void Cleanup()
    {
        // Remove this window from the shared display manager
        if (!string.IsNullOrWhiteSpace(_activeWindowName))
        {
            ImageShowManager.RemoveWindow(_activeWindowName);
        }

        base.Cleanup();
    }
}
