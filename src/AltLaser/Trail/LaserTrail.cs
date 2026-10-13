namespace AltLaser.Trail;

internal readonly record struct TrailPoint(Point Position, long TimestampMs);

internal sealed record TrailStroke(DrawingMode Mode, List<TrailPoint> Points);

internal sealed class LaserTrail
{
    private const int ArrowTailIndex = 0;

    private readonly List<TrailStroke> _strokes = [];

    private DrawingMode _mode = LaserSettings.DefaultDrawingMode;

    public IReadOnlyList<TrailStroke> Strokes => _strokes;

    public bool IsStrokeOpen { get; private set; }

    public bool IsEmpty => _strokes.Count == 0;

    public int PointCount => _strokes.Sum(stroke => stroke.Points.Count);

    public int FadeDelayMs { get; set; } = LaserSettings.DefaultFadeDelayMs;

    public DrawingMode Mode
    {
        get => _mode;
        set
        {
            EndStroke();
            _mode = value;
        }
    }

    public void Add(Point position, long nowMs)
    {
        if (_mode == DrawingMode.Arrow)
        {
            AddArrowTip(position, nowMs);
            return;
        }

        AddFreePoint(position, nowMs);
    }

    public void EndStroke()
    {
        var isUnfinishedArrow = IsStrokeOpen && _strokes[^1] is { Mode: DrawingMode.Arrow, Points.Count: < 2 };

        if (isUnfinishedArrow)
            _strokes.RemoveAt(_strokes.Count - 1);

        IsStrokeOpen = false;
    }

    public float CalculateOpacity(long nowMs)
    {
        if (IsEmpty)
            return TrailFade.FullyVisible;

        var opacity = TrailFade.CalculateOpacity(LastPoint.TimestampMs, nowMs, FadeDelayMs);
        return opacity;
    }

    public void RemoveExpired(long nowMs)
    {
        if (CalculateOpacity(nowMs) <= TrailFade.FullyFaded)
        {
            Clear();
            return;
        }

        // A long fade delay must not be cut short by trimming the oldest points first.
        var maxAgeMs = Math.Max(LaserSettings.TrailMaxAgeMs, FadeDelayMs + LaserSettings.TrailFadeOutMs);
        var oldestAllowedMs = nowMs - maxAgeMs;

        foreach (var stroke in _strokes)
            stroke.Points.RemoveAll(point => point.TimestampMs < oldestAllowedMs);

        _strokes.RemoveAll(stroke => stroke.Points.Count == 0);
        IsStrokeOpen = IsStrokeOpen && !IsEmpty;
    }

    public void Clear()
    {
        _strokes.Clear();
        IsStrokeOpen = false;
    }

    private List<TrailPoint> OpenPoints => _strokes[^1].Points;

    private TrailPoint LastPoint => _strokes[^1].Points[^1];

    private void AddFreePoint(Point position, long nowMs)
    {
        if (IsStrokeOpen && IsTooClose(LastPoint.Position, position))
            return;

        if (!IsStrokeOpen || HasPausedSinceLastPoint(nowMs))
            StartStroke();

        OpenPoints.Add(new TrailPoint(position, nowMs));
    }

    // An arrow is just its tail and tip. Both are re-stamped every frame, so the arrow stays
    // fully visible while the pen is down and only starts fading once it lifts.
    private void AddArrowTip(Point position, long nowMs)
    {
        if (!IsStrokeOpen)
        {
            StartStroke();
            OpenPoints.Add(new TrailPoint(position, nowMs));
            return;
        }

        var tail = OpenPoints[ArrowTailIndex] with { TimestampMs = nowMs };

        OpenPoints.Clear();
        OpenPoints.Add(tail);

        if (!IsTooClose(tail.Position, position))
            OpenPoints.Add(new TrailPoint(position, nowMs));
    }

    private void StartStroke()
    {
        _strokes.Add(new TrailStroke(_mode, []));
        IsStrokeOpen = true;
    }

    private bool HasPausedSinceLastPoint(long nowMs)
    {
        var idleMs = nowMs - LastPoint.TimestampMs;

        var hasPaused = idleMs >= LaserSettings.StrokePauseMs;
        return hasPaused;
    }

    private static bool IsTooClose(Point from, Point to)
    {
        var deltaX = to.X - from.X;
        var deltaY = to.Y - from.Y;
        var minDistanceSquared = LaserSettings.MinPointDistancePx * LaserSettings.MinPointDistancePx;

        var isTooClose = deltaX * deltaX + deltaY * deltaY < minDistanceSquared;
        return isTooClose;
    }
}
