using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using TimelineAnimations.App.Helpers;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.ViewModels;

public partial class LayerViewModel : ViewModelBase
{
    public LayerViewModel(TimelineLayer model)
    {
        Model = model;
        fillBrush = ColorHelpers.Brush(model.Style.Fill, "#FFFFFF");
        strokeBrush = ColorHelpers.Brush(model.Style.Stroke, "#FFFFFF");
        RefreshMetadata();
        UpdatePreview(0);
    }

    public TimelineLayer Model { get; }

    public Guid Id => Model.Id;

    public LayerKind Kind => Model.Kind;

    public string KindLabel => Kind switch
    {
        LayerKind.Rectangle => "Card",
        LayerKind.Ellipse => "Orb",
        LayerKind.Text => "Caption",
        _ => "Layer"
    };

    public string Subtitle => $"{KindLabel} • {Math.Round(Width):0}×{Math.Round(Height):0}";

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private bool isVisible;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private int zIndex;

    [ObservableProperty]
    private string fillHex = "#FFFFFF";

    [ObservableProperty]
    private string strokeHex = "#FFFFFF";

    [ObservableProperty]
    private string textContent = string.Empty;

    [ObservableProperty]
    private double fontSize;

    [ObservableProperty]
    private double cornerRadius;

    [ObservableProperty]
    private double x;

    [ObservableProperty]
    private double y;

    [ObservableProperty]
    private double width;

    [ObservableProperty]
    private double height;

    [ObservableProperty]
    private double rotation;

    [ObservableProperty]
    private double opacity;

    [ObservableProperty]
    private SolidColorBrush fillBrush;

    [ObservableProperty]
    private SolidColorBrush strokeBrush;

    public void RefreshMetadata()
    {
        Name = Model.Name;
        IsVisible = Model.IsVisible;
        ZIndex = Model.ZIndex;
        FillHex = Model.Style.Fill;
        StrokeHex = Model.Style.Stroke;
        TextContent = Model.Style.Text;
        FontSize = Model.Style.FontSize;
        CornerRadius = Model.Style.CornerRadius;
        FillBrush = ColorHelpers.Brush(FillHex, "#FFFFFF");
        StrokeBrush = ColorHelpers.Brush(StrokeHex, "#FFFFFF");
        OnPropertyChanged(nameof(KindLabel));
        OnPropertyChanged(nameof(Subtitle));
    }

    public void UpdatePreview(double time)
    {
        var snapshot = TimelineInterpolationService.SampleLayer(Model, time);
        X = snapshot.X;
        Y = snapshot.Y;
        Width = snapshot.Width;
        Height = snapshot.Height;
        Rotation = snapshot.Rotation;
        Opacity = snapshot.Opacity;
        CornerRadius = snapshot.CornerRadius;
        TextContent = snapshot.Text;
        FontSize = snapshot.FontSize;
        OnPropertyChanged(nameof(Subtitle));
    }
}
