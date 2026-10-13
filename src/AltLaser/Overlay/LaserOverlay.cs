using AltLaser.Settings;
using AltLaser.Trail;
using static AltLaser.Interop.NativeMethods;

namespace AltLaser.Overlay;

internal sealed class LaserOverlay : Form
{
    private readonly record struct FrameSignature(
        Point? Head,
        int PointCount,
        byte Opacity);

    private const float MaxOpacityByte = byte.MaxValue;

    private readonly Func<bool> _isPenDown;
    private readonly LaserTrail _trail = new();
    private readonly System.Windows.Forms.Timer _frameTimer;

    private FrameSignature? _lastSignature;
    private Color _laserColor = LaserSettings.DefaultLaserColor;
    private bool _isPointing;

    public bool IsPointing => _isPointing;

    public LaserOverlay(Func<bool> isPenDown)
    {
        _isPenDown = isPenDown;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        TopMost = true;

        _frameTimer = new System.Windows.Forms.Timer { Interval = LaserSettings.FrameIntervalMs };
        _frameTimer.Tick += (_, _) => RenderNextFrame();
    }

    public void Configure(UserSettings settings)
    {
        _trail.Mode = settings.DrawingMode;
        _trail.FadeDelayMs = settings.FadeDelayMs;
        _laserColor = settings.LaserColor;
        _lastSignature = null;
    }

    public void StartPointing()
    {
        _isPointing = true;
        _lastSignature = null;

        RenderNextFrame();
        Show();
        _frameTimer.Start();
    }

    public void StopPointing()
    {
        _isPointing = false;
    }

    private void RenderNextFrame()
    {
        var nowMs = Environment.TickCount64;
        var isDrawing = _isPointing && _isPenDown();
        var head = isDrawing ? MousePosition : (Point?)null;

        _trail.RemoveExpired(nowMs);
        UpdateTrail(head, nowMs);

        if (!_isPointing && _trail.IsEmpty)
        {
            FinishFadeOut();
            return;
        }

        var signature = CreateSignature(head, nowMs);

        if (signature == _lastSignature)
            return;

        PresentFrame(head, nowMs);
        _lastSignature = signature;
    }

    private void UpdateTrail(Point? head, long nowMs)
    {
        if (head is not { } position)
        {
            _trail.EndStroke();
            return;
        }

        _trail.Add(position, nowMs);
    }

    private FrameSignature CreateSignature(Point? head, long nowMs)
    {
        var opacity = _trail.CalculateOpacity(nowMs);
        var opacityByte = (byte)MathF.Round(opacity * MaxOpacityByte);

        var signature = new FrameSignature(
            head,
            _trail.PointCount,
            opacityByte);

        return signature;
    }

    private void FinishFadeOut()
    {
        _frameTimer.Stop();
        Hide();
    }

    private void PresentFrame(Point? head, long nowMs)
    {
        using var frame = _trail.IsEmpty && head is null
            ? TrailRenderer.RenderEmpty()
            : TrailRenderer.Render(TrailGeometry.Build(_trail, head, nowMs), _laserColor);

        LayeredWindow.Present(Handle, frame);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;

            parameters.ExStyle |= WsExLayered
                | WsExTransparent
                | WsExToolWindow
                | WsExTopmost
                | WsExNoActivate;

            return parameters;
        }
    }

    protected override void WndProc(ref Message message)
    {
        var isDpiChange = message.Msg == WmDpiChanged;

        if (isDpiChange)
            return;

        base.WndProc(ref message);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _frameTimer.Dispose();

        base.Dispose(disposing);
    }
}
