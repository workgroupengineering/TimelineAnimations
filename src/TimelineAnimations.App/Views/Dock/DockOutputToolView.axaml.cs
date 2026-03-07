using Avalonia.Controls;
using TimelineAnimations.App.Services;
using TimelineAnimations.App.ViewModels.Dock;

namespace TimelineAnimations.App.Views.Dock;

public partial class DockOutputToolView : UserControl
{
    public DockOutputToolView()
    {
        InitializeComponent();
    }

    private async void ImportAnimationFormatClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if ((DataContext as DockOutputToolViewModel)?.Host is not { } host)
        {
            return;
        }

        await DockWorkspaceFileActionService.ImportAnimationFormatAsync(TopLevel.GetTopLevel(this), host);
    }

    private async void ExportAnimationFormatClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if ((DataContext as DockOutputToolViewModel)?.Host is not { } host)
        {
            return;
        }

        await DockWorkspaceFileActionService.ExportAnimationFormatAsync(TopLevel.GetTopLevel(this), host);
    }

    private void PreviewPublishProfileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if ((DataContext as DockOutputToolViewModel)?.Host is not { } host)
        {
            return;
        }

        DockWorkspaceFileActionService.PreviewPublishProfile(host);
    }

    private async void ExportPublishProfileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if ((DataContext as DockOutputToolViewModel)?.Host is not { } host)
        {
            return;
        }

        await DockWorkspaceFileActionService.ExportPublishProfileAsync(TopLevel.GetTopLevel(this), host);
    }
}
