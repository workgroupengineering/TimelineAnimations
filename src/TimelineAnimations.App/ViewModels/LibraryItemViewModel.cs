using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using TimelineAnimations.App.Helpers;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public partial class LibraryItemViewModel : ViewModelBase
{
    public LibraryItemViewModel(LibraryItem model)
    {
        Model = model;
        previewBrush = ColorHelpers.Brush(model.Template.Style.Fill, "#FFFFFF");
        RefreshMetadata();
    }

    public LibraryItem Model { get; }

    public Guid Id => Model.Id;

    public string SymbolKindLabel => Model.SymbolKind switch
    {
        SymbolKind.Graphic => "Graphic",
        SymbolKind.MovieClip => "Movie Clip",
        SymbolKind.Button => "Button",
        _ => Model.SymbolKind.ToString()
    };

    public string AssetKindLabel => Model.Template.Kind switch
    {
        LayerKind.Rectangle => "Vector Card",
        LayerKind.Ellipse => "Vector Orb",
        LayerKind.Text => "Text Symbol",
        LayerKind.Path => "Vector Path",
        _ => Model.Template.Kind.ToString()
    };

    public bool IsComponent => Model.IsComponent;

    public string ComponentBadge => IsComponent
        ? string.IsNullOrWhiteSpace(Model.ComponentCategory) ? "Component" : Model.ComponentCategory
        : string.Empty;

    public string TimelineLabel => $"{GetPrimaryLayerCount(Model)} layers • {Model.Duration:0.##} s • {Model.FrameRate:0.#} fps";

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string subtitle = string.Empty;

    [ObservableProperty]
    private SolidColorBrush previewBrush;

    public void RefreshMetadata()
    {
        Name = Model.Name;
        Subtitle = BuildSubtitle(Model.Template);
        var previewFill = Model.Template.Kind == LayerKind.Path
            ? Model.Template.Style.Stroke
            : Model.Template.Style.UseGradient
                ? Model.Template.Style.GradientFrom
                : Model.Template.Style.Fill;
        PreviewBrush = ColorHelpers.Brush(previewFill, "#FFFFFF");
        OnPropertyChanged(nameof(SymbolKindLabel));
        OnPropertyChanged(nameof(AssetKindLabel));
        OnPropertyChanged(nameof(IsComponent));
        OnPropertyChanged(nameof(ComponentBadge));
        OnPropertyChanged(nameof(TimelineLabel));
    }

    private static string BuildSubtitle(TimelineLayer template)
    {
        if (template.Kind == LayerKind.Text && !string.IsNullOrWhiteSpace(template.Style.Text))
        {
            var text = template.Style.Text.Length > 24
                ? $"{template.Style.Text[..24]}..."
                : template.Style.Text;
            return $"Text • {text}";
        }

        if (template.Kind == LayerKind.Path)
        {
            return $"Path • {template.Style.PathPoints.Count} points • {template.Tracks.Count} tracks";
        }

        return $"{Math.Round(template.Defaults.Width):0}×{Math.Round(template.Defaults.Height):0} • {template.Tracks.Count} tracks";
    }

    private static int GetPrimaryLayerCount(LibraryItem item)
    {
        return item.SymbolKind == SymbolKind.Button
            ? item.ButtonStates.FirstOrDefault(state => state.State == ButtonVisualState.Up)?.Layers.Count ?? 0
            : item.Layers.Count;
    }
}
