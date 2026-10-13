using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace AltLaser.Pointer;

internal sealed record CursorImage(Bitmap Bitmap, Point Hotspot) : IDisposable
{
    public void Dispose() => Bitmap.Dispose();
}

internal static class LaserCursorArt
{
    private const float DesignSizePx = 32f;
    private const float TipOffset = 3f;
    private const float PenAngleDegrees = 45f;

    private const float BodyWidth = 8f;
    private const float BodyRadius = BodyWidth / 2;
    private const float BodyLength = 26f;
    private const float OuterOutlineWidth = 2.2f;
    private const float InnerOutlineWidth = 1.1f;

    private const float EmitterRadius = 1.3f;
    private const float LedCenter = 7f;
    private const float ButtonCenter = 13f;
    private const float ButtonRadius = 2f;
    private const float ButtonRimWidth = 0.8f;
    private const float LedWidth = 2.8f;
    private const float LedHeight = 2f;
    private const float HighlightWidth = 1.6f;
    private const float HighlightFrontInset = 2f;
    private const float HighlightBackInset = 3f;

    private static readonly Color HaloColor = Color.White;
    private static readonly Color OutlineColor = Color.FromArgb(255, 27, 34, 48);
    private static readonly Color BodyColor = Color.FromArgb(255, 62, 70, 84);
    private static readonly Color HighlightColor = Color.FromArgb(150, 150, 160, 178);
    private static readonly Color ButtonColor = Color.FromArgb(255, 38, 44, 56);
    private static readonly Color ButtonRimColor = Color.FromArgb(255, 150, 160, 175);

    public static CursorImage Draw(float scale, Color laserColor)
    {
        var sizePx = (int)MathF.Round(DesignSizePx * scale);
        var bitmap = new Bitmap(sizePx, sizePx, PixelFormat.Format32bppArgb);

        using (var graphics = CreatePointerCanvas(bitmap, scale))
        {
            DrawBody(graphics);
            DrawHighlight(graphics);
            DrawLed(graphics, laserColor);
            DrawButton(graphics);
            DrawEmitter(graphics, laserColor);
        }

        var hotspot = new Point(
            (int)MathF.Round(TipOffset * scale),
            (int)MathF.Round(TipOffset * scale));

        var image = new CursorImage(bitmap, hotspot);
        return image;
    }

    private static Graphics CreatePointerCanvas(Bitmap bitmap, float scale)
    {
        var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        graphics.ScaleTransform(scale, scale);
        graphics.TranslateTransform(TipOffset, TipOffset);
        graphics.RotateTransform(PenAngleDegrees);

        return graphics;
    }

    private static void DrawBody(Graphics graphics)
    {
        DrawCapsule(graphics, HaloColor, BodyWidth + OuterOutlineWidth * 2);
        DrawCapsule(graphics, OutlineColor, BodyWidth + InnerOutlineWidth * 2);
        DrawCapsule(graphics, BodyColor, BodyWidth);
    }

    private static void DrawCapsule(Graphics graphics, Color color, float width)
    {
        using var pen = new Pen(color, width)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };

        graphics.DrawLine(pen, BodyRadius, 0, BodyLength - BodyRadius, 0);
    }

    private static void DrawHighlight(Graphics graphics)
    {
        using var pen = new Pen(HighlightColor, HighlightWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };

        var highlightY = -BodyRadius / 2;
        var start = BodyRadius + HighlightFrontInset;
        var end = BodyLength - BodyRadius - HighlightBackInset;

        graphics.DrawLine(pen, start, highlightY, end, highlightY);
    }

    private static void DrawLed(Graphics graphics, Color laserColor)
    {
        using var brush = new SolidBrush(laserColor);

        var led = new RectangleF(
            LedCenter - LedWidth / 2,
            -LedHeight / 2,
            LedWidth,
            LedHeight);

        graphics.FillEllipse(brush, led);
    }

    private static void DrawButton(Graphics graphics)
    {
        using var brush = new SolidBrush(ButtonColor);
        using var rim = new Pen(ButtonRimColor, ButtonRimWidth);

        var button = CircleAround(ButtonCenter, 0, ButtonRadius);

        graphics.FillEllipse(brush, button);
        graphics.DrawEllipse(rim, button);
    }

    private static void DrawEmitter(Graphics graphics, Color laserColor)
    {
        using var brush = new SolidBrush(laserColor);

        graphics.FillEllipse(brush, CircleAround(0, 0, EmitterRadius));
    }

    private static RectangleF CircleAround(float x, float y, float radius)
    {
        var diameter = radius * 2;

        var circle = new RectangleF(x - radius, y - radius, diameter, diameter);
        return circle;
    }
}
