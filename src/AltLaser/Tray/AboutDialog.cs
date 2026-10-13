using System.Diagnostics;
using AltLaser.Localization;

namespace AltLaser.Tray;

internal static class AboutDialog
{
    private const string AuthorHandle = "@thiagocajadev";
    private const string AuthorUrl = "https://github.com/thiagocajadev";
    private const string RepositoryUrl = "https://github.com/thiagocajadev/alt-laser";
    private const int VersionFieldCount = 3;

    private static bool _isOpen;

    public static void Show()
    {
        if (_isOpen)
            return;

        _isOpen = true;

        try
        {
            using var appIcon = ExtractAppIcon();

            TaskDialog.ShowDialog(CreatePage(appIcon), TaskDialogStartupLocation.CenterScreen);
        }
        finally
        {
            _isOpen = false;
        }
    }

    private static TaskDialogPage CreatePage(Icon? appIcon)
    {
        var page = new TaskDialogPage
        {
            Caption = UiText.About,
            Heading = $"{UiText.AppName} {Version}",
            Text = $"{UiText.AboutTagline}\n\n"
                + $"{UiText.MadeBy} <a href=\"{AuthorUrl}\">{AuthorHandle}</a>\n"
                + $"<a href=\"{RepositoryUrl}\">{RepositoryUrl.Replace("https://", string.Empty)}</a>",
            EnableLinks = true,
            Icon = appIcon is null ? TaskDialogIcon.Information : new TaskDialogIcon(appIcon),
            Buttons = { TaskDialogButton.OK },
        };

        page.LinkClicked += (_, args) => OpenInBrowser(args.LinkHref);

        return page;
    }

    private static Icon? ExtractAppIcon() =>
        Environment.ProcessPath is { } exePath ? Icon.ExtractAssociatedIcon(exePath) : null;

    private static string Version =>
        typeof(AboutDialog).Assembly.GetName().Version?.ToString(VersionFieldCount) ?? string.Empty;

    private static void OpenInBrowser(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();
}
