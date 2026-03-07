using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.VisualTree;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using TimelineAnimations.App.Controls;
using TimelineAnimations.App.Helpers;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.App.ViewModels.Dock;
using TimelineAnimations.App.Views.Dock;
using TimelineAnimations.App;

namespace TimelineAnimations.App.Tests;

internal static class HeadlessTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UseSkia()
            .WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false
            });
    }
}

public sealed class AvaloniaHeadlessFixture
{
    public static HeadlessUnitTestSession Session { get; } = HeadlessUnitTestSession.StartNew(typeof(HeadlessTestAppBuilder));
}

public sealed class TimelineHeadlessTests
{
    [Fact]
    public async Task DockWorkspaceHostView_Renders_InHeadlessWindow()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var viewModel = new MainWindowViewModel();
            viewModel.DockWorkspace.ApplyPresetCommand.Execute("Animate");
            viewModel.DockWorkspace.RestoreAllSurfacesCommand.Execute(null);

            var view = new DockWorkspaceHostView
            {
                DataContext = viewModel.DockWorkspace
            };

            Assert.NotNull(view.FindControl<DockControl>("WorkspaceDockControl"));
            Assert.NotNull(view.FindControl<ToggleButton>("DockSelectToolButton"));
        }, CancellationToken.None);
    }

    [Fact]
    public async Task DockWorkspaceHostView_BindsFactory_ForFloatingWindows()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var host = new MainWindowViewModel();
            var dockWorkspace = host.DockWorkspace;
            var view = new DockWorkspaceHostView
            {
                DataContext = dockWorkspace
            };

            var window = new Window
            {
                Width = 1400,
                Height = 900,
                Content = view
            };

            window.Show();
            window.UpdateLayout();

            var dockControl = view.FindControl<DockControl>("WorkspaceDockControl");

            Assert.NotNull(dockControl);
            Assert.Same(dockWorkspace.Factory, dockControl!.Factory);
            Assert.Same(dockWorkspace.HostWindowFactory, dockControl.HostWindowFactory);
            Assert.False(dockControl.InitializeFactory);
            Assert.False(dockControl.InitializeLayout);

            var sourceSurface = FindDockable(dockWorkspace.Layout, DockWorkspaceSurfaceIds.SourceMonitorTool);
            Assert.NotNull(sourceSurface);

            dockWorkspace.Factory.FloatDockable(sourceSurface!);

            Assert.NotNull(dockWorkspace.Layout);
            Assert.NotNull(dockWorkspace.Layout!.Windows);
            Assert.NotEmpty(dockWorkspace.Layout.Windows!);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task TitleBarDragHelper_AllowsDragOnlyForNonInteractiveChrome()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var titleBar = new TitleBarDragGrid();
            var title = new TextBlock { Text = "TimelineAnimations Studio" };
            var actionButton = new Button { Content = "Save" };

            titleBar.Children.Add(title);
            titleBar.Children.Add(actionButton);

            Assert.True(TitleBarDragHelper.ShouldBeginWindowDrag(title, titleBar));
            Assert.True(TitleBarDragHelper.ShouldBeginWindowDrag(titleBar, titleBar));
            Assert.False(TitleBarDragHelper.ShouldBeginWindowDrag(title, titleBar, clickCount: 2));
            Assert.False(TitleBarDragHelper.ShouldBeginWindowDrag(actionButton, titleBar));
            Assert.True(TitleBarDragHelper.ShouldToggleWindowState(title, titleBar));
            Assert.False(TitleBarDragHelper.ShouldToggleWindowState(actionButton, titleBar));
        }, CancellationToken.None);
    }

    [Fact]
    public async Task DockProjectToolView_Renders_Project_And_Assets_Workspace()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var host = new MainWindowViewModel();
            var view = new DockProjectToolView
            {
                DataContext = new DockProjectToolViewModel
                {
                    Host = host,
                    Id = "ProjectTool",
                    Title = "Project"
                }
            };

            var window = new Window
            {
                Width = 900,
                Height = 900,
                Content = view
            };

            window.Show();
            window.UpdateLayout();

            var tabs = view.FindControl<TabControl>("ProjectTabs");
            Assert.NotNull(tabs);

            tabs!.SelectedIndex = 1;
            window.UpdateLayout();

            Assert.Contains(view.GetVisualDescendants().OfType<PaletteItemControl>(), static _ => true);
            Assert.Contains(
                view.GetVisualDescendants().OfType<TextBlock>(),
                text => string.Equals(text.Text, "Interactive building blocks", StringComparison.Ordinal));
        }, CancellationToken.None);
    }

    [Fact]
    public async Task DockInspectorToolView_Renders_Classic_Property_And_Action_Surfaces()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var host = new MainWindowViewModel();
            var view = new DockInspectorToolView
            {
                DataContext = new DockInspectorToolViewModel
                {
                    Host = host,
                    Id = "InspectorTool",
                    Title = "Inspector"
                }
            };

            var window = new Window
            {
                Width = 900,
                Height = 1000,
                Content = view
            };

            window.Show();
            window.UpdateLayout();

            var tabs = view.FindControl<TabControl>("InspectorTabs");
            Assert.NotNull(tabs);
            Assert.Contains(
                view.GetVisualDescendants().OfType<TextBlock>(),
                text => string.Equals(text.Text, "Masks, blends, effects", StringComparison.Ordinal));

            tabs!.SelectedIndex = 1;
            window.UpdateLayout();

            Assert.Contains(
                view.GetVisualDescendants().OfType<TextBlock>(),
                text => string.Equals(text.Text, "Events and lightweight actions", StringComparison.Ordinal));
            Assert.Contains(
                view.GetVisualDescendants().OfType<TextBlock>(),
                text => string.Equals(text.Text, "Blend-style state authoring", StringComparison.Ordinal));
        }, CancellationToken.None);
    }

    [Fact]
    public async Task DockOutputToolView_Renders_Interop_And_Publish_Workspace()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var host = new MainWindowViewModel();
            var view = new DockOutputToolView
            {
                DataContext = new DockOutputToolViewModel
                {
                    Host = host,
                    Id = "OutputTool",
                    Title = "Output"
                }
            };

            var window = new Window
            {
                Width = 1200,
                Height = 1000,
                Content = view
            };

            window.Show();
            window.UpdateLayout();

            Assert.Contains(view.GetVisualDescendants().OfType<AnimationExchangeCodeEditorControl>(), static _ => true);
            Assert.Contains(view.GetVisualDescendants().OfType<AnimationExchangeVisualPreviewControl>(), static _ => true);
            Assert.Contains(
                view.GetVisualDescendants().OfType<TextBlock>(),
                text => string.Equals(text.Text, "Preview and export profiles", StringComparison.Ordinal));
        }, CancellationToken.None);
    }

    [Fact]
    public async Task DockFramesToolView_Renders_ExpandedTimelineToolbar()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var host = new MainWindowViewModel();
            var view = new DockFramesToolView
            {
                DataContext = new DockFramesToolViewModel
                {
                    Host = host,
                    Id = "FramesTimelineTool",
                    Title = "Frames"
                }
            };

            Assert.NotNull(view.FindControl<Button>("FramesStopButton"));
            Assert.NotNull(view.FindControl<ToggleButton>("FramesAutoToggle"));
            Assert.NotNull(view.FindControl<MenuItem>("FramesMarkersMenu"));
        }, CancellationToken.None);
    }

    [Fact]
    public async Task DockCurvesToolView_Renders_ExpandedCurveEditorSurface()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var host = new MainWindowViewModel();
            var view = new DockCurvesToolView
            {
                DataContext = new DockCurvesToolViewModel
                {
                    Host = host,
                    Id = "CurvesTimelineTool",
                    Title = "Curves"
                }
            };

            var window = new Window
            {
                Width = 1200,
                Height = 900,
                Content = view
            };

            window.Show();
            window.UpdateLayout();

            Assert.Contains(view.GetVisualDescendants().OfType<Button>(), button => string.Equals(button.Content as string, "Stop", StringComparison.Ordinal));
            Assert.Contains(view.GetVisualDescendants().OfType<ToggleButton>(), button => string.Equals(button.Content as string, "Auto", StringComparison.Ordinal));
            Assert.Contains(view.GetVisualDescendants().OfType<EasingCurveEditorControl>(), static _ => true);
            Assert.Contains(
                view.GetVisualDescendants().OfType<TextBlock>(),
                text => string.Equals(text.Text, "Shape tween", StringComparison.Ordinal));
        }, CancellationToken.None);
    }

    private static IDockable? FindDockable(IDockable? dockable, string id)
    {
        if (dockable is null)
        {
            return null;
        }

        if (string.Equals(dockable.Id, id, StringComparison.Ordinal))
        {
            return dockable;
        }

        if (dockable is IDock dock && dock.VisibleDockables is { Count: > 0 } visibleDockables)
        {
            foreach (var child in visibleDockables)
            {
                var match = FindDockable(child, id);
                if (match is not null)
                {
                    return match;
                }
            }
        }

        if (dockable is IRootDock rootDock)
        {
            foreach (var collection in new[]
                     {
                         rootDock.HiddenDockables,
                         rootDock.LeftPinnedDockables,
                         rootDock.RightPinnedDockables,
                         rootDock.TopPinnedDockables,
                         rootDock.BottomPinnedDockables
                     })
            {
                if (collection is null)
                {
                    continue;
                }

                foreach (var child in collection)
                {
                    var match = FindDockable(child, id);
                    if (match is not null)
                    {
                        return match;
                    }
                }
            }

            if (rootDock.Windows is { Count: > 0 } windows)
            {
                foreach (var dockWindow in windows)
                {
                    var match = FindDockable(dockWindow.Layout, id);
                    if (match is not null)
                    {
                        return match;
                    }
                }
            }
        }

        return null;
    }
}
