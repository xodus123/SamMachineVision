using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Nodes.Event;

[NodeInfo("Keyboard Event", NodeCategories.Event, Description = "Receive keyboard events from ImageShow window")]
public class KeyboardEventNode : BaseNode, IKeyboardEventReceiver
{
    private OutputPort<int> _keyCodeOutput = null!;
    private OutputPort<string> _keyNameOutput = null!;
    private OutputPort<bool> _isPressedOutput = null!;

    private int _lastKeyCode = -1;
    private string _lastKeyName = "";
    private bool _isPressed;
    private readonly object _lock = new();
    private bool _subscribed;

    protected override void Setup()
    {
        _keyCodeOutput = AddOutput<int>("KeyCode");
        _keyNameOutput = AddOutput<string>("KeyName");
        _isPressedOutput = AddOutput<bool>("IsPressed");
    }

    public void OnKeyboardEvent(KeyboardEventData eventData)
    {
        lock (_lock)
        {
            _lastKeyCode = eventData.KeyCode;
            _lastKeyName = eventData.KeyName;
            _isPressed = eventData.EventType == KeyEventType.KeyDown;
        }

        IsDirty = true;
    }

    private void OnKeyFromBus(int keyCode)
    {
        lock (_lock)
        {
            _lastKeyCode = keyCode;
            _lastKeyName = ((char)keyCode).ToString();
            _isPressed = true;
        }

        IsDirty = true;
    }

    public override void Process()
    {
        // Subscribe to RuntimeEventBus on first execution
        if (!_subscribed)
        {
            RuntimeEventBus.KeyEvent += OnKeyFromBus;
            _subscribed = true;
        }

        lock (_lock)
        {
            if (_lastKeyCode >= 0)
            {
                SetOutputValue(_keyCodeOutput, _lastKeyCode);
                SetOutputValue(_keyNameOutput, _lastKeyName);
                SetOutputValue(_isPressedOutput, _isPressed);
            }
        }

        Error = null;
    }

    public override void Cleanup()
    {
        if (_subscribed)
        {
            RuntimeEventBus.KeyEvent -= OnKeyFromBus;
            _subscribed = false;
        }
        base.Cleanup();
    }
}
