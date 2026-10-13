using System.Runtime.InteropServices;
using static AltLaser.Interop.NativeMethods;

namespace AltLaser.Input;

internal sealed class AltHoldDetector : IDisposable
{
    private const int WhKeyboardLowLevel = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const int FlagsOffset = 8;
    private const int InjectedFlag = 0x10;
    private const byte MenuMaskKey = 0xE8;
    private const uint KeyUpFlag = 0x0002;

    private readonly LowLevelHookThread _hook;
    private readonly SynchronizationContext _uiContext;
    private readonly Action _onHoldStarted;
    private readonly Action _onHoldEnded;

    // Only touched by the hook thread while it runs.
    private bool _isHoldKeyDown;
    private bool _isHolding;

    public bool IsListening => _hook.IsRunning;

    public AltHoldDetector(Action onHoldStarted, Action onHoldEnded)
    {
        _onHoldStarted = onHoldStarted;
        _onHoldEnded = onHoldEnded;
        _hook = new LowLevelHookThread(WhKeyboardLowLevel, OnKeyboardEvent);
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
    }

    public void Start() => _hook.Start();

    public void Stop()
    {
        if (!IsListening)
            return;

        _hook.Stop();

        _isHoldKeyDown = false;
        _isHolding = false;
    }

    // Only observes the keys: every event still reaches the other apps.
    private bool OnKeyboardEvent(IntPtr message, IntPtr data)
    {
        HandleKeyEvent(message, data);
        return false;
    }

    private void HandleKeyEvent(IntPtr message, IntPtr data)
    {
        if (IsInjected(data))
            return;

        var isHoldKey = Marshal.ReadInt32(data) == (int)LaserSettings.HoldToPointKey;
        var isKeyDown = message == WmKeyDown || message == WmSysKeyDown;
        var isKeyUp = message == WmKeyUp || message == WmSysKeyUp;

        if (isHoldKey && isKeyDown)
            BeginHold();
        else if (isHoldKey && isKeyUp)
            ReleaseHoldKey();
        else if (isKeyDown)
            EndHold();
    }

    private void BeginHold()
    {
        if (_isHoldKeyDown)
            return;

        _isHoldKeyDown = true;
        _isHolding = true;

        _uiContext.Post(_ =>
        {
            MaskMenuActivation();
            _onHoldStarted();
        }, null);
    }

    private void ReleaseHoldKey()
    {
        _isHoldKeyDown = false;

        EndHold();
    }

    private void EndHold()
    {
        if (!_isHolding)
            return;

        _isHolding = false;

        _uiContext.Post(_ => _onHoldEnded(), null);
    }

    private static bool IsInjected(IntPtr data)
    {
        var flags = Marshal.ReadInt32(data, FlagsOffset);

        var isInjected = (flags & InjectedFlag) != 0;
        return isInjected;
    }

    private static void MaskMenuActivation()
    {
        keybd_event(MenuMaskKey, 0, 0, UIntPtr.Zero);
        keybd_event(MenuMaskKey, 0, KeyUpFlag, UIntPtr.Zero);
    }

    public void Dispose() => Stop();
}
