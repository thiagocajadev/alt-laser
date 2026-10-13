using AltLaser.Trail;
using static AltLaser.Interop.NativeMethods;

namespace AltLaser.Overlay;

internal static class LayeredWindow
{
    private static readonly Color TransparentBackground = Color.FromArgb(0);

    public static void Present(IntPtr windowHandle, RenderedFrame frame)
    {
        var screenDc = GetDC(IntPtr.Zero);
        var memoryDc = CreateCompatibleDC(screenDc);
        var bitmapHandle = frame.Bitmap.GetHbitmap(TransparentBackground);
        var previousBitmap = SelectObject(memoryDc, bitmapHandle);

        try
        {
            UpdateWindowPixels(windowHandle, screenDc, memoryDc, frame);
        }
        finally
        {
            SelectObject(memoryDc, previousBitmap);
            DeleteObject(bitmapHandle);
            DeleteDC(memoryDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private static void UpdateWindowPixels(
        IntPtr windowHandle,
        IntPtr screenDc,
        IntPtr memoryDc,
        RenderedFrame frame)
    {
        var destination = new NativePoint(frame.ScreenOrigin.X, frame.ScreenOrigin.Y);
        var size = new NativeSize(frame.Bitmap.Width, frame.Bitmap.Height);
        var sourceOrigin = new NativePoint(0, 0);

        var blend = new BlendFunction
        {
            BlendOp = AcSrcOver,
            SourceConstantAlpha = byte.MaxValue,
            AlphaFormat = AcSrcAlpha,
        };

        UpdateLayeredWindow(
            windowHandle,
            screenDc,
            ref destination,
            ref size,
            memoryDc,
            ref sourceOrigin,
            crKey: 0,
            ref blend,
            UlwAlpha);
    }
}
