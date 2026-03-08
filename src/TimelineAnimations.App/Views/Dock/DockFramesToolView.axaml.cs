using Avalonia.Controls;
using TimelineAnimations.App.Controls;
using TimelineAnimations.App.Models;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.App.ViewModels.Dock;

namespace TimelineAnimations.App.Views.Dock;

public partial class DockFramesToolView : UserControl
{
    public DockFramesToolView()
    {
        InitializeComponent();
        HookInteractions();
    }

    private MainWindowViewModel? ViewModel => (DataContext as DockFramesToolViewModel)?.Host;

    private void HookInteractions()
    {
        FrameTimelineHeader.FrameRequested += HandleFrameRequested;
        FrameTimelineHeader.RulerInteractionRequested += HandleFrameRulerInteractionRequested;
        FrameTimelineHeader.InteractionStateChanged += HandleFrameInteractionStateChanged;
        FrameTimelineBody.FrameRequested += HandleFrameRequested;
        FrameTimelineBody.LayerSelectionRequested += HandleFrameLayerSelectionRequested;
        FrameTimelineBody.RangeSelectionRequested += HandleFrameRangeSelectionRequested;
        FrameTimelineBody.HierarchyToggleRequested += HandleFrameHierarchyToggleRequested;
        FrameTimelineBody.InteractionStateChanged += HandleFrameInteractionStateChanged;
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
