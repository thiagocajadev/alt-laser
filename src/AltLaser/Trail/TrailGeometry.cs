namespace AltLaser.Trail;

internal sealed record ArrowShape(Point Tail, Point Tip, Point LeftWing, Point RightWing);

internal sealed record FrameGeometry(
    IReadOnlyList<Point[]> Strokes,
    IReadOnlyList<ArrowShape> Arrows,
    float TrailOpacity,
    Point? Head,
    Rectangle ScreenBounds);

internal static class TrailGeometry
{
    private const float MaxHeadShareOfArrow = 0.5f;

    private static readonly int BoundsPaddingPx = (int)MathF.Ceiling(LaserSettings.HeadGlowRadius) + 2;
    private static readonly float ArrowHeadAngleRadians = LaserSettings.ArrowHeadAngleDegrees * MathF.PI / 180f;

    public static FrameGeometry Build(LaserTrail trail, Point? head, long nowMs)
    {
        var strokes = BuildStrokes(trail, head);
        var arrows = BuildArrows(trail);
        var opacity = trail.CalculateOpacity(nowMs) * LaserSettings.TrailOpacity;
        var bounds = CalculateBounds(strokes, arrows, head);

        var geometry = new FrameGeometry(strokes, arrows, opacity, head, bounds);
        return geometry;
    }

    private static List<Point[]> BuildStrokes(LaserTrail trail, Point? head)
    {
        var strokes = trail.Strokes
            .Where(stroke => stroke.Mode == DrawingMode.Free)
            .Select(stroke => stroke.Points.Select(point => point.Position).ToList())
            .ToList();

        var canExtendLastStroke = trail.IsStrokeOpen && trail.Strokes[^1].Mode == DrawingMode.Free;

        if (head is { } headPosition && canExtendLastStroke && strokes[^1][^1] != headPosition)
            strokes[^1].Add(headPosition);

        var strokeArrays = strokes.Select(stroke => stroke.ToArray()).ToList();
        return strokeArrays;
    }

    private static List<ArrowShape> BuildArrows(LaserTrail trail)
    {
        var arrows = trail.Strokes
            .Where(stroke => stroke.Mode == DrawingMode.Arrow && stroke.Points.Count == 2)
            .Select(stroke => BuildArrow(stroke.Points[0].Position, stroke.Points[1].Position))
            .ToList();

        return arrows;
    }

    private static ArrowShape BuildArrow(Point tail, Point tip)
    {
        var deltaX = tip.X - tail.X;
        var deltaY = tip.Y - tail.Y;
        var length = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
        var direction = MathF.Atan2(deltaY, deltaX);
        var headLength = MathF.Min(LaserSettings.ArrowHeadLength, length * MaxHeadShareOfArrow);

        var leftWing = PointBehind(tip, direction - ArrowHeadAngleRadians, headLength);
        var rightWing = PointBehind(tip, direction + ArrowHeadAngleRadians, headLength);

        var arrow = new ArrowShape(tail, tip, leftWing, rightWing);
        return arrow;
    }

    private static Point PointBehind(Point tip, float angle, float distance)
    {
        var point = new Point(
            (int)MathF.Round(tip.X - distance * MathF.Cos(angle)),
            (int)MathF.Round(tip.Y - distance * MathF.Sin(angle)));

        return point;
    }

    private static Rectangle CalculateBounds(List<Point[]> strokes, List<ArrowShape> arrows, Point? head)
    {
        var positions = strokes.SelectMany(stroke => stroke).ToList();

        foreach (var arrow in arrows)
            positions.AddRange([arrow.Tail, arrow.Tip, arrow.LeftWing, arrow.RightWing]);

        if (head is { } headPosition)
            positions.Add(headPosition);

        var bounds = Rectangle.FromLTRB(
            positions.Min(position => position.X),
            positions.Min(position => position.Y),
            positions.Max(position => position.X),
            positions.Max(position => position.Y));

        bounds.Inflate(BoundsPaddingPx, BoundsPaddingPx);
        return bounds;
    }
}
