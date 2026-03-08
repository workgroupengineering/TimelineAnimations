using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TimelineAnimations.App.Controls;

public partial class TimelineStatChipControl : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<TimelineStatChipControl, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<TimelineStatChipControl, string>(nameof(Value), string.Empty);

    public static readonly StyledProperty<string> SummaryProperty =
        AvaloniaProperty.Register<TimelineStatChipControl, string>(nameof(Summary), string.Empty);

    public static readonly StyledProperty<double> MinChipWidthProperty =
        AvaloniaProperty.Register<TimelineStatChipControl, double>(nameof(MinChipWidth), 96d);

    public TimelineStatChipControl()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Summary
    {
        get => GetValue(SummaryProperty);
        set => SetValue(SummaryProperty, value);
    }

    public double MinChipWidth
    {
        get => GetValue(MinChipWidthProperty);
        set => SetValue(MinChipWidthProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
