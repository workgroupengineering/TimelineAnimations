using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using TimelineAnimations.App.Helpers;
using TimelineAnimations.App.Models;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.ViewModels;

public partial class TimelineTrackRowViewModel : ViewModelBase
{
    public TimelineTrackRowViewModel(Guid layerId, TimelineTrackRowKind rowKind, AnimatedProperty? property = null, TimelineTrackGroupKind? groupKind = null)
    {
        LayerId = layerId;
        RowKind = rowKind;
        Property = property;
        GroupKind = groupKind;
    }

    public Guid LayerId { get; }

    public TimelineTrackRowKind RowKind { get; }

    public AnimatedProperty? Property { get; }

    public TimelineTrackGroupKind? GroupKind { get; }

    public ObservableCollection<KeyframeViewModel> Keyframes { get; } = [];

    public bool IsLayerHeader => RowKind == TimelineTrackRowKind.LayerHeader;

    public bool IsGroupHeader => RowKind == TimelineTrackRowKind.PropertyGroupHeader;

    public bool IsTrackRow => RowKind == TimelineTrackRowKind.PropertyTrack;

    public bool CanSelectTrack => IsTrackRow && Property is not null;

    public double RowHeight => RowKind switch
    {
        TimelineTrackRowKind.LayerHeader => 42d,
        TimelineTrackRowKind.PropertyGroupHeader => 30d,
        _ => 46d
    };

    public string TrackTitle => Property switch
    {
        AnimatedProperty.X => "Position X",
        AnimatedProperty.Y => "Position Y",
        AnimatedProperty.Width => "Width",
        AnimatedProperty.Height => "Height",
        AnimatedProperty.ScaleX => "Scale X",
        AnimatedProperty.ScaleY => "Scale Y",
        AnimatedProperty.SkewX => "Skew X",
        AnimatedProperty.SkewY => "Skew Y",
        AnimatedProperty.Rotation => "Rotation",
        AnimatedProperty.RotationX => "Rotation X",
        AnimatedProperty.RotationY => "Rotation Y",
        AnimatedProperty.Opacity => "Opacity",
        AnimatedProperty.ZDepth => "Z Depth",
        null => string.Empty,
        _ => Property.ToString() ?? string.Empty
    };

    public string GroupTitle => GroupKind switch
    {
        TimelineTrackGroupKind.Layout => "Layout",
        TimelineTrackGroupKind.Scale => "Scale",
        TimelineTrackGroupKind.Rotation3D => "Rotation + 3D",
        TimelineTrackGroupKind.Appearance => "Appearance",
        _ => string.Empty
    };

    [ObservableProperty]
    private string layerName = string.Empty;

    [ObservableProperty]
    private string layerKindLabel = string.Empty;

    [ObservableProperty]
    private string summaryLabel = string.Empty;

    [ObservableProperty]
    private ISolidColorBrush fillBrush = ColorHelpers.Brush("#FFFFFF");

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isLayerSelected;

    [ObservableProperty]
    private bool isLocked;

    [ObservableProperty]
    private bool isMuted;

    [ObservableProperty]
    private bool isSolo;

    [ObservableProperty]
    private bool isFolder;

    [ObservableProperty]
    private int depth;

    [ObservableProperty]
    private int indentLevel;

    [ObservableProperty]
    private bool isFirstForLayer;

    [ObservableProperty]
    private bool hasExpander;

    [ObservableProperty]
    private bool isExpanded = true;

    [ObservableProperty]
    private double currentValue;

    public string CurrentValueLabel => Property switch
    {
        AnimatedProperty.Opacity => $"{CurrentValue:P0}",
        AnimatedProperty.ScaleX or AnimatedProperty.ScaleY => $"{CurrentValue * 100:0.#}%",
        null => string.Empty,
        _ => $"{CurrentValue:0.##}"
    };

    public void LoadLayerHeaderFromLayer(TimelineLayer layer, bool isSelectedLayer, bool isExpanded)
    {
        LayerName = layer.Name;
        LayerKindLabel = layer.Kind switch
        {
            LayerKind.Folder => "Folder",
            LayerKind.Audio => "Audio",
            LayerKind.Text => "Text",
            LayerKind.Path => "Vector",
            LayerKind.Video => "Media",
            LayerKind.AvaloniaControl => "Control",
            _ => layer.Kind.ToString()
        };
        SummaryLabel = BuildLayerSummary(layer);
        FillBrush = ColorHelpers.Brush(layer.Style.Fill, "#FFFFFF");
        IsLayerSelected = isSelectedLayer;
        IsSelected = isSelectedLayer;
        IsLocked = layer.IsLocked;
        IsMuted = layer.IsMuted;
        IsSolo = layer.IsSolo;
        IsFolder = layer.Kind == LayerKind.Folder;
        HasExpander = layer.Kind != LayerKind.Audio;
        IsExpanded = isExpanded;
        Keyframes.Clear();
        CurrentValue = 0d;
        OnPropertyChanged(nameof(CurrentValueLabel));
    }

    public void LoadGroupHeaderFromLayer(TimelineLayer layer, int keyedPropertyCount)
    {
        LayerName = layer.Name;
        LayerKindLabel = GroupTitle;
        SummaryLabel = keyedPropertyCount > 0
            ? $"{keyedPropertyCount} keyed {(keyedPropertyCount == 1 ? "property" : "properties")}"
            : "Current values and motion curve lanes";
        FillBrush = ColorHelpers.Brush(layer.Style.Fill, "#FFFFFF");
        IsLayerSelected = false;
        IsSelected = false;
        IsLocked = layer.IsLocked;
        IsMuted = layer.IsMuted;
        IsSolo = layer.IsSolo;
        IsFolder = false;
        HasExpander = false;
        IsExpanded = true;
        Keyframes.Clear();
        CurrentValue = 0d;
        OnPropertyChanged(nameof(CurrentValueLabel));
    }

    public void LoadFromLayer(
        TimelineLayer layer,
        double time,
        double frameRate,
        int totalFrames,
        Guid? selectedLayerId,
        AnimatedProperty selectedProperty,
        Guid? selectedKeyframeId,
        bool firstForLayer)
    {
        if (!IsTrackRow || Property is null)
        {
            Keyframes.Clear();
            return;
        }

        LayerName = layer.Name;
        LayerKindLabel = string.Empty;
        SummaryLabel = string.Empty;
        FillBrush = ColorHelpers.Brush(layer.Style.Fill, "#FFFFFF");
        IsLayerSelected = selectedLayerId == layer.Id;
        IsSelected = selectedLayerId == layer.Id && selectedProperty == Property.Value;
        IsLocked = layer.IsLocked;
        IsMuted = layer.IsMuted;
        IsSolo = layer.IsSolo;
        IsFolder = layer.Kind == LayerKind.Folder;
        IsFirstForLayer = firstForLayer;
        CurrentValue = FrameTimelineService.SampleProperty(layer, Property.Value, time, frameRate, totalFrames);
        OnPropertyChanged(nameof(CurrentValueLabel));

        Keyframes.Clear();
        var track = layer.Tracks.FirstOrDefault(item => item.Property == Property.Value);
        if (track is not null)
        {
            foreach (var keyframe in track.Keyframes.OrderBy(item => item.Time))
            {
                Keyframes.Add(new KeyframeViewModel(keyframe)
                {
                    IsSelected = keyframe.Id == selectedKeyframeId
                });
            }
        }
    }

    public void RefreshCurrentValue(TimelineLayer layer, double time, double frameRate, int totalFrames)
    {
        if (!IsTrackRow || Property is null)
        {
            return;
        }

        CurrentValue = FrameTimelineService.SampleProperty(layer, Property.Value, time, frameRate, totalFrames);
        OnPropertyChanged(nameof(CurrentValueLabel));
    }

    public void SyncTrackFromLayer(
        TimelineLayer layer,
        double time,
        double frameRate,
        int totalFrames,
        Guid? selectedLayerId,
        AnimatedProperty selectedProperty,
        Guid? selectedKeyframeId)
    {
        if (!IsTrackRow || Property is null)
        {
            return;
        }

        LayerName = layer.Name;
        FillBrush = ColorHelpers.Brush(layer.Style.Fill, "#FFFFFF");
        IsLocked = layer.IsLocked;
        IsMuted = layer.IsMuted;
        IsSolo = layer.IsSolo;
        RefreshCurrentValue(layer, time, frameRate, totalFrames);
        UpdateSelection(selectedLayerId, selectedProperty, selectedKeyframeId);

        var orderedKeyframes = layer.Tracks
            .FirstOrDefault(item => item.Property == Property.Value)?
            .Keyframes
            .OrderBy(item => item.Time)
            .ToArray()
            ?? [];

        for (var index = 0; index < orderedKeyframes.Length; index++)
        {
            var model = orderedKeyframes[index];
            var existingIndex = FindKeyframeIndex(model.Id);
            KeyframeViewModel keyframeViewModel;
            if (existingIndex >= 0)
            {
                keyframeViewModel = Keyframes[existingIndex];
                keyframeViewModel.RefreshFromModel();
                if (existingIndex != index)
                {
                    Keyframes.Move(existingIndex, index);
                }
            }
            else
            {
                keyframeViewModel = new KeyframeViewModel(model);
                Keyframes.Insert(Math.Min(index, Keyframes.Count), keyframeViewModel);
            }

            keyframeViewModel.IsSelected = model.Id == selectedKeyframeId;
        }

        for (var index = Keyframes.Count - 1; index >= orderedKeyframes.Length; index--)
        {
            Keyframes.RemoveAt(index);
        }
    }

    public void UpdateSelection(Guid? selectedLayerId, AnimatedProperty selectedProperty, Guid? selectedKeyframeId)
    {
        IsLayerSelected = selectedLayerId == LayerId;
        IsSelected = IsLayerHeader
            ? IsLayerSelected
            : IsTrackRow && Property is not null && selectedLayerId == LayerId && selectedProperty == Property.Value;

        foreach (var keyframe in Keyframes)
        {
            keyframe.IsSelected = keyframe.Id == selectedKeyframeId;
        }
    }

    public void ApplyHierarchyState(int depth, int extraIndent = 0)
    {
        Depth = Math.Max(0, depth);
        IndentLevel = Math.Max(0, depth + extraIndent);
    }

    private int FindKeyframeIndex(Guid keyframeId)
    {
        for (var index = 0; index < Keyframes.Count; index++)
        {
            if (Keyframes[index].Id == keyframeId)
            {
                return index;
            }
        }

        return -1;
    }

    private static string BuildLayerSummary(TimelineLayer layer)
    {
        if (layer.Kind == LayerKind.Folder)
        {
            return layer.IsExpanded ? "Folder expanded" : "Folder collapsed";
        }

        var keyedProperties = layer.Tracks.Count(track => track.Keyframes.Count > 1);
        var keyframeCount = layer.Tracks.Sum(track => track.Keyframes.Count);
        return keyedProperties > 0
            ? $"{keyedProperties} animated groups • {keyframeCount} keyframes"
            : "No animated property curves yet";
    }
}
