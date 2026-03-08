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
        AnimatedProperty.RotationX => "Rotation X",
        AnimatedProperty.RotationY => "Rotation Y",
        AnimatedProperty.ZDepth => "Z Depth",
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

    public void SyncKeyframesFromLayer(TimelineLayer layer, Guid? selectedKeyframeId)
    {
        var orderedKeyframes = layer.Tracks
            .FirstOrDefault(item => item.Property == Property)?
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

    public void UpdateKeyframeSelection(Guid? selectedKeyframeId)
    {
        foreach (var keyframe in Keyframes)
        {
            keyframe.IsSelected = keyframe.Id == selectedKeyframeId;
        }
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
}
