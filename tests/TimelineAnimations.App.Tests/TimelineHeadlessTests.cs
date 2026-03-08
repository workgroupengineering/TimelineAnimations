using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using TimelineAnimations.App.Controls;
using TimelineAnimations.App.Helpers;
using TimelineAnimations.App.Services;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.App.ViewModels.Dock;
using TimelineAnimations.App.Views.Dock;
using TimelineAnimations.App;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;
using TimelineAnimations.Rendering.Models;

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
    public async Task DockWorkspaceHostView_UsesFluentIcons_OnToolRail()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var viewModel = new MainWindowViewModel();
            var view = new DockWorkspaceHostView
            {
                DataContext = viewModel.DockWorkspace
            };

            var window = new Window
            {
                Width = 1400,
                Height = 900,
                Content = view
            };

            window.Show();
            window.UpdateLayout();

            var selectButton = view.FindControl<ToggleButton>("DockSelectToolButton");

            Assert.NotNull(selectButton);
            Assert.NotNull(ToolTip.GetTip(selectButton!));
            Assert.True(view.GetVisualDescendants().OfType<PathIcon>().Count() >= 12);
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
            Assert.Equal(WindowState.Maximized, TitleBarDragHelper.GetNextWindowState(WindowState.Normal, canResize: true));
            Assert.Equal(WindowState.Normal, TitleBarDragHelper.GetNextWindowState(WindowState.Maximized, canResize: true));
            Assert.Equal(WindowState.FullScreen, TitleBarDragHelper.GetNextWindowState(WindowState.FullScreen, canResize: true));
            Assert.Equal(WindowState.Normal, TitleBarDragHelper.GetNextWindowState(WindowState.Normal, canResize: false));
        }, CancellationToken.None);
    }

    [Fact]
    public async Task MainWindowNativeMenuFactory_CreatesCoreTopLevelMenus()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var viewModel = new MainWindowViewModel();
            var factory = new MainWindowNativeMenuFactory();
            var menu = factory.Create(
                viewModel,
                new MainWindowNativeMenuActions(
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    _ => Task.CompletedTask,
                    _ => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => { },
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => { },
                    () => { },
                    () => true,
                    () => true,
                    () => true));

            var headers = menu.Items
                .OfType<NativeMenuItem>()
                .Select(item => item.Header)
                .ToArray();

            Assert.Contains("_File", headers);
            Assert.Contains("_Edit", headers);
            Assert.Contains("_View", headers);
            Assert.Contains("_Insert", headers);
            Assert.Contains("Modify", headers);
            Assert.Contains("C_ontrol", headers);
            Assert.Contains("_Window", headers);
            Assert.Contains("_Help", headers);
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
    public async Task DockLibraryToolView_Renders_Library_Metadata_Workspace()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var host = new MainWindowViewModel();
            var view = new DockLibraryToolView
            {
                DataContext = new DockLibraryToolViewModel
                {
                    Host = host,
                    Id = "LibraryTool",
                    Title = "Library"
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

            Assert.Contains(
                view.GetVisualDescendants().OfType<TextBlock>(),
                text => string.Equals(text.Text, "Reusable symbols and assets", StringComparison.Ordinal));
            Assert.Contains(view.GetVisualDescendants().OfType<ListBox>(), static _ => true);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task DockMovieExplorerToolView_Renders_Search_And_Tree()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var host = new MainWindowViewModel();
            var view = new DockMovieExplorerToolView
            {
                DataContext = new DockMovieExplorerToolViewModel
                {
                    Host = host,
                    Id = "MovieExplorerTool",
                    Title = "Movie Explorer"
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

            Assert.Contains(view.GetVisualDescendants().OfType<TextBox>(), textBox => string.Equals(textBox.Watermark?.ToString(), "Search scenes, layers, markers, symbols, media...", StringComparison.Ordinal));
            Assert.Contains(view.GetVisualDescendants().OfType<TreeView>(), static _ => true);
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
            Assert.Contains(view.GetVisualDescendants().OfType<ColorPropertyEditorControl>(), static _ => true);

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
    public async Task DockHistoryToolView_Renders_RecentOperations()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var host = new MainWindowViewModel();
            var view = new DockHistoryToolView
            {
                DataContext = new DockHistoryToolViewModel
                {
                    Host = host,
                    Id = "HistoryTool",
                    Title = "History"
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

            Assert.Contains(
                view.GetVisualDescendants().OfType<TextBlock>(),
                text => string.Equals(text.Text, "Recent editing operations", StringComparison.Ordinal));
            Assert.Contains(view.GetVisualDescendants().OfType<Button>(), button => string.Equals(button.Content as string, "Clear", StringComparison.Ordinal));
        }, CancellationToken.None);
    }

    [Fact]
    public async Task ColorPropertyEditorControl_UpdatesValue_FromPopupSliders()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var view = new ColorPropertyEditorControl
            {
                Value = "#112233"
            };

            var window = new Window
            {
                Width = 360,
                Height = 240,
                Content = view
            };

            window.Show();
            window.UpdateLayout();

            var popup = view.FindControl<Popup>("PickerPopup");
            var redSlider = view.FindControl<Slider>("RedSlider");
            var greenSlider = view.FindControl<Slider>("GreenSlider");
            var blueSlider = view.FindControl<Slider>("BlueSlider");

            Assert.NotNull(popup);
            Assert.NotNull(redSlider);
            Assert.NotNull(greenSlider);
            Assert.NotNull(blueSlider);

            popup!.IsOpen = true;
            redSlider!.Value = 64;
            greenSlider!.Value = 128;
            blueSlider!.Value = 192;

            Assert.Equal("#4080C0", view.Value);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task StudioSliderTheme_Uses_Compact_AdobeStyle_ThumbGeometry()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var horizontalSlider = new Slider
            {
                Width = 180,
                Minimum = 0,
                Maximum = 100,
                Value = 48
            };

            var verticalSlider = new Slider
            {
                Height = 180,
                Minimum = 0,
                Maximum = 100,
                Value = 48,
                Orientation = Avalonia.Layout.Orientation.Vertical
            };

            var view = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    horizontalSlider,
                    verticalSlider
                }
            };

            var window = new Window
            {
                Width = 260,
                Height = 300,
                Content = view
            };

            window.Show();
            window.UpdateLayout();

            var horizontalThumb = horizontalSlider.GetVisualDescendants().OfType<Thumb>().FirstOrDefault();
            var verticalThumb = verticalSlider.GetVisualDescendants().OfType<Thumb>().FirstOrDefault();

            Assert.NotNull(horizontalThumb);
            Assert.NotNull(verticalThumb);
            Assert.True(horizontalSlider.GetVisualDescendants().OfType<RepeatButton>().Count() >= 2);
            Assert.True(verticalSlider.GetVisualDescendants().OfType<RepeatButton>().Count() >= 2);

            Assert.InRange(horizontalThumb!.Bounds.Width, 8.5, 9.5);
            Assert.InRange(horizontalThumb.Bounds.Height, 15.5, 16.5);
            Assert.InRange(verticalThumb!.Bounds.Width, 15.5, 16.5);
            Assert.InRange(verticalThumb.Bounds.Height, 8.5, 9.5);
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
    public async Task AnimationExchangeVisualPreviewControl_Loads_RuntimeAvaloniaPreview()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var preview = AnimationExchangePreviewService.BuildPreview(
                SampleProjectFactory.Create(),
                TimelineAnimations.Core.Models.AnimationExchangeFormat.AvaloniaXaml);

            var view = new AnimationExchangeVisualPreviewControl
            {
                Format = TimelineAnimations.Core.Models.AnimationExchangeFormat.AvaloniaXaml,
                PreviewText = preview.Code,
                PreviewDocument = preview.PreviewDocument,
                PreviewTime = 0d,
                PreviewSummary = preview.VisualSummary
            };

            var window = new Window
            {
                Width = 1200,
                Height = 900,
                Content = view
            };

            window.Show();
            window.UpdateLayout();

            var runtimeHost = view.FindControl<ContentPresenter>("RuntimePreviewHost");
            var caption = view.FindControl<TextBlock>("PreviewCaption");

            Assert.NotNull(runtimeHost);
            Assert.NotNull(caption);
            Assert.True(
                runtimeHost!.Content is not null,
                $"Runtime host was empty. Caption: {caption!.Text}");
            Assert.DoesNotContain("failed", caption!.Text ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task AnimationExchangeVisualPreviewControl_Renders_BitmapPreview_With_SelectedEngine()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var previewDocument = SampleProjectFactory.Create();
            var view = new AnimationExchangeVisualPreviewControl
            {
                Format = TimelineAnimations.Core.Models.AnimationExchangeFormat.HtmlCss,
                PreviewDocument = previewDocument,
                PreviewTime = 0d,
                PreviewSummary = "Bitmap preview",
                RenderingEngine = RenderingEngineKind.SkiaSharp
            };

            var window = new Window
            {
                Width = 1200,
                Height = 900,
                Content = view
            };

            window.Show();
            window.UpdateLayout();

            var bitmapPreview = view.FindControl<Image>("BitmapPreviewImage");
            var caption = view.FindControl<TextBlock>("PreviewCaption");

            Assert.NotNull(bitmapPreview);
            Assert.NotNull(caption);
            Assert.NotNull(bitmapPreview!.Source);
            Assert.Contains("Bitmap preview", caption!.Text ?? string.Empty, StringComparison.Ordinal);
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

            var window = new Window
            {
                Width = 1200,
                Height = 900,
                Content = view
            };

            window.Show();
            window.UpdateLayout();

            Assert.True(view.GetVisualDescendants().OfType<Button>().Any());
            Assert.True(view.GetVisualDescendants().OfType<ToggleButton>().Any());
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
            Assert.NotNull(view.FindControl<TimelineEditorControl>("TimelineEditorHeader"));
            Assert.NotNull(view.FindControl<TimelineEditorControl>("TimelineEditorBody"));
            Assert.Contains(
                view.GetVisualDescendants().OfType<TextBlock>(),
                text => string.Equals(text.Text, "Shape tween", StringComparison.Ordinal));
        }, CancellationToken.None);
    }

    [Fact]
    public async Task CompositeFrameRenderer_Renders_AnimateBlendModes()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var darken = RenderCompositeBlendPixel(LayerBlendMode.Darken);
            var lighten = RenderCompositeBlendPixel(LayerBlendMode.Lighten);
            var difference = RenderCompositeBlendPixel(LayerBlendMode.Difference);

            Assert.Equal((byte)80, darken.R);
            Assert.Equal((byte)100, darken.G);
            Assert.Equal((byte)40, darken.B);

            Assert.Equal((byte)200, lighten.R);
            Assert.Equal((byte)180, lighten.G);
            Assert.Equal((byte)220, lighten.B);

            Assert.Equal((byte)120, difference.R);
            Assert.Equal((byte)80, difference.G);
            Assert.Equal((byte)180, difference.B);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task CompositeFrameRenderer_Renders_AlphaAndEraseBlendModesAsMasks()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var alpha = RenderCompositeBlendPixel(LayerBlendMode.Alpha, 0.5d);
            var erase = RenderCompositeBlendPixel(LayerBlendMode.Erase, 0.5d);

            Assert.InRange(alpha.A, (byte)120, (byte)155);
            Assert.Equal((byte)200, alpha.R);
            Assert.Equal((byte)100, alpha.G);
            Assert.Equal((byte)40, alpha.B);

            Assert.InRange(erase.A, (byte)100, (byte)155);
            Assert.Equal((byte)200, erase.R);
            Assert.Equal((byte)100, erase.G);
            Assert.Equal((byte)40, erase.B);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task RenderingEngineHostService_Renders_With_Avalonia_And_Skia_Engines()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var state = new CompositeSceneState(
                0d,
                80d,
                80d,
                false,
                "#000000",
                "#000000",
                new AnimateDocumentSettings(),
                new Dictionary<Guid, LibraryItem>(),
                new Dictionary<Guid, MediaAsset>(),
                [
                    CreateRenderableSample(10d, 10d, 40d, 40d, "#C86428", LayerBlendMode.Normal, 1d),
                    CreateRenderableSample(10d, 10d, 40d, 40d, "#50B4DC", LayerBlendMode.Screen, 0.8d)
                ],
                activeCamera: null);

            using var avaloniaBitmap = RenderingEngineHostService.RenderBitmap(state, RenderingEngineKind.Avalonia, includeBackground: false);
            using var skiaBitmap = RenderingEngineHostService.RenderBitmap(state, RenderingEngineKind.SkiaSharp, includeBackground: false);

            var avaloniaPixel = ReadPixel(avaloniaBitmap, 20, 20);
            var skiaPixel = ReadPixel(skiaBitmap, 20, 20);

            AssertColorClose(avaloniaPixel, skiaPixel, 1);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task RenderingEngineHostService_Renders_Skia_Native_Text_Path_And_Control_Content()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var state = new CompositeSceneState(
                0d,
                240d,
                140d,
                false,
                "#000000",
                "#000000",
                new AnimateDocumentSettings(),
                new Dictionary<Guid, LibraryItem>(),
                new Dictionary<Guid, MediaAsset>(),
                [
                    CreateRenderableSample(10d, 10d, 50d, 50d, "#C86428", LayerBlendMode.Normal, 1d),
                    CreateTextSample(70d, 10d, 90d, 40d, "Hi"),
                    CreatePathSample(),
                    CreateAvaloniaButtonSample()
                ],
                activeCamera: null);

            using var skiaBitmap = RenderingEngineHostService.RenderBitmap(state, RenderingEngineKind.SkiaSharp, includeBackground: false);

            Assert.True(ReadPixel(skiaBitmap, 30, 30).A > 0);
            Assert.True(ReadPixel(skiaBitmap, 90, 25).A > 0);
            Assert.True(ReadPixel(skiaBitmap, 180, 40).A > 0);
            Assert.True(ReadPixel(skiaBitmap, 45, 95).A > 0);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task RenderingEngineHostService_Renders_Solid_Stage_Background_And_Transparent_Mode()
    {
        await AvaloniaHeadlessFixture.Session.Dispatch(() =>
        {
            var opaqueState = new CompositeSceneState(
                0d,
                48d,
                48d,
                false,
                "#204060",
                "#204060",
                new AnimateDocumentSettings(),
                new Dictionary<Guid, LibraryItem>(),
                new Dictionary<Guid, MediaAsset>(),
                [],
                activeCamera: null);
            using var opaqueBitmap = RenderingEngineHostService.RenderBitmap(opaqueState, RenderingEngineKind.Avalonia, includeBackground: true);
            var opaquePixel = ReadPixel(opaqueBitmap, 10, 10);
            Assert.Equal((byte)255, opaquePixel.A);
            Assert.Equal((byte)0x20, opaquePixel.R);
            Assert.Equal((byte)0x40, opaquePixel.G);
            Assert.Equal((byte)0x60, opaquePixel.B);

            var transparentState = new CompositeSceneState(
                0d,
                48d,
                48d,
                true,
                "#204060",
                "#204060",
                new AnimateDocumentSettings(),
                new Dictionary<Guid, LibraryItem>(),
                new Dictionary<Guid, MediaAsset>(),
                [],
                activeCamera: null);
            using var transparentBitmap = RenderingEngineHostService.RenderBitmap(transparentState, RenderingEngineKind.Avalonia, includeBackground: true);
            var transparentPixel = ReadPixel(transparentBitmap, 10, 10);
            Assert.Equal((byte)0, transparentPixel.A);
        }, CancellationToken.None);
    }

    private static Color RenderCompositeBlendPixel(LayerBlendMode blendMode, double sourceOpacity = 1d)
    {
        var backdrop = CreateRenderableSample(10d, 10d, 40d, 40d, "#C86428", LayerBlendMode.Normal, 1d);
        var source = CreateRenderableSample(10d, 10d, 40d, 40d, "#50B4DC", blendMode, sourceOpacity);
        var state = new CompositeSceneState(
            0d,
            80d,
            80d,
            false,
            "#000000",
            "#000000",
            new AnimateDocumentSettings(),
            new Dictionary<Guid, LibraryItem>(),
            new Dictionary<Guid, MediaAsset>(),
            [backdrop, source],
            activeCamera: null);

        using var bitmap = CompositeFrameRenderer.RenderBitmap(state, includeBackground: false);
        return ReadPixel(bitmap, 20, 20);
    }

    private static RenderableLayerSample CreateRenderableSample(double x, double y, double width, double height, string fill, LayerBlendMode blendMode, double opacity)
    {
        var snapshot = new LayerSnapshot(
            x,
            y,
            width,
            height,
            1d,
            1d,
            0d,
            0d,
            0d,
            opacity,
            0d,
            0d,
            0d,
            0d,
            0d,
            fill,
            fill,
            string.Empty,
            0d,
            0d,
            false,
            fill,
            fill,
            ShapeDrawingMode.Merge,
            PrimitiveShapeType.Rectangle,
            false,
            0d,
            360d,
            false,
            "#57C9FF",
            [],
            5,
            0.46d,
            true,
            new AvaloniaControlSettings(),
            new LayerCompositeSettings { BlendMode = blendMode },
            new LayerTextSettings())
        {
            HasStroke = false
        };

        return new RenderableLayerSample
        {
            SourceLayerId = Guid.NewGuid(),
            Kind = LayerKind.Rectangle,
            ZIndex = blendMode == LayerBlendMode.Normal ? 0 : 1,
            Snapshot = snapshot
        };
    }

    private static RenderableLayerSample CreateTextSample(double x, double y, double width, double height, string text)
    {
        var snapshot = new LayerSnapshot(
            x,
            y,
            width,
            height,
            1d,
            1d,
            0d,
            0d,
            0d,
            1d,
            16d,
            16d,
            16d,
            16d,
            16d,
            "#F7F5ED",
            "#7FE9FF",
            text,
            20d,
            1.6d,
            false,
            "#F7F5ED",
            "#7FE9FF",
            ShapeDrawingMode.Merge,
            PrimitiveShapeType.None,
            false,
            0d,
            360d,
            false,
            "#7FE9FF",
            [],
            5,
            0.46d,
            true,
            new AvaloniaControlSettings(),
            new LayerCompositeSettings { BlendMode = LayerBlendMode.Normal },
            new LayerTextSettings())
        {
            HasStroke = true
        };

        return new RenderableLayerSample
        {
            SourceLayerId = Guid.NewGuid(),
            Kind = LayerKind.Text,
            ZIndex = 1,
            Snapshot = snapshot
        };
    }

    private static RenderableLayerSample CreatePathSample()
    {
        var snapshot = new LayerSnapshot(
            0d,
            0d,
            240d,
            140d,
            1d,
            1d,
            0d,
            0d,
            0d,
            1d,
            0d,
            0d,
            0d,
            0d,
            0d,
            "#2DD4BF",
            "#8EEAFF",
            string.Empty,
            0d,
            2d,
            false,
            "#2DD4BF",
            "#8EEAFF",
            ShapeDrawingMode.Merge,
            PrimitiveShapeType.None,
            true,
            0d,
            360d,
            false,
            "#8EEAFF",
            [
                new VectorPointModel { X = 165d, Y = 20d },
                new VectorPointModel { X = 205d, Y = 65d },
                new VectorPointModel { X = 160d, Y = 85d }
            ],
            5,
            0.46d,
            true,
            new AvaloniaControlSettings(),
            new LayerCompositeSettings { BlendMode = LayerBlendMode.Normal },
            new LayerTextSettings());

        return new RenderableLayerSample
        {
            SourceLayerId = Guid.NewGuid(),
            Kind = LayerKind.Path,
            ZIndex = 2,
            Snapshot = snapshot
        };
    }

    private static RenderableLayerSample CreateAvaloniaButtonSample()
    {
        var snapshot = new LayerSnapshot(
            20d,
            70d,
            90d,
            40d,
            1d,
            1d,
            0d,
            0d,
            0d,
            1d,
            18d,
            18d,
            18d,
            18d,
            18d,
            "#1F7DFF",
            "#9BCAFF",
            string.Empty,
            0d,
            1.6d,
            false,
            "#1F7DFF",
            "#9BCAFF",
            ShapeDrawingMode.Merge,
            PrimitiveShapeType.None,
            true,
            0d,
            360d,
            false,
            "#9BCAFF",
            [],
            5,
            0.46d,
            true,
            new AvaloniaControlSettings
            {
                Kind = AvaloniaControlKind.Button,
                Content = "Go"
            },
            new LayerCompositeSettings { BlendMode = LayerBlendMode.Normal },
            new LayerTextSettings());

        return new RenderableLayerSample
        {
            SourceLayerId = Guid.NewGuid(),
            Kind = LayerKind.AvaloniaControl,
            ZIndex = 3,
            Snapshot = snapshot
        };
    }

    private static Color ReadPixel(WriteableBitmap bitmap, int x, int y)
    {
        using var framebuffer = bitmap.Lock();
        var offset = (y * framebuffer.RowBytes) + (x * 4);
        var pixels = new byte[4];
        Marshal.Copy(framebuffer.Address + offset, pixels, 0, 4);
        return Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);
    }

    private static void AssertColorClose(Color expected, Color actual, byte tolerance)
    {
        Assert.InRange(actual.A, Math.Max(0, expected.A - tolerance), Math.Min(255, expected.A + tolerance));
        Assert.InRange(actual.R, Math.Max(0, expected.R - tolerance), Math.Min(255, expected.R + tolerance));
        Assert.InRange(actual.G, Math.Max(0, expected.G - tolerance), Math.Min(255, expected.G + tolerance));
        Assert.InRange(actual.B, Math.Max(0, expected.B - tolerance), Math.Min(255, expected.B + tolerance));
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
