using Avalonia.Controls;
using TimelineAnimations.App.Controls;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.App.ViewModels.Dock;

namespace TimelineAnimations.App.Views.Dock;

public partial class DockTimelineToolView : UserControl
{
    public DockTimelineToolView()
    {
        InitializeComponent();
        HookInteractions();
    }

    private MainWindowViewModel? ViewModel => (DataContext as DockTimelineToolViewModel)?.Host;

    private void HookInteractions()
    {
        TimelineEditor.ScrubRequested += HandleTimelineScrubRequested;
        TimelineEditor.TrackSelectionRequested += HandleTrackSelectionRequested;
        TimelineEditor.KeyframeSelectionRequested += HandleKeyframeSelectionRequested;
        TimelineEditor.KeyframeMoveRequested += HandleKeyframeMoveRequested;
        TimelineEditor.KeyframeAddRequested += HandleKeyframeAddRequested;
        TimelineEditor.KeyframeInteractionStateChanged += HandleTimelineInteractionStateChanged;

        FrameTimeline.FrameRequested += HandleFrameRequested;
        FrameTimeline.LayerSelectionRequested += HandleFrameLayerSelectionRequested;
        FrameTimeline.RangeSelectionRequested += HandleFrameRangeSelectionRequested;
    }

    private void HandleTimelineScrubRequested(object? sender, TimelineScrubRequestedEventArgs e)
    {
        ViewModel?.Scrub(e.Time);
    }

    private void HandleTrackSelectionRequested(object? sender, TimelineTrackSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectTrack(e.LayerId, e.Property);
    }

    private void HandleKeyframeSelectionRequested(object? sender, TimelineKeyframeSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectKeyframe(e.LayerId, e.Property, e.KeyframeId);
    }

    private void HandleKeyframeMoveRequested(object? sender, TimelineKeyframeMoveRequestedEventArgs e)
    {
        ViewModel?.MoveKeyframe(e.LayerId, e.Property, e.KeyframeId, e.Time);
    }

    private void HandleKeyframeAddRequested(object? sender, TimelineKeyframeAddRequestedEventArgs e)
    {
        ViewModel?.AddKeyframeAt(e.LayerId, e.Property, e.Time);
    }

    private void HandleTimelineInteractionStateChanged(object? sender, TimelineInteractionStateChangedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (e.IsActive)
        {
            ViewModel.BeginInteractiveChange();
        }
        else
        {
            ViewModel.CommitInteractiveChange("Keyframe moved");
        }
    }

    private void HandleFrameRequested(object? sender, FrameTimelineFrameRequestedEventArgs e)
    {
        ViewModel?.SeekFrame(e.Frame);
    }

    private void HandleFrameLayerSelectionRequested(object? sender, FrameTimelineLayerSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectLayer(e.LayerId);
    }

    private void HandleFrameRangeSelectionRequested(object? sender, FrameTimelineRangeSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectFrameRange(e.LayerId, e.StartFrame, e.EndFrame);
    }
}
