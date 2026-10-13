using AltLaser.Input;
using AltLaser.Localization;
using AltLaser.Overlay;
using AltLaser.Pointer;
using AltLaser.Settings;
using AltLaser.Tray;

namespace AltLaser;

internal sealed class AltLaserApp : ApplicationContext
{
    private const string SingleInstanceMutexName = @"Local\AltLaser.SingleInstance";

    private readonly LeftButtonCapture _leftButton;
    private readonly LaserOverlay _overlay;
    private readonly TrayIcon _trayIcon;
    private readonly AltHoldDetector _altHold;
    private readonly GlobalHotkey? _armHotkey;

    private LaserCursor? _laserCursor;
    private UserSettings _settings;
    private bool _isArmed = true;

    public static void Run()
    {
        using var instanceLock = new Mutex(
            initiallyOwned: true,
            SingleInstanceMutexName,
            out var isFirstInstance);

        if (!isFirstInstance)
            return;

        GuardSystemCursors();
        ApplicationConfiguration.Initialize();

        using var app = new AltLaserApp();
        Application.Run(app);
    }

    private static void GuardSystemCursors()
    {
        LaserCursor.RestoreSystemCursors();

        AppDomain.CurrentDomain.ProcessExit += (_, _) => LaserCursor.RestoreSystemCursors();
        AppDomain.CurrentDomain.UnhandledException += (_, _) => LaserCursor.RestoreSystemCursors();
    }

    private AltLaserApp()
    {
        _settings = UserSettingsStore.Load();
        UiText.Language = _settings.Language;

        _leftButton = new LeftButtonCapture();
        _overlay = new LaserOverlay(isPenDown: IsPenDown);
        _altHold = new AltHoldDetector(onHoldStarted: OnAltHoldStarted, onHoldEnded: OnAltHoldEnded);

        _trayIcon = new TrayIcon(new TrayActions(
            Toggle: ToggleArmed,
            SelectMode: mode => ChangeSettings(_settings with { DrawingMode = mode }),
            SelectColor: color => ChangeSettings(_settings with { LaserColor = color }),
            SelectFadeDelay: delayMs => ChangeSettings(_settings with { FadeDelayMs = delayMs }),
            SelectLanguage: language => ChangeSettings(_settings with { Language = language }),
            Exit: ExitThread));

        _overlay.Configure(_settings);
        ApplyMode();
        _trayIcon.ShowStartupNotice();

        _laserCursor = CreateLaserCursor();
        _armHotkey = RegisterArmHotkey();
    }

    private LaserCursor? CreateLaserCursor()
    {
        if (!LaserSettings.ReplaceSystemCursor)
            return null;

        var creation = LaserCursor.Create(_settings.LaserColor);

        if (!creation.IsSuccess)
            _trayIcon.ShowWarning(creation.Error!);

        return creation.Value;
    }

    private GlobalHotkey? RegisterArmHotkey()
    {
        var registration = GlobalHotkey.Register(LaserSettings.ToggleHotkey, onPressed: ToggleArmed);

        if (!registration.IsSuccess)
            _trayIcon.ShowWarning(registration.Error!);

        return registration.Value;
    }

    private bool IsPenDown()
    {
        if (!LaserSettings.RequireClickToDraw)
            return true;

        return _leftButton.IsPressed;
    }

    private void ToggleArmed()
    {
        _isArmed = !_isArmed;

        ApplyMode();
    }

    private void ChangeSettings(UserSettings settings)
    {
        var hasNewColor = !_settings.HasColor(settings.LaserColor);

        _settings = settings;
        UiText.Language = settings.Language;
        _overlay.Configure(settings);

        if (hasNewColor)
            RecreateLaserCursor();

        RefreshTray();
        UserSettingsStore.Save(settings);
    }

    private void RecreateLaserCursor()
    {
        var previousCursor = _laserCursor;

        _laserCursor = CreateLaserCursor();
        previousCursor?.Dispose();
    }

    private void ApplyMode()
    {
        RefreshTray();

        if (!_isArmed)
        {
            _altHold.Stop();
            HideLaser();
            return;
        }

        _altHold.Start();
    }

    private void OnAltHoldStarted()
    {
        if (!_isArmed)
            return;

        ShowLaser();
    }

    private void OnAltHoldEnded() => HideLaser();

    private void ShowLaser()
    {
        if (_overlay.IsPointing)
            return;

        if (LaserSettings.RequireClickToDraw)
            _leftButton.Start();

        _overlay.StartPointing();
        _laserCursor?.ApplyToSystem();
    }

    private void HideLaser()
    {
        if (!_overlay.IsPointing)
            return;

        _overlay.StopPointing();
        _leftButton.Stop();
        LaserCursor.RestoreSystemCursors();
    }

    private void RefreshTray() => _trayIcon.ShowState(_isArmed, _settings);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            LaserCursor.RestoreSystemCursors();

            _armHotkey?.Dispose();
            _altHold.Dispose();
            _laserCursor?.Dispose();
            _trayIcon.Dispose();
            _overlay.Dispose();
            _leftButton.Dispose();
        }

        base.Dispose(disposing);
    }
}
