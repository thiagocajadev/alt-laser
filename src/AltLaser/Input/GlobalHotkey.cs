using AltLaser.Interop;

using AltLaser.Localization;

namespace AltLaser.Input;

internal sealed class GlobalHotkey : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 1;
    private const uint NoRepeatModifier = 0x4000;

    private readonly Action _onPressed;

    public static Result<GlobalHotkey> Register(HotkeyBinding binding, Action onPressed)
    {
        var hotkey = new GlobalHotkey(onPressed);

        var isRegistered = NativeMethods.RegisterHotKey(
            hotkey.Handle,
            HotkeyId,
            (uint)binding.Modifiers | NoRepeatModifier,
            (uint)binding.Key);

        if (!isRegistered)
        {
            hotkey.DestroyHandle();
            return Result<GlobalHotkey>.Failure(UiText.HotkeyInUse(binding.DisplayName));
        }

        var registration = Result<GlobalHotkey>.Success(hotkey);
        return registration;
    }

    private GlobalHotkey(Action onPressed)
    {
        _onPressed = onPressed;
        CreateHandle(new CreateParams());
    }

    protected override void WndProc(ref Message message)
    {
        var isOurHotkey = message.Msg == WmHotkey && message.WParam == HotkeyId;

        if (isOurHotkey)
            _onPressed();

        base.WndProc(ref message);
    }

    public void Dispose()
    {
        NativeMethods.UnregisterHotKey(Handle, HotkeyId);
        DestroyHandle();
    }
}
