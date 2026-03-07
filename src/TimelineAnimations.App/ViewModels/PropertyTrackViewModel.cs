using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.ViewModels;

public partial class PropertyTrackViewModel : ViewModelBase
{
    public PropertyTrackViewModel(AnimatedProperty property)
    {
        Property = property;
    }

    public AnimatedProperty Property { get; }

    public string Title => Property switch
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

    public ObservableCollection<KeyframeViewModel> Keyframes { get; } = [];

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private double currentValue;

    public string CurrentValueLabel => Property switch
    {
        AnimatedProperty.Opacity => $"{CurrentValue:P0}",
        AnimatedProperty.ScaleX or AnimatedProperty.ScaleY => $"{CurrentValue * 100:0.#}%",
        _ => $"{CurrentValue:0.##}"
    };

    public void LoadFromLayer(TimelineLayer layer, double time, Guid? selectedKeyframeId)
    {
        CurrentValue = TimelineInterpolationService.SampleProperty(layer, Property, time);
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

        OnPropertyChanged(nameof(CurrentValueLabel));
    }

    public void LoadFromLayer(TimelineLayer layer, double time, double frameRate, int totalFrames, Guid? selectedKeyframeId)
    {
        CurrentValue = FrameTimelineService.SampleProperty(layer, Property, time, frameRate, totalFrames);
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

        OnPropertyChanged(nameof(CurrentValueLabel));
    }

    public void RefreshCurrentValue(TimelineLayer layer, double time)
    {
        CurrentValue = TimelineInterpolationService.SampleProperty(layer, Property, time);
        OnPropertyChanged(nameof(CurrentValueLabel));
    }

    public void RefreshCurrentValue(TimelineLayer layer, double time, double frameRate, int totalFrames)
    {
        CurrentValue = FrameTimelineService.SampleProperty(layer, Property, time, frameRate, totalFrames);
        OnPropertyChanged(nameof(CurrentValueLabel));
    }

    public void UpdateKeyframeSelection(Guid? selectedKeyframeId)
    {
        foreach (var keyframe in Keyframes)
        {
            keyframe.IsSelected = keyframe.Id == selectedKeyframeId;
        }
    }
}
