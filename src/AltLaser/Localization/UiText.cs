using static AltLaser.Interop.NativeMethods;

namespace AltLaser.Localization;

// Every user-facing string, in English and Brazilian Portuguese. The tray menu picks the
// language; until then it follows the Windows display language (any Portuguese variant gets
// pt-BR), read through Win32 because InvariantGlobalization hides the culture from .NET.
internal static class UiText
{
    public static readonly AppLanguage SystemLanguage =
        (GetUserDefaultUILanguage() & PrimaryLanguageMask) == LangPortuguese
            ? AppLanguage.Portuguese
            : AppLanguage.English;

    public static AppLanguage Language { get; set; } = SystemLanguage;

    public const string AppName = "Alt Laser";
    public const string EnglishName = "English";
    public const string PortugueseName = "Português (Brasil)";

    public static string LanguageMenu => Pick("Language", "Idioma");

    public static string TurnOn(string hotkey) => Pick($"Turn on ({hotkey})", $"Ligar ({hotkey})");
    public static string TurnOff(string hotkey) => Pick($"Turn off ({hotkey})", $"Desligar ({hotkey})");
    public static string Arrow => Pick("Arrow", "Seta");
    public static string FreeDraw => Pick("Free draw", "Livre");
    public static string Color => Pick("Color", "Cor");
    public static string FadeAfter => Pick("Fade after", "Sumir após");
    public static string Custom => Pick("Custom…", "Personalizado…");
    public static string CustomWithValue(string value) => Pick($"Custom ({value})…", $"Personalizado ({value})…");
    public static string About => Pick($"About {AppName}", $"Sobre o {AppName}");
    public static string Exit => Pick("Exit", "Sair");

    public static string Red => Pick("Red", "Vermelho");
    public static string Orange => Pick("Orange", "Laranja");
    public static string Yellow => Pick("Yellow", "Amarelo");
    public static string Green => Pick("Green", "Verde");
    public static string Blue => Pick("Blue", "Azul");
    public static string Purple => Pick("Purple", "Roxo");

    public static string TrayTooltip(DrawingMode mode, bool isArmed, string hotkey)
    {
        var modeLabel = mode == DrawingMode.Arrow ? Pick("arrow", "seta") : Pick("free draw", "livre");
        var stateLabel = isArmed
            ? Pick($"{modeLabel}, hold Alt to point", $"{modeLabel}, segure o Alt para apontar")
            : Pick("off", "desligado");

        var tooltip = $"{AppName} — {stateLabel} ({hotkey})";
        return tooltip;
    }

    public static string StartupTitle => Pick($"{AppName} is on", $"{AppName} está ligado");

    public static string StartupMessage(string hotkey) => Pick(
        $"Hold left Alt and move the mouse to draw an arrow. Right-click this icon for free draw, color and fade time. {hotkey} turns it off or on.",
        $"Segure o Alt esquerdo e mova o mouse para desenhar uma seta. Clique com o botão direito neste ícone para desenho livre, cor e tempo para sumir. {hotkey} liga e desliga.");

    public static string HotkeyInUse(string hotkey) => Pick(
        $"{hotkey} is already in use by another app.",
        $"O {hotkey} já está em uso por outro app.");

    public static string CursorFailed => Pick(
        "Could not create the laser cursor.",
        "Não foi possível criar o cursor de laser.");

    public static string AboutTagline => Pick(
        "Laser pointer for presentations and screen sharing on Windows.",
        "Ponteiro laser para apresentações e compartilhamento de tela no Windows.");

    public static string MadeBy => Pick("Made by", "Feito por");

    public static string FadePrompt(string min, string max) => Pick(
        $"Seconds before strokes fade ({min} to {max}):",
        $"Segundos até os traços sumirem ({min} a {max}):");

    public static string Ok => "OK";
    public static string Cancel => Pick("Cancel", "Cancelar");

    private static string Pick(string english, string portuguese) =>
        Language == AppLanguage.Portuguese ? portuguese : english;
}
