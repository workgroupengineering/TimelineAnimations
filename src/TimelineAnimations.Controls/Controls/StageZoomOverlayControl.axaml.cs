using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TimelineAnimations.App.Controls;

public partial class StageZoomOverlayControl : UserControl
{
    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<StageZoomOverlayControl, double>(nameof(Zoom), 1d, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<StageZoomOverlayControl, double>(nameof(Minimum), 0.25d);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<StageZoomOverlayControl, double>(nameof(Maximum), 8d);

    public static readonly StyledProperty<string> ZoomLabelProperty =
        AvaloniaProperty.Register<StageZoomOverlayControl, string>(nameof(ZoomLabel), "100%");

    public event EventHandler? FitRequested;

    public event EventHandler? ActualSizeRequested;

    public StageZoomOverlayControl()
    {
        InitializeComponent();
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public string ZoomLabel
    {
        get => GetValue(ZoomLabelProperty);
        set => SetValue(ZoomLabelProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void HandleFitClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void HandleActualSizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ActualSizeRequested?.Invoke(this, EventArgs.Empty);
    }

    private void HandlePresetClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag } ||
            !double.TryParse(tag, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return;
        }

        Zoom = Math.Clamp(value, Minimum, Maximum);
    }
}
