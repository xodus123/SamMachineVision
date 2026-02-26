using OpenCvSharp;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Input;

[NodeInfo("Image Show", NodeCategories.Input, Description = "Display image in output window with mouse event support")]
public class ImageShowNode : BaseNode, IMouseEventReceiver
{
    private InputPort<Mat> _imageInput = null!;
    private OutputPort<int> _mouseXOutput = null!;
    private OutputPort<int> _mouseYOutput = null!;
    private OutputPort<string> _mouseEventOutput = null!;
    private OutputPort<bool> _mousePressedOutput = null!;

    private MouseEventData? _lastEvent;
    private bool _isPressed;
    private readonly object _lock = new();

    protected override void Setup()
    {
        _imageInput = AddInput<Mat>("Image");
        _mouseXOutput = AddOutput<int>("MouseX");
        _mouseYOutput = AddOutput<int>("MouseY");
        _mouseEventOutput = AddOutput<string>("MouseEvent");
        _mousePressedOutput = AddOutput<bool>("MousePressed");
    }

    public void OnMouseEvent(MouseEventData eventData)
    {
        lock (_lock)
        {
            _lastEvent = eventData;

            switch (eventData.EventType)
            {
                case MouseEventType.LeftDown:
                case MouseEventType.RightDown:
                case MouseEventType.MiddleDown:
                    _isPressed = true;
                    break;
                case MouseEventType.LeftUp:
                case MouseEventType.RightUp:
                case MouseEventType.MiddleUp:
                    _isPressed = false;
                    break;
            }
        }

        IsDirty = true;
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

            // Output mouse event data
            lock (_lock)
            {
                if (_lastEvent != null)
                {
                    SetOutputValue(_mouseXOutput, _lastEvent.X);
                    SetOutputValue(_mouseYOutput, _lastEvent.Y);
                    SetOutputValue(_mouseEventOutput, _lastEvent.EventType.ToString());
                    SetOutputValue(_mousePressedOutput, _isPressed);
                }
            }

            // Set preview for ExecuteOutput panel display
            SetPreview(image);
            Error = null;
        }
        catch (Exception ex)
        {
            Error = $"Image Show error: {ex.Message}";
        }
    }
}
