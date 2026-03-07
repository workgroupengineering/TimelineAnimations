using Avalonia.Controls;
using TimelineAnimations.App.Services;
using TimelineAnimations.App.ViewModels.Dock;

namespace TimelineAnimations.App.Views.Dock;

public partial class DockProjectToolView : UserControl
{
    public DockProjectToolView()
    {
        InitializeComponent();
    }

    private async void ImportAudioClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if ((DataContext as DockProjectToolViewModel)?.Host is not { } host)
        {
            return;
        }

        await DockWorkspaceFileActionService.ImportAudioAsync(TopLevel.GetTopLevel(this), host);
    }

    private async void ImportVideoClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if ((DataContext as DockProjectToolViewModel)?.Host is not { } host)
        {
            return;
        }

        await DockWorkspaceFileActionService.ImportVideoAsync(TopLevel.GetTopLevel(this), host);
    }
}
