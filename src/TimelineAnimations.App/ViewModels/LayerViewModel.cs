using Avalonia;
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

    public LayerCompositeRole CompositeRole => Model.Compositing.Role;

    public LayerBlendMode BlendMode => Model.Compositing.BlendMode;

    public string RoleLabel => CompositeRole switch
    {
        LayerCompositeRole.Mask => "Mask",
        LayerCompositeRole.Guide => "Guide",
        LayerCompositeRole.Camera => "Camera",
        _ => "Layer"
    };

    public string BlendModeLabel => BlendMode == LayerBlendMode.Normal
        ? "Normal"
        : BlendMode.ToString();

    public string KindLabel => CompositeRole switch
    {
        LayerCompositeRole.Mask => "Mask",
        LayerCompositeRole.Guide => "Guide",
        LayerCompositeRole.Camera => "Camera",
        _ => Kind switch
        {
            LayerKind.Folder => "Folder",
            LayerKind.Rectangle => "Card",
            LayerKind.Ellipse => "Orb",
            LayerKind.Text => "Caption",
            LayerKind.Path => "Vector",
            LayerKind.AvaloniaControl => Model.Style.AvaloniaControl.Kind switch
            {
                AvaloniaControlKind.Button => "Button",
                AvaloniaControlKind.TextBlock => "TextBlock",
                AvaloniaControlKind.TextBox => "TextBox",
                AvaloniaControlKind.CheckBox => "CheckBox",
                AvaloniaControlKind.ToggleButton => "Toggle",
                AvaloniaControlKind.Slider => "Slider",
                AvaloniaControlKind.ProgressBar => "Progress",
                AvaloniaControlKind.Image => "Image",
                AvaloniaControlKind.ComboBox => "ComboBox",
                AvaloniaControlKind.ListBox => "ListBox",
                AvaloniaControlKind.TabControl => "Tabs",
                AvaloniaControlKind.Grid => "Grid",
                AvaloniaControlKind.StackPanel => "Stack",
                AvaloniaControlKind.PathIcon => "Icon",
                AvaloniaControlKind.Panel => "Panel",
                _ => "Border"
            },
            LayerKind.Video => "Video",
            LayerKind.Audio => "Audio",
            _ => "Layer"
        }
    };

    public string Subtitle
    {
        get
        {
            var sizeLabel = Kind == LayerKind.Path
                ? $"{Model.Style.PathPoints.Count} pts • {Math.Round(Width):0}×{Math.Round(Height):0}"
                : Kind == LayerKind.Folder
                    ? HasChildren ? "Hierarchy folder" : "Empty folder"
                : Kind == LayerKind.Audio
                    ? $"{Math.Max(0.05d, Model.Media.ClipDuration):0.00}s • {Model.Media.PlaybackMode}"
                : $"{Math.Round(Width):0}×{Math.Round(Height):0}";

            return CompositeRole switch
            {
                LayerCompositeRole.Mask => $"{sizeLabel} • masks {Math.Max(1, Model.Compositing.MaskLayerCount)} layer(s)",
                LayerCompositeRole.Guide => $"{sizeLabel} • stage guide",
                LayerCompositeRole.Camera => $"{sizeLabel} • viewport",
                _ when Model.ShowAsOutline => $"{KindLabel} • {sizeLabel} • outline",
                _ when Model.GuidedByLayerId is not null => $"{KindLabel} • {sizeLabel} • guided",
                _ when BlendMode != LayerBlendMode.Normal => $"{KindLabel} • {sizeLabel} • {BlendModeLabel}",
                _ => $"{KindLabel} • {sizeLabel}"
            };
        }
    }

    public bool HasLibraryLink => !string.IsNullOrWhiteSpace(LibraryLinkLabel);

    public bool IsRenderable => IsStageRenderable && IsVisible && IsFrameVisible;

    public bool IsStageRenderable => MediaTimelineService.IsStageRenderable(Model);

    public Thickness HierarchyMargin => new(Math.Max(0, Depth) * 14d, 0, 0, 0);

    public string StateBadge
    {
        get
        {
            if (IsSolo && IsMuted)
            {
                return "Solo • Muted";
            }

            if (IsSolo)
            {
                return "Solo";
            }

            if (IsMuted)
            {
                return "Muted";
            }

            if (IsLocked && !IsVisible)
            {
                return CompositeRole == LayerCompositeRole.Normal
                    ? "Hidden • Locked"
                    : $"{RoleLabel} • Hidden • Locked";
            }

            if (IsLocked)
            {
                return CompositeRole == LayerCompositeRole.Normal
                    ? "Locked"
                    : $"{RoleLabel} • Locked";
            }

            if (!IsVisible)
            {
                return CompositeRole == LayerCompositeRole.Normal
                    ? "Hidden"
                    : $"{RoleLabel} • Hidden";
            }

            return CompositeRole == LayerCompositeRole.Normal
                ? "Visible"
                : $"{RoleLabel} • Visible";
        }
    }

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string libraryLinkLabel = string.Empty;

    [ObservableProperty]
    private bool isVisible;

    [ObservableProperty]
    private bool isFrameVisible = true;

    [ObservableProperty]
    private bool isLocked;

    [ObservableProperty]
    private bool isMuted;

    [ObservableProperty]
    private bool isSolo;

    [ObservableProperty]
    private bool isExpanded = true;

    [ObservableProperty]
    private bool isFolder;

    [ObservableProperty]
    private bool hasChildren;

    [ObservableProperty]
    private int depth;

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
    private double scaleX = 1d;

    [ObservableProperty]
    private double scaleY = 1d;

    [ObservableProperty]
    private double skewX;

    [ObservableProperty]
    private double skewY;

    [ObservableProperty]
    private double rotation;

    [ObservableProperty]
    private double opacity;

    [ObservableProperty]
    private ISolidColorBrush fillBrush;

    [ObservableProperty]
    private ISolidColorBrush strokeBrush;

    public void RefreshMetadata()
    {
        Name = Model.Name;
        IsVisible = Model.IsVisible;
        IsLocked = Model.IsLocked;
        IsMuted = Model.IsMuted;
        IsSolo = Model.IsSolo;
        IsExpanded = Model.IsExpanded;
        IsFolder = Model.Kind == LayerKind.Folder;
        ZIndex = Model.ZIndex;
        FillHex = Model.Style.Fill;
        StrokeHex = Model.Style.Stroke;
        TextContent = Model.Style.Text;
        FontSize = Model.Style.FontSize;
        CornerRadius = Model.Style.CornerRadius;
        var previewFill = Model.Kind switch
        {
            LayerKind.Path => Model.Style.Stroke,
            LayerKind.Audio or LayerKind.Video => Model.Style.Fill,
            _ => Model.Style.UseGradient ? Model.Style.GradientFrom : FillHex
        };
        FillBrush = Model.Style.HasFill
            ? ColorHelpers.Brush(previewFill, "#FFFFFF")
            : ColorHelpers.Brush("#00000000", "#00000000");
        StrokeBrush = Model.Style.HasStroke
            ? ColorHelpers.Brush(StrokeHex, "#FFFFFF")
            : ColorHelpers.Brush("#00000000", "#00000000");
        OnPropertyChanged(nameof(KindLabel));
        OnPropertyChanged(nameof(RoleLabel));
        OnPropertyChanged(nameof(BlendModeLabel));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(StateBadge));
        OnPropertyChanged(nameof(HasLibraryLink));
        OnPropertyChanged(nameof(IsStageRenderable));
        OnPropertyChanged(nameof(IsRenderable));
        OnPropertyChanged(nameof(HierarchyMargin));
    }

    public void ApplyHierarchyState(int depth, bool hasChildren)
    {
        Depth = Math.Max(0, depth);
        HasChildren = hasChildren;
        IsFolder = Model.Kind == LayerKind.Folder;
        IsExpanded = Model.IsExpanded;
        OnPropertyChanged(nameof(HierarchyMargin));
    }

    public void UpdatePreview(double time)
    {
        var snapshot = TimelineInterpolationService.SampleLayer(Model, time);
        X = snapshot.X;
        Y = snapshot.Y;
        Width = snapshot.Width;
        Height = snapshot.Height;
        ScaleX = snapshot.ScaleX;
        ScaleY = snapshot.ScaleY;
        SkewX = snapshot.SkewX;
        SkewY = snapshot.SkewY;
        Rotation = snapshot.Rotation;
        Opacity = snapshot.Opacity;
        CornerRadius = snapshot.CornerRadius;
        TextContent = snapshot.Text;
        FontSize = snapshot.FontSize;
        IsFrameVisible = true;
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(StateBadge));
        OnPropertyChanged(nameof(HasLibraryLink));
        OnPropertyChanged(nameof(IsRenderable));
    }

    public void UpdatePreview(double time, double frameRate, int totalFrames)
    {
        var snapshot = FrameTimelineService.SampleLayer(Model, time, frameRate, totalFrames);
        if (snapshot is null)
        {
            var fallback = TimelineInterpolationService.SampleLayer(Model, time);
            X = fallback.X;
            Y = fallback.Y;
            Width = fallback.Width;
            Height = fallback.Height;
            ScaleX = fallback.ScaleX;
            ScaleY = fallback.ScaleY;
            SkewX = fallback.SkewX;
            SkewY = fallback.SkewY;
            Rotation = fallback.Rotation;
            Opacity = fallback.Opacity;
            CornerRadius = fallback.CornerRadius;
            TextContent = fallback.Text;
            FontSize = fallback.FontSize;
            IsFrameVisible = false;
            OnPropertyChanged(nameof(Subtitle));
            OnPropertyChanged(nameof(IsRenderable));
            return;
        }

        X = snapshot.Value.X;
        Y = snapshot.Value.Y;
        Width = snapshot.Value.Width;
        Height = snapshot.Value.Height;
        ScaleX = snapshot.Value.ScaleX;
        ScaleY = snapshot.Value.ScaleY;
        SkewX = snapshot.Value.SkewX;
        SkewY = snapshot.Value.SkewY;
        Rotation = snapshot.Value.Rotation;
        Opacity = snapshot.Value.Opacity;
        CornerRadius = snapshot.Value.CornerRadius;
        TextContent = snapshot.Value.Text;
        FontSize = snapshot.Value.FontSize;
        IsFrameVisible = true;
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(StateBadge));
        OnPropertyChanged(nameof(HasLibraryLink));
        OnPropertyChanged(nameof(IsRenderable));
    }

    public void SetLibraryLink(string? symbolName, SymbolKind? symbolKind)
    {
        LibraryLinkLabel = string.IsNullOrWhiteSpace(symbolName)
            ? string.Empty
            : symbolKind is null
                ? symbolName
                : $"{symbolName} • {symbolKind.Value switch
                {
                    SymbolKind.Graphic => "Graphic",
                    SymbolKind.MovieClip => "Movie Clip",
                    SymbolKind.Button => "Button",
                    _ => symbolKind.Value.ToString()
                }}";
        OnPropertyChanged(nameof(HasLibraryLink));
    }

    public void SetMediaLink(string? mediaName, MediaAssetKind? mediaKind)
    {
        LibraryLinkLabel = string.IsNullOrWhiteSpace(mediaName)
            ? string.Empty
            : mediaKind is null
                ? mediaName
                : $"{mediaName} • {mediaKind.Value switch
                {
                    MediaAssetKind.Audio => "Audio Clip",
                    MediaAssetKind.Video => "Video Clip",
                    _ => mediaKind.Value.ToString()
                }}";
        OnPropertyChanged(nameof(HasLibraryLink));
    }
}
