namespace TimelineAnimations.Core.Models;

public sealed class AvaloniaControlSettings
{
    public AvaloniaControlKind Kind { get; set; } = AvaloniaControlKind.Border;

    public string Content { get; set; } = string.Empty;

    public string SecondaryContent { get; set; } = string.Empty;

    public bool IsChecked { get; set; }

    public double Value { get; set; } = 50d;

    public double Minimum { get; set; }

    public double Maximum { get; set; } = 100d;

    public bool IsIndeterminate { get; set; }

    public string Source { get; set; } = string.Empty;

    public AvaloniaControlSettings Clone()
    {
        return new AvaloniaControlSettings
        {
            Kind = Kind,
            Content = Content,
            SecondaryContent = SecondaryContent,
            IsChecked = IsChecked,
            Value = Value,
            Minimum = Minimum,
            Maximum = Maximum,
            IsIndeterminate = IsIndeterminate,
            Source = Source
        };
    }
}
