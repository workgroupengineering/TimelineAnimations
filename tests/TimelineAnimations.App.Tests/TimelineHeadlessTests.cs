using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.VisualTree;
using Dock.Avalonia.Controls;
using TimelineAnimations.App.Controls;
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
            Assert.NotNull(view.FindControl<Button>("DockOpenButton"));
            Assert.NotNull(view.FindControl<Button>("DockSaveButton"));
            Assert.NotNull(view.FindControl<Button>("DockProjectExportButton"));
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
}
