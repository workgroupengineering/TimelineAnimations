using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using TimelineAnimations.App.Helpers;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.ViewModels;

public partial class TimelineTrackRowViewModel : ViewModelBase
{
    public TimelineTrackRowViewModel(Guid layerId, AnimatedProperty property)
    {
        LayerId = layerId;
        Property = property;
    }

    public Guid LayerId { get; }

    public AnimatedProperty Property { get; }

    public ObservableCollection<KeyframeViewModel> Keyframes { get; } = [];

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
        AnimatedProperty.Opacity => "Opacity",
        _ => Property.ToString()
    };

    [ObservableProperty]
    private string layerName = string.Empty;

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
    private bool isFirstForLayer;

    [ObservableProperty]
    private double currentValue;

    public string CurrentValueLabel => Property switch
    {
        AnimatedProperty.Opacity => $"{CurrentValue:P0}",
        AnimatedProperty.ScaleX or AnimatedProperty.ScaleY => $"{CurrentValue * 100:0.#}%",
        _ => $"{CurrentValue:0.##}"
    };

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
        LayerName = layer.Name;
        FillBrush = ColorHelpers.Brush(layer.Style.Fill, "#FFFFFF");
        IsLayerSelected = selectedLayerId == layer.Id;
        IsSelected = selectedLayerId == layer.Id && selectedProperty == Property;
        IsLocked = layer.IsLocked;
        IsMuted = layer.IsMuted;
        IsSolo = layer.IsSolo;
        IsFolder = layer.Kind == LayerKind.Folder;
        IsFirstForLayer = firstForLayer;
        CurrentValue = FrameTimelineService.SampleProperty(layer, Property, time, frameRate, totalFrames);
        OnPropertyChanged(nameof(CurrentValueLabel));

        Keyframes.Clear();
        var track = layer.Tracks.FirstOrDefault(item => item.Property == Property);
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
        CurrentValue = FrameTimelineService.SampleProperty(layer, Property, time, frameRate, totalFrames);
        OnPropertyChanged(nameof(CurrentValueLabel));
    }

    public void UpdateSelection(Guid? selectedLayerId, AnimatedProperty selectedProperty, Guid? selectedKeyframeId)
    {
        IsLayerSelected = selectedLayerId == LayerId;
        IsSelected = selectedLayerId == LayerId && selectedProperty == Property;

        foreach (var keyframe in Keyframes)
        {
            keyframe.IsSelected = keyframe.Id == selectedKeyframeId;
        }
    }

    public void ApplyHierarchyState(int depth)
    {
        Depth = Math.Max(0, depth);
    }
}
