using Avalonia.Controls;
using TimelineAnimations.App.Controls;
using TimelineAnimations.App.Models;
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
        TimelineEditorHeader.ScrubRequested += HandleTimelineScrubRequested;
        TimelineEditorHeader.KeyframeInteractionStateChanged += HandleTimelineInteractionStateChanged;
        TimelineEditorBody.ScrubRequested += HandleTimelineScrubRequested;
        TimelineEditorBody.LayerSelectionRequested += HandleTimelineLayerSelectionRequested;
        TimelineEditorBody.TrackSelectionRequested += HandleTrackSelectionRequested;
        TimelineEditorBody.HierarchyToggleRequested += HandleTimelineHierarchyToggleRequested;
        TimelineEditorBody.KeyframeSelectionRequested += HandleKeyframeSelectionRequested;
        TimelineEditorBody.KeyframeMoveRequested += HandleKeyframeMoveRequested;
        TimelineEditorBody.KeyframeAddRequested += HandleKeyframeAddRequested;
        TimelineEditorBody.KeyframeInteractionStateChanged += HandleTimelineInteractionStateChanged;

        FrameTimelineHeader.FrameRequested += HandleFrameRequested;
        FrameTimelineHeader.RulerInteractionRequested += HandleFrameRulerInteractionRequested;
        FrameTimelineHeader.InteractionStateChanged += HandleFrameInteractionStateChanged;
        FrameTimelineBody.FrameRequested += HandleFrameRequested;
        FrameTimelineBody.LayerSelectionRequested += HandleFrameLayerSelectionRequested;
        FrameTimelineBody.RangeSelectionRequested += HandleFrameRangeSelectionRequested;
        FrameTimelineBody.HierarchyToggleRequested += HandleFrameHierarchyToggleRequested;
        FrameTimelineBody.InteractionStateChanged += HandleFrameInteractionStateChanged;
    }

    private void HandleTimelineScrubRequested(object? sender, TimelineScrubRequestedEventArgs e)
    {
        ViewModel?.Scrub(e.Time);
    }

    private void HandleTrackSelectionRequested(object? sender, TimelineTrackSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectTrack(e.LayerId, e.Property);
    }

    private void HandleTimelineLayerSelectionRequested(object? sender, TimelineLayerSelectionRequestedEventArgs e)
    {
        ViewModel?.SelectLayer(e.LayerId);
    }

    private void HandleTimelineHierarchyToggleRequested(object? sender, TimelineHierarchyToggleRequestedEventArgs e)
    {
        ViewModel?.ToggleTimelineHierarchy(e.LayerId);
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
            var kind = e.InteractionKind == TimelineEditorInteractionKind.Scrub
                ? InteractiveChangeKind.TimelineNavigation
                : InteractiveChangeKind.KeyframeDrag;
            ViewModel.BeginInteractiveChange(kind);
        }
        else
        {
            var statusMessage = e.InteractionKind == TimelineEditorInteractionKind.Scrub
                ? "Timeline scrubbed"
                : "Keyframe moved";
            ViewModel.CommitInteractiveChange(statusMessage);
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

    private void HandleFrameHierarchyToggleRequested(object? sender, FrameTimelineHierarchyToggleRequestedEventArgs e)
    {
        ViewModel?.ToggleFrameHierarchy(e.LayerId);
    }

    private void HandleFrameRulerInteractionRequested(object? sender, FrameTimelineRulerInteractionRequestedEventArgs e)
    {
        ViewModel?.ApplyFrameRulerInteraction(e.InteractionKind, e.Frame);
    }

    private void HandleFrameInteractionStateChanged(object? sender, FrameTimelineInteractionStateChangedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (e.IsActive)
        {
            var kind = e.InteractionKind == FrameTimelineInteractionKind.PlayheadScrub
                ? InteractiveChangeKind.TimelineNavigation
                : InteractiveChangeKind.FrameTimelineDrag;
            ViewModel.BeginInteractiveChange(kind);
        }
        else
        {
            var statusMessage = e.InteractionKind == FrameTimelineInteractionKind.PlayheadScrub
                ? "Timeline scrubbed"
                : "Frame range updated";
            ViewModel.CommitInteractiveChange(statusMessage);
        }
    }
}
