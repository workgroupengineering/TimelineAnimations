using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using TimelineAnimations.App.Helpers;

namespace TimelineAnimations.App.Controls;

/// <summary>
/// A grid that starts native window dragging when the pointer presses a non-interactive title-bar region.
/// </summary>
public class TitleBarDragGrid : Grid
{
    public TitleBarDragGrid()
    {
        Background = Brushes.Transparent;
        AddHandler(InputElement.DoubleTappedEvent, OnTitleBarDoubleTapped);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.Handled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is not Window window || !window.IsExtendedIntoWindowDecorations)
        {
            return;
        }

        if (!TitleBarDragHelper.ShouldBeginWindowDrag(e.Source as Visual, this, e.ClickCount))
        {
            return;
        }

        window.BeginMoveDrag(e);
        e.Handled = true;
    }

    private void OnTitleBarDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is not Window window || !window.IsExtendedIntoWindowDecorations)
        {
            return;
        }

        if (!TitleBarDragHelper.ShouldToggleWindowState(e.Source as Visual, this))
        {
            return;
        }

        var nextState = TitleBarDragHelper.GetNextWindowState(window.WindowState, window.CanResize);
        if (nextState == window.WindowState)
        {
            return;
        }

        window.WindowState = nextState;
        e.Handled = true;
    }
}
