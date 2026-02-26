namespace MVXTester.Core.Models;

/// <summary>
/// Static event bus for runtime communication between display nodes (ImageShow)
/// and event source nodes (MouseInput). Events from OpenCV windows are published
/// here and consumed by source nodes without creating graph cycles.
/// </summary>
public static class RuntimeEventBus
{
    /// <summary>Fired when a mouse event occurs on an ImageShow OpenCV window.</summary>
    public static event Action<MouseEventData>? MouseEvent;

    /// <summary>Fired when a key is pressed on an ImageShow OpenCV window (via WaitKey).</summary>
    public static event Action<int>? KeyEvent;

    public static void RaiseMouseEvent(MouseEventData data) => MouseEvent?.Invoke(data);
    public static void RaiseKeyEvent(int keyCode) => KeyEvent?.Invoke(keyCode);
}
