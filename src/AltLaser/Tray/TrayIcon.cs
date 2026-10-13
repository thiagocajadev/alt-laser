using System.Globalization;
using AltLaser.Localization;
using AltLaser.Settings;

namespace AltLaser.Tray;

internal sealed record TrayActions(
    Action Toggle,
    Action<DrawingMode> SelectMode,
    Action<Color> SelectColor,
    Action<int> SelectFadeDelay,
    Action<AppLanguage> SelectLanguage,
    Action Exit);

internal sealed class TrayIcon : IDisposable
{
    private const string ActiveIconResource = "TrayOn.ico";
    private const string InactiveIconResource = "TrayOff.ico";
    private const int BalloonTimeoutMs = 5000;
    private const int SwatchSizePx = 16;
    private const float MsPerSecond = 1000f;

    private readonly TrayActions _actions;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _armItem;
    private readonly ToolStripMenuItem _arrowModeItem;
    private readonly ToolStripMenuItem _freeModeItem;
    private readonly ToolStripMenuItem _colorMenu;
    private readonly ToolStripMenuItem _customColorItem;
    private readonly ToolStripMenuItem _customFadeDelayItem;
    private readonly ToolStripMenuItem _englishItem;
    private readonly ToolStripMenuItem _portugueseItem;
    private readonly List<(ToolStripItem Item, Func<string> Label)> _labeledItems = [];
    private readonly List<(ToolStripMenuItem Item, Color Color)> _colorItems = [];
    private readonly List<(ToolStripMenuItem Item, int DelayMs)> _fadeDelayItems = [];
    private readonly List<Bitmap> _swatches = [];
    private readonly Icon _activeIcon;
    private readonly Icon _inactiveIcon;

    private Color _currentColor = LaserSettings.DefaultLaserColor;
    private int _currentFadeDelayMs = LaserSettings.DefaultFadeDelayMs;

    public TrayIcon(TrayActions actions)
    {
        _actions = actions;
        _activeIcon = LoadEmbeddedIcon(ActiveIconResource);
        _inactiveIcon = LoadEmbeddedIcon(InactiveIconResource);
        _armItem = new ToolStripMenuItem(string.Empty, null, (_, _) => actions.Toggle());
        _arrowModeItem = Labeled(new ToolStripMenuItem(string.Empty, null, (_, _) => actions.SelectMode(DrawingMode.Arrow)), () => UiText.Arrow);
        _freeModeItem = Labeled(new ToolStripMenuItem(string.Empty, null, (_, _) => actions.SelectMode(DrawingMode.Free)), () => UiText.FreeDraw);
        _customColorItem = Labeled(new ToolStripMenuItem(string.Empty, null, (_, _) => PickCustomColor()), () => UiText.Custom);
        _customFadeDelayItem = new ToolStripMenuItem(string.Empty, null, (_, _) => PickCustomFadeDelay());
        _englishItem = new ToolStripMenuItem(UiText.EnglishName, null, (_, _) => actions.SelectLanguage(AppLanguage.English));
        _portugueseItem = new ToolStripMenuItem(UiText.PortugueseName, null, (_, _) => actions.SelectLanguage(AppLanguage.Portuguese));
        _colorMenu = BuildColorMenu();

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = BuildMenu(),
            Visible = true,
        };
    }

    public void ShowState(bool isArmed, UserSettings settings)
    {
        var hotkey = LaserSettings.ToggleHotkey.DisplayName;

        foreach (var (item, label) in _labeledItems)
            item.Text = label();

        _notifyIcon.Icon = isArmed ? _activeIcon : _inactiveIcon;
        _notifyIcon.Text = UiText.TrayTooltip(settings.DrawingMode, isArmed, hotkey);
        _armItem.Text = isArmed ? UiText.TurnOff(hotkey) : UiText.TurnOn(hotkey);
        _arrowModeItem.Checked = settings.DrawingMode == DrawingMode.Arrow;
        _freeModeItem.Checked = settings.DrawingMode == DrawingMode.Free;
        _englishItem.Checked = settings.Language == AppLanguage.English;
        _portugueseItem.Checked = settings.Language == AppLanguage.Portuguese;

        ShowColor(settings);
        ShowFadeDelay(settings.FadeDelayMs);
    }

    public void ShowStartupNotice()
    {
        var hotkey = LaserSettings.ToggleHotkey.DisplayName;

        _notifyIcon.ShowBalloonTip(
            BalloonTimeoutMs,
            UiText.StartupTitle,
            UiText.StartupMessage(hotkey),
            ToolTipIcon.Info);
    }

    public void ShowWarning(string message)
    {
        _notifyIcon.ShowBalloonTip(
            BalloonTimeoutMs,
            UiText.AppName,
            message,
            ToolTipIcon.Warning);
    }

    private void ShowColor(UserSettings settings)
    {
        _currentColor = settings.LaserColor;

        foreach (var (item, color) in _colorItems)
            item.Checked = settings.HasColor(color);

        _customColorItem.Checked = !_colorItems.Any(entry => entry.Item.Checked);

        var previousSwatch = _colorMenu.Image;
        _colorMenu.Image = DrawSwatch(settings.LaserColor);
        previousSwatch?.Dispose();
    }

    private void ShowFadeDelay(int fadeDelayMs)
    {
        _currentFadeDelayMs = fadeDelayMs;

        foreach (var (item, delayMs) in _fadeDelayItems)
            item.Checked = delayMs == fadeDelayMs;

        var isCustom = !LaserSettings.FadeDelayPresetsMs.Contains(fadeDelayMs);

        _customFadeDelayItem.Checked = isCustom;
        _customFadeDelayItem.Text = isCustom ? UiText.CustomWithValue(FormatSeconds(fadeDelayMs)) : UiText.Custom;
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add(_armItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_arrowModeItem);
        menu.Items.Add(_freeModeItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_colorMenu);
        menu.Items.Add(BuildFadeDelayMenu());
        menu.Items.Add(BuildLanguageMenu());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Labeled(new ToolStripMenuItem(string.Empty, null, (_, _) => AboutDialog.Show()), () => UiText.About));
        menu.Items.Add(Labeled(new ToolStripMenuItem(string.Empty, null, (_, _) => _actions.Exit()), () => UiText.Exit));

        return menu;
    }

    private ToolStripMenuItem BuildColorMenu()
    {
        var colorMenu = Labeled(new ToolStripMenuItem(), () => UiText.Color);

        foreach (var preset in LaserSettings.ColorPresets)
        {
            var swatch = TrackSwatch(DrawSwatch(preset.Color));
            var item = Labeled(new ToolStripMenuItem(string.Empty, swatch, (_, _) => _actions.SelectColor(preset.Color)), preset.Name);

            _colorItems.Add((item, preset.Color));
            colorMenu.DropDownItems.Add(item);
        }

        colorMenu.DropDownItems.Add(new ToolStripSeparator());
        colorMenu.DropDownItems.Add(_customColorItem);

        return colorMenu;
    }

    private ToolStripMenuItem BuildFadeDelayMenu()
    {
        var fadeMenu = Labeled(new ToolStripMenuItem(), () => UiText.FadeAfter);

        foreach (var delayMs in LaserSettings.FadeDelayPresetsMs)
        {
            var label = FormatSeconds(delayMs);
            var item = new ToolStripMenuItem(label, null, (_, _) => _actions.SelectFadeDelay(delayMs));

            _fadeDelayItems.Add((item, delayMs));
            fadeMenu.DropDownItems.Add(item);
        }

        fadeMenu.DropDownItems.Add(new ToolStripSeparator());
        fadeMenu.DropDownItems.Add(_customFadeDelayItem);

        return fadeMenu;
    }

    private ToolStripMenuItem BuildLanguageMenu()
    {
        var languageMenu = Labeled(new ToolStripMenuItem(), () => UiText.LanguageMenu);

        languageMenu.DropDownItems.Add(_englishItem);
        languageMenu.DropDownItems.Add(_portugueseItem);

        return languageMenu;
    }

    private ToolStripMenuItem Labeled(ToolStripMenuItem item, Func<string> label)
    {
        _labeledItems.Add((item, label));
        return item;
    }

    private void PickCustomColor()
    {
        using var dialog = new ColorDialog
        {
            Color = _currentColor,
            FullOpen = true,
        };

        if (dialog.ShowDialog() == DialogResult.OK)
            _actions.SelectColor(Color.FromArgb(byte.MaxValue, dialog.Color));
    }

    private void PickCustomFadeDelay()
    {
        if (FadeDelayDialog.Ask(_currentFadeDelayMs) is { } delayMs)
            _actions.SelectFadeDelay(delayMs);
    }

    private Bitmap TrackSwatch(Bitmap swatch)
    {
        _swatches.Add(swatch);
        return swatch;
    }

    private static Bitmap DrawSwatch(Color color)
    {
        var swatch = new Bitmap(SwatchSizePx, SwatchSizePx);

        using (var graphics = Graphics.FromImage(swatch))
        using (var brush = new SolidBrush(color))
        {
            graphics.Clear(Color.Transparent);
            graphics.FillRectangle(brush, 1, 1, SwatchSizePx - 2, SwatchSizePx - 2);
            graphics.DrawRectangle(Pens.DimGray, 1, 1, SwatchSizePx - 3, SwatchSizePx - 3);
        }

        return swatch;
    }

    private static string FormatSeconds(int milliseconds)
    {
        var seconds = milliseconds / MsPerSecond;

        var label = $"{seconds.ToString("0.#", CultureInfo.InvariantCulture)} s";
        return label;
    }

    private static Icon LoadEmbeddedIcon(string resourceName)
    {
        using var stream = typeof(TrayIcon).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded icon '{resourceName}' is missing.");

        var icon = new Icon(stream, SystemInformation.SmallIconSize);
        return icon;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _activeIcon.Dispose();
        _inactiveIcon.Dispose();

        _colorMenu.Image?.Dispose();

        foreach (var swatch in _swatches)
            swatch.Dispose();
    }
}
