using static AltLaser.Interop.NativeMethods;

namespace AltLaser.Input;

internal sealed class LeftButtonCapture : IDisposable
{
    private const int WhMouseLowLevel = 14;
    private const int WmLeftButtonDown = 0x0201;
    private const int WmLeftButtonUp = 0x0202;
    private const int HookActionCode = 0;
    private static readonly IntPtr SwallowEvent = new(1);

    private readonly LowLevelMouseProc _hookCallback;
    private IntPtr _hookHandle;

    public bool IsPressed { get; private set; }

    public bool IsCapturing => _hookHandle != IntPtr.Zero;

    public LeftButtonCapture()
    {
        _hookCallback = OnMouseEvent;
    }

    public void Start()
    {
        if (IsCapturing)
            return;

        _hookHandle = SetWindowsHookEx(
            WhMouseLowLevel,
            _hookCallback,
            GetModuleHandle(null),
            0);
    }

    public void Stop()
    {
        if (!IsCapturing)
            return;

        UnhookWindowsHookEx(_hookHandle);

        _hookHandle = IntPtr.Zero;
        IsPressed = false;
    }

    private IntPtr OnMouseEvent(int code, IntPtr message, IntPtr data)
    {
        var isLeftButtonEvent = message == WmLeftButtonDown || message == WmLeftButtonUp;

        if (code != HookActionCode || !isLeftButtonEvent)
            return CallNextHookEx(_hookHandle, code, message, data);

        IsPressed = message == WmLeftButtonDown;

        return SwallowEvent;
    }

    public void Dispose() => Stop();
}
