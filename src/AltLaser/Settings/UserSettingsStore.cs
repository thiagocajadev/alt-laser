using System.Text.Json;
using System.Text.Json.Serialization;
using AltLaser.Localization;

namespace AltLaser.Settings;

// Keeps the tray menu choices in %AppData%\AltLaser\settings.json. A missing or broken file
// falls back to the defaults, and a failed save only means the choice is not remembered.
internal static class UserSettingsStore
{
    private sealed record StoredSettings(
        DrawingMode DrawingMode,
        string LaserColor,
        int FadeDelayMs,
        AppLanguage? Language);

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AltLaser",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static UserSettings Load()
    {
        try
        {
            var json = File.ReadAllText(FilePath);
            var stored = JsonSerializer.Deserialize<StoredSettings>(json, JsonOptions);

            var settings = stored is null ? UserSettings.Default : FromStored(stored);
            return settings;
        }
        catch (Exception exception) when (IsExpectedFailure(exception))
        {
            return UserSettings.Default;
        }
    }

    public static void Save(UserSettings settings)
    {
        try
        {
            var stored = new StoredSettings(
                settings.DrawingMode,
                ColorTranslator.ToHtml(Color.FromArgb(settings.LaserColor.ToArgb())),
                settings.FadeDelayMs,
                settings.Language);

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(stored, JsonOptions));
        }
        catch (Exception exception) when (IsExpectedFailure(exception))
        {
        }
    }

    private static UserSettings FromStored(StoredSettings stored)
    {
        var mode = Enum.IsDefined(stored.DrawingMode)
            ? stored.DrawingMode
            : LaserSettings.DefaultDrawingMode;

        var color = string.IsNullOrWhiteSpace(stored.LaserColor)
            ? LaserSettings.DefaultLaserColor
            : Color.FromArgb(byte.MaxValue, ColorTranslator.FromHtml(stored.LaserColor));

        var fadeDelayMs = Math.Clamp(stored.FadeDelayMs, LaserSettings.MinFadeDelayMs, LaserSettings.MaxFadeDelayMs);

        var language = stored.Language is { } storedLanguage && Enum.IsDefined(storedLanguage)
            ? storedLanguage
            : UiText.SystemLanguage;

        var settings = new UserSettings(mode, color, fadeDelayMs, language);
        return settings;
    }

    private static bool IsExpectedFailure(Exception exception) =>
        exception is IOException
            or UnauthorizedAccessException
            or JsonException
            or ArgumentException
            or FormatException;
}
