using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace TimelineAnimations.App.Helpers;

/// <summary>
/// Determines whether a pointer press inside a title-bar host should begin a window drag.
/// </summary>
public static class TitleBarDragHelper
{
    /// <summary>
    /// Returns <see langword="true"/> when the visual source is inside <paramref name="boundary"/>
    /// and is not owned by an interactive child control.
    /// </summary>
    public static bool ShouldBeginWindowDrag(Visual? source, Visual boundary, int clickCount = 1)
    {
        if (clickCount > 1)
        {
            return false;
        }

        return IsTitleBarGestureTarget(source, boundary);
    }

    /// <summary>
    /// Returns <see langword="true"/> when a title-bar double tap should toggle window state.
    /// </summary>
    public static bool ShouldToggleWindowState(Visual? source, Visual boundary)
    {
        return IsTitleBarGestureTarget(source, boundary);
    }

    /// <summary>
    /// Computes the next state for a title-bar double tap.
    /// </summary>
    public static WindowState GetNextWindowState(WindowState currentState, bool canResize)
    {
        if (!canResize || currentState == WindowState.FullScreen)
        {
            return currentState;
        }

        return currentState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private static bool IsTitleBarGestureTarget(Visual? source, Visual boundary)
    {
        Visual? current = source;
        while (current is not null)
        {
            if (BlocksWindowDrag(current))
            {
                return false;
            }

            if (ReferenceEquals(current, boundary))
            {
                return true;
            }

            current = current.GetVisualParent();
        }

        return false;
    }

    private static bool BlocksWindowDrag(Visual visual)
    {
        if (visual is Button || visual is ToggleButton || visual is CheckBox || visual is TextBox || visual is Slider)
        {
            return true;
        }

        if (visual is Menu || visual is MenuItem || visual is SelectingItemsControl)
        {
            return true;
        }

        return false;
    }
}
