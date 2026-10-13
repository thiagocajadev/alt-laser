using AltLaser.Localization;

namespace AltLaser;

// Compile-time tuning. Mode, color and fade delay are only the defaults: the tray menu
// changes them at runtime and UserSettingsStore remembers the choice.
internal static class LaserSettings
{
    public static readonly HotkeyBinding ToggleHotkey = new(
        Modifiers: HotkeyModifiers.Alt,
        Key: Keys.L,
        DisplayName: "Alt+L");

    public const Keys HoldToPointKey = Keys.LMenu;

    public const DrawingMode DefaultDrawingMode = DrawingMode.Arrow;
    public static readonly Color DefaultLaserColor = Color.FromArgb(255, 255, 45, 85);
    public const int DefaultFadeDelayMs = 700;

    public static readonly IReadOnlyList<ColorPreset> ColorPresets =
    [
        new(() => UiText.Red, DefaultLaserColor),
        new(() => UiText.Orange, Color.FromArgb(255, 255, 149, 0)),
        new(() => UiText.Yellow, Color.FromArgb(255, 255, 204, 0)),
        new(() => UiText.Green, Color.FromArgb(255, 52, 199, 89)),
        new(() => UiText.Blue, Color.FromArgb(255, 0, 122, 255)),
        new(() => UiText.Purple, Color.FromArgb(255, 175, 82, 222)),
    ];

    public static readonly IReadOnlyList<int> FadeDelayPresetsMs = [400, 700, 1500, 3000, 6000, 10000];
    public const int MinFadeDelayMs = 100;
    public const int MaxFadeDelayMs = 60000;

    public static readonly bool RequireClickToDraw = false;
    public static readonly bool ReplaceSystemCursor = true;

    public const int StrokePauseMs = 200;
    public const int TrailFadeOutMs = 500;
    public const int TrailMaxAgeMs = 4000;
    public const float TrailWidth = 6f;
    public const float TrailOpacity = 0.9f;
    public const float TrailCurveTension = 0.5f;
    public const int MinPointDistancePx = 2;

    public const float ArrowHeadLength = 22f;
    public const float ArrowHeadAngleDegrees = 30f;

    public const float HeadRadius = 5f;
    public const float HeadGlowRadius = 13f;
    public const int HeadGlowAlpha = 70;

    public const int FrameIntervalMs = 15;
}

internal sealed record ColorPreset(Func<string> Name, Color Color);

internal sealed record HotkeyBinding(
    HotkeyModifiers Modifiers,
    Keys Key,
    string DisplayName);

[Flags]
internal enum HotkeyModifiers : uint
{
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008,
}
