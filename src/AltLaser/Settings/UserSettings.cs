using AltLaser.Localization;

namespace AltLaser.Settings;

internal sealed record UserSettings(
    DrawingMode DrawingMode,
    Color LaserColor,
    int FadeDelayMs,
    AppLanguage Language)
{
    public static UserSettings Default { get; } = new(
        LaserSettings.DefaultDrawingMode,
        LaserSettings.DefaultLaserColor,
        LaserSettings.DefaultFadeDelayMs,
        UiText.SystemLanguage);

    public bool HasColor(Color color) => LaserColor.ToArgb() == color.ToArgb();
}
