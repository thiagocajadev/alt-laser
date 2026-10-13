using AltLaser.Localization;

namespace AltLaser.Tray;

// Asks for a custom fade delay in seconds. The NumericUpDown only takes positive values with
// one decimal place inside the allowed range, so nothing else can reach the settings.
internal sealed class FadeDelayDialog : Form
{
    private const decimal MsPerSecond = 1000m;
    private const int DecimalPlaces = 1;
    private const decimal StepSeconds = 0.1m;
    private const char DecimalPoint = '.';
    private const char DecimalComma = ',';
    private const char MinusSign = '-';

    private static readonly decimal MinSeconds = LaserSettings.MinFadeDelayMs / MsPerSecond;
    private static readonly decimal MaxSeconds = LaserSettings.MaxFadeDelayMs / MsPerSecond;

    private readonly NumericUpDown _secondsInput;

    public static int? Ask(int currentDelayMs)
    {
        using var dialog = new FadeDelayDialog(currentDelayMs);

        var delayMs = dialog.ShowDialog() == DialogResult.OK ? dialog.DelayMs : (int?)null;
        return delayMs;
    }

    private FadeDelayDialog(int currentDelayMs)
    {
        Text = UiText.FadeAfter;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        _secondsInput = new NumericUpDown
        {
            Minimum = MinSeconds,
            Maximum = MaxSeconds,
            DecimalPlaces = DecimalPlaces,
            Increment = StepSeconds,
            Value = Math.Clamp(currentDelayMs / MsPerSecond, MinSeconds, MaxSeconds),
            Width = 80,
            TextAlign = HorizontalAlignment.Right,
        };

        _secondsInput.KeyPress += (_, args) => FilterKey(args);

        var okButton = new Button { Text = UiText.Ok, DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = UiText.Cancel, DialogResult = DialogResult.Cancel };

        AcceptButton = okButton;
        CancelButton = cancelButton;

        Controls.Add(BuildLayout(okButton, cancelButton));
    }

    private int DelayMs => (int)(Math.Round(_secondsInput.Value, DecimalPlaces) * MsPerSecond);

    private Control BuildLayout(Button okButton, Button cancelButton)
    {
        var prompt = new Label
        {
            Text = UiText.FadePrompt($"{MinSeconds:0.0}", $"{MaxSeconds:0}"),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
        };

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 0),
        };

        buttons.Controls.AddRange([cancelButton, okButton]);

        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };

        layout.Controls.Add(prompt);
        layout.Controls.Add(_secondsInput);
        layout.Controls.Add(buttons);

        return layout;
    }

    // Typing a comma means a decimal point here, and a minus sign is never valid.
    private static void FilterKey(KeyPressEventArgs args)
    {
        if (args.KeyChar == DecimalComma)
            args.KeyChar = DecimalPoint;

        if (args.KeyChar == MinusSign)
            args.Handled = true;
    }
}
