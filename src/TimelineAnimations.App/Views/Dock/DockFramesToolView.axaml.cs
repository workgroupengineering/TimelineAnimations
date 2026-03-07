using Avalonia.Controls;
using TimelineAnimations.App.Controls;
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
        FrameTimeline.FrameRequested += HandleFrameRequested;
        FrameTimeline.LayerSelectionRequested += HandleFrameLayerSelectionRequested;
        FrameTimeline.RangeSelectionRequested += HandleFrameRangeSelectionRequested;
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
