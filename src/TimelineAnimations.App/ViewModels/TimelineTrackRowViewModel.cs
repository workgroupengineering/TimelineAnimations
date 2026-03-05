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
        AnimatedProperty.Rotation => "Rotation",
        AnimatedProperty.Opacity => "Opacity",
        _ => Property.ToString()
    };

    [ObservableProperty]
    private string layerName = string.Empty;

    [ObservableProperty]
    private SolidColorBrush fillBrush = ColorHelpers.Brush("#FFFFFF");

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isLayerSelected;

    [ObservableProperty]
    private bool isFirstForLayer;

    [ObservableProperty]
    private double currentValue;

    public string CurrentValueLabel => Property == AnimatedProperty.Opacity
        ? $"{CurrentValue:P0}"
        : $"{CurrentValue:0.##}";

    public void LoadFromLayer(
        TimelineLayer layer,
        double time,
        Guid? selectedLayerId,
        AnimatedProperty selectedProperty,
        Guid? selectedKeyframeId,
        bool firstForLayer)
    {
        LayerName = layer.Name;
        FillBrush = ColorHelpers.Brush(layer.Style.Fill, "#FFFFFF");
        IsLayerSelected = selectedLayerId == layer.Id;
        IsSelected = selectedLayerId == layer.Id && selectedProperty == Property;
        IsFirstForLayer = firstForLayer;
        CurrentValue = TimelineInterpolationService.SampleProperty(layer, Property, time);
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

    public void RefreshCurrentValue(TimelineLayer layer, double time)
    {
        CurrentValue = TimelineInterpolationService.SampleProperty(layer, Property, time);
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
}
