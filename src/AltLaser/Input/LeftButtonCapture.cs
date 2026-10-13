namespace AltLaser.Input;

internal sealed class LeftButtonCapture : IDisposable
{
    private const int WhMouseLowLevel = 14;
    private const int WmLeftButtonDown = 0x0201;
    private const int WmLeftButtonUp = 0x0202;

    private readonly LowLevelHookThread _hook;

    // Written by the hook thread, read by the UI thread.
    private volatile bool _isPressed;

    public bool IsPressed => _isPressed;

    public bool IsCapturing => _hook.IsRunning;

    public LeftButtonCapture()
    {
        _hook = new LowLevelHookThread(WhMouseLowLevel, OnMouseEvent);
    }

    public void Start() => _hook.Start();

    public void Stop()
    {
        if (!IsCapturing)
            return;

        _hook.Stop();
        _isPressed = false;
    }

    // Swallows left button events so drawing does not click through to the apps below.
    private bool OnMouseEvent(IntPtr message, IntPtr data)
    {
        var isLeftButtonEvent = message == WmLeftButtonDown || message == WmLeftButtonUp;

        if (!isLeftButtonEvent)
            return false;

        _isPressed = message == WmLeftButtonDown;
        return true;
    }

    public void Dispose() => Stop();
}
