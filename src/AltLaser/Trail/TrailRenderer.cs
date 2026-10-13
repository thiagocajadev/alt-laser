using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace AltLaser.Trail;

internal sealed record RenderedFrame(Bitmap Bitmap, Point ScreenOrigin) : IDisposable
{
    public void Dispose() => Bitmap.Dispose();
}

internal static class TrailRenderer
{
    private const int EmptyFrameSizePx = 1;

    public static RenderedFrame Render(FrameGeometry geometry, Color color)
    {
        var bounds = geometry.ScreenBounds;
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

        using (var graphics = CreateCanvas(bitmap, bounds.Location))
        {
            DrawTrailLayer(graphics, geometry, color);

            if (geometry.Head is { } head)
                DrawHead(graphics, head, color);
        }

        var frame = new RenderedFrame(bitmap, bounds.Location);
        return frame;
    }

    public static RenderedFrame RenderEmpty()
    {
        var bitmap = new Bitmap(EmptyFrameSizePx, EmptyFrameSizePx, PixelFormat.Format32bppArgb);

        var frame = new RenderedFrame(bitmap, Point.Empty);
        return frame;
    }

    private static Graphics CreateCanvas(Bitmap bitmap, Point screenOrigin)
    {
        var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.Clear(Color.Transparent);
        graphics.TranslateTransform(-screenOrigin.X, -screenOrigin.Y);

        return graphics;
    }

    private static void DrawTrailLayer(Graphics graphics, FrameGeometry geometry, Color color)
    {
        if (geometry.TrailOpacity >= TrailFade.FullyVisible)
        {
            DrawTrail(graphics, geometry, color);
            return;
        }

        var bounds = geometry.ScreenBounds;
        using var layer = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

        using (var layerGraphics = CreateCanvas(layer, bounds.Location))
            DrawTrail(layerGraphics, geometry, color);

        using var fadeAttributes = CreateOpacityAttributes(geometry.TrailOpacity);

        graphics.DrawImage(
            layer,
            bounds,
            0,
            0,
            bounds.Width,
            bounds.Height,
            GraphicsUnit.Pixel,
            fadeAttributes);
    }

    private static ImageAttributes CreateOpacityAttributes(float opacity)
    {
        var opacityMatrix = new ColorMatrix { Matrix33 = opacity };
        var attributes = new ImageAttributes();

        attributes.SetColorMatrix(opacityMatrix);
        return attributes;
    }

    private static void DrawTrail(Graphics graphics, FrameGeometry geometry, Color color)
    {
        using var pen = new Pen(color, LaserSettings.TrailWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };

        using var dotBrush = new SolidBrush(color);

        foreach (var stroke in geometry.Strokes)
            DrawStroke(graphics, pen, dotBrush, stroke);

        foreach (var arrow in geometry.Arrows)
            DrawArrow(graphics, pen, arrow);
    }

    private static void DrawArrow(Graphics graphics, Pen pen, ArrowShape arrow)
    {
        graphics.DrawLine(pen, arrow.Tail, arrow.Tip);
        graphics.DrawLines(pen, [arrow.LeftWing, arrow.Tip, arrow.RightWing]);
    }

    private static void DrawStroke(
        Graphics graphics,
        Pen pen,
        Brush dotBrush,
        Point[] stroke)
    {
        if (stroke.Length == 1)
        {
            graphics.FillEllipse(dotBrush, CircleAround(stroke[0], LaserSettings.TrailWidth / 2));
            return;
        }

        graphics.DrawCurve(pen, stroke, LaserSettings.TrailCurveTension);
    }

    private static void DrawHead(Graphics graphics, Point head, Color color)
    {
        var glowColor = Color.FromArgb(LaserSettings.HeadGlowAlpha, color);

        using var glowBrush = new SolidBrush(glowColor);
        using var coreBrush = new SolidBrush(color);

        graphics.FillEllipse(glowBrush, CircleAround(head, LaserSettings.HeadGlowRadius));
        graphics.FillEllipse(coreBrush, CircleAround(head, LaserSettings.HeadRadius));
    }

    private static RectangleF CircleAround(Point center, float radius)
    {
        var diameter = radius * 2;

        var circle = new RectangleF(center.X - radius, center.Y - radius, diameter, diameter);
        return circle;
    }
}
