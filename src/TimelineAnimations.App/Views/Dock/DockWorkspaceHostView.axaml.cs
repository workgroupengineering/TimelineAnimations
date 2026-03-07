using Avalonia.Controls;
using Avalonia.VisualTree;
using TimelineAnimations.App.Services;
using TimelineAnimations.App.ViewModels.Dock;

namespace TimelineAnimations.App.Views.Dock;

public partial class DockWorkspaceHostView : UserControl
{
    public DockWorkspaceHostView()
    {
        InitializeComponent();
    }

    private DockWorkspaceHostViewModel? ViewModel => DataContext as DockWorkspaceHostViewModel;

    private async void OpenDocumentClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.Host is not { } host)
        {
            return;
        }

        await DockWorkspaceFileActionService.OpenDocumentAsync(TopLevel.GetTopLevel(this), host, ResetStageViewport);
    }

    private async void SaveDocumentClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.Host is not { } host)
        {
            return;
        }

        await DockWorkspaceFileActionService.SaveDocumentAsync(TopLevel.GetTopLevel(this), host);
    }

    private async void ExportFrameClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.Host is not { } host)
        {
            return;
        }

        await DockWorkspaceFileActionService.ExportCurrentFrameAsync(TopLevel.GetTopLevel(this), host);
    }

    private async void ExportSequenceClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.Host is not { } host)
        {
            return;
        }

        await DockWorkspaceFileActionService.ExportSequenceAsync(TopLevel.GetTopLevel(this), host);
    }

    private async void ExportProjectClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.Host is not { } host)
        {
            return;
        }

        await DockWorkspaceFileActionService.ExportProjectSequenceAsync(TopLevel.GetTopLevel(this), host);
    }

    private void ResetStageViewport()
    {
        foreach (var stageView in this.GetVisualDescendants().OfType<DockStageDocumentView>())
        {
            stageView.ResetViewport();
            break;
        }
    }
}
