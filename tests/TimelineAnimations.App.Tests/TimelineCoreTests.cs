using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Tests;

public class TimelineCoreTests
{
    [Fact]
    public void SampleProperty_Interpolates_BetweenNeighbouringKeyframes()
    {
        var layer = new TimelineLayer
        {
            Defaults = new LayerDefaults { X = 120 },
            Tracks =
            [
                new LayerTrack
                {
                    Property = AnimatedProperty.X,
                    Keyframes =
                    [
                        new KeyframeModel { Time = 1, Value = 200 },
                        new KeyframeModel { Time = 3, Value = 400 }
                    ]
                }
            ]
        };

        var beforeFirst = TimelineInterpolationService.SampleProperty(layer, AnimatedProperty.X, 0.5);
        var between = TimelineInterpolationService.SampleProperty(layer, AnimatedProperty.X, 2);
        var afterLast = TimelineInterpolationService.SampleProperty(layer, AnimatedProperty.X, 4);

        Assert.Equal(120, beforeFirst);
        Assert.Equal(300, between);
        Assert.Equal(400, afterLast);
    }

    [Fact]
    public void MoveKeyframe_ReordersAndMergesConflictingTimes()
    {
        var layer = new TimelineLayer
        {
            Tracks =
            [
                new LayerTrack
                {
                    Property = AnimatedProperty.Opacity,
                    Keyframes =
                    [
                        new KeyframeModel { Time = 0, Value = 0.2 },
                        new KeyframeModel { Time = 1, Value = 0.5 },
                        new KeyframeModel { Time = 2, Value = 1.0 }
                    ]
                }
            ]
        };

        var movedId = layer.Tracks[0].Keyframes[2].Id;
        var moved = TimelineEditingService.MoveKeyframe(layer, AnimatedProperty.Opacity, movedId, 1, 6);

        Assert.True(moved);
        Assert.Equal(2, layer.Tracks[0].Keyframes.Count);
        Assert.Collection(
            layer.Tracks[0].Keyframes,
            first => Assert.Equal(0, first.Time),
            second => Assert.Equal(1, second.Time));
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_SampleComposition()
    {
        var document = SampleProjectFactory.Create();
        var json = DocumentSerializer.ToJson(document);
        var restored = DocumentSerializer.FromJson(json);

        Assert.Equal(document.Name, restored.Name);
        Assert.Equal(document.Duration, restored.Duration);
        Assert.Equal(document.Layers.Count, restored.Layers.Count);
        Assert.Equal(document.Layers[2].Style.Text, restored.Layers[2].Style.Text);
        Assert.Equal(document.Layers[1].Tracks[0].Keyframes.Count, restored.Layers[1].Tracks[0].Keyframes.Count);
    }

    [Fact]
    public void UndoRedoStack_TraversesRecordedStates()
    {
        var history = new UndoRedoStack<int>(1);

        Assert.True(history.Record(2));
        Assert.True(history.Record(3));
        Assert.True(history.TryUndo(out var undoState));
        Assert.Equal(2, undoState);
        Assert.True(history.TryUndo(out undoState));
        Assert.Equal(1, undoState);
        Assert.True(history.TryRedo(out var redoState));
        Assert.Equal(2, redoState);
    }
}
