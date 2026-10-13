namespace AltLaser.Trail;

internal static class TrailFade
{
    public const float FullyVisible = 1f;
    public const float FullyFaded = 0f;

    public static float CalculateOpacity(long lastPointMs, long nowMs, int fadeDelayMs)
    {
        var idleMs = nowMs - lastPointMs;
        var fadingMs = idleMs - fadeDelayMs;
        var fadeProgress = (float)fadingMs / LaserSettings.TrailFadeOutMs;

        var opacity = Math.Clamp(FullyVisible - fadeProgress, FullyFaded, FullyVisible);
        return opacity;
    }
}
