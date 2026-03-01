using System.Collections.Concurrent;
using OpenCvSharp;
using MVXTester.Core.Models;

namespace MVXTester.Nodes.Input;

/// <summary>
/// Singleton manager that runs a single dedicated thread for ALL OpenCV HighGUI operations.
/// OpenCV HighGUI requires all ImShow/WaitKey/SetMouseCallback calls to be on the same thread.
/// Multiple ImageShow nodes register their windows here, and the shared thread handles them all.
/// </summary>
public static class ImageShowManager
{
    private static Thread? _displayThread;
    private static volatile bool _running;
    private static readonly object _startLock = new();

    /// <summary>
    /// Registered windows: windowName → pending image + mouse callback.
    /// </summary>
    private static readonly ConcurrentDictionary<string, WindowEntry> _windows = new();

    /// <summary>
    /// Windows pending removal (from Cleanup).
    /// </summary>
    private static readonly ConcurrentQueue<string> _removeQueue = new();

    private class WindowEntry
    {
        public Mat? PendingImage;
        public readonly object ImageLock = new();
        public bool WindowCreated;
        public bool CallbackRegistered;
        public MouseCallback? MouseCallbackDelegate;
        public Action<MouseEventTypes, int, int, MouseEventFlags, IntPtr>? NodeCallback;
    }

    /// <summary>
    /// Register or update an image for display in a named window.
    /// Called from ImageShowNode.Process() on any thread.
    /// </summary>
    public static void ShowImage(string windowName, Mat image,
        Action<MouseEventTypes, int, int, MouseEventFlags, IntPtr>? mouseCallback = null)
    {
        var entry = _windows.GetOrAdd(windowName, _ => new WindowEntry());

        lock (entry.ImageLock)
        {
            entry.PendingImage?.Dispose();
            entry.PendingImage = image.Clone();
        }

        if (mouseCallback != null)
            entry.NodeCallback = mouseCallback;

        EnsureThreadRunning();
    }

    /// <summary>
    /// Unregister a window. It will be destroyed on the next display loop iteration.
    /// </summary>
    public static void RemoveWindow(string windowName)
    {
        _removeQueue.Enqueue(windowName);
    }

    /// <summary>
    /// Stop the display thread and close all windows. Called when runtime stops.
    /// </summary>
    public static void Shutdown()
    {
        _running = false;
        _displayThread?.Join(2000);
        _displayThread = null;

        // Dispose any remaining pending images
        foreach (var kvp in _windows)
        {
            lock (kvp.Value.ImageLock)
            {
                kvp.Value.PendingImage?.Dispose();
                kvp.Value.PendingImage = null;
            }
        }
        _windows.Clear();
    }

    private static void EnsureThreadRunning()
    {
        if (_displayThread != null && _running) return;

        lock (_startLock)
        {
            if (_displayThread != null && _running) return;

            _running = true;
            _displayThread = new Thread(DisplayLoop)
            {
                IsBackground = true,
                Name = "ImageShowManager_DisplayThread"
            };
            _displayThread.Start();
        }
    }

    /// <summary>
    /// Single shared display thread loop.
    /// Iterates over ALL registered windows and calls ImShow for each.
    /// WaitKey is called once per loop iteration to process all window messages.
    /// </summary>
    private static void DisplayLoop()
    {
        try
        {
            while (_running)
            {
                // Process pending removals
                while (_removeQueue.TryDequeue(out var removeName))
                {
                    if (_windows.TryRemove(removeName, out var removed))
                    {
                        lock (removed.ImageLock)
                        {
                            removed.PendingImage?.Dispose();
                            removed.PendingImage = null;
                        }
                        if (removed.WindowCreated)
                        {
                            try { Cv2.DestroyWindow(removeName); } catch { }
                        }
                    }
                }

                bool anyWindowActive = false;

                // Update all registered windows
                foreach (var kvp in _windows)
                {
                    var windowName = kvp.Key;
                    var entry = kvp.Value;

                    Mat? imageToShow = null;
                    lock (entry.ImageLock)
                    {
                        if (entry.PendingImage != null)
                        {
                            imageToShow = entry.PendingImage;
                            entry.PendingImage = null;
                        }
                    }

                    if (imageToShow != null)
                    {
                        Cv2.ImShow(windowName, imageToShow);
                        entry.WindowCreated = true;

                        if (!entry.CallbackRegistered && entry.NodeCallback != null)
                        {
                            // Create mouse callback that forwards to the node
                            var callback = entry.NodeCallback;
                            entry.MouseCallbackDelegate = (evt, x, y, flags, userData) =>
                                callback(evt, x, y, flags, userData);
                            Cv2.SetMouseCallback(windowName, entry.MouseCallbackDelegate);
                            entry.CallbackRegistered = true;
                        }

                        imageToShow.Dispose();
                    }

                    if (entry.WindowCreated)
                        anyWindowActive = true;
                }

                // Single WaitKey call processes messages for ALL windows
                if (anyWindowActive)
                {
                    try
                    {
                        var key = Cv2.WaitKey(1);
                        if (key >= 0)
                        {
                            RuntimeEventBus.RaiseKeyEvent(key);
                        }
                    }
                    catch
                    {
                        // A window may have been closed by user
                    }
                }
                else if (_windows.IsEmpty)
                {
                    // All windows removed - auto-shutdown thread
                    _running = false;
                    break;
                }
                else
                {
                    Thread.Sleep(16);
                }
            }
        }
        catch { }
        finally
        {
            // Destroy all remaining windows
            foreach (var kvp in _windows)
            {
                if (kvp.Value.WindowCreated)
                {
                    try { Cv2.DestroyWindow(kvp.Key); } catch { }
                }
                lock (kvp.Value.ImageLock)
                {
                    kvp.Value.PendingImage?.Dispose();
                    kvp.Value.PendingImage = null;
                }
            }
            _windows.Clear();
            try { Cv2.DestroyAllWindows(); } catch { }
        }
    }
}
