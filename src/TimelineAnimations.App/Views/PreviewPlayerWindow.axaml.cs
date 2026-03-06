using Avalonia.Controls;
using TimelineAnimations.App.ViewModels;

namespace TimelineAnimations.App.Views;

public partial class PreviewPlayerWindow : Window
{
    public PreviewPlayerWindow()
    {
        InitializeComponent();
        Closed += HandleClosed;
    }

    private void HandleClosed(object? sender, EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
