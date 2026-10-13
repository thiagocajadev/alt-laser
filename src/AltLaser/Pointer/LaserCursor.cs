using AltLaser.Localization;
using static AltLaser.Interop.NativeMethods;

namespace AltLaser.Pointer;

internal sealed class LaserCursor : IDisposable
{
    private const float BaseDpi = 96f;

    private static readonly uint[] ReplacedCursorIds =
    [
        OcrNormal,
        OcrIBeam,
        OcrHand,
    ];

    private readonly IntPtr _cursorHandle;

    public static Result<LaserCursor> Create(Color color)
    {
        var scale = GetDpiForSystem() / BaseDpi;

        using var image = LaserCursorArt.Draw(scale, color);
        var cursorHandle = CreateCursorHandle(image);

        if (cursorHandle == IntPtr.Zero)
            return Result<LaserCursor>.Failure(UiText.CursorFailed);

        var cursor = Result<LaserCursor>.Success(new LaserCursor(cursorHandle));
        return cursor;
    }

    public static void RestoreSystemCursors() =>
        SystemParametersInfo(SpiSetCursors, 0, IntPtr.Zero, 0);

    private LaserCursor(IntPtr cursorHandle)
    {
        _cursorHandle = cursorHandle;
    }

    public void ApplyToSystem()
    {
        foreach (var cursorId in ReplacedCursorIds)
            SetSystemCursor(CopyIcon(_cursorHandle), cursorId);
    }

    private static IntPtr CreateCursorHandle(CursorImage image)
    {
        var iconHandle = image.Bitmap.GetHicon();

        if (!GetIconInfo(iconHandle, out var iconInfo))
        {
            DestroyIcon(iconHandle);
            return IntPtr.Zero;
        }

        iconInfo.IsIcon = false;
        iconInfo.HotspotX = image.Hotspot.X;
        iconInfo.HotspotY = image.Hotspot.Y;

        var cursorHandle = CreateIconIndirect(ref iconInfo);

        DeleteObject(iconInfo.MaskBitmap);
        DeleteObject(iconInfo.ColorBitmap);
        DestroyIcon(iconHandle);

        return cursorHandle;
    }

    public void Dispose() => DestroyIcon(_cursorHandle);
}
