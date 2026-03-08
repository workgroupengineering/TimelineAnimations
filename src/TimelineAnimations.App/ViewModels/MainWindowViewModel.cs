using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimelineAnimations.App.Controls;
using TimelineAnimations.App.Services;
using TimelineAnimations.App.Models;
using TimelineAnimations.App.ViewModels.Dock;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;
using TimelineAnimations.Rendering.Models;

namespace TimelineAnimations.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const double MinCanvasZoomFactor = 0.25d;
    private const double MaxCanvasZoomFactor = 8d;
    private const double MinTimelineDockHeight = 300d;
    private const double MaxTimelineDockHeight = 960d;
    private const int ProgramMonitorPlaybackRefreshIntervalMilliseconds = 125;
    private const int PlaybackInspectorRefreshIntervalMilliseconds = 66;
    private const int PlaybackTimelinePreviewRefreshIntervalMilliseconds = 125;
    private const int PlaybackAudioMeterRefreshIntervalMilliseconds = 100;
    private readonly DispatcherTimer _playbackTimer;
    private readonly Stopwatch _playbackClock = new();
    private TimelineDocument _document = SampleProjectFactory.Create();
    private UndoRedoStack<EditorStateSnapshot>? _history;
    private bool _isApplyingHistory;
    private bool _isInteractiveChange;
    private InteractiveChangeKind _interactiveChangeKind = InteractiveChangeKind.Generic;
    private bool _suppressSceneSelection;
    private bool _suppressSceneEditor;
    private bool _suppressSceneSurfaceEditor;
    private bool _suppressFrameSelectionRefresh;
    private bool _suppressAnimateDocumentEditor;
    private bool _suppressSelectedKeyframeEditor;
    private bool _suppressInspector;
    private bool _suppressCustomEasingEditor;
    private bool _suppressFrameRateEditor;
    private bool _suppressSymbolInstanceEditor;
    private bool _suppressButtonStateEditor;
    private bool _suppressBehaviorEditor;
    private bool _suppressPublishProfileEditor;
    private bool _suppressFrameActionScriptEditor;
    private bool _suppressLibraryItemEditor;
    private bool _suppressComponentParameterEditor;
    private bool _suppressComponentOverrideEditor;
    private bool _suppressLibraryFilterSelection;
    private bool _suppressWorkspacePreset;
    private bool _isApplyingWorkspaceLayout;
    private bool _workspaceLayoutLoaded;
    private double _playbackOriginTime;
    private FrameRangeClipboard? _frameClipboard;
    private Guid? _editingLibraryItemId;
    private Guid? _editingSceneId;
    private Guid? _editingSceneLayerId;
    private EditorStateSnapshot? _prototypeSnapshot;
    private readonly Dictionary<string, string> _prototypeVariables = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<Guid> _collapsedTimelineLayers = [];
    private WorkspacePanelMode _leftRestoreMode = WorkspacePanelMode.Docked;
    private WorkspacePanelMode _rightRestoreMode = WorkspacePanelMode.Docked;
    private WorkspacePanelMode _timelineRestoreMode = WorkspacePanelMode.Docked;
    private bool _isNativeMenuExported;
    private bool _capturePanelHistory;
    private long _lastProgramMonitorRefreshTick = -1;
    private long _lastPlaybackInspectorRefreshTick = -1;
    private long _lastPlaybackTimelinePreviewRefreshTick = -1;
    private long _lastPlaybackAudioRefreshTick = -1;

    public MainWindowViewModel()
    {
        PaletteItems = new ObservableCollection<PaletteItemViewModel>
        {
            new()
            {
                Title = "Neon Card",
                Subtitle = "Rounded focal shape",
                Fill = "#24E5C1",
                Accent = "#9BFFF0",
                Kind = LayerKind.Rectangle,
                DragData = "palette:shape:Rectangle"
            },
            new()
            {
                Title = "Solar Orb",
                Subtitle = "Soft circular light",
                Fill = "#FF8A4C",
                Accent = "#FFE0B8",
                Kind = LayerKind.Ellipse,
                DragData = "palette:shape:Ellipse"
            },
            new()
            {
                Title = "Headline",
                Subtitle = "Large text caption",
                Fill = "#F7F5ED",
                Accent = "#B6C8FF",
                Kind = LayerKind.Text,
                DragData = "palette:shape:Text"
            }
        };
        AvaloniaToolboxItems = new ObservableCollection<PaletteItemViewModel>
        {
            CreateAvaloniaPaletteItem(AvaloniaControlKind.Border, "Card Surface", "Bordered visual container", "#243651", "#8EEAFF"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.Button, "Button", "Native button control", "#1F7DFF", "#9BCAFF"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.TextBlock, "TextBlock", "Display text visual", "#F7F5ED", "#B6C8FF"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.TextBox, "TextBox", "Editable text input", "#25364B", "#9FD4FF"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.CheckBox, "CheckBox", "Boolean option control", "#263953", "#8FF1D6"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.ToggleButton, "Toggle", "Two-state action button", "#8B5CF6", "#D3C6FF"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.Slider, "Slider", "Ranged value editor", "#283D57", "#57C9FF"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.ProgressBar, "Progress", "Progress feedback strip", "#204A62", "#53E3C4"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.ComboBox, "ComboBox", "Selection dropdown control", "#304860", "#B6D7FF"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.ListBox, "ListBox", "Selectable item list", "#233B54", "#9BD1FF"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.TabControl, "Tabs", "Tabbed content surface", "#2E355A", "#D0C4FF"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.Grid, "Grid", "Cell-based layout surface", "#233149", "#A8C4FF"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.StackPanel, "Stack", "Stacked layout host", "#1C3552", "#8FE2FF"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.PathIcon, "PathIcon", "Vector icon visual", "#1B2A45", "#7FE9FF"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.Image, "Image", "Image placeholder visual", "#3B445A", "#FFD58C"),
            CreateAvaloniaPaletteItem(AvaloniaControlKind.Panel, "Panel", "Layout surface / visual host", "#1E2D44", "#A8C4FF")
        };
        SeedColorSwatches();
        var rendererPreferences = RendererPreferencesPersistenceService.Load();
        selectedRenderingEngine = rendererPreferences.SelectedEngine;
        RenderingEngineHostService.SelectedEngine = rendererPreferences.SelectedEngine;

        _playbackTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Background, HandlePlaybackTick);
        LoadDocument(SampleProjectFactory.Create(), "Sample Composition");
        RestoreWorkspaceLayout();
        _workspaceLayoutLoaded = true;
        DockWorkspace = new DockWorkspaceHostViewModel(this);
        _capturePanelHistory = true;
        AppendHistoryEntry(StatusMessage);
    }

    private static PaletteItemViewModel CreateAvaloniaPaletteItem(
        AvaloniaControlKind kind,
        string title,
        string subtitle,
        string fill,
        string accent)
    {
        return new PaletteItemViewModel
        {
            Title = title,
            Subtitle = subtitle,
            Fill = fill,
            Accent = accent,
            Kind = LayerKind.AvaloniaControl,
            AvaloniaControlKind = kind,
            DragData = $"palette:control:{kind}"
        };
    }

    public ObservableCollection<LayerViewModel> Layers { get; } = [];

    public ObservableCollection<SceneViewModel> Scenes { get; } = [];

    public ObservableCollection<LibraryItemViewModel> LibraryItems { get; } = [];

    public ObservableCollection<string> LibraryFolderFilters { get; } = [];

    public ObservableCollection<LibraryItemViewModel> ComponentItems { get; } = [];

    public ObservableCollection<ComponentParameterDefinitionViewModel> SelectedComponentParameters { get; } = [];

    public ObservableCollection<ComponentParameterOverrideViewModel> SelectedComponentOverrideItems { get; } = [];

    public ObservableCollection<MediaAssetViewModel> MediaAssets { get; } = [];

    public ObservableCollection<InteractionBehaviorViewModel> Behaviors { get; } = [];

    public ObservableCollection<ActionScriptSnippetViewModel> ActionScriptSnippets { get; } = [];

    public ObservableCollection<MotionPresetViewModel> MotionPresets { get; } = [];

    public ObservableCollection<RigPoseViewModel> RigPoses { get; } = [];

    public ObservableCollection<PublishProfileViewModel> PublishProfiles { get; } = [];

    public ObservableCollection<PublishValidationIssue> PublishValidationIssues { get; } = [];

    public ObservableCollection<PublishValidationIssue> AnimateDocumentCompatibilityIssues { get; } = [];

    public ObservableCollection<PropertyTrackViewModel> Tracks { get; } = [];

    public ObservableCollection<LayerFrameTimelineRowViewModel> FrameRows { get; } = [];

    public ObservableCollection<FrameLabelViewModel> SceneFrameLabels { get; } = [];

    public ObservableCollection<SceneMarkerViewModel> SceneMarkers { get; } = [];

    public ObservableCollection<TimelineTrackRowViewModel> TimelineRows { get; } = [];

    public ObservableCollection<VisualStateGroupViewModel> VisualStateGroups { get; } = [];

    public ObservableCollection<VisualStateViewModel> VisualStates { get; } = [];

    public ObservableCollection<PaletteItemViewModel> PaletteItems { get; }

    public ObservableCollection<PaletteItemViewModel> AvaloniaToolboxItems { get; }

    public ObservableCollection<AnimationExchangeIssue> AnimationExchangeIssues { get; } = [];

    public ObservableCollection<AnimationExchangeIssue> AnimationExchangePreviewIssues { get; } = [];

    public ObservableCollection<ColorSwatchViewModel> ColorSwatches { get; } = [];

    public ObservableCollection<HistoryEntryViewModel> HistoryEntries { get; } = [];

    public ObservableCollection<MovieExplorerNodeViewModel> MovieExplorerNodes { get; } = [];

    public DockWorkspaceHostViewModel DockWorkspace { get; }

    public IReadOnlyList<EasingKind> AvailableEasings { get; } = Enum.GetValues<EasingKind>();

    public IReadOnlyList<SymbolKind> AvailableSymbolKinds { get; } = Enum.GetValues<SymbolKind>();

    public IReadOnlyList<SymbolPlaybackMode> AvailableSymbolPlaybackModes { get; } = Enum.GetValues<SymbolPlaybackMode>();

    public IReadOnlyList<ButtonVisualState> AvailableButtonStates { get; } = Enum.GetValues<ButtonVisualState>();

    public IReadOnlyList<SceneMarkerKind> AvailableSceneMarkerKinds { get; } = Enum.GetValues<SceneMarkerKind>();

    public IReadOnlyList<SceneTransitionKind> AvailableSceneTransitionKinds { get; } = Enum.GetValues<SceneTransitionKind>();

    public IReadOnlyList<InteractionTriggerKind> AvailableInteractionTriggers { get; } = Enum.GetValues<InteractionTriggerKind>();

    public IReadOnlyList<InteractionActionKind> AvailableInteractionActions { get; } = Enum.GetValues<InteractionActionKind>();

    public IReadOnlyList<PublishOutputKind> AvailablePublishOutputs { get; } = Enum.GetValues<PublishOutputKind>();

    public IReadOnlyList<AnimateDocumentType> AvailableAnimateDocumentTypes { get; } = Enum.GetValues<AnimateDocumentType>();

    public IReadOnlyList<AnimateDocumentUnits> AvailableAnimateDocumentUnits { get; } = Enum.GetValues<AnimateDocumentUnits>();

    public IReadOnlyList<ComponentParameterKind> AvailableComponentParameterKinds { get; } = Enum.GetValues<ComponentParameterKind>();

    public IReadOnlyList<ComponentParameterBindingTarget> AvailableComponentParameterBindingTargets { get; } = Enum.GetValues<ComponentParameterBindingTarget>();

    public IReadOnlyList<LayerCompositeRole> AvailableCompositeRoles { get; } = Enum.GetValues<LayerCompositeRole>();

    public IReadOnlyList<LayerBlendMode> AvailableBlendModes { get; } = Enum.GetValues<LayerBlendMode>();

    public IReadOnlyList<FlashColorEffectMode> AvailableFlashColorEffectModes { get; } = Enum.GetValues<FlashColorEffectMode>();

    public IReadOnlyList<LayerBevelType> AvailableBevelTypes { get; } = Enum.GetValues<LayerBevelType>();

    public IReadOnlyList<LayerGradientKind> AvailableGradientKinds { get; } = Enum.GetValues<LayerGradientKind>();

    public IReadOnlyList<ShapeDrawingMode> AvailableShapeDrawingModes { get; } = Enum.GetValues<ShapeDrawingMode>();

    public IReadOnlyList<VectorHandleMode> AvailableVectorHandleModes { get; } = Enum.GetValues<VectorHandleMode>();

    public IReadOnlyList<LayerStrokeCapStyle> AvailableStrokeCapStyles { get; } = Enum.GetValues<LayerStrokeCapStyle>();

    public IReadOnlyList<LayerStrokeJoinStyle> AvailableStrokeJoinStyles { get; } = Enum.GetValues<LayerStrokeJoinStyle>();

    public IReadOnlyList<MediaPlaybackMode> AvailableMediaPlaybackModes { get; } = Enum.GetValues<MediaPlaybackMode>();

    public IReadOnlyList<LayerTextAlignment> AvailableTextAlignments { get; } = Enum.GetValues<LayerTextAlignment>();

    public IReadOnlyList<FlashTextFieldKind> AvailableTextFieldKinds { get; } = Enum.GetValues<FlashTextFieldKind>();

    public IReadOnlyList<FlashTextLineMode> AvailableTextLineModes { get; } = Enum.GetValues<FlashTextLineMode>();

    public IReadOnlyList<FlashTextAntiAliasMode> AvailableTextAntiAliasModes { get; } = Enum.GetValues<FlashTextAntiAliasMode>();

    public IReadOnlyList<WorkspacePanelMode> AvailableWorkspacePanelModes { get; } = Enum.GetValues<WorkspacePanelMode>();

    public IReadOnlyList<WorkspaceLayoutPreset> AvailableWorkspacePresets { get; } = Enum.GetValues<WorkspaceLayoutPreset>();

    public IReadOnlyList<string> AvailableWorkspaceFocusTargets { get; } = ["Classic", "Stage", "Timeline", "Tools", "Inspector"];

    public IReadOnlyList<AnimationExchangeFormat> AvailableAnimationExchangeFormats { get; } = Enum.GetValues<AnimationExchangeFormat>();

    public IReadOnlyList<AnimationExchangePreviewMode> AvailableAnimationExchangePreviewModes { get; } = Enum.GetValues<AnimationExchangePreviewMode>();

    public IReadOnlyList<RenderingEngineKind> AvailableRenderingEngines { get; } = RenderingEngineHostService.AvailableEngines;

    public IReadOnlyList<ZoomToolMode> AvailableZoomToolModes { get; } = Enum.GetValues<ZoomToolMode>();

    public IReadOnlyList<LassoToolMode> AvailableLassoToolModes { get; } = Enum.GetValues<LassoToolMode>();

    public IReadOnlyList<PencilToolMode> AvailablePencilToolModes { get; } = Enum.GetValues<PencilToolMode>();

    public IReadOnlyList<EraserToolMode> AvailableEraserToolModes { get; } = Enum.GetValues<EraserToolMode>();

    public IReadOnlyList<VectorStrokeProfilePreset> AvailableVectorStrokeProfiles { get; } = Enum.GetValues<VectorStrokeProfilePreset>();

    public IReadOnlyList<PaintBucketGapSize> AvailablePaintBucketGapSizes { get; } = Enum.GetValues<PaintBucketGapSize>();

    public TimelineDocument Document => _document;

    public bool IsEditingSymbol => _editingLibraryItemId is not null;

    public LibraryItem? EditingLibraryItem => _editingLibraryItemId is Guid libraryItemId
        ? _document.LibraryItems.FirstOrDefault(item => item.Id == libraryItemId)
        : null;

    public string CurrentTimeLabel => $"{CurrentTime:0.00}s";

    public string DurationLabel => $"{Duration:0.00}s";

    public string FrameRateLabel => $"{SceneFrameRate:0.#} fps";

    public string CanvasSizeLabel => $"{CanvasWidth:0} × {CanvasHeight:0}";

    public string CanvasZoomLabel => $"{CanvasZoom * 100:0}%";

    public string CanvasViewRotationLabel => $"{CanvasViewRotation:0}°";

    public string RenderingEngineSummary => SelectedRenderingEngine switch
    {
        RenderingEngineKind.SkiaSharp => "SkiaSharp renderer active for stage, preview, and export output.",
        _ => "Avalonia renderer active for stage, preview, and export output."
    };

    public string CanvasViewportHint => SelectedDrawingTool switch
    {
        DrawingTool.Hand => "Drag to pan the viewport. Middle-drag and Space-drag still pan as a temporary hand tool.",
        DrawingTool.Zoom => ZoomToolModeEditor == ZoomToolMode.Out
            ? "Click to zoom out or drag a marquee to zoom to a region."
            : "Click to zoom in or drag a marquee to zoom to a region.",
        DrawingTool.RotateView => "Drag across the stage to rotate the view. Reset returns to 0°.",
        DrawingTool.Width => "Drag selected vector width points horizontally to taper or widen the stroke.",
        _ => "Wheel pans, Ctrl/Cmd+wheel zooms, middle-drag pans, and Fit restores the stage."
    };

    public string ApplicationTitle => "TimelineAnimations Studio";

    public bool UseIntegratedTitleBar => OperatingSystem.IsMacOS();

    public bool IsNativeMenuExported => _isNativeMenuExported;

    public bool IsFallbackAppMenuVisible => !_isNativeMenuExported;

    public bool UseNativeMenuTitleBarLayout => UseIntegratedTitleBar && _isNativeMenuExported;

    public bool ShowWorkspacePresetStrip => !UseNativeMenuTitleBarLayout;

    public bool ShowSecondaryWorkspaceActions => !UseNativeMenuTitleBarLayout;

    public bool ShowDockWorkspaceOrganizerMenus => !UseNativeMenuTitleBarLayout;

    public ExtendClientAreaChromeHints IntegratedTitleBarChromeHints => UseIntegratedTitleBar
        ? ExtendClientAreaChromeHints.PreferSystemChrome | ExtendClientAreaChromeHints.OSXThickTitleBar
        : ExtendClientAreaChromeHints.Default;

    public double IntegratedTitleBarHeightHint => UseNativeMenuTitleBarLayout
        ? 54
        : UseIntegratedTitleBar ? 44 : -1;

    public Thickness WindowContentMargin => UseIntegratedTitleBar
        ? new Thickness(6, 4, 6, 6)
        : new Thickness(6);

    public Thickness TitleBarChromePadding => UseIntegratedTitleBar
        ? new Thickness(4, 7, 4, 3)
        : new Thickness(4, 3);

    public double TitleBarLeadingInsetWidth => UseIntegratedTitleBar ? 78 : 0;

    public void SetNativeMenuExported(bool isExported)
    {
        if (_isNativeMenuExported == isExported)
        {
            return;
        }

        _isNativeMenuExported = isExported;
        OnPropertyChanged(nameof(IsNativeMenuExported));
        OnPropertyChanged(nameof(IsFallbackAppMenuVisible));
        OnPropertyChanged(nameof(UseNativeMenuTitleBarLayout));
        OnPropertyChanged(nameof(ShowWorkspacePresetStrip));
        OnPropertyChanged(nameof(ShowSecondaryWorkspaceActions));
        OnPropertyChanged(nameof(ShowDockWorkspaceOrganizerMenus));
        OnPropertyChanged(nameof(IntegratedTitleBarHeightHint));
    }

    public int TotalFrames => FrameTimelineService.GetTotalFrames(Duration, SceneFrameRate);

    public int CurrentFrame => FrameTimelineService.TimeToFrame(CurrentTime, SceneFrameRate, TotalFrames);

    public string CurrentFrameLabel => $"F{CurrentFrame + 1}/{TotalFrames}";

    public string CurrentSceneInFrameLabel => $"In F{CurrentSceneInFrame + 1}";

    public string CurrentSceneOutFrameLabel => $"Out F{CurrentSceneOutFrame + 1}";

    public string CurrentSceneWorkAreaLabel => $"Work {CurrentSceneWorkAreaStartFrame + 1}-{CurrentSceneWorkAreaEndFrame + 1}";

    public double TimelineSurfaceWidth => Math.Max(920, (Duration * TimelineZoom) + 200);

    public double TimelineSurfaceHeight => Math.Max(416, 52 + TimelineRows.Sum(static row => row.RowHeight) + 12);

    public double TimelineSurfaceBodyHeight => Math.Max(364, TimelineSurfaceHeight - 52);

    public double FrameTimelineSurfaceWidth => Math.Max(920, 206 + (TotalFrames * FrameTimelineZoom) + 32);

    public double FrameTimelineSurfaceHeight => Math.Max(184, 64 + FrameRows.Sum(static row => row.RowHeight) + 12);

    public double FrameTimelineSurfaceBodyHeight => Math.Max(120, FrameTimelineSurfaceHeight - 64);

    public GridLength LeftPanelColumnWidth => LeftPanelMode == WorkspacePanelMode.Docked ? LeftDockWidth : new GridLength(0);

    public GridLength LeftSplitterColumnWidth => LeftPanelMode == WorkspacePanelMode.Docked ? new GridLength(6) : new GridLength(0);

    public GridLength RightPanelColumnWidth => RightPanelMode == WorkspacePanelMode.Docked ? RightDockWidth : new GridLength(0);

    public GridLength RightSplitterColumnWidth => RightPanelMode == WorkspacePanelMode.Docked ? new GridLength(6) : new GridLength(0);

    public GridLength TimelinePanelRowHeight => TimelinePanelMode == WorkspacePanelMode.Docked ? TimelineDockHeight : new GridLength(0);

    public GridLength TimelineSplitterRowHeight => TimelinePanelMode == WorkspacePanelMode.Docked ? new GridLength(6) : new GridLength(0);

    public int LeftPanelColumnSpan => LeftPanelMode == WorkspacePanelMode.Overlay ? 3 : 1;

    public double LeftPanelHostWidth => LeftPanelMode == WorkspacePanelMode.Overlay ? Math.Max(244, LeftDockWidth.Value) : double.NaN;

    public HorizontalAlignment LeftPanelHorizontalAlignment => LeftPanelMode == WorkspacePanelMode.Overlay ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;

    public Thickness LeftPanelHostMargin => LeftPanelMode == WorkspacePanelMode.Overlay ? new Thickness(0, 0, 8, 0) : new Thickness(0);

    public int LeftPanelZIndex => LeftPanelMode == WorkspacePanelMode.Overlay ? 12 : 0;

    public bool IsLeftPanelVisible => LeftPanelMode != WorkspacePanelMode.Hidden;

    public int RightPanelGridColumn => RightPanelMode == WorkspacePanelMode.Overlay ? 2 : 4;

    public int RightPanelColumnSpan => RightPanelMode == WorkspacePanelMode.Overlay ? 3 : 1;

    public double RightPanelHostWidth => RightPanelMode == WorkspacePanelMode.Overlay ? Math.Max(292, RightDockWidth.Value) : double.NaN;

    public HorizontalAlignment RightPanelHorizontalAlignment => RightPanelMode == WorkspacePanelMode.Overlay ? HorizontalAlignment.Right : HorizontalAlignment.Stretch;

    public Thickness RightPanelHostMargin => RightPanelMode == WorkspacePanelMode.Overlay ? new Thickness(8, 0, 0, 0) : new Thickness(0);

    public int RightPanelZIndex => RightPanelMode == WorkspacePanelMode.Overlay ? 12 : 0;

    public bool IsRightPanelVisible => RightPanelMode != WorkspacePanelMode.Hidden;

    public int TimelinePanelGridRow => TimelinePanelMode == WorkspacePanelMode.Overlay ? 0 : 2;

    public int TimelinePanelRowSpan => TimelinePanelMode == WorkspacePanelMode.Overlay ? 3 : 1;

    public double TimelinePanelHostHeight => TimelinePanelMode == WorkspacePanelMode.Overlay ? Math.Max(MinTimelineDockHeight, TimelineDockHeight.Value) : double.NaN;

    public VerticalAlignment TimelinePanelVerticalAlignment => TimelinePanelMode == WorkspacePanelMode.Overlay ? VerticalAlignment.Bottom : VerticalAlignment.Stretch;

    public Thickness TimelinePanelHostMargin => TimelinePanelMode == WorkspacePanelMode.Overlay ? new Thickness(0, 16, 0, 0) : new Thickness(0);

    public int TimelinePanelZIndex => TimelinePanelMode == WorkspacePanelMode.Overlay ? 14 : 0;

    public bool IsTimelinePanelVisible => TimelinePanelMode != WorkspacePanelMode.Hidden;

    public string LeftPanelToggleLabel => LeftPanelMode == WorkspacePanelMode.Hidden ? "Show Tools" : "Hide Tools";

    public string RightPanelToggleLabel => RightPanelMode == WorkspacePanelMode.Hidden ? "Show Inspector" : "Hide Inspector";

    public string TimelinePanelToggleLabel => TimelinePanelMode == WorkspacePanelMode.Hidden ? "Show Timeline" : "Hide Timeline";

    public string WorkspaceLayoutLabel => SelectedWorkspacePreset == WorkspaceLayoutPreset.Custom
        ? "Custom"
        : SelectedWorkspacePreset.ToString();

    public string WorkspaceLayoutSummary =>
        $"{WorkspaceLayoutLabel} • Tools {GetWorkspacePanelStateLabel(LeftPanelMode)} • Inspector {GetWorkspacePanelStateLabel(RightPanelMode)} • Timeline {GetWorkspacePanelStateLabel(TimelinePanelMode)}";

    public string WorkspaceShortcutSummary =>
        "Focus: Ctrl+1 Classic, Ctrl+2 Stage, Ctrl+3 Timeline, Ctrl+4 Tools, Ctrl+5 Inspector. Toggle: Ctrl+Shift+1 Tools, Ctrl+Shift+2 Inspector, Ctrl+Shift+3 Timeline.";

    public bool IsLeftPanelCollapsedHandleVisible => LeftPanelMode == WorkspacePanelMode.Hidden;

    public bool IsRightPanelCollapsedHandleVisible => RightPanelMode == WorkspacePanelMode.Hidden;

    public bool IsTimelinePanelCollapsedHandleVisible => TimelinePanelMode == WorkspacePanelMode.Hidden;

    public bool HasHiddenWorkspacePanels => IsLeftPanelHidden || IsRightPanelHidden || IsTimelinePanelHidden;

    public bool IsLeftPanelDocked => LeftPanelMode == WorkspacePanelMode.Docked;

    public bool IsLeftPanelOverlay => LeftPanelMode == WorkspacePanelMode.Overlay;

    public bool IsLeftPanelHidden => LeftPanelMode == WorkspacePanelMode.Hidden;

    public bool IsRightPanelDocked => RightPanelMode == WorkspacePanelMode.Docked;

    public bool IsRightPanelOverlay => RightPanelMode == WorkspacePanelMode.Overlay;

    public bool IsRightPanelHidden => RightPanelMode == WorkspacePanelMode.Hidden;

    public bool IsTimelinePanelDocked => TimelinePanelMode == WorkspacePanelMode.Docked;

    public bool IsTimelinePanelOverlay => TimelinePanelMode == WorkspacePanelMode.Overlay;

    public bool IsTimelinePanelHidden => TimelinePanelMode == WorkspacePanelMode.Hidden;

    public bool IsFramesTimelineViewActive => SelectedTimelineView == TimelineWorkspaceView.Frames;

    public bool IsCurvesTimelineViewActive => SelectedTimelineView == TimelineWorkspaceView.Curves;

    public string TimelineWorkspaceViewLabel => SelectedTimelineView == TimelineWorkspaceView.Frames
        ? "Frames"
        : "Curves";

    public string TimelineWorkspaceViewSummary => SelectedTimelineView == TimelineWorkspaceView.Frames
        ? "Exposure sheet, cels, labels, and onion controls."
        : "Property graph editor with keyframes and interpolation curves.";

    public bool CanUndo => !IsPrototypeMode && _history?.CanUndo == true;

    public bool CanRedo => !IsPrototypeMode && _history?.CanRedo == true;

    public bool SelectedLayerIsText => SelectedLayer?.Kind == LayerKind.Text;

    public bool SelectedLayerIsAvaloniaControl => SelectedLayer?.Kind == LayerKind.AvaloniaControl;

    public bool SelectedLayerIsPath => SelectedLayer?.Kind == LayerKind.Path;

    public bool SelectedLayerIsFolder => SelectedLayer?.Kind == LayerKind.Folder;

    public bool SelectedLayerSupportsCornerRadius => SelectedLayer?.Kind is LayerKind.Rectangle or LayerKind.AvaloniaControl or LayerKind.Video;

    public bool SelectedLayerSupportsPrimitiveCornerRadii => SelectedLayer?.Kind == LayerKind.Rectangle;

    public bool SelectedLayerSupportsPrimitiveEllipse => SelectedLayer?.Kind == LayerKind.Ellipse;

    public bool SelectedLayerSupportsPolyStar => SelectedLayer?.Kind == LayerKind.Path && SelectedLayer.Model.Style.PrimitiveShape == PrimitiveShapeType.PolyStar;

    public bool CanEditShapeAuthoring => CanEditSelection && (SelectedLayer?.Kind is LayerKind.Rectangle or LayerKind.Ellipse or LayerKind.Path);

    public bool CanUseShapeQuickControls => CanEditShapeAuthoring || SelectedDrawingTool is DrawingTool.Rectangle or DrawingTool.Ellipse or DrawingTool.PolyStar;

    public bool ShapeAuthoringShowsRectangleOptions => SelectedLayerSupportsPrimitiveCornerRadii || (SelectedLayer is null && SelectedDrawingTool == DrawingTool.Rectangle);

    public bool ShapeAuthoringShowsEllipseOptions => SelectedLayerSupportsPrimitiveEllipse || (SelectedLayer is null && SelectedDrawingTool == DrawingTool.Ellipse);

    public bool ShapeAuthoringShowsPolyStarOptions => SelectedLayerSupportsPolyStar || (SelectedLayer is null && SelectedDrawingTool == DrawingTool.PolyStar);

    public bool SelectedLayerIsMask => SelectedLayer?.Model.Compositing.Role == LayerCompositeRole.Mask;

    public bool SelectedLayerIsGuide => SelectedLayer?.Model.Compositing.Role == LayerCompositeRole.Guide;

    public bool SelectedLayerIsCamera => SelectedLayer?.Model.Compositing.Role == LayerCompositeRole.Camera;

    public bool SelectedLayerIsVideo => SelectedLayer?.Kind == LayerKind.Video;

    public bool SelectedLayerIsAudio => SelectedLayer?.Kind == LayerKind.Audio;

    public bool SelectedLayerIsMedia => SelectedLayerIsVideo || SelectedLayerIsAudio;

    public Guid? SelectedLayerId => SelectedLayer?.Id;

    public bool HasSelectedKeyframe => SelectedKeyframeId is not null;

    public bool HasSelectedCustomEasing => HasSelectedKeyframe && SelectedKeyframeEasing == EasingKind.Custom;

    public bool CanMutateDocument => !IsPlaying && !IsPrototypeMode;

    public bool CanEditSelection => CanMutateDocument && SelectedLayer is not null && !SelectedLayer.IsLocked && !SelectedLayerIsFolder;

    public bool CanEditPropertyTracks => CanEditSelection && !SelectedLayerIsAudio;

    public bool CanEditStyleSurface => CanMutateDocument && (CanEditSelection || SelectedDrawingTool is
        DrawingTool.Eyedropper or
        DrawingTool.PaintBucket or
        DrawingTool.InkBottle or
        DrawingTool.Width or
        DrawingTool.Warp or
        DrawingTool.Rig or
        DrawingTool.Rectangle or
        DrawingTool.Ellipse or
        DrawingTool.PolyStar or
        DrawingTool.Text or
        DrawingTool.Line or
        DrawingTool.Pen or
        DrawingTool.Brush or
        DrawingTool.Pencil or
        DrawingTool.Eraser);

    public bool CanEditToolOptions => !IsPrototypeMode;

    public bool ShowZoomToolOptions => SelectedDrawingTool == DrawingTool.Zoom;

    public bool ShowRotateViewToolOptions => SelectedDrawingTool == DrawingTool.RotateView || Math.Abs(CanvasViewRotation) > 0.001d;

    public bool ShowLassoToolOptions => SelectedDrawingTool == DrawingTool.Lasso;

    public bool ShowPencilToolOptions => SelectedDrawingTool == DrawingTool.Pencil;

    public bool ShowEraserToolOptions => SelectedDrawingTool == DrawingTool.Eraser;

    public bool ShowSubselectToolOptions => SelectedDrawingTool == DrawingTool.Subselect;

    public bool ShowPaintBucketToolOptions => SelectedDrawingTool == DrawingTool.PaintBucket;

    public bool ShowWidthToolOptions => SelectedDrawingTool == DrawingTool.Width;

    public bool ShowStrokeProfileToolOptions => SelectedDrawingTool is DrawingTool.Line or DrawingTool.Pen or DrawingTool.Brush or DrawingTool.Pencil or DrawingTool.Width;

    public string ToolOptionsSummary => SelectedDrawingTool switch
    {
        DrawingTool.Subselect => "Direct vector point editing with transform handles suppressed.",
        DrawingTool.Lasso => $"{LassoToolModeEditor} lasso • {(LassoContactSensitiveSelection ? "contact-sensitive" : "enclosed selection")}",
        DrawingTool.Hand => "Viewport navigation tool",
        DrawingTool.Zoom => $"Zoom {ZoomToolModeEditor}",
        DrawingTool.RotateView => $"View rotation {CanvasViewRotationLabel}",
        DrawingTool.PaintBucket => $"Paint Bucket • {PaintBucketGapSizeEditor} gaps • {(PaintBucketLockFillEditor ? "lock fill" : "replace fill transform")}",
        DrawingTool.Width => $"Width tool • {VectorStrokeProfilePresetEditor} • strength {VectorStrokeProfileStrengthEditor:0.##}",
        DrawingTool.Pencil => $"Pencil {PencilToolModeEditor} • {VectorStrokeProfilePresetEditor} • stroke {StrokeThicknessEditor:0.#}",
        DrawingTool.Eraser => $"{EraserToolModeEditor} erase • radius {EraserSizeEditor:0.#}",
        DrawingTool.Brush => $"Brush {VectorStrokeProfilePresetEditor} • stroke {StrokeThicknessEditor:0.#}",
        DrawingTool.Pen => $"Pen {VectorStrokeProfilePresetEditor} • stroke {StrokeThicknessEditor:0.#}",
        DrawingTool.Line => $"Line {VectorStrokeProfilePresetEditor} • stroke {StrokeThicknessEditor:0.#}",
        _ => ToolModeLabel
    };

    public string SelectedPathPointSummary => !HasSelectedPathPoint || SelectedLayer is null
        ? "No vector point selected."
        : $"Point {SelectedPathPointIndexEditor + 1}/{SelectedLayer.Model.Style.PathPoints.Count} • {SelectedPathPointHandleModeEditor} • width {SelectedPathPointWidthScaleEditor:0.##}x";

    public bool CanAddCameraLayer => !IsEditingSymbol && CanMutateDocument;

    public bool CanInsertSelectedMediaAsset => SelectedMediaAsset is not null && !IsEditingSymbol && CanMutateDocument;

    public bool CanInsertSelectedComponent => SelectedComponentItem is not null && !IsEditingSymbol && CanMutateDocument;

    public bool CanEditSelectedComponentParameters => SelectedComponentItem is not null && CanMutateDocument;

    public bool CanRemoveSelectedComponentParameter => CanEditSelectedComponentParameters && SelectedComponentParameter is not null;

    public bool CanEditPathPoints => CanEditSelection && SelectedLayerIsPath && SelectedLayer?.Model.SourceLibraryItemId is null;

    public bool HasSelectedPathPoint => CanEditPathPoints &&
                                        SelectedPathPointIndexEditor >= 0 &&
                                        SelectedLayer is not null &&
                                        SelectedPathPointIndexEditor < SelectedLayer.Model.Style.PathPoints.Count;

    public bool CanInsertPathPointAfterSelected => HasSelectedPathPoint && SelectedLayer is not null && SelectedLayer.Model.Style.PathPoints.Count >= 2;

    public bool CanDeleteSelectedPathPoint => HasSelectedPathPoint && SelectedLayer is not null && SelectedLayer.Model.Style.PathPoints.Count > 2;

    public bool CanReverseSelectedPath => CanEditPathPoints && SelectedLayer is not null && SelectedLayer.Model.Style.PathPoints.Count > 1;

    public bool CanApplySelectedStrokeProfile => CanEditPathPoints && SelectedLayer is not null && SelectedLayer.Model.Style.PathPoints.Count > 1;

    public bool CanOptimizeSelectedPath => CanEditPathPoints && SelectedLayer is not null && SelectedLayer.Model.Style.PathPoints.Count > 2;

    public bool CanEditGradientTransform => CanEditSelection && SelectedLayer is not null && SelectedLayer.Model.Style.UseGradient;

    public bool CanAlignSelection => CanEditSelection && !SelectedLayerIsAudio;

    public bool CanEditAvaloniaControl => CanEditSelection && SelectedLayerIsAvaloniaControl;

    public bool CanCaptureShapeKeyframe => CanEditSelection && SelectedLayerIsPath;

    public bool CanDeleteCurrentShapeKeyframe => CanEditSelection && FindCurrentShapeKeyframeModel() is not null;

    public bool CanEditCustomEasingCurve => HasSelectedCustomEasing && CanEditPropertyTracks;

    public bool CanToggleLayerState => CanMutateDocument && SelectedLayer is not null;

    public bool CanCreateFolder => CanMutateDocument;

    public bool CanGroupSelectionIntoFolder => CanMutateDocument && SelectedLayer is not null && !SelectedLayerIsFolder;

    public bool CanRemoveSelectionFromFolder => CanMutateDocument && SelectedLayer?.Model.ParentLayerId is not null;

    public bool CanToggleSelectedFolderExpanded => CanMutateDocument && SelectedLayerIsFolder;

    public bool CanUseEditorialTools => CanEditSelection && SelectedLayerIsMedia;

    public bool CanDeleteSelectedKeyframe => CanEditPropertyTracks && SelectedKeyframeId is not null;

    public bool CanCaptureMotionPreset => CanEditPropertyTracks && SelectedLayer is not null;

    public bool CanApplySelectedMotionPreset => CanEditPropertyTracks && SelectedMotionPreset is not null && SelectedLayer is not null;

    public bool CanDeleteSelectedMotionPreset => SelectedMotionPreset is not null;

    public bool CanCopyFrameRange => CanEditSelection && SelectedLayer is not null && SelectionStartFrame >= 0 && SelectionEndFrame >= 0;

    public bool CanPasteFrameRange => CanEditSelection && _frameClipboard is not null;

    public bool CanInsertFromLibrary => SelectedLibraryItem is not null && CanMutateDocument;

    public bool CanEditSelectedLibraryItem => SelectedLibraryItem is not null && CanMutateDocument;

    public bool CanCreateSymbolFromSelection => SelectedLayer is not null && CanMutateDocument;

    public bool CanCreateComponentFromSelection => SelectedLayer is not null && !IsEditingSymbol && CanMutateDocument;

    public bool CanUpdateLinkedSymbol => SelectedLayer?.Model.SourceLibraryItemId is not null && CanMutateDocument;

    public bool CanRelinkSelectionToSelectedLibraryItem =>
        CanMutateDocument &&
        !IsEditingSymbol &&
        SelectedLayer?.Model.SourceLibraryItemId is not null &&
        SelectedLibraryItem is not null &&
        SelectedLayer.Model.SourceLibraryItemId != SelectedLibraryItem.Id;

    public bool CanEnterSymbolEdit => !IsPrototypeMode && !IsEditingSymbol && (SelectedLibraryItem is not null || SelectedLayer?.Model.SourceLibraryItemId is not null);

    public bool CanExitSymbolEdit => IsEditingSymbol;

    public bool CanDeleteScene => CanMutateDocument && Scenes.Count > 1 && SelectedScene is not null;

    public bool CanMoveSceneBackward => CanMutateDocument && SelectedScene is not null && Scenes.IndexOf(SelectedScene) > 0;

    public bool CanMoveSceneForward => CanMutateDocument && SelectedScene is not null && SelectedScene != Scenes.LastOrDefault();

    public bool CanEditScenes => !IsEditingSymbol && CanMutateDocument;

    public bool CanEditCanvasProperties => !IsEditingSymbol && CanMutateDocument && SelectedScene is not null;

    public bool CanInsertStageItems => CanMutateDocument;

    public bool CanUseSelectionActions => CanMutateDocument && SelectedLayer is not null;

    public bool CanEditTextMenu => CanMutateDocument && SelectedLayer is not null && (SelectedLayerIsText || SelectedLayerIsAvaloniaControl);

    public bool CanManageFrameLabels => !IsEditingSymbol && CanMutateDocument && SelectedScene is not null;

    public bool CanTogglePrototypeModeMenu => !IsEditingSymbol;

    public bool CanStopPlayback => IsPlaying;

    public bool CanEditPublishProfiles => !IsEditingSymbol && !IsPlaying;

    public bool CanDeletePublishProfile => CanEditPublishProfiles && SelectedPublishProfile is not null && PublishProfiles.Count > 1;

    public bool CanPreviewSelectedPublishProfile => CanEditPublishProfiles && SelectedPublishProfile is not null;

    public bool CanExportSelectedPublishProfile => CanPreviewSelectedPublishProfile && !HasPublishErrors;

    public bool SelectedLayerIsSymbolInstance => SelectedLayer?.Model.SourceLibraryItemId is not null;

    public bool CanEditSelectedLayerInstanceMetadata => CanEditSelection && SelectedLayerIsSymbolInstance;

    public bool CanEditBitmapCacheMetadata => CanEditSelectedLayerInstanceMetadata;

    public bool CanEditGuideBinding => CanEditSelection &&
        SelectedLayer is not null &&
        !SelectedLayerIsGuide &&
        !SelectedLayerIsFolder &&
        !SelectedLayerIsAudio &&
        SelectedLayer.Model.Compositing.Role != LayerCompositeRole.Camera;

    public bool CanEditTransformParenting => CanEditSelection &&
        SelectedLayer is not null &&
        !SelectedLayerIsFolder &&
        !SelectedLayerIsAudio &&
        SelectedLayer.Model.Compositing.Role != LayerCompositeRole.Camera;

    public IReadOnlyList<LayerViewModel> AvailableTransformParentLayers =>
    [
        .. Layers.Where(item =>
            item.Id != SelectedLayerId &&
            !item.IsFolder &&
            item.Kind != LayerKind.Audio &&
            item.Model.Compositing.Role != LayerCompositeRole.Camera &&
            (SelectedLayer is null || LayerParentingService.CanSetParent(_document.Layers, SelectedLayer.Id, item.Id)))
    ];

    public IReadOnlyList<LayerViewModel> AvailableGuideLayers =>
    [
        .. Layers.Where(item => item.Id != SelectedLayerId && item.CompositeRole == LayerCompositeRole.Guide)
    ];

    public bool HasAvailableGuideLayers => AvailableGuideLayers.Count > 0;

    public bool HasGuideBinding => SelectedLayer?.Model.GuidedByLayerId is not null;

    public bool HasTransformParent => SelectedLayer?.Model.TransformParenting.ParentLayerId is not null;

    public bool CanEditWarpRigging => CanEditSelection &&
        SelectedLayer is not null &&
        SelectedLayer.Kind == LayerKind.Path &&
        !SelectedLayerIsSymbolInstance;

    public bool HasWarpPins => SelectedLayer?.Model.Warp.Pins.Count > 0;

    public bool HasRigBones => SelectedLayer?.Model.Rig.Bones.Count > 0;

    public bool HasRigPoses => RigPoses.Count > 0;

    public bool CanApplySelectedRigPose => CanEditWarpRigging && SelectedRigPose is not null;

    public bool CanDeleteSelectedRigPose => CanApplySelectedRigPose;

    public bool SelectedLayerIsComponentInstance => SelectedLayerIsSymbolInstance &&
        _document.LibraryItems.FirstOrDefault(item => item.Id == SelectedLayer!.Model.SourceLibraryItemId)?.IsComponent == true;

    public bool CanEditSelectedComponentOverrides => SelectedLayerIsComponentInstance && SelectedLayer is not null && !SelectedLayer.IsLocked && CanMutateDocument;

    public bool CanResetSelectedComponentOverride => CanEditSelectedComponentOverrides && SelectedComponentOverrideItem is not null;

    public bool EditingSymbolIsButton => EditingLibraryItem?.SymbolKind == SymbolKind.Button;

    public bool SelectedSymbolInstanceIsButton => SelectedLayerIsSymbolInstance &&
        _document.LibraryItems.FirstOrDefault(item => item.Id == SelectedLayer!.Model.SourceLibraryItemId)?.SymbolKind == SymbolKind.Button;

    public string SelectedLayerVisibilityLabel => SelectedLayer?.IsVisible == false ? "Show Layer" : "Hide Layer";

    public string SelectedLayerLockLabel => SelectedLayer?.IsLocked == true ? "Unlock Layer" : "Lock Layer";

    public string SelectedLayerMuteLabel => SelectedLayer?.IsMuted == true ? "Unmute Layer" : "Mute Layer";

    public string SelectedLayerSoloLabel => SelectedLayer?.IsSolo == true ? "Unsolo Layer" : "Solo Layer";

    public string SelectedLayerExpandLabel => SelectedLayer?.IsExpanded == false ? "Expand Folder" : "Collapse Folder";

    public string SelectedLayerStateLabel
    {
        get
        {
            if (SelectedLayer is null)
            {
                return "No layer selected";
            }

            if (SelectedLayer.IsLocked && !SelectedLayer.IsVisible)
            {
                return "Hidden on stage and protected from edits.";
            }

            if (SelectedLayer.IsLocked)
            {
                return "Visible on stage but protected from edits.";
            }

            if (!SelectedLayer.IsVisible)
            {
                return "Hidden from the stage and render output.";
            }

            if (SelectedLayer.IsMuted)
            {
                return "Muted from preview and export output.";
            }

            if (SelectedLayer.IsSolo)
            {
                return "Soloed for focused preview and export output.";
            }

            return SelectedLayer.Model.Compositing.Role switch
            {
                LayerCompositeRole.Mask => "Used as a live alpha mask for the following layers.",
                LayerCompositeRole.Guide => "Visible as an editor guide and skipped from render output.",
                LayerCompositeRole.Camera => "Drives stage pan and zoom for preview and export output.",
                _ => "Visible on stage and editable."
            };
        }
    }

    public string SelectionHeadline => SelectedLayer is null
        ? "Nothing selected"
        : $"{SelectedLayer.Name} • {SelectedLayer.KindLabel}{GetSelectionStateSuffix(SelectedLayer)}";

    public string SelectedLibrarySummary => SelectedLibraryItem is null
        ? "No library symbol selected"
        : $"{SelectedLibraryItem.Name} • {SelectedLibraryItem.SymbolKindLabel} • {SelectedLibraryItem.FolderPathLabel} • {SelectedLibraryItem.LinkageLabel} • {SelectedLibraryItem.FlashInteropLabel}";

    public string SelectedComponentSummary => SelectedComponentItem is null
        ? "No component selected"
        : $"{SelectedComponentItem.Name} • {SelectedComponentItem.ComponentBadge} • {SelectedComponentItem.Subtitle} • {SelectedComponentParameters.Count} params";

    public string SelectedComponentParameterSummary => SelectedComponentItem is null
        ? "Select a component to define reusable parameters."
        : SelectedComponentParameters.Count == 0
            ? "No component parameters yet."
            : $"{SelectedComponentParameters.Count} parameters • {SelectedComponentParameters.FirstOrDefault()?.Subtitle}";

    public string SelectedComponentOverrideSummary => !SelectedLayerIsComponentInstance
        ? "Select a component instance to override parameter values."
        : SelectedComponentOverrideItems.Count == 0
            ? "This component exposes no instance parameters."
            : $"{SelectedComponentOverrideItems.Count} overrides available";

    public string SelectedMediaSummary => SelectedMediaAsset is null
        ? "No media clip selected"
        : $"{SelectedMediaAsset.Name} • {SelectedMediaAsset.KindLabel} • {SelectedMediaAsset.Subtitle}";

    public string SelectedPublishProfileSummary => SelectedPublishProfile is null
        ? "No publish profile selected"
        : $"{SelectedPublishProfile.Name} • {SelectedPublishProfile.FormatLabel} • {SelectedPublishProfile.Subtitle}";

    public string SelectedAnimationExchangeSummary => SelectedAnimationExchangeFormat switch
    {
        AnimationExchangeFormat.AvaloniaXaml => "Avalonia keyframe export with selector-based XAML animations and control-aware fallback import.",
        AnimationExchangeFormat.FlashXfl => "Flash XFL authoring export with scenes, symbols, exact motion tracks, scale/skew transform parity, linkage-sharing metadata, Scale-9 grids, registration points, guide bindings, outline layers, instance cache metadata, advanced text-field metadata, media payload packaging, publish settings, supplemental package retention, layer effects, and media sync round-trip.",
        AnimationExchangeFormat.SvgSmil => "SVG/SMIL export with baked transform motion and shape-aware fallback import.",
        AnimationExchangeFormat.HtmlCss => "HTML/CSS export with absolute-positioned stage layers, @keyframes motion, and XHTML fallback import.",
        _ => "Animation interchange"
    };

    public string CurrentDocumentFileFormatLabel => TimelineDocumentFileService.GetDisplayName(CurrentDocumentFileFormat);

    public string DocumentFileSummary => $"{FileLabel} • {CurrentDocumentFileFormatLabel} • {AnimateDocumentTypeLabel}";

    public string AnimateDocumentTypeLabel => AnimateDocumentProfileService.GetDisplayName(AnimateDocumentTypeEditor);

    public string AnimateDocumentSummary => AnimateDocumentProfileService.GetSummary(Document.Animate);

    public string AnimateDocumentCompatibilitySummary
    {
        get
        {
            if (AnimateDocumentCompatibilityIssues.Count == 0)
            {
                return $"{AnimateDocumentTypeLabel} target is compatible with the current authoring features.";
            }

            return AnimateDocumentCompatibilityIssues.Count == 1
                ? AnimateDocumentCompatibilityIssues[0].Message
                : $"{AnimateDocumentCompatibilityIssues.Count} target-compatibility warnings for {AnimateDocumentTypeLabel}.";
        }
    }

    public bool HasAnimateDocumentCompatibilityIssues => AnimateDocumentCompatibilityIssues.Count > 0;

    public string AnimateDocumentConversionSummary => HasAnimateDocumentCompatibilityIssues
        ? $"Converting to {AnimateDocumentTypeLabel} keeps the current content, but {AnimateDocumentCompatibilityIssues.Count} compatibility item(s) may need manual adjustment."
        : $"Converting to {AnimateDocumentTypeLabel} reapplies Animate-style target defaults and keeps the current authoring content compatible.";

    public string AnimateDocumentConvertButtonLabel => $"Convert To {AnimateDocumentTypeLabel}";

    public string LastAnimationExchangeSummaryDisplay => string.IsNullOrWhiteSpace(LastAnimationExchangeSummary)
        ? "No animation interop activity yet."
        : LastAnimationExchangeSummary;

    public bool HasAnimationExchangeIssues => AnimationExchangeIssues.Count > 0;

    public string AnimationExchangePreviewSummaryDisplay => string.IsNullOrWhiteSpace(AnimationExchangePreviewSummary)
        ? "Preview not generated yet."
        : AnimationExchangePreviewSummary;

    public string AnimationExchangePreviewFileSummary => string.IsNullOrWhiteSpace(AnimationExchangePreviewFileName)
        ? "No generated file yet."
        : AnimationExchangePreviewFileName;

    public bool HasAnimationExchangePreviewIssues => AnimationExchangePreviewIssues.Count > 0;

    public bool IsAnimationExchangeCodeVisible => SelectedAnimationExchangePreviewMode != AnimationExchangePreviewMode.Preview;

    public bool IsAnimationExchangeVisualVisible => SelectedAnimationExchangePreviewMode != AnimationExchangePreviewMode.Code;

    public bool IsAnimationExchangeSplitMode => SelectedAnimationExchangePreviewMode == AnimationExchangePreviewMode.Split;

    public GridLength AnimationExchangeCodeColumnWidth => SelectedAnimationExchangePreviewMode == AnimationExchangePreviewMode.Preview
        ? new GridLength(0)
        : new GridLength(1, GridUnitType.Star);

    public GridLength AnimationExchangeSplitterColumnWidth => SelectedAnimationExchangePreviewMode == AnimationExchangePreviewMode.Split
        ? new GridLength(8)
        : new GridLength(0);

    public GridLength AnimationExchangeVisualColumnWidth => SelectedAnimationExchangePreviewMode == AnimationExchangePreviewMode.Code
        ? new GridLength(0)
        : new GridLength(1, GridUnitType.Star);

    public string AnimationExchangePreviewModeSummary => SelectedAnimationExchangePreviewMode switch
    {
        AnimationExchangePreviewMode.Code => "Code only",
        AnimationExchangePreviewMode.Preview => "Visual preview only",
        AnimationExchangePreviewMode.Split => "Split code and preview",
        _ => "Preview layout"
    };

    public string SelectedSceneSummary => SelectedScene is null
        ? "No scene selected"
        : $"{SelectedScene.Name} • {SelectedScene.Subtitle}";

    public string CanvasSceneSummary => SelectedScene is null
        ? "No scene selected"
        : $"{SelectedScene.Name} • {CanvasSizeLabel} • {DurationLabel} • {FrameRateLabel}";

    public string CanvasBackgroundSummary
    {
        get
        {
            var stageLabel = TransparentStageBackground
                ? "Stage transparent"
                : $"Stage {BackgroundFrom}";
            var pasteboardLabel = MatchPasteboardToStageColor
                ? "pasteboard matched"
                : $"pasteboard {PasteboardColor}";
            return $"{stageLabel} • {pasteboardLabel}";
        }
    }

    public string AnimateDocumentOptionsSummary
    {
        get
        {
            var parts = new List<string>();
            if (AnimateDocumentResponsiveEditor)
            {
                parts.Add("responsive");
            }

            if (AnimateDocumentExternalScriptEditor)
            {
                parts.Add("external script");
            }

            if (AnimateDocumentWebFontsEditor)
            {
                parts.Add("web fonts");
            }

            if (AnimateDocumentAdvancedLayersEditor)
            {
                parts.Add("advanced layers");
            }

            if (AnimateDocumentEnable3DEditor)
            {
                parts.Add($"3D {AnimateDocumentPerspectiveAngleEditor:0.#}deg");
            }

            return parts.Count == 0 ? "No extra target options enabled." : string.Join(" • ", parts);
        }
    }

    public string SelectedActionScriptSnippetSummary => SelectedActionScriptSnippet is null
        ? $"No snippet selected • {AnimateDocumentTypeLabel}"
        : $"{SelectedActionScriptSnippet.CategoryLabel} • {SelectedActionScriptSnippet.Summary}";

    public bool CanInsertActionScriptSnippetIntoFrame => CanEditCurrentFrameActionScript && SelectedActionScriptSnippet is not null;

    public bool CanInsertActionScriptSnippetIntoBehavior => CanEditSelectedBehaviorScript && SelectedActionScriptSnippet is not null;

    public string ModifyMenuHeader => SelectedLayer is null ? "Modify" : $"Modify {SelectedLayer.Name}";

    public string ModifyMenuSummary => SelectedLayer is null
        ? "No layer selected"
        : $"{SelectionHeadline} • {SelectedLayerStateLabel}";

    public string TextMenuSummary => SelectedLayerIsText
        ? $"{SelectedLayer?.Name} text styling and font options."
        : SelectedLayerIsAvaloniaControl
            ? $"{SelectedLayer?.Name} control content and font options."
            : "Select a text layer or Avalonia control to edit text operations.";

    public bool SelectedTextUsesInteractiveFieldMetadata => SelectedLayerIsText && TextFieldKindEditor != FlashTextFieldKind.Static;

    public bool SelectedTextUsesVariableName => SelectedTextUsesInteractiveFieldMetadata;

    public bool SelectedLayerUsesFlashAlphaEffect => LayerFlashColorEffectModeEditor == FlashColorEffectMode.Alpha;

    public bool SelectedLayerUsesFlashTintEffect => LayerFlashColorEffectModeEditor == FlashColorEffectMode.Tint;

    public bool SelectedLayerUsesFlashBrightnessEffect => LayerFlashColorEffectModeEditor == FlashColorEffectMode.Brightness;

    public bool SelectedLayerUsesFlashAdvancedColorEffect => LayerFlashColorEffectModeEditor == FlashColorEffectMode.Advanced;

    public bool SelectedLayerUsesMiterJoin => StrokeJoinStyleEditor == LayerStrokeJoinStyle.Miter;

    public string FlashColorEffectSummary => LayerFlashColorEffectModeEditor switch
    {
        FlashColorEffectMode.Alpha => $"Alpha {LayerFlashAlphaPercentEditor:0.#}%",
        FlashColorEffectMode.Tint => $"Tint {LayerFlashTintColorEditor} • {LayerFlashTintPercentEditor:0.#}%",
        FlashColorEffectMode.Brightness => $"Brightness {LayerFlashBrightnessPercentEditor:0.#}%",
        FlashColorEffectMode.Advanced => $"Advanced RGBA • R {LayerFlashRedPercentEditor:0.#}%/{LayerFlashRedOffsetEditor:0.#} • G {LayerFlashGreenPercentEditor:0.#}%/{LayerFlashGreenOffsetEditor:0.#} • B {LayerFlashBluePercentEditor:0.#}%/{LayerFlashBlueOffsetEditor:0.#}",
        _ => "No Flash color effect"
    };

    public string SelectedTextFieldSummary
    {
        get
        {
            if (!SelectedLayerIsText)
            {
                return "Select a text layer to edit Flash field metadata.";
            }

            var parts = new List<string>
            {
                TextFieldKindEditor.ToString(),
                TextLineModeEditor.ToString()
            };

            if (SelectedTextUsesInteractiveFieldMetadata)
            {
                parts.Add(TextSelectableEditor ? "selectable" : "non-selectable");
                if (TextShowBorderEditor)
                {
                    parts.Add("border");
                }

                if (!string.IsNullOrWhiteSpace(TextVariableNameEditor))
                {
                    parts.Add($"var {TextVariableNameEditor}");
                }
            }

            if (TextMaxCharactersEditor > 0)
            {
                parts.Add($"max {TextMaxCharactersEditor}");
            }

            if (TextPasswordEditor)
            {
                parts.Add("password");
            }

            if (TextUseDeviceFontsEditor)
            {
                parts.Add("device fonts");
            }

            parts.Add($"AA {TextAntiAliasModeEditor}");

            return string.Join(" • ", parts);
        }
    }

    public string ControlMenuSummary => IsPrototypeMode
        ? "Prototype mode is active."
        : IsPlaying
            ? $"Playback running on {SelectedScene?.Name ?? "the current scene"}."
            : "Playback idle.";

    public string HelpMenuSummary => $"{WorkspaceSummary} • {DocumentFileSummary}";

    public bool IsSelectToolActive => SelectedDrawingTool == DrawingTool.Select;

    public bool IsSubselectToolActive => SelectedDrawingTool == DrawingTool.Subselect;

    public bool IsLassoToolActive => SelectedDrawingTool == DrawingTool.Lasso;

    public bool IsHandToolActive => SelectedDrawingTool == DrawingTool.Hand;

    public bool IsZoomToolActive => SelectedDrawingTool == DrawingTool.Zoom;

    public bool IsRotateViewToolActive => SelectedDrawingTool == DrawingTool.RotateView;

    public bool IsEyedropperToolActive => SelectedDrawingTool == DrawingTool.Eyedropper;

    public bool IsPaintBucketToolActive => SelectedDrawingTool == DrawingTool.PaintBucket;

    public bool IsInkBottleToolActive => SelectedDrawingTool == DrawingTool.InkBottle;

    public bool IsWidthToolActive => SelectedDrawingTool == DrawingTool.Width;

    public bool IsWarpToolActive => SelectedDrawingTool == DrawingTool.Warp;

    public bool IsRigToolActive => SelectedDrawingTool == DrawingTool.Rig;

    public bool IsRectangleToolActive => SelectedDrawingTool == DrawingTool.Rectangle;

    public bool IsEllipseToolActive => SelectedDrawingTool == DrawingTool.Ellipse;

    public bool IsPolyStarToolActive => SelectedDrawingTool == DrawingTool.PolyStar;

    public bool IsTextToolActive => SelectedDrawingTool == DrawingTool.Text;

    public bool IsLineToolActive => SelectedDrawingTool == DrawingTool.Line;

    public bool IsPenToolActive => SelectedDrawingTool == DrawingTool.Pen;

    public bool IsBrushToolActive => SelectedDrawingTool == DrawingTool.Brush;

    public bool IsPencilToolActive => SelectedDrawingTool == DrawingTool.Pencil;

    public bool IsEraserToolActive => SelectedDrawingTool == DrawingTool.Eraser;

    public bool IsAvaloniaXamlExchangeSelected => SelectedAnimationExchangeFormat == AnimationExchangeFormat.AvaloniaXaml;

    public bool IsFlashXflExchangeSelected => SelectedAnimationExchangeFormat == AnimationExchangeFormat.FlashXfl;

    public bool IsSvgAnimationExchangeSelected => SelectedAnimationExchangeFormat == AnimationExchangeFormat.SvgSmil;

    public bool IsHtmlAnimationExchangeSelected => SelectedAnimationExchangeFormat == AnimationExchangeFormat.HtmlCss;

    public string WorkspaceSummary => IsEditingSymbol
        ? $"Symbol isolation • {EditingLibraryItem?.Name} • {EditingLibraryItem?.SymbolKind}"
        : IsPrototypeMode
            ? $"Prototype preview • {SelectedScene?.Name ?? "Scene"}"
            : SelectedSceneSummary;

    public bool IsDockWorkspaceVisible => UseDockWorkspace;

    public bool IsClassicWorkspaceVisible => !UseDockWorkspace;

    public string ToolModeLabel => SelectedDrawingTool switch
    {
        DrawingTool.Select => "Select and transform",
        DrawingTool.Subselect => "Direct point editing",
        DrawingTool.Lasso => "Freeform selection",
        DrawingTool.Hand => "Pan the stage view",
        DrawingTool.Zoom => "Zoom the stage view",
        DrawingTool.RotateView => "Rotate the stage view",
        DrawingTool.Eyedropper => "Sample style from the stage",
        DrawingTool.PaintBucket => "Apply fill and gradient",
        DrawingTool.InkBottle => "Apply stroke styling",
        DrawingTool.Width => "Edit vector stroke width",
        DrawingTool.Warp => "Warp vector assets",
        DrawingTool.Rig => "Rig vector assets",
        DrawingTool.Rectangle => "Draw rectangles",
        DrawingTool.Ellipse => "Draw ellipses",
        DrawingTool.PolyStar => "Draw stars and polygons",
        DrawingTool.Text => "Draw text plates",
        DrawingTool.Line => "Draw vector lines",
        DrawingTool.Pen => "Sketch polyline paths",
        DrawingTool.Brush => "Paint freehand strokes",
        DrawingTool.Pencil => "Draw pencil strokes",
        DrawingTool.Eraser => "Erase stage content",
        _ => SelectedDrawingTool.ToString()
    };

    public string ShapeAuthoringSummary
    {
        get
        {
            if (SelectedLayer is null)
            {
                return $"Tool mode • {ShapeDrawingModeEditor}";
            }

            var style = SelectedLayer.Model.Style;
            return style.PrimitiveShape switch
            {
                PrimitiveShapeType.Rectangle => $"{style.DrawingMode} rectangle • {style.CornerRadiusTopLeft:0}/{style.CornerRadiusTopRight:0}/{style.CornerRadiusBottomRight:0}/{style.CornerRadiusBottomLeft:0}",
                PrimitiveShapeType.Ellipse => $"{style.DrawingMode} ellipse • {style.EllipseStartAngle:0}° → {style.EllipseSweepAngle:0}°",
                PrimitiveShapeType.PolyStar => $"{style.DrawingMode} {(style.PolyStarIsStar ? "star" : "polygon")} • {style.PolyStarSides} sides",
                _ => $"{style.DrawingMode} shape"
            };
        }
    }

    public string MotionPresetSummary => SelectedMotionPreset is null
        ? $"{MotionPresets.Count} motion preset(s)"
        : $"{SelectedMotionPreset.Name} • {SelectedMotionPreset.Description}";

    public string OnionSkinSummary
    {
        get
        {
            if (!OnionSkinEnabled)
            {
                return "Onion skin disabled";
            }

            var mode = OnionSkinKeyframesOnly ? "keyframes only" : "all sampled frames";
            return $"{OnionSkinBefore}/{OnionSkinAfter} • {mode} • base {OnionSkinBaseOpacity:0.00} • falloff {OnionSkinOpacityFalloff:0.00}";
        }
    }

    public string SelectedFrameRangeSummary
    {
        get
        {
            if (SelectedLayer is null || SelectionStartFrame < 0 || SelectionEndFrame < 0)
            {
                return "No frame range selected";
            }

            var start = Math.Min(SelectionStartFrame, SelectionEndFrame) + 1;
            var end = Math.Max(SelectionStartFrame, SelectionEndFrame) + 1;
            return start == end
                ? $"{SelectedLayer.Name} • Frame {start}"
                : $"{SelectedLayer.Name} • Frames {start}-{end}";
        }
    }

    public int CurrentSceneInFrame
    {
        get
        {
            if (SelectedScene is null)
            {
                return 0;
            }

            SceneTimelineService.EnsureTimelineMetadata(SelectedScene.Model, TotalFrames);
            return SelectedScene.Model.InFrame;
        }
    }

    public int CurrentSceneOutFrame
    {
        get
        {
            if (SelectedScene is null)
            {
                return Math.Max(0, TotalFrames - 1);
            }

            SceneTimelineService.EnsureTimelineMetadata(SelectedScene.Model, TotalFrames);
            return SelectedScene.Model.OutFrame;
        }
    }

    public int CurrentSceneWorkAreaStartFrame
    {
        get
        {
            if (SelectedScene is null)
            {
                return 0;
            }

            SceneTimelineService.EnsureTimelineMetadata(SelectedScene.Model, TotalFrames);
            return SelectedScene.Model.WorkAreaStartFrame;
        }
    }

    public int CurrentSceneWorkAreaEndFrame
    {
        get
        {
            if (SelectedScene is null)
            {
                return Math.Max(0, TotalFrames - 1);
            }

            SceneTimelineService.EnsureTimelineMetadata(SelectedScene.Model, TotalFrames);
            return SelectedScene.Model.WorkAreaEndFrame;
        }
    }

    public string SelectedSceneMarkerSummary => SelectedSceneMarker is null
        ? "No editorial marker selected."
        : $"{SelectedSceneMarker.KindLabel} • Frame {SelectedSceneMarker.Frame + 1} • {SelectedSceneMarker.Name}";

    public string SceneTransitionSummary
    {
        get
        {
            if (SelectedScene is null)
            {
                return "Select a scene to edit sequence transitions.";
            }

            var transition = SelectedScene.Model.OutgoingTransition;
            return transition.Kind == SceneTransitionKind.None
                ? "No outgoing transition."
                : $"{transition.Kind} • {transition.Duration:0.00}s";
        }
    }

    public string SourceMonitorRangeSummary => SourceMonitorDuration <= 0d
        ? "No source range"
        : $"{SourceMonitorInTime:0.00}s → {SourceMonitorOutTime:0.00}s";

    public string ProgramMonitorSummary => PlayAllScenes
        ? $"Sequence • {GetCurrentSequenceTime():0.00}s"
        : $"Scene • {CurrentTime:0.00}s";

    public string SelectedVisualStateSummary => SelectedVisualState is null
        ? "No visual state selected."
        : $"{SelectedVisualState.Name} • {SelectedVisualState.Subtitle}";

    public bool CanEditSceneMarkers => !IsEditingSymbol && !IsPrototypeMode && SelectedScene is not null;

    public bool CanRemoveSelectedSceneMarker => CanEditSceneMarkers && SelectedSceneMarker is not null;

    public bool CanSetSceneWorkArea => CanEditSceneMarkers && SelectionStartFrame >= 0 && SelectionEndFrame >= 0;

    public bool CanEditVisualStates => CanEditSelection && SelectedLayer is not null && SelectedLayer.Kind != LayerKind.Audio;

    public bool CanApplySelectedVisualState => CanEditVisualStates && SelectedVisualStateGroup is not null && SelectedVisualState is not null;

    public bool CanRemoveSelectedVisualState => CanApplySelectedVisualState;

    public bool CanRemoveSelectedVisualStateGroup => CanEditVisualStates && SelectedVisualStateGroup is not null;

    public bool HasProgramMonitorBitmap => ProgramMonitorBitmap is not null;

    public bool HasSourceMonitorBitmap => SourceMonitorBitmap is not null;

    public string SelectedKeyframeSummary
    {
        get
        {
            if (SelectedKeyframeId is null)
            {
                return "No keyframe selected";
            }

            var track = Tracks.FirstOrDefault(item => item.Property == SelectedProperty);
            var keyframe = track?.Keyframes.FirstOrDefault(item => item.Id == SelectedKeyframeId.Value);
            return keyframe is null
                ? "No keyframe selected"
                : $"{track!.Title} @ {keyframe.TimeLabel} • {keyframe.ValueLabel} • {keyframe.EasingLabel}";
        }
    }

    public string ShapeTweenSummary
    {
        get
        {
            if (!SelectedLayerIsPath || SelectedLayer is null)
            {
                return "Select a vector path to author morph keys.";
            }

            var count = SelectedLayer.Model.ShapeKeyframes.Count;
            var current = FindCurrentShapeKeyframeModel();
            return current is null
                ? $"{count} morph keys stored on this path."
                : $"{count} morph keys stored • current key at {current.Time:0.00}s";
        }
    }

    public string CompositingSummary
    {
        get
        {
            if (SelectedLayer is null)
            {
                return "Select a layer to edit masks, blend modes, effects, and camera depth.";
            }

            var compositing = SelectedLayer.Model.Compositing;
            var flashEffect = compositing.FlashColorEffectMode == FlashColorEffectMode.None
                ? "no Flash effect"
                : $"{compositing.FlashColorEffectMode} effect";
            return $"{SelectedLayer.KindLabel} • {compositing.Role} • {compositing.BlendMode} • {flashEffect} • depth {compositing.ParallaxDepth:0.##}";
        }
    }

    public string MediaLayerSummary
    {
        get
        {
            if (SelectedLayer is null || !SelectedLayerIsMedia)
            {
                return "Select an audio or video layer to edit playback timing and clip settings.";
            }

            var asset = SelectedLayer.Model.Media.SourceMediaAssetId is Guid mediaAssetId
                ? _document.MediaAssets.FirstOrDefault(item => item.Id == mediaAssetId)
                : null;
            return asset is null
                ? "Media source missing"
                : $"{asset.Name} • {SelectedLayer.Model.Media.PlaybackMode} • starts {SelectedLayer.Model.Media.StartTime:0.00}s • duration {Math.Max(0.05d, SelectedLayer.Model.Media.ClipDuration):0.00}s • {MediaRepeatSummary} • {MediaAudioSummary}";
        }
    }

    public string LayerHierarchySummary => SelectedLayer is null
        ? "Select a layer to organize hierarchy."
        : SelectedLayerIsFolder
            ? $"{SelectedLayer.Name} • folder • {(SelectedLayer.IsExpanded ? "expanded" : "collapsed")}"
            : SelectedLayer.Model.ParentLayerId is Guid parentId
                ? $"{SelectedLayer.Name} • nested under {Layers.FirstOrDefault(item => item.Id == parentId)?.Name ?? "folder"}"
                : $"{SelectedLayer.Name} • root layer";

    public string GuideBindingSummary
    {
        get
        {
            if (SelectedLayer is null)
            {
                return "Select a layer to assign a motion guide.";
            }

            if (SelectedLayerIsGuide)
            {
                return "Guide layers steer other layers and are skipped from final output.";
            }

            if (!CanEditGuideBinding)
            {
                return "Guide binding is not available for the current layer type.";
            }

            if (SelectedLayer.Model.GuidedByLayerId is not Guid guideLayerId)
            {
                return HasAvailableGuideLayers
                    ? "No guide layer assigned."
                    : "No guide layers are available in the current scene.";
            }

            var guide = Layers.FirstOrDefault(item => item.Id == guideLayerId);
            return $"{guide?.Name ?? "Guide"} • orient {(GuideOrientToPathEditor ? "on" : "off")} • snap {(GuideSnapToPathEditor ? "on" : "off")}";
        }
    }

    public string TransformParentingSummary
    {
        get
        {
            if (SelectedLayer is null)
            {
                return "Select a layer to bind it to a transform parent.";
            }

            if (!CanEditTransformParenting)
            {
                return "Transform parenting is not available for the current layer type.";
            }

            if (SelectedLayer.Model.TransformParenting.ParentLayerId is not Guid parentLayerId)
            {
                return AvailableTransformParentLayers.Count > 0
                    ? "No transform parent assigned."
                    : "No eligible parent layers are available in the current scene.";
            }

            var parent = Layers.FirstOrDefault(item => item.Id == parentLayerId);
            return $"{parent?.Name ?? "Parent"} • bound at {SelectedLayer.Model.TransformParenting.BindParentX:0.#}, {SelectedLayer.Model.TransformParenting.BindParentY:0.#}";
        }
    }

    public string WarpRigSummary
    {
        get
        {
            if (SelectedLayer is null)
            {
                return "Select a vector path to edit warp pins and modern rig data.";
            }

            if (!CanEditWarpRigging)
            {
                return "Warp and rig editing is currently available on editable vector paths.";
            }

            var warp = SelectedLayer.Model.Warp;
            var rig = SelectedLayer.Model.Rig;
            return $"{warp.Pins.Count} warp pins • mesh {warp.MeshColumns}x{warp.MeshRows} • {rig.Bones.Count} bones • {rig.Poses.Count} poses";
        }
    }

    public string SelectedRigPoseSummary => SelectedRigPose is null
        ? "No pose selected."
        : $"{SelectedRigPose.Name} • {SelectedRigPose.Time:0.00}s";

    public string LayerAuthoringSummary
    {
        get
        {
            if (SelectedLayer is null)
            {
                return "Select a layer to edit Animate authoring metadata.";
            }

            var parts = new List<string>();
            if (LayerOutlineEditor)
            {
                parts.Add($"outline {LayerOutlineColorEditor}");
            }

            if (SelectedLayerIsSymbolInstance && LayerCacheAsBitmapEditor)
            {
                parts.Add($"cache {LayerBitmapCacheBackgroundEditor}");
            }

            return parts.Count == 0 ? "No extra Flash authoring metadata on this layer." : string.Join(" • ", parts);
        }
    }

    public string EditMultipleFramesSummary => !EditMultipleFramesEnabled
        ? "Edit Multiple Frames off"
        : SelectionStartFrame < 0 || SelectionEndFrame < 0
            ? "Edit Multiple Frames on"
            : $"Edit frames {Math.Min(SelectionStartFrame, SelectionEndFrame) + 1}-{Math.Max(SelectionStartFrame, SelectionEndFrame) + 1}";

    public string MediaAudioSummary => SelectedLayer is null || !SelectedLayerIsMedia
        ? "No media mix"
        : $"vol {MediaVolumeEditor:0.00} • gain {MediaGainDbEditor:+0.0;-0.0;0.0} dB • pan {MediaPanEditor:+0.00;-0.00;0.00}";

    public string MediaRepeatSummary => SelectedLayer is null || !SelectedLayerIsMedia
        ? "single pass"
        : MediaPlaybackModeEditor == MediaPlaybackMode.Stop
            ? "stop sync"
            : MediaRepeatForeverEditor || MediaLoopEditor
                ? "repeat forever"
                : MediaRepeatCountEditor <= 1
                    ? "single pass"
                    : $"repeat x{MediaRepeatCountEditor}";

    public string AudioMeterSummary => !SelectedLayerIsAudio
        ? "No audio meter"
        : AudioMeterIsAudible
            ? $"Peak {AudioMeterPeak:0.00} • RMS {AudioMeterRms:0.00} • L {AudioMeterLeftGain:0.00} • R {AudioMeterRightGain:0.00}"
            : "Silent";

    public bool CanEditBehaviors => !IsEditingSymbol &&
        CanMutateDocument &&
        SelectedLayer is not null &&
        !SelectedLayer.IsLocked &&
        SelectedLayer.Kind is not (LayerKind.Audio or LayerKind.Folder) &&
        SelectedLayer.Model.Compositing.Role != LayerCompositeRole.Camera;

    public bool CanAddBehavior => CanEditBehaviors;

    public bool CanDeleteSelectedBehavior => CanEditBehaviors && SelectedBehavior is not null;

    public bool SelectedBehaviorUsesTriggerArgument => SelectedBehaviorTriggerEditor == InteractionTriggerKind.FrameLabelReached;

    public bool SelectedBehaviorUsesTargetScene => SelectedBehaviorActionEditor == InteractionActionKind.GoToScene;

    public bool SelectedBehaviorUsesTargetFrameLabel => SelectedBehaviorActionEditor == InteractionActionKind.GoToFrameLabel;

    public bool SelectedBehaviorUsesTargetLayer => SelectedBehaviorActionEditor is InteractionActionKind.SetLayerVisibility or InteractionActionKind.SetButtonState or InteractionActionKind.ApplyVisualState;

    public bool SelectedBehaviorUsesButtonState => SelectedBehaviorActionEditor == InteractionActionKind.SetButtonState;

    public bool SelectedBehaviorUsesBoolValue => SelectedBehaviorActionEditor == InteractionActionKind.SetLayerVisibility;

    public bool SelectedBehaviorUsesVariable => SelectedBehaviorActionEditor == InteractionActionKind.SetVariable;

    public bool SelectedBehaviorUsesVisualState => SelectedBehaviorActionEditor == InteractionActionKind.ApplyVisualState;

    public IReadOnlyList<string> AvailableBehaviorVisualStateGroups =>
        ResolveBehaviorVisualStateTargetLayer()?.VisualStateGroups
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Name)
            .ToArray() ?? [];

    public IReadOnlyList<string> AvailableBehaviorVisualStates => GetAvailableBehaviorVisualStates(SelectedBehaviorTargetVisualStateGroupEditor);

    public bool CanEditCurrentFrameActionScript => !IsEditingSymbol && CanMutateDocument && FindCurrentFrameLabelModel() is not null;

    public bool CanEditSelectedBehaviorScript => CanEditBehaviors && SelectedBehavior is not null;

    public string SelectedBehaviorSummary => SelectedBehavior is null
        ? "No interaction selected"
        : SelectedBehavior.Summary;

    public string CurrentFrameActionSummary
    {
        get
        {
            var label = FindCurrentFrameLabelModel();
            return label is null
                ? "Add a frame label at the current frame to attach scripted actions."
                : $"{label.Name} • {ActionScriptService.GetSummary(label.Script)}";
        }
    }

    public string SelectedBehaviorScriptSummary => SelectedBehavior is null
        ? "Select a behavior to author a script block."
        : $"{SelectedBehavior.Name} • {ActionScriptService.GetSummary(SelectedBehavior.Model.Script)}";

    public string PrototypeVariableSummary => _prototypeVariables.Count == 0
        ? "No runtime variables"
        : string.Join(" • ", _prototypeVariables.OrderBy(item => item.Key).Select(item => $"{item.Key}={item.Value}"));

    public string PrototypeSummary => IsPrototypeMode
        ? $"{PrototypeStatusMessage} • {PrototypeVariableSummary}"
        : "Prototype mode lets you test buttons, cues, and lightweight behaviors without dirtying the document.";

    public string SwatchesSummary => $"{ColorSwatches.Count} saved swatches ready for fill, stroke, gradients, and scene backgrounds.";

    public string HistoryPanelSummary => HistoryEntries.Count == 0
        ? "No recorded editing operations yet."
        : $"{HistoryEntries.Count} recent operation(s) • undo {(CanUndo ? "available" : "unavailable")} • redo {(CanRedo ? "available" : "unavailable")}";

    public string MovieExplorerSummary => MovieExplorerNodes.Count == 0
        ? "No document tree items available."
        : $"{MovieExplorerNodes.Count} top-level branch(es) • filter by name, kind, label, marker, media, or symbol.";

    public bool CanFocusSelectedMovieExplorerNode => SelectedMovieExplorerNode?.CanFocus == true;

    public string SelectedMovieExplorerSummary => SelectedMovieExplorerNode?.Summary ?? "Select a node to focus the corresponding scene, layer, symbol, cue, or preset.";

    public bool HasPublishErrors => PublishValidationIssues.Any(issue => string.Equals(issue.Severity, "Error", StringComparison.OrdinalIgnoreCase));

    public bool PublishOutputIsSpriteSheet => PublishOutputKindEditor == PublishOutputKind.SpriteSheet;

    public bool PublishOutputRequiresDirectory => PublishOutputKindEditor == PublishOutputKind.PngSequence;

    public bool PublishOutputIsSingleFile => !PublishOutputRequiresDirectory;

    [ObservableProperty]
    private string documentName = "Sample Composition";

    [ObservableProperty]
    private string fileLabel = "Unsaved";

    [ObservableProperty]
    private TimelineDocumentFileFormat currentDocumentFileFormat = TimelineDocumentFileFormat.NativeProject;

    [ObservableProperty]
    private double duration = 6;

    [ObservableProperty]
    private double canvasWidth = 1280;

    [ObservableProperty]
    private double canvasHeight = 720;

    [ObservableProperty]
    private string backgroundFrom = "#09111F";

    [ObservableProperty]
    private string backgroundTo = "#182748";

    [ObservableProperty]
    private bool transparentStageBackground;

    [ObservableProperty]
    private string pasteboardColor = "#B9BEC7";

    [ObservableProperty]
    private bool matchPasteboardToStageColor;

    [ObservableProperty]
    private LayerViewModel? selectedLayer;

    [ObservableProperty]
    private SceneViewModel? selectedScene;

    [ObservableProperty]
    private LibraryItemViewModel? selectedLibraryItem;

    [ObservableProperty]
    private string librarySearchText = string.Empty;

    [ObservableProperty]
    private string movieExplorerSearchText = string.Empty;

    [ObservableProperty]
    private string selectedLibraryFolderFilter = "All folders";

    [ObservableProperty]
    private LibraryItemViewModel? selectedComponentItem;

    [ObservableProperty]
    private ComponentParameterDefinitionViewModel? selectedComponentParameter;

    [ObservableProperty]
    private ComponentParameterOverrideViewModel? selectedComponentOverrideItem;

    [ObservableProperty]
    private MovieExplorerNodeViewModel? selectedMovieExplorerNode;

    [ObservableProperty]
    private MediaAssetViewModel? selectedMediaAsset;

    [ObservableProperty]
    private InteractionBehaviorViewModel? selectedBehavior;

    [ObservableProperty]
    private PublishProfileViewModel? selectedPublishProfile;

    [ObservableProperty]
    private SymbolKind selectedLibrarySymbolKind = SymbolKind.Graphic;

    [ObservableProperty]
    private string libraryItemNameEditor = string.Empty;

    [ObservableProperty]
    private string libraryFolderEditor = string.Empty;

    [ObservableProperty]
    private string libraryLinkageIdEditor = string.Empty;

    [ObservableProperty]
    private string libraryBaseClassEditor = string.Empty;

    [ObservableProperty]
    private bool libraryExportForRuntimeEditor;

    [ObservableProperty]
    private bool libraryImportForRuntimeEditor;

    [ObservableProperty]
    private bool libraryExportInFirstFrameEditor = true;

    [ObservableProperty]
    private string librarySharedLibraryPathEditor = string.Empty;

    [ObservableProperty]
    private bool libraryUpdateAutomaticallyEditor;

    [ObservableProperty]
    private bool libraryUseScale9GridEditor;

    [ObservableProperty]
    private double libraryScale9LeftEditor = 16d;

    [ObservableProperty]
    private double libraryScale9TopEditor = 16d;

    [ObservableProperty]
    private double libraryScale9RightEditor = 144d;

    [ObservableProperty]
    private double libraryScale9BottomEditor = 84d;

    [ObservableProperty]
    private string librarySourceAssetPathEditor = string.Empty;

    [ObservableProperty]
    private string componentParameterNameEditor = string.Empty;

    [ObservableProperty]
    private ComponentParameterKind componentParameterKindEditor = ComponentParameterKind.String;

    [ObservableProperty]
    private ComponentParameterBindingTarget componentParameterBindingTargetEditor = ComponentParameterBindingTarget.Text;

    [ObservableProperty]
    private string componentParameterTargetLayerNameEditor = string.Empty;

    [ObservableProperty]
    private string componentParameterDefaultValueEditor = string.Empty;

    [ObservableProperty]
    private string componentParameterDescriptionEditor = string.Empty;

    [ObservableProperty]
    private string componentOverrideValueEditor = string.Empty;

    [ObservableProperty]
    private double libraryRegistrationPointXEditor;

    [ObservableProperty]
    private double libraryRegistrationPointYEditor;

    [ObservableProperty]
    private ButtonVisualState editingButtonState = ButtonVisualState.Up;

    [ObservableProperty]
    private AnimatedProperty selectedProperty = AnimatedProperty.X;

    [ObservableProperty]
    private Guid? selectedKeyframeId;

    [ObservableProperty]
    private double currentTime;

    [ObservableProperty]
    private double canvasZoom = 1d;

    [ObservableProperty]
    private double timelineZoom = 150d;

    [ObservableProperty]
    private double frameTimelineZoom = 18d;

    [ObservableProperty]
    private GridLength leftDockWidth = new(252);

    [ObservableProperty]
    private GridLength rightDockWidth = new(300);

    [ObservableProperty]
    private GridLength timelineDockHeight = new(460);

    [ObservableProperty]
    private WorkspacePanelMode leftPanelMode = WorkspacePanelMode.Docked;

    [ObservableProperty]
    private WorkspacePanelMode rightPanelMode = WorkspacePanelMode.Docked;

    [ObservableProperty]
    private WorkspacePanelMode timelinePanelMode = WorkspacePanelMode.Docked;

    [ObservableProperty]
    private WorkspaceLayoutPreset selectedWorkspacePreset = WorkspaceLayoutPreset.Classic;

    [ObservableProperty]
    private string selectedWorkspaceFocusTarget = "Stage";

    [ObservableProperty]
    private TimelineWorkspaceView selectedTimelineView = TimelineWorkspaceView.Frames;

    [ObservableProperty]
    private bool useDockWorkspace = true;

    [ObservableProperty]
    private RenderingEngineKind selectedRenderingEngine = RenderingEngineKind.Avalonia;

    [ObservableProperty]
    private AnimationExchangeFormat selectedAnimationExchangeFormat = AnimationExchangeFormat.AvaloniaXaml;

    [ObservableProperty]
    private AnimationExchangePreviewMode selectedAnimationExchangePreviewMode = AnimationExchangePreviewMode.Split;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private bool isPrototypeMode;

    [ObservableProperty]
    private bool loopPlayback = true;

    [ObservableProperty]
    private bool playAllScenes;

    [ObservableProperty]
    private bool autoKey = true;

    [ObservableProperty]
    private bool snapToGrid = true;

    [ObservableProperty]
    private DrawingTool selectedDrawingTool = DrawingTool.Select;

    [ObservableProperty]
    private ZoomToolMode zoomToolModeEditor = ZoomToolMode.In;

    [ObservableProperty]
    private LassoToolMode lassoToolModeEditor = LassoToolMode.Freeform;

    [ObservableProperty]
    private bool lassoContactSensitiveSelection = true;

    [ObservableProperty]
    private PencilToolMode pencilToolModeEditor = PencilToolMode.Smooth;

    [ObservableProperty]
    private EraserToolMode eraserToolModeEditor = EraserToolMode.Object;

    [ObservableProperty]
    private VectorStrokeProfilePreset vectorStrokeProfilePresetEditor = VectorStrokeProfilePreset.Uniform;

    [ObservableProperty]
    private double vectorStrokeProfileStrengthEditor = 0.78d;

    [ObservableProperty]
    private PaintBucketGapSize paintBucketGapSizeEditor = PaintBucketGapSize.Small;

    [ObservableProperty]
    private bool paintBucketLockFillEditor = true;

    [ObservableProperty]
    private double eraserSizeEditor = 42d;

    [ObservableProperty]
    private bool syncBrushAndEraserSize = true;

    [ObservableProperty]
    private double canvasViewRotation;

    [ObservableProperty]
    private ShapeDrawingMode shapeDrawingModeEditor = ShapeDrawingMode.Merge;

    [ObservableProperty]
    private int polyStarSidesEditor = 5;

    [ObservableProperty]
    private double polyStarInnerRadiusEditor = 0.46d;

    [ObservableProperty]
    private bool polyStarIsStarEditor = true;

    [ObservableProperty]
    private bool onionSkinEnabled = true;

    [ObservableProperty]
    private int onionSkinBefore = 2;

    [ObservableProperty]
    private int onionSkinAfter = 2;

    [ObservableProperty]
    private bool onionSkinKeyframesOnly;

    [ObservableProperty]
    private double onionSkinBaseOpacity = 0.14d;

    [ObservableProperty]
    private double onionSkinOpacityFalloff = 0.06d;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private string prototypeStatusMessage = "Prototype idle";

    [ObservableProperty]
    private string publishProfileNameEditor = string.Empty;

    [ObservableProperty]
    private AnimateDocumentType animateDocumentTypeEditor = AnimateDocumentType.Html5Canvas;

    [ObservableProperty]
    private AnimateDocumentUnits animateDocumentUnitsEditor = AnimateDocumentUnits.Pixels;

    [ObservableProperty]
    private bool animateDocumentResponsiveEditor = true;

    [ObservableProperty]
    private bool animateDocumentExternalScriptEditor = true;

    [ObservableProperty]
    private bool animateDocumentWebFontsEditor = true;

    [ObservableProperty]
    private bool animateDocumentAdvancedLayersEditor = true;

    [ObservableProperty]
    private bool animateDocumentEnable3DEditor = true;

    [ObservableProperty]
    private double animateDocumentPerspectiveAngleEditor = 55d;

    [ObservableProperty]
    private double animateDocumentVanishingPointXEditor = 0.5d;

    [ObservableProperty]
    private double animateDocumentVanishingPointYEditor = 0.5d;

    [ObservableProperty]
    private string animateDocumentTemplateNameEditor = "HTML5 Canvas";

    [ObservableProperty]
    private PublishOutputKind publishOutputKindEditor = PublishOutputKind.PngSequence;

    [ObservableProperty]
    private int publishWidthEditor = 1280;

    [ObservableProperty]
    private int publishHeightEditor = 720;

    [ObservableProperty]
    private double publishFrameRateEditor = 24d;

    [ObservableProperty]
    private bool publishPlayAllScenesEditor;

    [ObservableProperty]
    private bool publishTransparentBackgroundEditor;

    [ObservableProperty]
    private bool publishIncludeSceneGraphEditor = true;

    [ObservableProperty]
    private bool publishIncludeValidationReportEditor = true;

    [ObservableProperty]
    private int publishSpriteSheetColumnsEditor = 6;

    [ObservableProperty]
    private int publishQualityEditor = 90;

    [ObservableProperty]
    private string layerNameEditor = string.Empty;

    [ObservableProperty]
    private string sceneNameEditor = string.Empty;

    [ObservableProperty]
    private double sceneFrameRate = 24;

    [ObservableProperty]
    private string fillHexEditor = "#FFFFFF";

    [ObservableProperty]
    private string strokeHexEditor = "#FFFFFF";

    [ObservableProperty]
    private bool fillEnabledEditor = true;

    [ObservableProperty]
    private bool strokeEnabledEditor = true;

    [ObservableProperty]
    private double strokeThicknessEditor = 1.6d;

    [ObservableProperty]
    private bool useGradientEditor;

    [ObservableProperty]
    private LayerGradientKind gradientKindEditor = LayerGradientKind.Linear;

    [ObservableProperty]
    private double gradientAngleEditor = 45d;

    [ObservableProperty]
    private double gradientCenterXEditor = 0.5d;

    [ObservableProperty]
    private double gradientCenterYEditor = 0.5d;

    [ObservableProperty]
    private double gradientScaleXEditor = 1d;

    [ObservableProperty]
    private double gradientScaleYEditor = 1d;

    [ObservableProperty]
    private string gradientFromEditor = "#FFFFFF";

    [ObservableProperty]
    private string gradientToEditor = "#FFFFFF";

    [ObservableProperty]
    private LayerStrokeCapStyle strokeCapStyleEditor = LayerStrokeCapStyle.Round;

    [ObservableProperty]
    private LayerStrokeJoinStyle strokeJoinStyleEditor = LayerStrokeJoinStyle.Round;

    [ObservableProperty]
    private double strokeMiterLimitEditor = 3d;

    [ObservableProperty]
    private double cornerRadiusTopLeftEditor = 28d;

    [ObservableProperty]
    private double cornerRadiusTopRightEditor = 28d;

    [ObservableProperty]
    private double cornerRadiusBottomRightEditor = 28d;

    [ObservableProperty]
    private double cornerRadiusBottomLeftEditor = 28d;

    [ObservableProperty]
    private double ellipseStartAngleEditor;

    [ObservableProperty]
    private double ellipseSweepAngleEditor = 360d;

    [ObservableProperty]
    private string textEditor = string.Empty;

    [ObservableProperty]
    private string textFontFamilyEditor = string.Empty;

    [ObservableProperty]
    private bool textBoldEditor;

    [ObservableProperty]
    private bool textItalicEditor;

    [ObservableProperty]
    private LayerTextAlignment textAlignmentEditor = LayerTextAlignment.Left;

    [ObservableProperty]
    private double textLetterSpacingEditor;

    [ObservableProperty]
    private double textLineHeightEditor;

    [ObservableProperty]
    private bool textRenderAsHtmlEditor;

    [ObservableProperty]
    private int textMaxCharactersEditor;

    [ObservableProperty]
    private bool textPasswordEditor;

    [ObservableProperty]
    private bool textUseDeviceFontsEditor;

    [ObservableProperty]
    private FlashTextAntiAliasMode textAntiAliasModeEditor = FlashTextAntiAliasMode.Animation;

    [ObservableProperty]
    private FlashTextFieldKind textFieldKindEditor = FlashTextFieldKind.Static;

    [ObservableProperty]
    private FlashTextLineMode textLineModeEditor = FlashTextLineMode.SingleLine;

    [ObservableProperty]
    private bool textSelectableEditor = true;

    [ObservableProperty]
    private bool textShowBorderEditor;

    [ObservableProperty]
    private string textVariableNameEditor = string.Empty;

    [ObservableProperty]
    private bool pathClosedEditor;

    [ObservableProperty]
    private int selectedPathPointIndexEditor = -1;

    [ObservableProperty]
    private VectorHandleMode selectedPathPointHandleModeEditor = VectorHandleMode.Corner;

    [ObservableProperty]
    private double selectedPathPointWidthScaleEditor = 1d;

    [ObservableProperty]
    private string frameLabelEditor = string.Empty;

    [ObservableProperty]
    private string frameActionScriptEditor = string.Empty;

    [ObservableProperty]
    private ActionScriptSnippetViewModel? selectedActionScriptSnippet;

    [ObservableProperty]
    private string markerNameEditor = string.Empty;

    [ObservableProperty]
    private string markerNotesEditor = string.Empty;

    [ObservableProperty]
    private string markerColorEditor = "#57C9FF";

    [ObservableProperty]
    private SceneMarkerKind selectedMarkerKind = SceneMarkerKind.Cue;

    [ObservableProperty]
    private bool useWorkAreaPlayback;

    [ObservableProperty]
    private SceneMarkerViewModel? selectedSceneMarker;

    [ObservableProperty]
    private MediaPlaybackMode mediaPlaybackModeEditor = MediaPlaybackMode.Stream;

    [ObservableProperty]
    private double mediaStartTimeEditor;

    [ObservableProperty]
    private double mediaClipOffsetEditor;

    [ObservableProperty]
    private double mediaClipDurationEditor = 1d;

    [ObservableProperty]
    private bool mediaLoopEditor;

    [ObservableProperty]
    private int mediaRepeatCountEditor = 1;

    [ObservableProperty]
    private bool mediaRepeatForeverEditor;

    [ObservableProperty]
    private double mediaVolumeEditor = 1d;

    [ObservableProperty]
    private double mediaGainDbEditor;

    [ObservableProperty]
    private double mediaPanEditor;

    [ObservableProperty]
    private double mediaFadeInEditor;

    [ObservableProperty]
    private double mediaFadeOutEditor;

    [ObservableProperty]
    private double audioMeterPeak;

    [ObservableProperty]
    private double audioMeterRms;

    [ObservableProperty]
    private double audioMeterLeftGain;

    [ObservableProperty]
    private double audioMeterRightGain;

    [ObservableProperty]
    private bool audioMeterIsAudible;

    [ObservableProperty]
    private bool editMultipleFramesEnabled;

    [ObservableProperty]
    private SymbolPlaybackMode symbolPlaybackModeEditor = SymbolPlaybackMode.SceneTime;

    [ObservableProperty]
    private double symbolPlaybackOffsetEditor;

    [ObservableProperty]
    private int symbolLockedFrameEditor;

    [ObservableProperty]
    private ButtonVisualState symbolButtonStateEditor = ButtonVisualState.Up;

    [ObservableProperty]
    private string instanceNameEditor = string.Empty;

    [ObservableProperty]
    private bool layerOutlineEditor;

    [ObservableProperty]
    private string layerOutlineColorEditor = "#57C9FF";

    [ObservableProperty]
    private bool layerCacheAsBitmapEditor;

    [ObservableProperty]
    private string layerBitmapCacheBackgroundEditor = "#000000";

    [ObservableProperty]
    private LayerViewModel? selectedGuideLayer;

    [ObservableProperty]
    private LayerViewModel? selectedTransformParentLayer;

    [ObservableProperty]
    private bool guideOrientToPathEditor;

    [ObservableProperty]
    private bool guideSnapToPathEditor;

    [ObservableProperty]
    private bool warpEnabledEditor;

    [ObservableProperty]
    private int warpMeshColumnsEditor = 3;

    [ObservableProperty]
    private int warpMeshRowsEditor = 3;

    [ObservableProperty]
    private bool rigEnabledEditor;

    [ObservableProperty]
    private string rigPoseNameEditor = "Pose 1";

    [ObservableProperty]
    private RigPoseViewModel? selectedRigPose;

    [ObservableProperty]
    private string selectedBehaviorNameEditor = string.Empty;

    [ObservableProperty]
    private bool selectedBehaviorEnabledEditor = true;

    [ObservableProperty]
    private InteractionTriggerKind selectedBehaviorTriggerEditor = InteractionTriggerKind.PointerClick;

    [ObservableProperty]
    private string selectedBehaviorTriggerArgumentEditor = string.Empty;

    [ObservableProperty]
    private InteractionActionKind selectedBehaviorActionEditor = InteractionActionKind.Play;

    [ObservableProperty]
    private SceneViewModel? selectedBehaviorTargetScene;

    [ObservableProperty]
    private LayerViewModel? selectedBehaviorTargetLayer;

    [ObservableProperty]
    private string selectedBehaviorTargetFrameLabelEditor = string.Empty;

    [ObservableProperty]
    private ButtonVisualState selectedBehaviorTargetButtonStateEditor = ButtonVisualState.Up;

    [ObservableProperty]
    private bool selectedBehaviorBoolValueEditor = true;

    [ObservableProperty]
    private string selectedBehaviorVariableNameEditor = string.Empty;

    [ObservableProperty]
    private string selectedBehaviorVariableValueEditor = string.Empty;

    [ObservableProperty]
    private string selectedBehaviorTargetVisualStateGroupEditor = string.Empty;

    [ObservableProperty]
    private string selectedBehaviorTargetVisualStateEditor = string.Empty;

    [ObservableProperty]
    private string selectedBehaviorScriptEditor = string.Empty;

    [ObservableProperty]
    private int selectionStartFrame = -1;

    [ObservableProperty]
    private int selectionEndFrame = -1;

    [ObservableProperty]
    private SceneTransitionKind sceneTransitionKindEditor = SceneTransitionKind.None;

    [ObservableProperty]
    private double sceneTransitionDurationEditor = 0.35d;

    [ObservableProperty]
    private string sceneTransitionAccentColorEditor = "#000000";

    [ObservableProperty]
    private double inspectorX;

    [ObservableProperty]
    private double inspectorY;

    [ObservableProperty]
    private double inspectorWidth;

    [ObservableProperty]
    private double inspectorHeight;

    [ObservableProperty]
    private double inspectorScaleX = 1d;

    [ObservableProperty]
    private double inspectorScaleY = 1d;

    [ObservableProperty]
    private double inspectorSkewX;

    [ObservableProperty]
    private double inspectorSkewY;

    [ObservableProperty]
    private double inspectorRotation;

    [ObservableProperty]
    private double inspectorRotationX;

    [ObservableProperty]
    private double inspectorRotationY;

    [ObservableProperty]
    private double inspectorZDepth;

    [ObservableProperty]
    private double inspectorOpacity = 1;

    [ObservableProperty]
    private double inspectorCornerRadius = 28;

    [ObservableProperty]
    private double inspectorFontSize = 48;

    [ObservableProperty]
    private string lastAnimationExchangeSummary = string.Empty;

    [ObservableProperty]
    private string animationExchangePreviewCode = string.Empty;

    [ObservableProperty]
    private string animationExchangePreviewSummary = string.Empty;

    [ObservableProperty]
    private string animationExchangePreviewVisualSummary = string.Empty;

    [ObservableProperty]
    private string animationExchangePreviewFileName = string.Empty;

    [ObservableProperty]
    private TimelineDocument? animationExchangePreviewDocument;

    [ObservableProperty]
    private Bitmap? programMonitorBitmap;

    [ObservableProperty]
    private Bitmap? sourceMonitorBitmap;

    [ObservableProperty]
    private string sourceMonitorTitle = "No source";

    [ObservableProperty]
    private string sourceMonitorSummary = "Select a stage layer, library symbol, or media asset.";

    [ObservableProperty]
    private double sourceMonitorTime;

    [ObservableProperty]
    private double sourceMonitorDuration;

    [ObservableProperty]
    private double sourceMonitorInTime;

    [ObservableProperty]
    private double sourceMonitorOutTime;

    [ObservableProperty]
    private string visualStateGroupNameEditor = "CommonStates";

    [ObservableProperty]
    private string visualStateNameEditor = "State";

    [ObservableProperty]
    private VisualStateGroupViewModel? selectedVisualStateGroup;

    [ObservableProperty]
    private VisualStateViewModel? selectedVisualState;

    [ObservableProperty]
    private string avaloniaControlContentEditor = string.Empty;

    [ObservableProperty]
    private string avaloniaControlSecondaryContentEditor = string.Empty;

    [ObservableProperty]
    private bool avaloniaControlIsCheckedEditor;

    [ObservableProperty]
    private double avaloniaControlMinimumEditor;

    [ObservableProperty]
    private double avaloniaControlMaximumEditor = 100d;

    [ObservableProperty]
    private double avaloniaControlValueEditor = 50d;

    [ObservableProperty]
    private bool avaloniaControlIsIndeterminateEditor;

    [ObservableProperty]
    private string avaloniaControlSourceEditor = string.Empty;

    [ObservableProperty]
    private LayerCompositeRole layerRoleEditor = LayerCompositeRole.Normal;

    [ObservableProperty]
    private LayerBlendMode layerBlendModeEditor = LayerBlendMode.Normal;

    [ObservableProperty]
    private int maskLayerCountEditor = 1;

    [ObservableProperty]
    private double layerBlurEditor;

    [ObservableProperty]
    private double layerGlowSizeEditor = 18d;

    [ObservableProperty]
    private double layerGlowOpacityEditor;

    [ObservableProperty]
    private string layerGlowColorEditor = "#FFFFFF";

    [ObservableProperty]
    private double layerGradientGlowSizeEditor;

    [ObservableProperty]
    private double layerGradientGlowStrengthEditor;

    [ObservableProperty]
    private double layerGradientGlowAngleEditor = 45d;

    [ObservableProperty]
    private double layerGradientGlowDistanceEditor;

    [ObservableProperty]
    private string layerGradientGlowFromEditor = "#61E6FF";

    [ObservableProperty]
    private string layerGradientGlowToEditor = "#09111F";

    [ObservableProperty]
    private double layerShadowBlurEditor;

    [ObservableProperty]
    private double layerShadowOpacityEditor;

    [ObservableProperty]
    private double layerShadowOffsetXEditor;

    [ObservableProperty]
    private double layerShadowOffsetYEditor;

    [ObservableProperty]
    private string layerShadowColorEditor = "#09111F";

    [ObservableProperty]
    private string layerTintColorEditor = "#FFFFFF";

    [ObservableProperty]
    private double layerTintStrengthEditor;

    [ObservableProperty]
    private double layerBrightnessEditor;

    [ObservableProperty]
    private double layerContrastEditor;

    [ObservableProperty]
    private double layerHueShiftEditor;

    [ObservableProperty]
    private LayerBevelType layerBevelTypeEditor = LayerBevelType.Inner;

    [ObservableProperty]
    private double layerBevelSizeEditor;

    [ObservableProperty]
    private double layerBevelStrengthEditor;

    [ObservableProperty]
    private double layerBevelAngleEditor = 45d;

    [ObservableProperty]
    private double layerBevelDistanceEditor = 4d;

    [ObservableProperty]
    private string layerBevelHighlightColorEditor = "#FFFFFF";

    [ObservableProperty]
    private string layerBevelShadowColorEditor = "#09111F";

    [ObservableProperty]
    private LayerBevelType layerGradientBevelTypeEditor = LayerBevelType.Inner;

    [ObservableProperty]
    private double layerGradientBevelSizeEditor;

    [ObservableProperty]
    private double layerGradientBevelStrengthEditor;

    [ObservableProperty]
    private double layerGradientBevelAngleEditor = 45d;

    [ObservableProperty]
    private double layerGradientBevelDistanceEditor = 4d;

    [ObservableProperty]
    private string layerGradientBevelFromEditor = "#FFFFFF";

    [ObservableProperty]
    private string layerGradientBevelToEditor = "#09111F";

    [ObservableProperty]
    private FlashColorEffectMode layerFlashColorEffectModeEditor = FlashColorEffectMode.None;

    [ObservableProperty]
    private double layerFlashAlphaPercentEditor = 100d;

    [ObservableProperty]
    private string layerFlashTintColorEditor = "#FFFFFF";

    [ObservableProperty]
    private double layerFlashTintPercentEditor;

    [ObservableProperty]
    private double layerFlashBrightnessPercentEditor;

    [ObservableProperty]
    private double layerFlashRedPercentEditor = 100d;

    [ObservableProperty]
    private double layerFlashGreenPercentEditor = 100d;

    [ObservableProperty]
    private double layerFlashBluePercentEditor = 100d;

    [ObservableProperty]
    private double layerFlashAdvancedAlphaPercentEditor = 100d;

    [ObservableProperty]
    private double layerFlashRedOffsetEditor;

    [ObservableProperty]
    private double layerFlashGreenOffsetEditor;

    [ObservableProperty]
    private double layerFlashBlueOffsetEditor;

    [ObservableProperty]
    private double layerFlashAlphaOffsetEditor;

    [ObservableProperty]
    private double layerSaturationEditor = 1d;

    [ObservableProperty]
    private double layerParallaxDepthEditor = 1d;

    [ObservableProperty]
    private EasingKind selectedKeyframeEasing = EasingKind.Linear;

    [ObservableProperty]
    private MotionPresetViewModel? selectedMotionPreset;

    [ObservableProperty]
    private string motionPresetNameEditor = string.Empty;

    [ObservableProperty]
    private string motionPresetDescriptionEditor = string.Empty;

    [ObservableProperty]
    private double customEaseControl1X = 0.25d;

    [ObservableProperty]
    private double customEaseControl1Y = 0.1d;

    [ObservableProperty]
    private double customEaseControl2X = 0.25d;

    [ObservableProperty]
    private double customEaseControl2Y = 1d;

    partial void OnSelectedLayerChanged(LayerViewModel? oldValue, LayerViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsSelected = false;
        }

        if (newValue is not null)
        {
            newValue.IsSelected = true;
        }

        SelectedKeyframeId = null;
        ReloadTracks();
        UpdateFrameRowSelection();
        ReloadBehaviors();
        ReloadActionScriptSnippets(SelectedActionScriptSnippet?.Name);
        ReloadVisualStateGroups();
        ReloadSelectedComponentOverrides();
        RefreshInspector();
        OnPropertyChanged(nameof(SelectedLayerIsText));
        OnPropertyChanged(nameof(SelectedLayerId));
        OnPropertyChanged(nameof(SelectedKeyframeSummary));
        RefreshSelectionStateProperties();
        RefreshFrameSelectionStateProperties();
        RefreshLibraryStateProperties();
        RefreshAudioMeter();
        RefreshMonitorPreviews();
    }

    partial void OnSelectedDrawingToolChanged(DrawingTool value)
    {
        OnPropertyChanged(nameof(ToolModeLabel));
        OnPropertyChanged(nameof(CanEditStyleSurface));
        OnPropertyChanged(nameof(CanEditToolOptions));
        OnPropertyChanged(nameof(IsSelectToolActive));
        OnPropertyChanged(nameof(IsSubselectToolActive));
        OnPropertyChanged(nameof(IsLassoToolActive));
        OnPropertyChanged(nameof(IsHandToolActive));
        OnPropertyChanged(nameof(IsZoomToolActive));
        OnPropertyChanged(nameof(IsRotateViewToolActive));
        OnPropertyChanged(nameof(IsEyedropperToolActive));
        OnPropertyChanged(nameof(IsPaintBucketToolActive));
        OnPropertyChanged(nameof(IsInkBottleToolActive));
        OnPropertyChanged(nameof(IsWidthToolActive));
        OnPropertyChanged(nameof(IsWarpToolActive));
        OnPropertyChanged(nameof(IsRigToolActive));
        OnPropertyChanged(nameof(IsRectangleToolActive));
        OnPropertyChanged(nameof(IsEllipseToolActive));
        OnPropertyChanged(nameof(IsPolyStarToolActive));
        OnPropertyChanged(nameof(IsTextToolActive));
        OnPropertyChanged(nameof(IsLineToolActive));
        OnPropertyChanged(nameof(IsPenToolActive));
        OnPropertyChanged(nameof(IsBrushToolActive));
        OnPropertyChanged(nameof(IsPencilToolActive));
        OnPropertyChanged(nameof(IsEraserToolActive));
        OnPropertyChanged(nameof(ShowZoomToolOptions));
        OnPropertyChanged(nameof(ShowRotateViewToolOptions));
        OnPropertyChanged(nameof(ShowLassoToolOptions));
        OnPropertyChanged(nameof(ShowPencilToolOptions));
        OnPropertyChanged(nameof(ShowEraserToolOptions));
        OnPropertyChanged(nameof(ShowSubselectToolOptions));
        OnPropertyChanged(nameof(ShowPaintBucketToolOptions));
        OnPropertyChanged(nameof(ShowWidthToolOptions));
        OnPropertyChanged(nameof(ShowStrokeProfileToolOptions));
        OnPropertyChanged(nameof(CanvasViewportHint));
        OnPropertyChanged(nameof(ToolOptionsSummary));
        RefreshShapeStyleStateProperties();
        StatusMessage = value == DrawingTool.Select
            ? "Selection tool active"
            : $"{ToolModeLabel} tool active";
    }

    partial void OnLastAnimationExchangeSummaryChanged(string value)
    {
        OnPropertyChanged(nameof(LastAnimationExchangeSummaryDisplay));
    }

    partial void OnAnimationExchangePreviewCodeChanged(string value)
    {
        OnPropertyChanged(nameof(AnimationExchangePreviewFileSummary));
    }

    partial void OnAnimationExchangePreviewSummaryChanged(string value)
    {
        OnPropertyChanged(nameof(AnimationExchangePreviewSummaryDisplay));
    }

    partial void OnAnimationExchangePreviewFileNameChanged(string value)
    {
        OnPropertyChanged(nameof(AnimationExchangePreviewFileSummary));
    }

    partial void OnSelectedSceneChanged(SceneViewModel? oldValue, SceneViewModel? newValue)
    {
        if (_suppressSceneSelection)
        {
            return;
        }

        if (IsEditingSymbol)
        {
            _suppressSceneSelection = true;
            SelectedScene = oldValue ?? newValue;
            _suppressSceneSelection = false;
            return;
        }

        if (oldValue is not null)
        {
            oldValue.IsSelected = false;
            PersistActiveScene(oldValue.Model);
        }

        if (newValue is not null)
        {
            newValue.IsSelected = true;
            ActivateScene(newValue.Model);
            if (IsPlaying)
            {
                _playbackClock.Restart();
                _playbackOriginTime = 0;
            }
        }

        RefreshSceneEditor();
        RefreshSceneStateProperties();
        ReloadSceneMarkers();
        ReloadActionScriptSnippets(SelectedActionScriptSnippet?.Name);
        RefreshSceneTimelineEditors();
        RefreshMonitorPreviews();
        StatusMessage = newValue is null
            ? "No scene selected"
            : $"Scene switched to {newValue.Name}";
    }

    partial void OnSelectedLibraryItemChanged(LibraryItemViewModel? oldValue, LibraryItemViewModel? newValue)
    {
        RefreshSelectedLibraryItemEditor();
        RefreshLibraryStateProperties();
        RefreshSourceMonitorPreview();
    }

    partial void OnLibrarySearchTextChanged(string value)
    {
        ReloadLibraryItems();
    }

    partial void OnSelectedLibraryFolderFilterChanged(string value)
    {
        if (_suppressLibraryFilterSelection)
        {
            return;
        }

        ReloadLibraryItems();
    }

    partial void OnMovieExplorerSearchTextChanged(string value)
    {
        ReloadMovieExplorer();
    }

    partial void OnSelectedMovieExplorerNodeChanged(MovieExplorerNodeViewModel? oldValue, MovieExplorerNodeViewModel? newValue)
    {
        OnPropertyChanged(nameof(CanFocusSelectedMovieExplorerNode));
        OnPropertyChanged(nameof(SelectedMovieExplorerSummary));
    }

    partial void OnStatusMessageChanged(string value)
    {
        if (!_capturePanelHistory)
        {
            return;
        }

        AppendHistoryEntry(value);
    }

    partial void OnLibraryItemNameEditorChanged(string value)
    {
        ApplySelectedLibraryMetadata(
            item => item.Name = LibraryManagementService.EnsureUniqueLibraryName(_document, value, item.Id),
            "Library asset renamed");
    }

    partial void OnLibraryFolderEditorChanged(string value)
    {
        ApplySelectedLibraryMetadata(
            item => item.FolderPath = LibraryManagementService.NormalizeFolderPath(value),
            "Library folder updated");
    }

    partial void OnLibraryLinkageIdEditorChanged(string value)
    {
        ApplySelectedLibraryMetadata(
            item => item.LinkageId = LibraryManagementService.EnsureUniqueLinkageId(_document, value, item.Id, item.Name),
            "Library linkage updated");
    }

    partial void OnLibraryBaseClassEditorChanged(string value)
    {
        ApplySelectedLibraryMetadata(
            item => item.BaseClassName = LibraryManagementService.NormalizeClassName(value),
            "Library base class updated");
    }

    partial void OnLibraryExportForRuntimeEditorChanged(bool value)
    {
        ApplySelectedLibraryMetadata(
            item => item.ExportForRuntimeSharing = value,
            value ? "Runtime export enabled" : "Runtime export disabled");
    }

    partial void OnLibraryImportForRuntimeEditorChanged(bool value)
    {
        ApplySelectedLibraryMetadata(
            item => item.ImportForRuntimeSharing = value,
            value ? "Runtime import enabled" : "Runtime import disabled");
    }

    partial void OnLibraryExportInFirstFrameEditorChanged(bool value)
    {
        ApplySelectedLibraryMetadata(
            item => item.ExportInFirstFrame = value,
            value ? "First-frame export enabled" : "First-frame export disabled");
    }

    partial void OnLibrarySharedLibraryPathEditorChanged(string value)
    {
        ApplySelectedLibraryMetadata(
            item => item.SharedLibraryPath = LibraryManagementService.NormalizeSharedLibraryPath(value),
            "Shared library path updated");
    }

    partial void OnLibraryUpdateAutomaticallyEditorChanged(bool value)
    {
        ApplySelectedLibraryMetadata(
            item => item.UpdateAutomatically = value,
            value ? "Shared library auto-update enabled" : "Shared library auto-update disabled");
    }

    partial void OnLibraryUseScale9GridEditorChanged(bool value)
    {
        ApplySelectedLibraryMetadata(
            item =>
            {
                item.UseScale9Grid = value;
                LibraryManagementService.NormalizeScale9Grid(item);
            },
            value ? "Scale-9 grid enabled" : "Scale-9 grid disabled");
    }

    partial void OnLibraryScale9LeftEditorChanged(double value)
    {
        ApplySelectedLibraryMetadata(
            item =>
            {
                item.Scale9Left = Math.Max(0d, value);
                LibraryManagementService.NormalizeScale9Grid(item);
            },
            "Scale-9 left updated");
        RefreshSelectedLibraryItemEditor();
    }

    partial void OnLibraryScale9TopEditorChanged(double value)
    {
        ApplySelectedLibraryMetadata(
            item =>
            {
                item.Scale9Top = Math.Max(0d, value);
                LibraryManagementService.NormalizeScale9Grid(item);
            },
            "Scale-9 top updated");
        RefreshSelectedLibraryItemEditor();
    }

    partial void OnLibraryScale9RightEditorChanged(double value)
    {
        ApplySelectedLibraryMetadata(
            item =>
            {
                item.Scale9Right = value;
                LibraryManagementService.NormalizeScale9Grid(item);
            },
            "Scale-9 right updated");
        RefreshSelectedLibraryItemEditor();
    }

    partial void OnLibraryScale9BottomEditorChanged(double value)
    {
        ApplySelectedLibraryMetadata(
            item =>
            {
                item.Scale9Bottom = value;
                LibraryManagementService.NormalizeScale9Grid(item);
            },
            "Scale-9 bottom updated");
        RefreshSelectedLibraryItemEditor();
    }

    partial void OnLibrarySourceAssetPathEditorChanged(string value)
    {
        ApplySelectedLibraryMetadata(
            item => item.SourceAssetPath = LibraryManagementService.NormalizeSourceAssetPath(value),
            "Library source path updated");
    }

    partial void OnLibraryRegistrationPointXEditorChanged(double value)
    {
        ApplySelectedLibraryMetadata(
            item =>
            {
                item.RegistrationPointX = value;
                LibraryManagementService.NormalizeRegistrationPoint(item);
            },
            "Registration point X updated");
        RefreshSelectedLibraryItemEditor();
    }

    partial void OnLibraryRegistrationPointYEditorChanged(double value)
    {
        ApplySelectedLibraryMetadata(
            item =>
            {
                item.RegistrationPointY = value;
                LibraryManagementService.NormalizeRegistrationPoint(item);
            },
            "Registration point Y updated");
        RefreshSelectedLibraryItemEditor();
    }

    partial void OnLeftDockWidthChanged(GridLength value)
    {
        RefreshWorkspaceLayoutProperties();
        PersistWorkspaceLayoutIfReady();
    }

    partial void OnRightDockWidthChanged(GridLength value)
    {
        RefreshWorkspaceLayoutProperties();
        PersistWorkspaceLayoutIfReady();
    }

    partial void OnTimelineDockHeightChanged(GridLength value)
    {
        RefreshWorkspaceLayoutProperties();
        PersistWorkspaceLayoutIfReady();
    }

    partial void OnLeftPanelModeChanged(WorkspacePanelMode value)
    {
        if (value != WorkspacePanelMode.Hidden)
        {
            _leftRestoreMode = value;
        }

        RefreshWorkspaceLayoutProperties();
        MarkWorkspacePresetCustom();
        PersistWorkspaceLayoutIfReady();
        StatusMessage = $"Left panel {GetWorkspacePanelModeLabel(value)}";
    }

    partial void OnRightPanelModeChanged(WorkspacePanelMode value)
    {
        if (value != WorkspacePanelMode.Hidden)
        {
            _rightRestoreMode = value;
        }

        RefreshWorkspaceLayoutProperties();
        MarkWorkspacePresetCustom();
        PersistWorkspaceLayoutIfReady();
        StatusMessage = $"Right panel {GetWorkspacePanelModeLabel(value)}";
    }

    partial void OnTimelinePanelModeChanged(WorkspacePanelMode value)
    {
        if (value != WorkspacePanelMode.Hidden)
        {
            _timelineRestoreMode = value;
        }

        RefreshWorkspaceLayoutProperties();
        MarkWorkspacePresetCustom();
        PersistWorkspaceLayoutIfReady();
        StatusMessage = $"Timeline panel {GetWorkspacePanelModeLabel(value)}";
    }

    partial void OnSelectedTimelineViewChanged(TimelineWorkspaceView value)
    {
        OnPropertyChanged(nameof(IsFramesTimelineViewActive));
        OnPropertyChanged(nameof(IsCurvesTimelineViewActive));
        OnPropertyChanged(nameof(TimelineWorkspaceViewLabel));
        OnPropertyChanged(nameof(TimelineWorkspaceViewSummary));
    }

    partial void OnUseDockWorkspaceChanged(bool value)
    {
        OnPropertyChanged(nameof(IsDockWorkspaceVisible));
        OnPropertyChanged(nameof(IsClassicWorkspaceVisible));
        StatusMessage = value ? "Dock workspace active" : "Classic workspace active";
    }

    partial void OnSelectedRenderingEngineChanged(RenderingEngineKind value)
    {
        RenderingEngineHostService.SelectedEngine = value;
        RendererPreferencesPersistenceService.Save(new RendererPreferencesState
        {
            SelectedEngine = value
        });
        OnPropertyChanged(nameof(RenderingEngineSummary));
        RefreshMonitorPreviews();
        RefreshAnimationExchangePreviewCore(updateStatus: false);
        StatusMessage = value switch
        {
            RenderingEngineKind.SkiaSharp => "Rendering engine switched to SkiaSharp",
            _ => "Rendering engine switched to Avalonia"
        };
    }

    partial void OnSelectedAnimationExchangeFormatChanged(AnimationExchangeFormat value)
    {
        OnPropertyChanged(nameof(SelectedAnimationExchangeSummary));
        OnPropertyChanged(nameof(IsAvaloniaXamlExchangeSelected));
        OnPropertyChanged(nameof(IsFlashXflExchangeSelected));
        OnPropertyChanged(nameof(IsSvgAnimationExchangeSelected));
        OnPropertyChanged(nameof(IsHtmlAnimationExchangeSelected));
        RefreshAnimationExchangePreviewCore(updateStatus: false);
    }

    partial void OnSelectedAnimationExchangePreviewModeChanged(AnimationExchangePreviewMode value)
    {
        OnPropertyChanged(nameof(IsAnimationExchangeCodeVisible));
        OnPropertyChanged(nameof(IsAnimationExchangeVisualVisible));
        OnPropertyChanged(nameof(IsAnimationExchangeSplitMode));
        OnPropertyChanged(nameof(AnimationExchangeCodeColumnWidth));
        OnPropertyChanged(nameof(AnimationExchangeSplitterColumnWidth));
        OnPropertyChanged(nameof(AnimationExchangeVisualColumnWidth));
        OnPropertyChanged(nameof(AnimationExchangePreviewModeSummary));
    }

    partial void OnCurrentDocumentFileFormatChanged(TimelineDocumentFileFormat value)
    {
        OnPropertyChanged(nameof(CurrentDocumentFileFormatLabel));
        OnPropertyChanged(nameof(DocumentFileSummary));
        OnPropertyChanged(nameof(HelpMenuSummary));
    }

    partial void OnFileLabelChanged(string value)
    {
        OnPropertyChanged(nameof(DocumentFileSummary));
        OnPropertyChanged(nameof(HelpMenuSummary));
    }

    partial void OnDocumentNameChanged(string value)
    {
        OnPropertyChanged(nameof(HelpMenuSummary));
    }

    partial void OnAnimateDocumentTypeEditorChanged(AnimateDocumentType value)
    {
        if (_suppressAnimateDocumentEditor || IsEditingSymbol)
        {
            return;
        }

        AnimateDocumentProfileService.ApplyTargetDefaults(_document, value);
        RefreshAnimateDocumentEditor();
        ReloadActionScriptSnippets(SelectedActionScriptSnippet?.Name);
        RefreshSceneStateProperties();
        RefreshPublishValidation();
        RecordHistoryIfNeeded();
        StatusMessage = $"Document target converted to {AnimateDocumentProfileService.GetDisplayName(value)}";
    }

    partial void OnAnimateDocumentUnitsEditorChanged(AnimateDocumentUnits value)
    {
        if (_suppressAnimateDocumentEditor || IsEditingSymbol)
        {
            return;
        }

        _document.Animate.Units = value;
        RefreshSceneStateProperties();
        RefreshPublishValidation();
        RecordHistoryIfNeeded();
        StatusMessage = $"Document units set to {value}";
    }

    partial void OnAnimateDocumentResponsiveEditorChanged(bool value)
    {
        if (_suppressAnimateDocumentEditor || IsEditingSymbol)
        {
            return;
        }

        _document.Animate.IsResponsive = value;
        RefreshSceneStateProperties();
        RefreshPublishValidation();
        RecordHistoryIfNeeded();
        StatusMessage = value ? "Responsive document mode enabled" : "Responsive document mode disabled";
    }

    partial void OnAnimateDocumentExternalScriptEditorChanged(bool value)
    {
        if (_suppressAnimateDocumentEditor || IsEditingSymbol)
        {
            return;
        }

        _document.Animate.UseExternalScriptFile = value;
        RefreshSceneStateProperties();
        RefreshPublishValidation();
        RecordHistoryIfNeeded();
        StatusMessage = value ? "External script output enabled" : "External script output disabled";
    }

    partial void OnAnimateDocumentWebFontsEditorChanged(bool value)
    {
        if (_suppressAnimateDocumentEditor || IsEditingSymbol)
        {
            return;
        }

        _document.Animate.UseWebFonts = value;
        RefreshSceneStateProperties();
        RefreshPublishValidation();
        RecordHistoryIfNeeded();
        StatusMessage = value ? "Web fonts enabled for the document target" : "Web fonts disabled for the document target";
    }

    partial void OnAnimateDocumentAdvancedLayersEditorChanged(bool value)
    {
        if (_suppressAnimateDocumentEditor || IsEditingSymbol)
        {
            return;
        }

        _document.Animate.UseAdvancedLayers = value;
        RefreshSceneStateProperties();
        RefreshPublishValidation();
        RecordHistoryIfNeeded();
        StatusMessage = value ? "Advanced layers enabled" : "Advanced layers disabled";
    }

    partial void OnAnimateDocumentEnable3DEditorChanged(bool value)
    {
        if (_suppressAnimateDocumentEditor || IsEditingSymbol)
        {
            return;
        }

        _document.Animate.Enable3DTransforms = value;
        RefreshSceneStateProperties();
        RefreshPublishValidation();
        RecordHistoryIfNeeded();
        StatusMessage = value ? "Document 3D transforms enabled" : "Document 3D transforms disabled";
    }

    partial void OnAnimateDocumentPerspectiveAngleEditorChanged(double value)
    {
        if (_suppressAnimateDocumentEditor || IsEditingSymbol)
        {
            return;
        }

        var normalized = TimelineMath.Clamp(value, 1d, 89d);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressAnimateDocumentEditor = true;
            AnimateDocumentPerspectiveAngleEditor = normalized;
            _suppressAnimateDocumentEditor = false;
            return;
        }

        _document.Animate.PerspectiveAngle = normalized;
        RefreshSceneStateProperties();
        RefreshPublishValidation();
        RecordHistoryIfNeeded();
        StatusMessage = "Document perspective angle updated";
    }

    partial void OnAnimateDocumentVanishingPointXEditorChanged(double value)
    {
        if (_suppressAnimateDocumentEditor || IsEditingSymbol)
        {
            return;
        }

        var normalized = TimelineMath.Clamp(value, 0d, 1d);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressAnimateDocumentEditor = true;
            AnimateDocumentVanishingPointXEditor = normalized;
            _suppressAnimateDocumentEditor = false;
            return;
        }

        _document.Animate.VanishingPointX = normalized;
        RefreshSceneStateProperties();
        RefreshPublishValidation();
        RecordHistoryIfNeeded();
        StatusMessage = "Document vanishing point X updated";
    }

    partial void OnAnimateDocumentVanishingPointYEditorChanged(double value)
    {
        if (_suppressAnimateDocumentEditor || IsEditingSymbol)
        {
            return;
        }

        var normalized = TimelineMath.Clamp(value, 0d, 1d);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressAnimateDocumentEditor = true;
            AnimateDocumentVanishingPointYEditor = normalized;
            _suppressAnimateDocumentEditor = false;
            return;
        }

        _document.Animate.VanishingPointY = normalized;
        RefreshSceneStateProperties();
        RefreshPublishValidation();
        RecordHistoryIfNeeded();
        StatusMessage = "Document vanishing point Y updated";
    }

    partial void OnAnimateDocumentTemplateNameEditorChanged(string value)
    {
        if (_suppressAnimateDocumentEditor || IsEditingSymbol)
        {
            return;
        }

        _document.Animate.TemplateName = string.IsNullOrWhiteSpace(value)
            ? AnimateDocumentProfileService.GetDisplayName(_document.Animate.Type)
            : value.Trim();
        RefreshSceneStateProperties();
        RefreshPublishValidation();
        RecordHistoryIfNeeded();
        StatusMessage = "Document template name updated";
    }

    partial void OnSelectedWorkspacePresetChanged(WorkspaceLayoutPreset value)
    {
        OnPropertyChanged(nameof(WorkspaceLayoutLabel));
        OnPropertyChanged(nameof(WorkspaceLayoutSummary));

        if (_suppressWorkspacePreset || value == WorkspaceLayoutPreset.Custom)
        {
            return;
        }

        ApplyWorkspacePreset(value, updateStatus: true);
    }

    partial void OnSelectedComponentItemChanged(LibraryItemViewModel? oldValue, LibraryItemViewModel? newValue)
    {
        OnPropertyChanged(nameof(CanInsertSelectedComponent));
        OnPropertyChanged(nameof(CanEditSelectedComponentParameters));
        OnPropertyChanged(nameof(CanRemoveSelectedComponentParameter));
        OnPropertyChanged(nameof(SelectedComponentSummary));
        OnPropertyChanged(nameof(SelectedComponentParameterSummary));
        ReloadSelectedComponentParameters();
        RefreshSourceMonitorPreview();
    }

    partial void OnSelectedComponentParameterChanged(ComponentParameterDefinitionViewModel? oldValue, ComponentParameterDefinitionViewModel? newValue)
    {
        _suppressComponentParameterEditor = true;
        ComponentParameterNameEditor = newValue?.Model.Name ?? string.Empty;
        ComponentParameterKindEditor = newValue?.Model.Kind ?? ComponentParameterKind.String;
        ComponentParameterBindingTargetEditor = newValue?.Model.BindingTarget ?? ComponentParameterBindingTarget.Text;
        ComponentParameterTargetLayerNameEditor = newValue?.Model.TargetLayerName ?? string.Empty;
        ComponentParameterDefaultValueEditor = newValue?.Model.DefaultValue ?? string.Empty;
        ComponentParameterDescriptionEditor = newValue?.Model.Description ?? string.Empty;
        _suppressComponentParameterEditor = false;
        OnPropertyChanged(nameof(CanRemoveSelectedComponentParameter));
    }

    partial void OnSelectedComponentOverrideItemChanged(ComponentParameterOverrideViewModel? oldValue, ComponentParameterOverrideViewModel? newValue)
    {
        _suppressComponentOverrideEditor = true;
        ComponentOverrideValueEditor = newValue?.Model.Value ?? string.Empty;
        _suppressComponentOverrideEditor = false;
        OnPropertyChanged(nameof(CanResetSelectedComponentOverride));
    }

    partial void OnComponentParameterNameEditorChanged(string value)
    {
        UpdateSelectedComponentParameter(
            parameter => parameter.Name = string.IsNullOrWhiteSpace(value) ? "parameter" : value.Trim(),
            "Component parameter name updated");
    }

    partial void OnComponentParameterKindEditorChanged(ComponentParameterKind value)
    {
        UpdateSelectedComponentParameter(
            parameter => parameter.Kind = value,
            "Component parameter kind updated");
    }

    partial void OnComponentParameterBindingTargetEditorChanged(ComponentParameterBindingTarget value)
    {
        UpdateSelectedComponentParameter(
            parameter => parameter.BindingTarget = value,
            "Component parameter binding target updated");
    }

    partial void OnComponentParameterTargetLayerNameEditorChanged(string value)
    {
        UpdateSelectedComponentParameter(
            parameter => parameter.TargetLayerName = value?.Trim() ?? string.Empty,
            "Component parameter target layer updated");
    }

    partial void OnComponentParameterDefaultValueEditorChanged(string value)
    {
        UpdateSelectedComponentParameter(
            parameter => parameter.DefaultValue = value ?? string.Empty,
            "Component parameter default updated");
    }

    partial void OnComponentParameterDescriptionEditorChanged(string value)
    {
        UpdateSelectedComponentParameter(
            parameter => parameter.Description = value ?? string.Empty,
            "Component parameter description updated");
    }

    partial void OnComponentOverrideValueEditorChanged(string value)
    {
        UpdateSelectedComponentOverride(
            parameter => parameter.Value = value ?? string.Empty,
            "Component override value updated");
    }

    partial void OnSelectedMediaAssetChanged(MediaAssetViewModel? oldValue, MediaAssetViewModel? newValue)
    {
        OnPropertyChanged(nameof(CanInsertSelectedMediaAsset));
        OnPropertyChanged(nameof(SelectedMediaSummary));
        RefreshSourceMonitorPreview();
    }

    partial void OnSelectedPublishProfileChanged(PublishProfileViewModel? oldValue, PublishProfileViewModel? newValue)
    {
        RefreshSelectedPublishProfileEditor();
        RefreshPublishStateProperties();
    }

    partial void OnSelectedBehaviorChanged(InteractionBehaviorViewModel? oldValue, InteractionBehaviorViewModel? newValue)
    {
        RefreshSelectedBehaviorEditor();
        OnPropertyChanged(nameof(CanDeleteSelectedBehavior));
        OnPropertyChanged(nameof(SelectedBehaviorSummary));
    }

    partial void OnSelectedMotionPresetChanged(MotionPresetViewModel? oldValue, MotionPresetViewModel? newValue)
    {
        _suppressInspector = true;
        MotionPresetNameEditor = newValue?.Name ?? string.Empty;
        MotionPresetDescriptionEditor = newValue?.Model.Description ?? string.Empty;
        _suppressInspector = false;
        RefreshMotionPresetStateProperties();
    }

    partial void OnMotionPresetNameEditorChanged(string value)
    {
        if (_suppressInspector || SelectedMotionPreset is null)
        {
            return;
        }

        var normalized = MotionPresetService.EnsureUniquePresetName(_document, value, SelectedMotionPreset.Id);
        if (!string.Equals(normalized, value, StringComparison.Ordinal))
        {
            _suppressInspector = true;
            MotionPresetNameEditor = normalized;
            _suppressInspector = false;
            return;
        }

        SelectedMotionPreset.Model.Name = normalized;
        ReloadMotionPresets(SelectedMotionPreset.Id);
        RecordHistoryIfNeeded();
        StatusMessage = "Motion preset renamed";
    }

    partial void OnMotionPresetDescriptionEditorChanged(string value)
    {
        if (_suppressInspector || SelectedMotionPreset is null)
        {
            return;
        }

        SelectedMotionPreset.Model.Description = value?.Trim() ?? string.Empty;
        ReloadMotionPresets(SelectedMotionPreset.Id);
        RecordHistoryIfNeeded();
        StatusMessage = "Motion preset description updated";
    }

    partial void OnIsPrototypeModeChanged(bool value)
    {
        RefreshPrototypeStateProperties();
        RefreshSelectionStateProperties();
        RefreshLibraryStateProperties();
        RefreshMediaStateProperties();
        RefreshSceneStateProperties();
        RefreshSymbolEditingProperties();
        OnPropertyChanged(nameof(CanInsertStageItems));
        OnPropertyChanged(nameof(CanUseSelectionActions));
        OnPropertyChanged(nameof(CanEditTextMenu));
        OnPropertyChanged(nameof(CanManageFrameLabels));
        OnPropertyChanged(nameof(ControlMenuSummary));
    }

    partial void OnIsPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanStopPlayback));
        OnPropertyChanged(nameof(CanMutateDocument));
        RefreshSelectionStateProperties();
        RefreshSceneStateProperties();
        RefreshLibraryStateProperties();
        RefreshMediaStateProperties();
        RefreshPublishStateProperties();
        RefreshBehaviorEditorStateProperties();
        RefreshVisualStateStateProperties();
        RefreshActionScriptStateProperties();
        RefreshSymbolEditingProperties();
        OnPropertyChanged(nameof(ControlMenuSummary));
    }

    partial void OnPrototypeStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(PrototypeSummary));
    }

    partial void OnPublishProfileNameEditorChanged(string value)
    {
        UpdateSelectedPublishProfile(
            profile => profile.Name = string.IsNullOrWhiteSpace(value) ? "Publish Profile" : value.Trim(),
            "Publish profile renamed");
    }

    partial void OnPublishOutputKindEditorChanged(PublishOutputKind value)
    {
        UpdateSelectedPublishProfile(
            profile => profile.OutputKind = value,
            "Publish format updated");
        RefreshPublishStateProperties();
    }

    partial void OnPublishWidthEditorChanged(int value)
    {
        UpdateSelectedPublishProfile(
            profile => profile.Width = Math.Max(1, value),
            "Publish width updated");
    }

    partial void OnPublishHeightEditorChanged(int value)
    {
        UpdateSelectedPublishProfile(
            profile => profile.Height = Math.Max(1, value),
            "Publish height updated");
    }

    partial void OnPublishFrameRateEditorChanged(double value)
    {
        UpdateSelectedPublishProfile(
            profile => profile.FrameRate = Math.Max(1d, value),
            "Publish frame rate updated");
    }

    partial void OnPublishPlayAllScenesEditorChanged(bool value)
    {
        UpdateSelectedPublishProfile(
            profile => profile.PlayAllScenes = value,
            value ? "Publish will include all scenes" : "Publish limited to active scene");
    }

    partial void OnPublishTransparentBackgroundEditorChanged(bool value)
    {
        UpdateSelectedPublishProfile(
            profile => profile.TransparentBackground = value,
            value ? "Transparent background enabled" : "Transparent background disabled");
    }

    partial void OnPublishIncludeSceneGraphEditorChanged(bool value)
    {
        UpdateSelectedPublishProfile(
            profile => profile.IncludeSceneGraph = value,
            value ? "Scene graph export enabled" : "Scene graph export disabled");
    }

    partial void OnPublishIncludeValidationReportEditorChanged(bool value)
    {
        UpdateSelectedPublishProfile(
            profile => profile.IncludeValidationReport = value,
            value ? "Validation report enabled" : "Validation report disabled");
    }

    partial void OnPublishSpriteSheetColumnsEditorChanged(int value)
    {
        UpdateSelectedPublishProfile(
            profile => profile.SpriteSheetColumns = Math.Max(1, value),
            "Sprite sheet columns updated");
    }

    partial void OnPublishQualityEditorChanged(int value)
    {
        UpdateSelectedPublishProfile(
            profile => profile.Quality = Math.Clamp(value, 1, 100),
            "Publish quality updated");
    }

    partial void OnEditingButtonStateChanged(ButtonVisualState oldValue, ButtonVisualState newValue)
    {
        if (_suppressButtonStateEditor || !IsEditingSymbol || EditingLibraryItem is null || EditingLibraryItem.SymbolKind != SymbolKind.Button)
        {
            return;
        }

        TimelineEditingService.SetEditableSymbolLayers(EditingLibraryItem, _document.Layers, Duration, SceneFrameRate, oldValue);
        LoadSymbolEditingWorkspace(EditingLibraryItem, newValue, null, CurrentTime);
        RecordHistoryIfNeeded();
        StatusMessage = $"Editing {EditingLibraryItem.Name} {newValue} state";
    }

    partial void OnSelectedPropertyChanged(AnimatedProperty value)
    {
        foreach (var track in Tracks)
        {
            track.IsSelected = track.Property == value;
        }

        UpdateTimelineRowSelection();
        OnPropertyChanged(nameof(SelectedKeyframeSummary));
    }

    partial void OnSelectedKeyframeIdChanged(Guid? value)
    {
        foreach (var track in Tracks)
        {
            track.UpdateKeyframeSelection(value);
        }

        UpdateTimelineRowSelection();
        RefreshSelectedKeyframeEditor();
        OnPropertyChanged(nameof(HasSelectedKeyframe));
        OnPropertyChanged(nameof(HasSelectedCustomEasing));
        OnPropertyChanged(nameof(CanEditCustomEasingCurve));
        OnPropertyChanged(nameof(SelectedKeyframeSummary));
        OnPropertyChanged(nameof(CanDeleteSelectedKeyframe));
    }

    partial void OnCurrentTimeChanged(double oldValue, double newValue)
    {
        var previousFrame = FrameTimelineService.TimeToFrame(oldValue, SceneFrameRate, TotalFrames);
        var currentFrame = FrameTimelineService.TimeToFrame(newValue, SceneFrameRate, TotalFrames);
        var frameChanged = previousFrame != currentFrame;

        OnPropertyChanged(nameof(CurrentTimeLabel));
        OnPropertyChanged(nameof(CurrentFrame));
        OnPropertyChanged(nameof(CurrentFrameLabel));
        OnPropertyChanged(nameof(ShapeTweenSummary));
        OnPropertyChanged(nameof(CanDeleteCurrentShapeKeyframe));

        if (_isInteractiveChange)
        {
            switch (_interactiveChangeKind)
            {
                case InteractiveChangeKind.KeyframeDrag:
                    return;

                case InteractiveChangeKind.FrameTimelineDrag:
                    return;

                default:
                    return;
            }
        }

        if (IsPlaying)
        {
            if (frameChanged)
            {
                if (ShouldRefreshTimelinePlaybackValues() &&
                    ShouldRefreshPlaybackInterval(ref _lastPlaybackTimelinePreviewRefreshTick, PlaybackTimelinePreviewRefreshIntervalMilliseconds))
                {
                    UpdateTrackPreviewValues();
                    UpdateTimelineRowPreviewValues();
                }

                if (ShouldRefreshAudioMeterDuringPlayback() &&
                    ShouldRefreshPlaybackInterval(ref _lastPlaybackAudioRefreshTick, PlaybackAudioMeterRefreshIntervalMilliseconds))
                {
                    RefreshAudioMeter();
                }
            }

            if (ShouldRefreshInspectorDuringPlayback() &&
                ShouldRefreshPlaybackInterval(ref _lastPlaybackInspectorRefreshTick, PlaybackInspectorRefreshIntervalMilliseconds))
            {
                RefreshInspectorPlaybackValues();
            }

            if (ShouldRefreshProgramMonitorDuringPlayback())
            {
                RefreshProgramMonitorPreview(force: false);
            }
            return;
        }

        RefreshLayerPreviewsAtCurrentTime();
        UpdateTrackPreviewValues();
        UpdateTimelineRowPreviewValues();
        RefreshAudioMeter();
        RefreshInspector();
        RefreshFrameLabelEditor();
        RefreshProgramMonitorPreview(force: true);
    }

    partial void OnSelectedSceneMarkerChanged(SceneMarkerViewModel? oldValue, SceneMarkerViewModel? newValue)
    {
        MarkerNameEditor = newValue?.Name ?? string.Empty;
        MarkerNotesEditor = newValue?.Notes ?? string.Empty;
        MarkerColorEditor = newValue?.Model.Color ?? "#57C9FF";
        SelectedMarkerKind = newValue?.Kind ?? SceneMarkerKind.Cue;
        OnPropertyChanged(nameof(SelectedSceneMarkerSummary));
        OnPropertyChanged(nameof(CanRemoveSelectedSceneMarker));
    }

    partial void OnSelectedVisualStateGroupChanged(VisualStateGroupViewModel? oldValue, VisualStateGroupViewModel? newValue)
    {
        ReloadVisualStates(newValue?.Id);
        VisualStateGroupNameEditor = newValue?.Name ?? "CommonStates";
        OnPropertyChanged(nameof(CanRemoveSelectedVisualStateGroup));
    }

    partial void OnSelectedVisualStateChanged(VisualStateViewModel? oldValue, VisualStateViewModel? newValue)
    {
        VisualStateNameEditor = newValue?.Name ?? "State";
        OnPropertyChanged(nameof(SelectedVisualStateSummary));
        OnPropertyChanged(nameof(CanApplySelectedVisualState));
        OnPropertyChanged(nameof(CanRemoveSelectedVisualState));
    }

    partial void OnSelectedActionScriptSnippetChanged(ActionScriptSnippetViewModel? oldValue, ActionScriptSnippetViewModel? newValue)
    {
        OnPropertyChanged(nameof(SelectedActionScriptSnippetSummary));
        OnPropertyChanged(nameof(CanInsertActionScriptSnippetIntoFrame));
        OnPropertyChanged(nameof(CanInsertActionScriptSnippetIntoBehavior));
    }

    partial void OnProgramMonitorBitmapChanged(Bitmap? oldValue, Bitmap? newValue)
    {
        oldValue?.Dispose();
        OnPropertyChanged(nameof(HasProgramMonitorBitmap));
    }

    partial void OnSourceMonitorBitmapChanged(Bitmap? oldValue, Bitmap? newValue)
    {
        oldValue?.Dispose();
        OnPropertyChanged(nameof(HasSourceMonitorBitmap));
    }

    partial void OnSourceMonitorTimeChanged(double value)
    {
        if (SourceMonitorDuration <= 0d)
        {
            return;
        }

        var normalized = TimelineMath.Clamp(value, 0d, SourceMonitorDuration);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            SourceMonitorTime = normalized;
            return;
        }

        RefreshSourceMonitorPreview();
    }

    partial void OnSourceMonitorDurationChanged(double value)
    {
        var normalized = Math.Max(0d, value);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            SourceMonitorDuration = normalized;
            return;
        }

        if (SourceMonitorInTime > normalized)
        {
            SourceMonitorInTime = normalized;
        }

        if (SourceMonitorOutTime > normalized)
        {
            SourceMonitorOutTime = normalized;
        }

        if (SourceMonitorOutTime <= 0d && normalized > 0d)
        {
            SourceMonitorOutTime = normalized;
        }

        OnPropertyChanged(nameof(SourceMonitorRangeSummary));
    }

    partial void OnSourceMonitorInTimeChanged(double value)
    {
        var normalized = TimelineMath.Clamp(value, 0d, Math.Max(0d, SourceMonitorDuration));
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            SourceMonitorInTime = normalized;
            return;
        }

        if (SourceMonitorOutTime < normalized)
        {
            SourceMonitorOutTime = normalized;
        }

        OnPropertyChanged(nameof(SourceMonitorRangeSummary));
    }

    partial void OnSourceMonitorOutTimeChanged(double value)
    {
        var normalized = TimelineMath.Clamp(value, SourceMonitorInTime, Math.Max(SourceMonitorInTime, SourceMonitorDuration));
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            SourceMonitorOutTime = normalized;
            return;
        }

        OnPropertyChanged(nameof(SourceMonitorRangeSummary));
    }

    partial void OnUseWorkAreaPlaybackChanged(bool value)
    {
        RefreshSceneStateProperties();
        RefreshMonitorPreviews();
    }

    partial void OnPlayAllScenesChanged(bool value)
    {
        RefreshProgramMonitorPreview(force: true);
    }

    partial void OnSceneNameEditorChanged(string value)
    {
        if (_suppressSceneEditor || SelectedScene is null || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        SelectedScene.Model.Name = value.Trim();
        SelectedScene.RefreshMetadata();
        _document.ActiveSceneId = SelectedScene.Id;
        RecordHistoryIfNeeded();
        RefreshSceneStateProperties();
        StatusMessage = "Scene renamed";
    }

    partial void OnSceneTransitionKindEditorChanged(SceneTransitionKind value)
    {
        if (_suppressSceneEditor || SelectedScene is null)
        {
            return;
        }

        SelectedScene.Model.OutgoingTransition.Kind = value;
        SelectedScene.RefreshMetadata();
        RecordHistoryIfNeeded();
        RefreshSceneStateProperties();
        RefreshMonitorPreviews();
        StatusMessage = value == SceneTransitionKind.None ? "Scene transition cleared" : $"{value} transition applied";
    }

    partial void OnSceneTransitionDurationEditorChanged(double value)
    {
        if (_suppressSceneEditor || SelectedScene is null)
        {
            return;
        }

        var normalized = Math.Max(0d, value);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            SceneTransitionDurationEditor = normalized;
            return;
        }

        SelectedScene.Model.OutgoingTransition.Duration = normalized;
        SelectedScene.RefreshMetadata();
        RecordHistoryIfNeeded();
        RefreshSceneStateProperties();
        RefreshMonitorPreviews();
        StatusMessage = $"Transition duration set to {normalized:0.00}s";
    }

    partial void OnSceneTransitionAccentColorEditorChanged(string value)
    {
        if (_suppressSceneEditor || SelectedScene is null || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        SelectedScene.Model.OutgoingTransition.AccentColor = value.Trim();
        RecordHistoryIfNeeded();
        RefreshSceneStateProperties();
        RefreshMonitorPreviews();
        StatusMessage = "Transition accent updated";
    }

    partial void OnSceneFrameRateChanged(double value)
    {
        if (_suppressFrameRateEditor || (!IsEditingSymbol && SelectedScene is null))
        {
            return;
        }

        var normalized = SceneEditingService.NormalizeFrameRate(value);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressFrameRateEditor = true;
            SceneFrameRate = normalized;
            _suppressFrameRateEditor = false;
            return;
        }

        if (IsEditingSymbol && EditingLibraryItem is not null)
        {
            EditingLibraryItem.FrameRate = normalized;
            RefreshSymbolEditingProperties();
        }
        else if (SelectedScene is not null)
        {
            SelectedScene.Model.FrameRate = normalized;
            SelectedScene.RefreshMetadata();
            RefreshSceneStateProperties();
        }

        RefreshFrameAwareState();
        if (SelectedScene is not null)
        {
            SceneTimelineService.EnsureTimelineMetadata(SelectedScene.Model, TotalFrames);
            ReloadSceneMarkers();
            RefreshSceneTimelineEditors();
        }
        RecordHistoryIfNeeded();
        StatusMessage = $"Scene frame rate set to {normalized:0.#} fps";
    }

    partial void OnTimelineZoomChanged(double value)
    {
        OnPropertyChanged(nameof(TimelineSurfaceWidth));
    }

    partial void OnFrameTimelineZoomChanged(double value)
    {
        OnPropertyChanged(nameof(FrameTimelineSurfaceWidth));
    }

    partial void OnCanvasZoomChanged(double value)
    {
        var normalized = Math.Clamp(value, MinCanvasZoomFactor, MaxCanvasZoomFactor);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            CanvasZoom = normalized;
            return;
        }

        OnPropertyChanged(nameof(CanvasZoomLabel));
    }

    partial void OnCanvasViewRotationChanged(double value)
    {
        var normalized = value;
        while (normalized <= -180d)
        {
            normalized += 360d;
        }

        while (normalized > 180d)
        {
            normalized -= 360d;
        }

        if (Math.Abs(normalized - value) > 0.0001d)
        {
            CanvasViewRotation = normalized;
            return;
        }

        OnPropertyChanged(nameof(CanvasViewRotationLabel));
        OnPropertyChanged(nameof(CanvasViewportHint));
        OnPropertyChanged(nameof(ShowRotateViewToolOptions));
        OnPropertyChanged(nameof(ToolOptionsSummary));
    }

    partial void OnZoomToolModeEditorChanged(ZoomToolMode value)
    {
        OnPropertyChanged(nameof(CanvasViewportHint));
        OnPropertyChanged(nameof(ToolOptionsSummary));
    }

    partial void OnLassoToolModeEditorChanged(LassoToolMode value)
    {
        OnPropertyChanged(nameof(ToolOptionsSummary));
    }

    partial void OnLassoContactSensitiveSelectionChanged(bool value)
    {
        OnPropertyChanged(nameof(ToolOptionsSummary));
    }

    partial void OnPencilToolModeEditorChanged(PencilToolMode value)
    {
        OnPropertyChanged(nameof(ToolOptionsSummary));
    }

    partial void OnEraserToolModeEditorChanged(EraserToolMode value)
    {
        OnPropertyChanged(nameof(ToolOptionsSummary));
    }

    partial void OnVectorStrokeProfilePresetEditorChanged(VectorStrokeProfilePreset value)
    {
        OnPropertyChanged(nameof(ToolOptionsSummary));
    }

    partial void OnVectorStrokeProfileStrengthEditorChanged(double value)
    {
        var normalized = TimelineMath.Clamp(value, 0.1d, 1d);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            VectorStrokeProfileStrengthEditor = normalized;
            return;
        }

        OnPropertyChanged(nameof(ToolOptionsSummary));
    }

    partial void OnPaintBucketGapSizeEditorChanged(PaintBucketGapSize value)
    {
        OnPropertyChanged(nameof(ToolOptionsSummary));
    }

    partial void OnPaintBucketLockFillEditorChanged(bool value)
    {
        OnPropertyChanged(nameof(ToolOptionsSummary));
    }

    partial void OnEraserSizeEditorChanged(double value)
    {
        var normalized = Math.Clamp(value, 6d, 240d);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            EraserSizeEditor = normalized;
            return;
        }

        OnPropertyChanged(nameof(ToolOptionsSummary));
    }

    partial void OnSyncBrushAndEraserSizeChanged(bool value)
    {
        if (value)
        {
            EraserSizeEditor = Math.Max(12d, StrokeThicknessEditor * 4d);
        }

        OnPropertyChanged(nameof(ToolOptionsSummary));
    }

    partial void OnDurationChanged(double value)
    {
        var normalized = SceneEditingService.NormalizeDuration(value);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressSceneSurfaceEditor = true;
            Duration = normalized;
            _suppressSceneSurfaceEditor = false;
            return;
        }

        OnPropertyChanged(nameof(DurationLabel));
        OnPropertyChanged(nameof(TimelineSurfaceWidth));
        _document.Duration = normalized;
        if (!IsEditingSymbol && SelectedScene is not null)
        {
            SelectedScene.Model.Duration = normalized;
            SelectedScene.RefreshMetadata();
        }

        if (CurrentTime > normalized)
        {
            CurrentTime = normalized;
        }

        RefreshSceneStateProperties();
        RefreshFrameAwareState();
        if (SelectedScene is not null)
        {
            SceneTimelineService.EnsureTimelineMetadata(SelectedScene.Model, TotalFrames);
            ReloadSceneMarkers();
            RefreshSceneTimelineEditors();
        }
        if (_suppressSceneSurfaceEditor || IsEditingSymbol)
        {
            return;
        }

        RecordHistoryIfNeeded();
        if (!_isInteractiveChange)
        {
            StatusMessage = $"Scene duration set to {normalized:0.##} seconds";
        }
    }

    partial void OnCanvasWidthChanged(double value)
    {
        var normalized = SceneEditingService.NormalizeCanvasExtent(value);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressSceneSurfaceEditor = true;
            CanvasWidth = normalized;
            _suppressSceneSurfaceEditor = false;
            return;
        }

        _document.CanvasWidth = normalized;
        if (!IsEditingSymbol && SelectedScene is not null)
        {
            SelectedScene.Model.CanvasWidth = normalized;
            SelectedScene.RefreshMetadata();
        }

        OnPropertyChanged(nameof(CanvasSizeLabel));
        RefreshSceneStateProperties();
        if (_suppressSceneSurfaceEditor || IsEditingSymbol)
        {
            return;
        }

        RecordHistoryIfNeeded();
        if (!_isInteractiveChange)
        {
            StatusMessage = $"Canvas width set to {normalized:0}px";
        }
    }

    partial void OnCanvasHeightChanged(double value)
    {
        var normalized = SceneEditingService.NormalizeCanvasExtent(value);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressSceneSurfaceEditor = true;
            CanvasHeight = normalized;
            _suppressSceneSurfaceEditor = false;
            return;
        }

        _document.CanvasHeight = normalized;
        if (!IsEditingSymbol && SelectedScene is not null)
        {
            SelectedScene.Model.CanvasHeight = normalized;
            SelectedScene.RefreshMetadata();
        }

        OnPropertyChanged(nameof(CanvasSizeLabel));
        RefreshSceneStateProperties();
        if (_suppressSceneSurfaceEditor || IsEditingSymbol)
        {
            return;
        }

        RecordHistoryIfNeeded();
        if (!_isInteractiveChange)
        {
            StatusMessage = $"Canvas height set to {normalized:0}px";
        }
    }

    partial void OnBackgroundFromChanged(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? SceneEditingService.DefaultStageBackgroundColor
            : value.Trim();
        if (!string.Equals(normalized, value, StringComparison.Ordinal))
        {
            _suppressSceneSurfaceEditor = true;
            BackgroundFrom = normalized;
            _suppressSceneSurfaceEditor = false;
            return;
        }

        _document.BackgroundFrom = normalized;
        if (!IsEditingSymbol && SelectedScene is not null)
        {
            SelectedScene.Model.BackgroundFrom = normalized;
            SelectedScene.RefreshMetadata();
        }

        OnPropertyChanged(nameof(CanvasBackgroundSummary));
        RefreshSceneStateProperties();
        if (_suppressSceneSurfaceEditor || IsEditingSymbol)
        {
            return;
        }

        RecordHistoryIfNeeded();
        if (!_isInteractiveChange)
        {
            StatusMessage = TransparentStageBackground
                ? "Stage color updated (stage remains transparent)"
                : "Stage color updated";
        }
    }

    partial void OnBackgroundToChanged(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "#182748" : value.Trim();
        if (!string.Equals(normalized, value, StringComparison.Ordinal))
        {
            _suppressSceneSurfaceEditor = true;
            BackgroundTo = normalized;
            _suppressSceneSurfaceEditor = false;
            return;
        }

        _document.BackgroundTo = normalized;
        if (!IsEditingSymbol && SelectedScene is not null)
        {
            SelectedScene.Model.BackgroundTo = normalized;
            SelectedScene.RefreshMetadata();
        }

        OnPropertyChanged(nameof(CanvasBackgroundSummary));
        RefreshSceneStateProperties();
        if (_suppressSceneSurfaceEditor || IsEditingSymbol)
        {
            return;
        }

        RecordHistoryIfNeeded();
        if (!_isInteractiveChange)
        {
            StatusMessage = "Legacy stage accent color updated";
        }
    }

    partial void OnTransparentStageBackgroundChanged(bool value)
    {
        _document.TransparentStageBackground = value;
        if (!IsEditingSymbol && SelectedScene is not null)
        {
            SelectedScene.Model.TransparentStageBackground = value;
            SelectedScene.RefreshMetadata();
        }

        OnPropertyChanged(nameof(CanvasBackgroundSummary));
        RefreshSceneStateProperties();
        if (_suppressSceneSurfaceEditor || IsEditingSymbol)
        {
            return;
        }

        RecordHistoryIfNeeded();
        if (!_isInteractiveChange)
        {
            StatusMessage = value ? "Stage set to no color" : "Stage solid background restored";
        }
    }

    partial void OnPasteboardColorChanged(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "#B9BEC7" : value.Trim();
        if (!string.Equals(normalized, value, StringComparison.Ordinal))
        {
            PasteboardColor = normalized;
            return;
        }

        OnPropertyChanged(nameof(CanvasBackgroundSummary));
        if (!_isInteractiveChange)
        {
            StatusMessage = "Pasteboard color updated";
        }
    }

    partial void OnMatchPasteboardToStageColorChanged(bool value)
    {
        OnPropertyChanged(nameof(CanvasBackgroundSummary));
        if (!_isInteractiveChange)
        {
            StatusMessage = value
                ? "Pasteboard matched to stage color"
                : "Pasteboard restored to custom color";
        }
    }

    partial void OnSelectionStartFrameChanged(int value)
    {
        if (_suppressFrameSelectionRefresh)
        {
            return;
        }

        UpdateFrameRowSelection();
        RefreshFrameSelectionStateProperties();
    }

    partial void OnSelectionEndFrameChanged(int value)
    {
        if (_suppressFrameSelectionRefresh)
        {
            return;
        }

        UpdateFrameRowSelection();
        RefreshFrameSelectionStateProperties();
    }

    partial void OnLayerNameEditorChanged(string value)
    {
        if (_suppressInspector || !CanEditSelection || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var selectedLayer = SelectedLayer!;
        selectedLayer.Model.Name = value.Trim();
        selectedLayer.RefreshMetadata();
        RecordHistoryIfNeeded();
        StatusMessage = "Layer renamed";
        OnPropertyChanged(nameof(SelectionHeadline));
    }

    partial void OnFillHexEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!CanEditSelection)
        {
            return;
        }

        var selectedLayer = SelectedLayer!;
        selectedLayer.Model.Style.Fill = value.Trim();
        selectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(selectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Fill updated";
    }

    partial void OnStrokeHexEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!CanEditSelection)
        {
            return;
        }

        var selectedLayer = SelectedLayer!;
        selectedLayer.Model.Style.Stroke = value.Trim();
        selectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(selectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Stroke updated";
    }

    partial void OnFillEnabledEditorChanged(bool value)
    {
        if (_suppressInspector || !CanEditSelection)
        {
            return;
        }

        SelectedLayer!.Model.Style.HasFill = value;
        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = value ? "Fill enabled" : "Fill disabled";
    }

    partial void OnStrokeEnabledEditorChanged(bool value)
    {
        if (_suppressInspector || !CanEditSelection)
        {
            return;
        }

        SelectedLayer!.Model.Style.HasStroke = value;
        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = value ? "Stroke enabled" : "Stroke disabled";
    }

    partial void OnStrokeThicknessEditorChanged(double value)
    {
        if (_suppressInspector)
        {
            return;
        }

        var normalized = Math.Max(1, value);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            StrokeThicknessEditor = normalized;
            _suppressInspector = false;
            return;
        }

        if (SyncBrushAndEraserSize)
        {
            var synchronizedSize = Math.Max(12d, normalized * 4d);
            if (Math.Abs(EraserSizeEditor - synchronizedSize) > 0.0001d)
            {
                EraserSizeEditor = synchronizedSize;
            }
        }

        OnPropertyChanged(nameof(ToolOptionsSummary));

        if (!CanEditSelection)
        {
            return;
        }

        SelectedLayer!.Model.Style.StrokeThickness = normalized;
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Stroke weight updated";
    }

    partial void OnUseGradientEditorChanged(bool value)
    {
        if (_suppressInspector || !CanEditSelection)
        {
            return;
        }

        SelectedLayer!.Model.Style.UseGradient = value;
        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        OnPropertyChanged(nameof(CanEditGradientTransform));
        RecordHistoryIfNeeded();
        StatusMessage = value ? "Gradient enabled" : "Gradient disabled";
    }

    partial void OnGradientKindEditorChanged(LayerGradientKind value)
    {
        if (_suppressInspector || !CanEditSelection)
        {
            return;
        }

        SelectedLayer!.Model.Style.GradientKind = value;
        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = $"{value} gradient selected";
    }

    partial void OnGradientAngleEditorChanged(double value)
    {
        if (_suppressInspector || !CanEditSelection)
        {
            return;
        }

        SelectedLayer!.Model.Style.GradientAngle = value;
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Gradient angle updated";
    }

    partial void OnGradientCenterXEditorChanged(double value)
    {
        if (_suppressInspector || !CanEditSelection)
        {
            return;
        }

        var normalized = Math.Clamp(value, 0d, 1d);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            GradientCenterXEditor = normalized;
            _suppressInspector = false;
            return;
        }

        SelectedLayer!.Model.Style.GradientCenterX = normalized;
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Gradient center X updated";
    }

    partial void OnGradientCenterYEditorChanged(double value)
    {
        if (_suppressInspector || !CanEditSelection)
        {
            return;
        }

        var normalized = Math.Clamp(value, 0d, 1d);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            GradientCenterYEditor = normalized;
            _suppressInspector = false;
            return;
        }

        SelectedLayer!.Model.Style.GradientCenterY = normalized;
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Gradient center Y updated";
    }

    partial void OnGradientScaleXEditorChanged(double value)
    {
        if (_suppressInspector || !CanEditSelection)
        {
            return;
        }

        var normalized = Math.Clamp(value, 0.1d, 2d);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            GradientScaleXEditor = normalized;
            _suppressInspector = false;
            return;
        }

        SelectedLayer!.Model.Style.GradientScaleX = normalized;
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Gradient scale X updated";
    }

    partial void OnGradientScaleYEditorChanged(double value)
    {
        if (_suppressInspector || !CanEditSelection)
        {
            return;
        }

        var normalized = Math.Clamp(value, 0.1d, 2d);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            GradientScaleYEditor = normalized;
            _suppressInspector = false;
            return;
        }

        SelectedLayer!.Model.Style.GradientScaleY = normalized;
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Gradient scale Y updated";
    }

    partial void OnGradientFromEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!CanEditSelection)
        {
            return;
        }

        SelectedLayer!.Model.Style.GradientFrom = value.Trim();
        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Gradient start updated";
    }

    partial void OnGradientToEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!CanEditSelection)
        {
            return;
        }

        SelectedLayer!.Model.Style.GradientTo = value.Trim();
        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Gradient end updated";
    }

    partial void OnStrokeCapStyleEditorChanged(LayerStrokeCapStyle value)
    {
        if (_suppressInspector || !CanEditSelection)
        {
            return;
        }

        SelectedLayer!.Model.Style.StrokeCapStyle = value;
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Stroke cap updated";
    }

    partial void OnStrokeJoinStyleEditorChanged(LayerStrokeJoinStyle value)
    {
        RefreshShapeStyleStateProperties();
        if (_suppressInspector || !CanEditSelection)
        {
            return;
        }

        SelectedLayer!.Model.Style.StrokeJoinStyle = value;
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Stroke join updated";
    }

    partial void OnStrokeMiterLimitEditorChanged(double value)
    {
        if (_suppressInspector)
        {
            return;
        }

        var normalized = Math.Max(1d, value);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            StrokeMiterLimitEditor = normalized;
            _suppressInspector = false;
            return;
        }

        if (!CanEditSelection)
        {
            return;
        }

        SelectedLayer!.Model.Style.StrokeMiterLimit = normalized;
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Stroke miter updated";
    }

    partial void OnCornerRadiusTopLeftEditorChanged(double value) => UpdateCornerRadiusEditor(
        value,
        normalized => CornerRadiusTopLeftEditor = normalized,
        (style, normalized) => style.CornerRadiusTopLeft = normalized,
        "Top-left radius updated");

    partial void OnCornerRadiusTopRightEditorChanged(double value) => UpdateCornerRadiusEditor(
        value,
        normalized => CornerRadiusTopRightEditor = normalized,
        (style, normalized) => style.CornerRadiusTopRight = normalized,
        "Top-right radius updated");

    partial void OnCornerRadiusBottomRightEditorChanged(double value) => UpdateCornerRadiusEditor(
        value,
        normalized => CornerRadiusBottomRightEditor = normalized,
        (style, normalized) => style.CornerRadiusBottomRight = normalized,
        "Bottom-right radius updated");

    partial void OnCornerRadiusBottomLeftEditorChanged(double value) => UpdateCornerRadiusEditor(
        value,
        normalized => CornerRadiusBottomLeftEditor = normalized,
        (style, normalized) => style.CornerRadiusBottomLeft = normalized,
        "Bottom-left radius updated");

    partial void OnEllipseStartAngleEditorChanged(double value)
    {
        var normalized = NormalizeShapeAngle(value);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            EllipseStartAngleEditor = normalized;
            _suppressInspector = false;
            return;
        }

        if (SelectedLayer is null)
        {
            RefreshShapeStyleStateProperties();
            return;
        }

        UpdateSelectedShapeAuthoring(
            style => style.EllipseStartAngle = normalized,
            "Ellipse start angle updated");
    }

    partial void OnEllipseSweepAngleEditorChanged(double value)
    {
        var normalized = Math.Clamp(value, -360d, 360d);
        if (Math.Abs(normalized) < 0.1d)
        {
            normalized = 360d;
        }

        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            EllipseSweepAngleEditor = normalized;
            _suppressInspector = false;
            return;
        }

        if (SelectedLayer is null)
        {
            RefreshShapeStyleStateProperties();
            return;
        }

        UpdateSelectedShapeAuthoring(
            style => style.EllipseSweepAngle = normalized,
            "Ellipse sweep updated");
    }

    partial void OnTextEditorChanged(string value)
    {
        if (_suppressInspector || !CanEditSelection)
        {
            return;
        }

        var selectedLayer = SelectedLayer!;
        selectedLayer.Model.Style.Text = value;
        selectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(selectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Text updated";
    }

    partial void OnTextFontFamilyEditorChanged(string value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.FontFamily = value?.Trim() ?? string.Empty,
            "Font family updated");
    }

    partial void OnTextBoldEditorChanged(bool value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.IsBold = value,
            value ? "Text bold enabled" : "Text bold disabled");
    }

    partial void OnTextItalicEditorChanged(bool value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.IsItalic = value,
            value ? "Text italic enabled" : "Text italic disabled");
    }

    partial void OnTextAlignmentEditorChanged(LayerTextAlignment value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.Alignment = value,
            "Text alignment updated");
    }

    partial void OnTextLetterSpacingEditorChanged(double value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.LetterSpacing = value,
            "Letter spacing updated");
    }

    partial void OnTextLineHeightEditorChanged(double value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.LineHeight = Math.Max(0d, value),
            "Line height updated");
    }

    partial void OnTextRenderAsHtmlEditorChanged(bool value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.RenderAsHtml = value,
            value ? "HTML text enabled" : "HTML text disabled");
    }

    partial void OnTextFieldKindEditorChanged(FlashTextFieldKind value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.FieldKind = value,
            "Flash text field type updated");
    }

    partial void OnTextLineModeEditorChanged(FlashTextLineMode value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.LineMode = value,
            "Flash text line mode updated");
    }

    partial void OnTextSelectableEditorChanged(bool value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.IsSelectable = value,
            value ? "Flash text field selectable" : "Flash text field selection disabled");
    }

    partial void OnTextShowBorderEditorChanged(bool value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.ShowBorder = value,
            value ? "Flash text border shown" : "Flash text border hidden");
    }

    partial void OnTextVariableNameEditorChanged(string value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.VariableName = value?.Trim() ?? string.Empty,
            "Flash text variable updated");
    }

    partial void OnTextMaxCharactersEditorChanged(int value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.MaxCharacters = Math.Max(0, value),
            "Flash text max characters updated");
    }

    partial void OnTextPasswordEditorChanged(bool value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.IsPassword = value,
            value ? "Flash password field enabled" : "Flash password field disabled");
    }

    partial void OnTextUseDeviceFontsEditorChanged(bool value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.UseDeviceFonts = value,
            value ? "Device fonts enabled" : "Device fonts disabled");
    }

    partial void OnTextAntiAliasModeEditorChanged(FlashTextAntiAliasMode value)
    {
        UpdateSelectedTextMetadata(
            settings => settings.AntiAliasMode = value,
            "Flash text anti-alias mode updated");
    }

    partial void OnPathClosedEditorChanged(bool value)
    {
        if (_suppressInspector || !CanEditSelection || !SelectedLayerIsPath)
        {
            return;
        }

        if (!TimelineEditingService.ApplyPathClosed(SelectedLayer!.Model, value, CurrentTime, ShouldCreateKeyframe(), Duration))
        {
            return;
        }

        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = value ? "Path closed" : "Path reopened";
        OnPropertyChanged(nameof(ShapeTweenSummary));
        OnPropertyChanged(nameof(CanDeleteCurrentShapeKeyframe));
    }

    partial void OnSelectedPathPointIndexEditorChanged(int value)
    {
        if (_suppressInspector)
        {
            return;
        }

        RefreshSelectedPathPointEditors();
        OnPropertyChanged(nameof(HasSelectedPathPoint));
        OnPropertyChanged(nameof(CanInsertPathPointAfterSelected));
        OnPropertyChanged(nameof(CanDeleteSelectedPathPoint));
        OnPropertyChanged(nameof(CanReverseSelectedPath));
        OnPropertyChanged(nameof(CanApplySelectedStrokeProfile));
        OnPropertyChanged(nameof(CanOptimizeSelectedPath));
        OnPropertyChanged(nameof(SelectedPathPointSummary));
    }

    partial void OnSelectedPathPointHandleModeEditorChanged(VectorHandleMode value)
    {
        if (_suppressInspector || !HasSelectedPathPoint || SelectedLayer is null)
        {
            return;
        }

        if (!TimelineEditingService.SetPathPointHandleMode(
                SelectedLayer.Model,
                SelectedPathPointIndexEditor,
                PathClosedEditor,
                value,
                CurrentTime,
                ShouldCreateKeyframe(),
                Duration))
        {
            return;
        }

        ReloadPreviewForLayer(SelectedLayer);
        RefreshSelectedPathPointEditors();
        RecordHistoryIfNeeded();
        StatusMessage = value == VectorHandleMode.Smooth ? "Point converted to smooth" : "Point converted to corner";
        OnPropertyChanged(nameof(ShapeTweenSummary));
        OnPropertyChanged(nameof(CanDeleteCurrentShapeKeyframe));
    }

    partial void OnSelectedPathPointWidthScaleEditorChanged(double value)
    {
        if (_suppressInspector || !HasSelectedPathPoint || SelectedLayer is null)
        {
            return;
        }

        var normalized = Math.Clamp(value, 0.2d, 4d);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            SelectedPathPointWidthScaleEditor = normalized;
            _suppressInspector = false;
            return;
        }

        if (!TimelineEditingService.SetPathPointStrokeWidthScale(
                SelectedLayer.Model,
                SelectedPathPointIndexEditor,
                normalized,
                CurrentTime,
                ShouldCreateKeyframe(),
                Duration))
        {
            return;
        }

        ReloadPreviewForLayer(SelectedLayer);
        RefreshSelectedPathPointEditors();
        RecordHistoryIfNeeded();
        StatusMessage = "Point width updated";
        OnPropertyChanged(nameof(ShapeTweenSummary));
        OnPropertyChanged(nameof(CanDeleteCurrentShapeKeyframe));
    }

    partial void OnSymbolPlaybackModeEditorChanged(SymbolPlaybackMode value)
    {
        if (_suppressSymbolInstanceEditor || !SelectedLayerIsSymbolInstance || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.SymbolPlaybackMode = value;
        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Instance playback mode updated";
    }

    partial void OnSymbolPlaybackOffsetEditorChanged(double value)
    {
        if (_suppressSymbolInstanceEditor || !SelectedLayerIsSymbolInstance || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.SymbolPlaybackOffset = value;
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Instance playback offset updated";
    }

    partial void OnSymbolLockedFrameEditorChanged(int value)
    {
        if (_suppressSymbolInstanceEditor || !SelectedLayerIsSymbolInstance || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.SymbolLockedFrame = Math.Max(0, value);
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Instance locked frame updated";
    }

    partial void OnSymbolButtonStateEditorChanged(ButtonVisualState value)
    {
        if (_suppressSymbolInstanceEditor || !SelectedLayerIsSymbolInstance || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.SymbolButtonState = value;
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Button preview state updated";
    }

    partial void OnInstanceNameEditorChanged(string value)
    {
        if (_suppressSymbolInstanceEditor || !SelectedLayerIsSymbolInstance || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.InstanceName = value?.Trim() ?? string.Empty;
        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Instance name updated";
    }

    partial void OnSelectedGuideLayerChanged(LayerViewModel? value)
    {
        if (_suppressInspector || !CanEditGuideBinding || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.GuidedByLayerId = value?.Id;
        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = value is null ? "Guide binding cleared" : $"Guide bound to {value.Name}";
        RefreshGuideBindingStateProperties();
    }

    partial void OnSelectedTransformParentLayerChanged(LayerViewModel? value)
    {
        if (_suppressInspector || !CanEditTransformParenting || SelectedLayer is null)
        {
            return;
        }

        if (value is not null && !LayerParentingService.CanSetParent(_document.Layers, SelectedLayer.Id, value.Id))
        {
            return;
        }

        var childSnapshot = SampleWorldLayerSnapshot(SelectedLayer.Model, CurrentTime);
        LayerSnapshot? parentSnapshot = value is null ? null : SampleWorldLayerSnapshot(value.Model, CurrentTime);
        LayerParentingService.BindToParent(SelectedLayer.Model, value?.Model, childSnapshot, parentSnapshot);
        SelectedLayer.RefreshMetadata();
        RebuildLayers(SelectedLayer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = value is null ? "Transform parent cleared" : $"Transform parent set to {value.Name}";
        RefreshTransformParentingStateProperties();
    }

    partial void OnGuideOrientToPathEditorChanged(bool value)
    {
        if (_suppressInspector || !CanEditGuideBinding || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.OrientToGuidePath = value;
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = value ? "Guide orientation enabled" : "Guide orientation disabled";
        RefreshGuideBindingStateProperties();
    }

    partial void OnGuideSnapToPathEditorChanged(bool value)
    {
        if (_suppressInspector || !CanEditGuideBinding || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.SnapToGuidePath = value;
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = value ? "Guide snap enabled" : "Guide snap disabled";
        RefreshGuideBindingStateProperties();
    }

    partial void OnWarpEnabledEditorChanged(bool value)
    {
        if (_suppressInspector || !CanEditWarpRigging || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.Warp.IsEnabled = value;
        if (value && SelectedLayer.Model.Warp.Pins.Count == 0)
        {
            LayerDeformationService.EnsureDefaultWarpPins(SelectedLayer.Model);
        }

        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = value ? "Asset warp enabled" : "Asset warp disabled";
        RefreshWarpRigStateProperties();
    }

    partial void OnWarpMeshColumnsEditorChanged(int value)
    {
        if (_suppressInspector || !CanEditWarpRigging || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.Warp.MeshColumns = Math.Clamp(value, 2, 12);
        LayerDeformationService.Normalize(SelectedLayer.Model);
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Warp mesh columns updated";
        RefreshWarpRigStateProperties();
    }

    partial void OnWarpMeshRowsEditorChanged(int value)
    {
        if (_suppressInspector || !CanEditWarpRigging || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.Warp.MeshRows = Math.Clamp(value, 2, 12);
        LayerDeformationService.Normalize(SelectedLayer.Model);
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Warp mesh rows updated";
        RefreshWarpRigStateProperties();
    }

    partial void OnRigEnabledEditorChanged(bool value)
    {
        if (_suppressInspector || !CanEditWarpRigging || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.Rig.IsEnabled = value;
        if (value && SelectedLayer.Model.Rig.Bones.Count == 0)
        {
            LayerDeformationService.EnsureDefaultRig(SelectedLayer.Model);
        }

        ReloadRigPoses();
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = value ? "Modern rig enabled" : "Modern rig disabled";
        RefreshWarpRigStateProperties();
    }

    partial void OnSelectedRigPoseChanged(RigPoseViewModel? value)
    {
        RefreshWarpRigStateProperties();
        if (value is not null)
        {
            RigPoseNameEditor = value.Name;
        }
    }

    partial void OnLayerOutlineEditorChanged(bool value)
    {
        UpdateSelectedLayerAuthoringMetadata(
            layer => layer.ShowAsOutline = value,
            value ? "Layer outline mode enabled" : "Layer outline mode disabled");
    }

    partial void OnLayerOutlineColorEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        UpdateSelectedLayerAuthoringMetadata(
            layer => layer.OutlineColor = value.Trim(),
            "Layer outline color updated");
    }

    partial void OnLayerCacheAsBitmapEditorChanged(bool value)
    {
        UpdateSelectedLayerAuthoringMetadata(
            layer => layer.CacheAsBitmap = value,
            value ? "Instance cache as bitmap enabled" : "Instance cache as bitmap disabled");
    }

    partial void OnLayerBitmapCacheBackgroundEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        UpdateSelectedLayerAuthoringMetadata(
            layer => layer.BitmapCacheBackgroundColor = value.Trim(),
            "Instance cache background updated");
    }

    partial void OnMediaPlaybackModeEditorChanged(MediaPlaybackMode value)
    {
        UpdateSelectedMediaLayer(
            settings => settings.PlaybackMode = value,
            "Media playback mode updated");
    }

    partial void OnMediaStartTimeEditorChanged(double value)
    {
        UpdateSelectedMediaLayer(
            settings => settings.StartTime = Math.Max(0d, value),
            "Media clip start updated");
    }

    partial void OnMediaClipOffsetEditorChanged(double value)
    {
        UpdateSelectedMediaLayer(
            settings => settings.ClipOffset = Math.Max(0d, value),
            "Media clip offset updated");
    }

    partial void OnMediaClipDurationEditorChanged(double value)
    {
        var normalized = Math.Max(0.05d, value);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            MediaClipDurationEditor = normalized;
            _suppressInspector = false;
            return;
        }

        UpdateSelectedMediaLayer(
            settings => settings.ClipDuration = normalized,
            "Media clip duration updated");
    }

    partial void OnMediaLoopEditorChanged(bool value)
    {
        UpdateSelectedMediaLayer(
            settings =>
            {
                settings.Loop = value;
                settings.RepeatForever = value;
            },
            value ? "Media looping enabled" : "Media looping disabled");
    }

    partial void OnMediaRepeatCountEditorChanged(int value)
    {
        var normalized = Math.Max(1, value);
        if (normalized != value)
        {
            _suppressInspector = true;
            MediaRepeatCountEditor = normalized;
            _suppressInspector = false;
            return;
        }

        UpdateSelectedMediaLayer(
            settings => settings.RepeatCount = normalized,
            normalized <= 1 ? "Media repeat count reset to single pass" : $"Media repeat count set to {normalized}");
    }

    partial void OnMediaRepeatForeverEditorChanged(bool value)
    {
        UpdateSelectedMediaLayer(
            settings =>
            {
                settings.RepeatForever = value;
                settings.Loop = value;
            },
            value ? "Media repeat forever enabled" : "Media repeat forever disabled");
    }

    partial void OnMediaVolumeEditorChanged(double value)
    {
        UpdateSelectedMediaLayer(
            settings => settings.Volume = TimelineMath.Clamp(value, 0d, 1d),
            "Media volume updated");
    }

    partial void OnMediaGainDbEditorChanged(double value)
    {
        UpdateSelectedMediaLayer(
            settings => settings.GainDb = TimelineMath.Clamp(value, -24d, 24d),
            "Media gain updated");
    }

    partial void OnMediaPanEditorChanged(double value)
    {
        UpdateSelectedMediaLayer(
            settings => settings.Pan = TimelineMath.Clamp(value, -1d, 1d),
            "Media pan updated");
    }

    partial void OnMediaFadeInEditorChanged(double value)
    {
        UpdateSelectedMediaLayer(
            settings => settings.FadeInDuration = Math.Max(0d, value),
            "Media fade in updated");
    }

    partial void OnMediaFadeOutEditorChanged(double value)
    {
        UpdateSelectedMediaLayer(
            settings => settings.FadeOutDuration = Math.Max(0d, value),
            "Media fade out updated");
    }

    partial void OnEditMultipleFramesEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(EditMultipleFramesSummary));
        StatusMessage = value ? "Edit Multiple Frames enabled" : "Edit Multiple Frames disabled";
    }

    partial void OnOnionSkinKeyframesOnlyChanged(bool value)
    {
        OnPropertyChanged(nameof(OnionSkinSummary));
        if (!_suppressInspector)
        {
            StatusMessage = value ? "Onion skin limited to keyed frames" : "Onion skin includes sampled frames";
        }
    }

    partial void OnOnionSkinBaseOpacityChanged(double value)
    {
        var normalized = TimelineMath.Clamp(value, 0.02d, 0.72d);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            OnionSkinBaseOpacity = normalized;
            _suppressInspector = false;
            return;
        }

        OnPropertyChanged(nameof(OnionSkinSummary));
    }

    partial void OnOnionSkinOpacityFalloffChanged(double value)
    {
        var normalized = TimelineMath.Clamp(value, 0.01d, 0.36d);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            OnionSkinOpacityFalloff = normalized;
            _suppressInspector = false;
            return;
        }

        OnPropertyChanged(nameof(OnionSkinSummary));
    }

    partial void OnShapeDrawingModeEditorChanged(ShapeDrawingMode value)
    {
        if (_suppressInspector)
        {
            return;
        }

        if (SelectedLayer is null)
        {
            RefreshShapeStyleStateProperties();
            return;
        }

        UpdateSelectedShapeAuthoring(
            style => style.DrawingMode = value,
            $"Drawing mode set to {value}");
    }

    partial void OnPolyStarSidesEditorChanged(int value)
    {
        var normalized = Math.Clamp(value, 3, 16);
        if (normalized != value)
        {
            _suppressInspector = true;
            PolyStarSidesEditor = normalized;
            _suppressInspector = false;
            return;
        }

        if (SelectedLayer is null)
        {
            RefreshShapeStyleStateProperties();
            return;
        }

        UpdateSelectedShapeAuthoring(
            style => style.PolyStarSides = normalized,
            "PolyStar sides updated");
    }

    partial void OnPolyStarInnerRadiusEditorChanged(double value)
    {
        var normalized = TimelineMath.Clamp(value, 0.08d, 0.95d);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            PolyStarInnerRadiusEditor = normalized;
            _suppressInspector = false;
            return;
        }

        if (SelectedLayer is null)
        {
            RefreshShapeStyleStateProperties();
            return;
        }

        UpdateSelectedShapeAuthoring(
            style => style.PolyStarInnerRadius = normalized,
            "PolyStar inner radius updated");
    }

    partial void OnPolyStarIsStarEditorChanged(bool value)
    {
        if (_suppressInspector)
        {
            return;
        }

        if (SelectedLayer is null)
        {
            RefreshShapeStyleStateProperties();
            return;
        }

        UpdateSelectedShapeAuthoring(
            style => style.PolyStarIsStar = value,
            value ? "PolyStar set to star mode" : "PolyStar set to polygon mode");
    }

    partial void OnSelectedBehaviorNameEditorChanged(string value)
    {
        UpdateSelectedBehavior(
            behavior => behavior.Name = string.IsNullOrWhiteSpace(value) ? "Behavior" : value.Trim(),
            "Behavior renamed");
    }

    partial void OnSelectedBehaviorEnabledEditorChanged(bool value)
    {
        UpdateSelectedBehavior(
            behavior => behavior.IsEnabled = value,
            value ? "Behavior enabled" : "Behavior disabled");
    }

    partial void OnSelectedBehaviorTriggerEditorChanged(InteractionTriggerKind value)
    {
        UpdateSelectedBehavior(
            behavior => behavior.Trigger = value,
            "Behavior trigger updated");
        RefreshBehaviorEditorStateProperties();
    }

    partial void OnSelectedBehaviorTriggerArgumentEditorChanged(string value)
    {
        UpdateSelectedBehavior(
            behavior => behavior.TriggerArgument = value?.Trim() ?? string.Empty,
            "Behavior trigger target updated");
    }

    partial void OnSelectedBehaviorActionEditorChanged(InteractionActionKind value)
    {
        var normalizeVisualStateTargets = value == InteractionActionKind.ApplyVisualState;
        UpdateSelectedBehavior(
            behavior => behavior.Action = value,
            "Behavior action updated",
            recordHistory: !normalizeVisualStateTargets);
        if (normalizeVisualStateTargets)
        {
            NormalizeSelectedBehaviorVisualStateTargets(recordHistory: true, "Behavior action updated", forceRecordHistory: true);
        }

        RefreshBehaviorEditorStateProperties();
    }

    partial void OnSelectedBehaviorTargetSceneChanged(SceneViewModel? oldValue, SceneViewModel? newValue)
    {
        UpdateSelectedBehavior(
            behavior => behavior.TargetSceneId = newValue?.Id,
            "Behavior scene target updated");
    }

    partial void OnSelectedBehaviorTargetLayerChanged(LayerViewModel? oldValue, LayerViewModel? newValue)
    {
        var normalizeVisualStateTargets = SelectedBehaviorUsesVisualState;
        UpdateSelectedBehavior(
            behavior => behavior.TargetLayerId = newValue?.Id,
            "Behavior layer target updated",
            recordHistory: !normalizeVisualStateTargets);
        if (normalizeVisualStateTargets)
        {
            NormalizeSelectedBehaviorVisualStateTargets(recordHistory: true, "Behavior layer target updated", forceRecordHistory: true);
        }
    }

    partial void OnSelectedBehaviorTargetFrameLabelEditorChanged(string value)
    {
        UpdateSelectedBehavior(
            behavior => behavior.TargetFrameLabel = value?.Trim() ?? string.Empty,
            "Behavior frame label updated");
    }

    partial void OnSelectedBehaviorTargetButtonStateEditorChanged(ButtonVisualState value)
    {
        UpdateSelectedBehavior(
            behavior => behavior.TargetButtonState = value,
            "Behavior button state updated");
    }

    partial void OnSelectedBehaviorBoolValueEditorChanged(bool value)
    {
        UpdateSelectedBehavior(
            behavior => behavior.BoolValue = value,
            value ? "Behavior will show the layer" : "Behavior will hide the layer");
    }

    partial void OnSelectedBehaviorVariableNameEditorChanged(string value)
    {
        UpdateSelectedBehavior(
            behavior => behavior.VariableName = value?.Trim() ?? string.Empty,
            "Behavior variable updated");
    }

    partial void OnSelectedBehaviorVariableValueEditorChanged(string value)
    {
        UpdateSelectedBehavior(
            behavior => behavior.VariableValue = value ?? string.Empty,
            "Behavior variable value updated");
    }

    partial void OnSelectedBehaviorTargetVisualStateGroupEditorChanged(string value)
    {
        var normalizeVisualStateTargets = SelectedBehaviorUsesVisualState;
        UpdateSelectedBehavior(
            behavior => behavior.TargetVisualStateGroup = value?.Trim() ?? string.Empty,
            "Behavior visual state group updated",
            recordHistory: !normalizeVisualStateTargets);
        if (normalizeVisualStateTargets)
        {
            NormalizeSelectedBehaviorVisualStateTargets(recordHistory: true, "Behavior visual state group updated", forceRecordHistory: true);
        }

        RefreshBehaviorEditorStateProperties();
    }

    partial void OnSelectedBehaviorTargetVisualStateEditorChanged(string value)
    {
        UpdateSelectedBehavior(
            behavior => behavior.TargetVisualState = value?.Trim() ?? string.Empty,
            "Behavior visual state updated");
    }

    partial void OnSelectedBehaviorScriptEditorChanged(string value)
    {
        UpdateSelectedBehavior(
            behavior => behavior.Script = value ?? string.Empty,
            "Behavior script updated");
        RefreshActionScriptStateProperties();
    }

    partial void OnFrameActionScriptEditorChanged(string value)
    {
        if (_suppressFrameActionScriptEditor || !CanEditCurrentFrameActionScript || FindCurrentFrameLabelModel() is not { } label)
        {
            return;
        }

        label.Script = value ?? string.Empty;
        RefreshActionScriptStateProperties();
        RecordHistoryIfNeeded();
        StatusMessage = "Frame action script updated";
    }

    partial void OnInspectorXChanged(double value) => ApplyInspectorValue(AnimatedProperty.X, value);

    partial void OnInspectorYChanged(double value) => ApplyInspectorValue(AnimatedProperty.Y, value);

    partial void OnInspectorWidthChanged(double value) => ApplyInspectorValue(AnimatedProperty.Width, value);

    partial void OnInspectorHeightChanged(double value) => ApplyInspectorValue(AnimatedProperty.Height, value);

    partial void OnInspectorScaleXChanged(double value) => ApplyInspectorValue(AnimatedProperty.ScaleX, value);

    partial void OnInspectorScaleYChanged(double value) => ApplyInspectorValue(AnimatedProperty.ScaleY, value);

    partial void OnInspectorSkewXChanged(double value) => ApplyInspectorValue(AnimatedProperty.SkewX, value);

    partial void OnInspectorSkewYChanged(double value) => ApplyInspectorValue(AnimatedProperty.SkewY, value);

    partial void OnInspectorRotationChanged(double value) => ApplyInspectorValue(AnimatedProperty.Rotation, value);

    partial void OnInspectorRotationXChanged(double value) => ApplyInspectorValue(AnimatedProperty.RotationX, value);

    partial void OnInspectorRotationYChanged(double value) => ApplyInspectorValue(AnimatedProperty.RotationY, value);

    partial void OnInspectorZDepthChanged(double value) => ApplyInspectorValue(AnimatedProperty.ZDepth, value);

    partial void OnInspectorOpacityChanged(double value) => ApplyInspectorValue(AnimatedProperty.Opacity, value);

    partial void OnInspectorCornerRadiusChanged(double value)
    {
        if (_suppressInspector || !CanEditSelection)
        {
            return;
        }

        var selectedLayer = SelectedLayer!;
        selectedLayer.Model.Style.CornerRadius = Math.Max(0, value);
        selectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(selectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Corner radius updated";
    }

    partial void OnInspectorFontSizeChanged(double value)
    {
        if (_suppressInspector || !CanEditSelection)
        {
            return;
        }

        var selectedLayer = SelectedLayer!;
        selectedLayer.Model.Style.FontSize = Math.Max(8, value);
        selectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(selectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Font size updated";
    }

    partial void OnAvaloniaControlContentEditorChanged(string value)
    {
        UpdateSelectedAvaloniaControl(
            settings =>
            {
                settings.Content = value ?? string.Empty;
                if (SelectedLayer is not null &&
                    settings.Kind is AvaloniaControlKind.Button or AvaloniaControlKind.TextBlock or AvaloniaControlKind.TextBox or AvaloniaControlKind.CheckBox or AvaloniaControlKind.ToggleButton)
                {
                    SelectedLayer.Model.Style.Text = value ?? string.Empty;
                }
            },
            "Avalonia control content updated");
    }

    partial void OnAvaloniaControlSecondaryContentEditorChanged(string value)
    {
        UpdateSelectedAvaloniaControl(
            settings => settings.SecondaryContent = value ?? string.Empty,
            "Avalonia control secondary content updated");
    }

    partial void OnAvaloniaControlIsCheckedEditorChanged(bool value)
    {
        UpdateSelectedAvaloniaControl(
            settings => settings.IsChecked = value,
            value ? "Avalonia control checked" : "Avalonia control unchecked");
    }

    partial void OnAvaloniaControlMinimumEditorChanged(double value)
    {
        UpdateSelectedAvaloniaControl(
            settings =>
            {
                settings.Minimum = value;
                settings.Maximum = Math.Max(settings.Maximum, settings.Minimum);
                settings.Value = TimelineMath.Clamp(settings.Value, settings.Minimum, settings.Maximum);
            },
            "Avalonia control minimum updated");
    }

    partial void OnAvaloniaControlMaximumEditorChanged(double value)
    {
        var normalized = Math.Max(value, AvaloniaControlMinimumEditor);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            AvaloniaControlMaximumEditor = normalized;
            _suppressInspector = false;
            return;
        }

        UpdateSelectedAvaloniaControl(
            settings =>
            {
                settings.Maximum = normalized;
                settings.Value = TimelineMath.Clamp(settings.Value, settings.Minimum, settings.Maximum);
            },
            "Avalonia control maximum updated");
    }

    partial void OnAvaloniaControlValueEditorChanged(double value)
    {
        UpdateSelectedAvaloniaControl(
            settings => settings.Value = TimelineMath.Clamp(value, settings.Minimum, settings.Maximum),
            "Avalonia control value updated");
    }

    partial void OnAvaloniaControlIsIndeterminateEditorChanged(bool value)
    {
        UpdateSelectedAvaloniaControl(
            settings => settings.IsIndeterminate = value,
            value ? "Progress set to indeterminate" : "Progress indeterminate cleared");
    }

    partial void OnAvaloniaControlSourceEditorChanged(string value)
    {
        UpdateSelectedAvaloniaControl(
            settings => settings.Source = value?.Trim() ?? string.Empty,
            "Avalonia image source updated");
    }

    partial void OnLayerRoleEditorChanged(LayerCompositeRole value)
    {
        if (_suppressInspector || !CanEditSelection || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.Compositing.Role = value;
        if (value == LayerCompositeRole.Camera)
        {
            SelectedLayer.Model.Compositing.BlendMode = LayerBlendMode.Normal;
            SelectedLayer.Model.Compositing.MaskLayerCount = 1;
        }

        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        ReloadFrameRows();
        ReloadTimelineRows();
        RefreshInspector();
        RefreshSelectionStateProperties();
        RecordHistoryIfNeeded();
        StatusMessage = $"{value} role applied";
    }

    partial void OnLayerBlendModeEditorChanged(LayerBlendMode value)
    {
        UpdateSelectedLayerCompositing(
            compositing => compositing.BlendMode = value,
            "Blend mode updated");
    }

    partial void OnMaskLayerCountEditorChanged(int value)
    {
        var normalized = Math.Max(1, value);
        if (normalized != value)
        {
            _suppressInspector = true;
            MaskLayerCountEditor = normalized;
            _suppressInspector = false;
            return;
        }

        UpdateSelectedLayerCompositing(
            compositing => compositing.MaskLayerCount = normalized,
            "Mask coverage updated");
    }

    partial void OnLayerBlurEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.BlurRadius = Math.Max(0d, value),
        "Blur updated");

    partial void OnLayerGlowSizeEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.GlowSize = Math.Max(0d, value),
        "Glow size updated");

    partial void OnLayerGlowOpacityEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.GlowOpacity = TimelineMath.Clamp(value, 0d, 1d),
        "Glow opacity updated");

    partial void OnLayerGlowColorEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        UpdateSelectedLayerCompositing(
            compositing => compositing.GlowColor = value.Trim(),
            "Glow color updated");
    }

    partial void OnLayerShadowBlurEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.ShadowBlur = Math.Max(0d, value),
        "Shadow blur updated");

    partial void OnLayerShadowOpacityEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.ShadowOpacity = TimelineMath.Clamp(value, 0d, 1d),
        "Shadow opacity updated");

    partial void OnLayerShadowOffsetXEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.ShadowOffsetX = value,
        "Shadow offset updated");

    partial void OnLayerShadowOffsetYEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.ShadowOffsetY = value,
        "Shadow offset updated");

    partial void OnLayerShadowColorEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        UpdateSelectedLayerCompositing(
            compositing => compositing.ShadowColor = value.Trim(),
            "Shadow color updated");
    }

    partial void OnLayerTintColorEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        UpdateSelectedLayerCompositing(
            compositing => compositing.TintColor = value.Trim(),
            "Tint color updated");
    }

    partial void OnLayerTintStrengthEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.TintStrength = TimelineMath.Clamp(value, 0d, 1d),
        "Tint strength updated");

    partial void OnLayerBrightnessEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.Brightness = TimelineMath.Clamp(value, -1d, 1d),
        "Brightness updated");

    partial void OnLayerContrastEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.Contrast = TimelineMath.Clamp(value, -1d, 1d),
        "Contrast updated");

    partial void OnLayerHueShiftEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.HueShift = value,
        "Hue shift updated");

    partial void OnLayerBevelTypeEditorChanged(LayerBevelType value) => UpdateSelectedLayerCompositing(
        compositing => compositing.BevelType = value,
        "Bevel type updated");

    partial void OnLayerBevelSizeEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.BevelSize = Math.Max(0d, value),
        "Bevel size updated");

    partial void OnLayerBevelStrengthEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.BevelStrength = Math.Max(0d, value),
        "Bevel strength updated");

    partial void OnLayerBevelAngleEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.BevelAngle = value,
        "Bevel angle updated");

    partial void OnLayerBevelDistanceEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.BevelDistance = Math.Max(0d, value),
        "Bevel distance updated");

    partial void OnLayerBevelHighlightColorEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        UpdateSelectedLayerCompositing(
            compositing => compositing.BevelHighlightColor = value.Trim(),
            "Bevel highlight updated");
    }

    partial void OnLayerBevelShadowColorEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        UpdateSelectedLayerCompositing(
            compositing => compositing.BevelShadowColor = value.Trim(),
            "Bevel shadow updated");
    }

    partial void OnLayerGradientGlowSizeEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.GradientGlowSize = Math.Max(0d, value),
        "Gradient glow size updated");

    partial void OnLayerGradientGlowStrengthEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.GradientGlowStrength = Math.Max(0d, value),
        "Gradient glow strength updated");

    partial void OnLayerGradientGlowAngleEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.GradientGlowAngle = value,
        "Gradient glow angle updated");

    partial void OnLayerGradientGlowDistanceEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.GradientGlowDistance = Math.Max(0d, value),
        "Gradient glow distance updated");

    partial void OnLayerGradientGlowFromEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        UpdateSelectedLayerCompositing(
            compositing => compositing.GradientGlowFrom = value.Trim(),
            "Gradient glow start updated");
    }

    partial void OnLayerGradientGlowToEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        UpdateSelectedLayerCompositing(
            compositing => compositing.GradientGlowTo = value.Trim(),
            "Gradient glow end updated");
    }

    partial void OnLayerGradientBevelTypeEditorChanged(LayerBevelType value) => UpdateSelectedLayerCompositing(
        compositing => compositing.GradientBevelType = value,
        "Gradient bevel type updated");

    partial void OnLayerGradientBevelSizeEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.GradientBevelSize = Math.Max(0d, value),
        "Gradient bevel size updated");

    partial void OnLayerGradientBevelStrengthEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.GradientBevelStrength = Math.Max(0d, value),
        "Gradient bevel strength updated");

    partial void OnLayerGradientBevelAngleEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.GradientBevelAngle = value,
        "Gradient bevel angle updated");

    partial void OnLayerGradientBevelDistanceEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.GradientBevelDistance = Math.Max(0d, value),
        "Gradient bevel distance updated");

    partial void OnLayerGradientBevelFromEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        UpdateSelectedLayerCompositing(
            compositing => compositing.GradientBevelFrom = value.Trim(),
            "Gradient bevel highlight updated");
    }

    partial void OnLayerGradientBevelToEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        UpdateSelectedLayerCompositing(
            compositing => compositing.GradientBevelTo = value.Trim(),
            "Gradient bevel shadow updated");
    }

    partial void OnLayerFlashColorEffectModeEditorChanged(FlashColorEffectMode value)
    {
        RefreshFlashColorEffectStateProperties();
        UpdateSelectedLayerCompositing(
            compositing => compositing.FlashColorEffectMode = value,
            "Flash color effect updated");
    }

    partial void OnLayerFlashAlphaPercentEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.FlashAlphaPercent = Math.Clamp(value, 0d, 1000d),
        "Flash alpha effect updated");

    partial void OnLayerFlashTintColorEditorChanged(string value)
    {
        if (_suppressInspector || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        UpdateSelectedLayerCompositing(
            compositing => compositing.FlashTintColor = value.Trim(),
            "Flash tint color updated");
    }

    partial void OnLayerFlashTintPercentEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.FlashTintPercent = Math.Clamp(value, 0d, 100d),
        "Flash tint amount updated");

    partial void OnLayerFlashBrightnessPercentEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.FlashBrightnessPercent = Math.Clamp(value, -100d, 100d),
        "Flash brightness effect updated");

    partial void OnLayerFlashRedPercentEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.FlashRedPercent = Math.Clamp(value, -1000d, 1000d),
        "Flash advanced red multiplier updated");

    partial void OnLayerFlashGreenPercentEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.FlashGreenPercent = Math.Clamp(value, -1000d, 1000d),
        "Flash advanced green multiplier updated");

    partial void OnLayerFlashBluePercentEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.FlashBluePercent = Math.Clamp(value, -1000d, 1000d),
        "Flash advanced blue multiplier updated");

    partial void OnLayerFlashAdvancedAlphaPercentEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.FlashAdvancedAlphaPercent = Math.Clamp(value, -1000d, 1000d),
        "Flash advanced alpha multiplier updated");

    partial void OnLayerFlashRedOffsetEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.FlashRedOffset = Math.Clamp(value, -255d, 255d),
        "Flash advanced red offset updated");

    partial void OnLayerFlashGreenOffsetEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.FlashGreenOffset = Math.Clamp(value, -255d, 255d),
        "Flash advanced green offset updated");

    partial void OnLayerFlashBlueOffsetEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.FlashBlueOffset = Math.Clamp(value, -255d, 255d),
        "Flash advanced blue offset updated");

    partial void OnLayerFlashAlphaOffsetEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.FlashAlphaOffset = Math.Clamp(value, -255d, 255d),
        "Flash advanced alpha offset updated");

    partial void OnLayerSaturationEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.Saturation = Math.Max(0d, value),
        "Saturation updated");

    partial void OnLayerParallaxDepthEditorChanged(double value) => UpdateSelectedLayerCompositing(
        compositing => compositing.ParallaxDepth = Math.Max(0.2d, value),
        "Camera depth updated");

    [RelayCommand]
    private void Undo()
    {
        if (IsPrototypeMode)
        {
            return;
        }

        if (_history?.TryUndo(out var snapshot) != true)
        {
            return;
        }

        ApplySnapshot(snapshot);
        UpdateHistoryAvailability();
        StatusMessage = "Undo applied";
    }

    [RelayCommand]
    private void Redo()
    {
        if (IsPrototypeMode)
        {
            return;
        }

        if (_history?.TryRedo(out var snapshot) != true)
        {
            return;
        }

        ApplySnapshot(snapshot);
        UpdateHistoryAvailability();
        StatusMessage = "Redo applied";
    }

    [RelayCommand]
    private void ResetBlank()
    {
        LoadDocument(BlankProjectFactory.Create(), "Blank Animation");
        StatusMessage = "Blank animation created";
    }

    [RelayCommand]
    private void ResetSample()
    {
        LoadDocument(SampleProjectFactory.Create(), "Sample Composition");
        StatusMessage = "Sample composition restored";
    }

    [RelayCommand]
    private void ApplyColorSwatch(string? parameter)
    {
        if (string.IsNullOrWhiteSpace(parameter))
        {
            return;
        }

        var separatorIndex = parameter.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex >= parameter.Length - 1)
        {
            return;
        }

        var target = parameter[..separatorIndex];
        var color = parameter[(separatorIndex + 1)..].Trim();
        if (!color.StartsWith('#'))
        {
            color = $"#{color}";
        }

        switch (target)
        {
            case "fill" when CanEditStyleSurface:
                FillHexEditor = color;
                StatusMessage = $"Swatch applied to fill ({color})";
                break;
            case "stroke" when CanEditStyleSurface:
                StrokeHexEditor = color;
                StatusMessage = $"Swatch applied to stroke ({color})";
                break;
            case "gradientFrom" when CanEditStyleSurface:
                GradientFromEditor = color;
                StatusMessage = $"Swatch applied to gradient start ({color})";
                break;
            case "gradientTo" when CanEditStyleSurface:
                GradientToEditor = color;
                StatusMessage = $"Swatch applied to gradient end ({color})";
                break;
            case "backgroundFrom" when CanEditCanvasProperties:
                BackgroundFrom = color;
                StatusMessage = $"Swatch applied to stage color ({color})";
                break;
            case "backgroundTo" when CanEditCanvasProperties:
                PasteboardColor = color;
                StatusMessage = $"Swatch applied to pasteboard ({color})";
                break;
        }
    }

    [RelayCommand]
    private void ClearHistoryEntries()
    {
        HistoryEntries.Clear();
        OnPropertyChanged(nameof(HistoryPanelSummary));
        StatusMessage = "History panel cleared";
    }

    [RelayCommand]
    private void FocusMovieExplorerNode(MovieExplorerNodeViewModel? node)
    {
        var targetNode = node ?? SelectedMovieExplorerNode;
        if (targetNode is null || string.IsNullOrWhiteSpace(targetNode.FocusToken))
        {
            return;
        }

        if (TryFocusMovieExplorerNode(targetNode.FocusToken))
        {
            SelectedMovieExplorerNode = targetNode;
            StatusMessage = $"{targetNode.Title} focused from Movie Explorer";
        }
    }

    [RelayCommand]
    private void CaptureMotionPreset()
    {
        if (!CanCaptureMotionPreset || SelectedLayer is null)
        {
            return;
        }

        var presetName = MotionPresetService.EnsureUniquePresetName(_document, MotionPresetNameEditor);
        var preset = MotionPresetService.CapturePreset(SelectedLayer.Model, presetName, MotionPresetDescriptionEditor);
        _document.MotionPresets.Add(preset);
        ReloadMotionPresets(preset.Id);
        RecordHistoryIfNeeded();
        StatusMessage = $"{preset.Name} captured";
    }

    [RelayCommand]
    private void ApplySelectedMotionPreset()
    {
        if (!CanApplySelectedMotionPreset || SelectedLayer is null || SelectedMotionPreset is null)
        {
            return;
        }

        if (!MotionPresetService.ApplyPreset(SelectedLayer.Model, SelectedMotionPreset.Model))
        {
            return;
        }

        PrimitiveShapeService.NormalizeStyle(SelectedLayer.Model.Style);
        if (SelectedLayer.Model.Style.PrimitiveShape == PrimitiveShapeType.PolyStar)
        {
            PrimitiveShapeService.UpdatePolyStarGeometry(SelectedLayer.Model);
        }

        ReloadPreviewForLayer(SelectedLayer);
        ReloadTracks();
        RefreshInspector();
        RefreshSelectionStateProperties();
        RecordHistoryIfNeeded();
        StatusMessage = $"{SelectedMotionPreset.Name} applied";
    }

    [RelayCommand]
    private void DeleteSelectedMotionPreset()
    {
        if (!CanDeleteSelectedMotionPreset || SelectedMotionPreset is null)
        {
            return;
        }

        var removedName = SelectedMotionPreset.Name;
        _document.MotionPresets.RemoveAll(item => item.Id == SelectedMotionPreset.Id);
        ReloadMotionPresets();
        RecordHistoryIfNeeded();
        StatusMessage = $"{removedName} removed";
    }

    [RelayCommand]
    private void InsertPathPointAfterSelected()
    {
        if (!CanInsertPathPointAfterSelected || SelectedLayer is null)
        {
            return;
        }

        var countBefore = SelectedLayer.Model.Style.PathPoints.Count;
        var sourceIndex = SelectedPathPointIndexEditor;
        if (!TimelineEditingService.InsertPathPoint(
                SelectedLayer.Model,
                sourceIndex,
                PathClosedEditor,
                CurrentTime,
                ShouldCreateKeyframe(),
                Duration))
        {
            return;
        }

        var countAfter = SelectedLayer.Model.Style.PathPoints.Count;
        SelectedPathPointIndexEditor = PathClosedEditor && sourceIndex == countBefore - 1 ? countAfter - 1 : Math.Min(countAfter - 1, sourceIndex + 1);
        ReloadPreviewForLayer(SelectedLayer);
        RefreshSelectedPathPointEditors();
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = "Path point inserted";
    }

    [RelayCommand]
    private void DeleteSelectedPathPoint()
    {
        if (!CanDeleteSelectedPathPoint || SelectedLayer is null)
        {
            return;
        }

        if (!TimelineEditingService.DeletePathPoint(
                SelectedLayer.Model,
                SelectedPathPointIndexEditor,
                CurrentTime,
                ShouldCreateKeyframe(),
                Duration))
        {
            return;
        }

        SelectedPathPointIndexEditor = Math.Min(SelectedPathPointIndexEditor, SelectedLayer.Model.Style.PathPoints.Count - 1);
        ReloadPreviewForLayer(SelectedLayer);
        RefreshSelectedPathPointEditors();
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = "Path point deleted";
    }

    [RelayCommand]
    private void ReverseSelectedPath()
    {
        if (!CanReverseSelectedPath || SelectedLayer is null)
        {
            return;
        }

        var oldIndex = SelectedPathPointIndexEditor;
        if (!TimelineEditingService.ReversePath(SelectedLayer.Model, CurrentTime, ShouldCreateKeyframe(), Duration))
        {
            return;
        }

        SelectedPathPointIndexEditor = oldIndex >= 0
            ? Math.Max(0, SelectedLayer.Model.Style.PathPoints.Count - 1 - oldIndex)
            : -1;
        ReloadPreviewForLayer(SelectedLayer);
        RefreshSelectedPathPointEditors();
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = "Path direction reversed";
    }

    [RelayCommand]
    private void ApplySelectedStrokeProfile()
    {
        if (!CanApplySelectedStrokeProfile || SelectedLayer is null)
        {
            return;
        }

        if (!TimelineEditingService.ApplyStrokeProfile(
                SelectedLayer.Model,
                VectorStrokeProfilePresetEditor,
                VectorStrokeProfileStrengthEditor,
                PathClosedEditor,
                CurrentTime,
                ShouldCreateKeyframe(),
                Duration))
        {
            return;
        }

        ReloadPreviewForLayer(SelectedLayer);
        RefreshSelectedPathPointEditors();
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = $"{VectorStrokeProfilePresetEditor} stroke profile applied";
    }

    [RelayCommand]
    private void SmoothSelectedPath()
    {
        if (!CanOptimizeSelectedPath || SelectedLayer is null)
        {
            return;
        }

        var snapshot = FrameTimelineService.SampleLayer(SelectedLayer.Model, CurrentTime, SceneFrameRate, TotalFrames)
            ?? TimelineInterpolationService.SampleLayer(SelectedLayer.Model, CurrentTime);
        if (!TimelineEditingService.SmoothPath(SelectedLayer.Model, snapshot, CurrentTime, ShouldCreateKeyframe(), Duration))
        {
            return;
        }

        ReloadPreviewForLayer(SelectedLayer);
        RefreshSelectedPathPointEditors();
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = "Path smoothed";
    }

    [RelayCommand]
    private void StraightenSelectedPath()
    {
        if (!CanOptimizeSelectedPath || SelectedLayer is null)
        {
            return;
        }

        var snapshot = FrameTimelineService.SampleLayer(SelectedLayer.Model, CurrentTime, SceneFrameRate, TotalFrames)
            ?? TimelineInterpolationService.SampleLayer(SelectedLayer.Model, CurrentTime);
        if (!TimelineEditingService.StraightenPath(SelectedLayer.Model, snapshot, CurrentTime, ShouldCreateKeyframe(), Duration))
        {
            return;
        }

        ReloadPreviewForLayer(SelectedLayer);
        RefreshSelectedPathPointEditors();
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = "Path straightened";
    }

    [RelayCommand]
    private void OptimizeSelectedPath()
    {
        if (!CanOptimizeSelectedPath || SelectedLayer is null)
        {
            return;
        }

        var snapshot = FrameTimelineService.SampleLayer(SelectedLayer.Model, CurrentTime, SceneFrameRate, TotalFrames)
            ?? TimelineInterpolationService.SampleLayer(SelectedLayer.Model, CurrentTime);
        if (!TimelineEditingService.OptimizePath(SelectedLayer.Model, snapshot, CurrentTime, ShouldCreateKeyframe(), Duration))
        {
            return;
        }

        SelectedPathPointIndexEditor = Math.Min(SelectedPathPointIndexEditor, SelectedLayer.Model.Style.PathPoints.Count - 1);
        ReloadPreviewForLayer(SelectedLayer);
        RefreshSelectedPathPointEditors();
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = "Path optimized";
    }

    [RelayCommand]
    private void TogglePrototypeMode()
    {
        if (IsPrototypeMode)
        {
            ExitPrototypeMode();
            return;
        }

        EnterPrototypeMode();
    }

    [RelayCommand]
    private void AddScene()
    {
        if (IsEditingSymbol)
        {
            return;
        }

        PersistActiveScene();
        var scene = SceneEditingService.CreateScene(GetUniqueSceneName("New Scene"), _document);
        SceneEditingService.AddScene(_document, scene);
        ReloadScenes(scene.Id);
        RecordHistoryIfNeeded();
        StatusMessage = $"{scene.Name} added";
    }

    [RelayCommand]
    private void AddPublishProfile()
    {
        if (!CanEditPublishProfiles)
        {
            return;
        }

        var profile = PublishProfileService.CreateProfile(_document, GetUniquePublishProfileName("New Profile"));
        _document.PublishProfiles.Add(profile);
        ReloadPublishProfiles(profile.Id);
        RecordHistoryIfNeeded();
        StatusMessage = $"{profile.Name} added";
    }

    [RelayCommand]
    private void DuplicatePublishProfile()
    {
        if (!CanEditPublishProfiles || SelectedPublishProfile?.Model is not { } source)
        {
            return;
        }

        var clone = source.Clone();
        clone.Name = GetUniquePublishProfileName($"{source.Name} Copy");
        _document.PublishProfiles.Add(clone);
        ReloadPublishProfiles(clone.Id);
        RecordHistoryIfNeeded();
        StatusMessage = $"{clone.Name} duplicated";
    }

    [RelayCommand]
    private void DeletePublishProfile()
    {
        if (!CanDeletePublishProfile || SelectedPublishProfile is null)
        {
            return;
        }

        var removedName = SelectedPublishProfile.Name;
        _document.PublishProfiles.RemoveAll(item => item.Id == SelectedPublishProfile.Id);
        ReloadPublishProfiles();
        RecordHistoryIfNeeded();
        StatusMessage = $"{removedName} removed";
    }

    [RelayCommand]
    private void DuplicateScene()
    {
        if (IsEditingSymbol || SelectedScene is null)
        {
            return;
        }

        PersistActiveScene();
        var duplicated = SceneEditingService.DuplicateScene(
            SelectedScene.Model,
            GetUniqueSceneName($"{SelectedScene.Name} Copy"));
        SceneEditingService.AddScene(_document, duplicated);
        ReloadScenes(duplicated.Id);
        RecordHistoryIfNeeded();
        StatusMessage = $"{duplicated.Name} duplicated";
    }

    [RelayCommand]
    private void DeleteScene()
    {
        if (IsEditingSymbol || SelectedScene is null || Scenes.Count <= 1)
        {
            return;
        }

        var index = Scenes.IndexOf(SelectedScene);
        var removedName = SelectedScene.Name;
        SceneEditingService.RemoveScene(_document, SelectedScene.Id);
        var nextSceneId = _document.Scenes[Math.Max(0, Math.Min(index, _document.Scenes.Count - 1))].Id;
        ReloadScenes(nextSceneId);
        RecordHistoryIfNeeded();
        StatusMessage = $"{removedName} removed";
    }

    [RelayCommand]
    private void MoveSceneForward()
    {
        if (IsEditingSymbol || SelectedScene is null)
        {
            return;
        }

        PersistActiveScene();
        SceneEditingService.BringForward(_document, SelectedScene.Id);
        ReloadScenes(SelectedScene.Id);
        RecordHistoryIfNeeded();
        StatusMessage = "Scene moved forward";
    }

    [RelayCommand]
    private void MoveSceneBackward()
    {
        if (IsEditingSymbol || SelectedScene is null)
        {
            return;
        }

        PersistActiveScene();
        SceneEditingService.SendBackward(_document, SelectedScene.Id);
        ReloadScenes(SelectedScene.Id);
        RecordHistoryIfNeeded();
        StatusMessage = "Scene moved backward";
    }

    [RelayCommand]
    private void TogglePlayback()
    {
        if (IsPlaying)
        {
            StopPlayback(false);
            return;
        }

        StartPlayback();
    }

    [RelayCommand]
    private void StopPlayback()
    {
        StopPlayback(true);
    }

    [RelayCommand]
    private void AddRectangle()
    {
        AddLayer(LayerKind.Rectangle, new Point(220, 180));
    }

    [RelayCommand]
    private void AddEllipse()
    {
        AddLayer(LayerKind.Ellipse, new Point(440, 220));
    }

    [RelayCommand]
    private void AddPolyStar()
    {
        if (!CanInsertStageItems)
        {
            return;
        }

        var placement = GetNextLibraryPlacement();
        var layer = CreatePolyStarLayer("Signal Star", new Rect(placement.X, placement.Y, 220d, 220d));
        TimelineEditingService.AddLayer(_document, layer);
        RebuildLayers(layer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = $"{layer.Name} added";
    }

    [RelayCommand]
    private void AddText()
    {
        AddLayer(LayerKind.Text, new Point(240, 420));
    }

    [RelayCommand]
    private void AddFolder()
    {
        if (!CanCreateFolder)
        {
            return;
        }

        var folder = TimelineEditingService.CreateFolderLayer(GetUniqueLayerName("Layer Folder"), _document.Layers.Count);
        TimelineEditingService.AddLayer(_document, folder);
        RebuildLayers(folder.Id);
        RecordHistoryIfNeeded();
        StatusMessage = "Folder added";
    }

    [RelayCommand]
    private void AddCameraLayer()
    {
        if (IsEditingSymbol)
        {
            return;
        }

        var layer = TimelineEditingService.CreateCameraLayer(
            GetUniqueLayerName("Scene Camera"),
            CanvasWidth,
            CanvasHeight,
            _document.Layers.Count);
        TimelineEditingService.AddLayer(_document, layer);
        RebuildLayers(layer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = "Camera layer added";
    }

    [RelayCommand]
    private void InsertSelectedMediaAsset()
    {
        if (SelectedMediaAsset is null || IsEditingSymbol)
        {
            return;
        }

        TimelineLayer layer = SelectedMediaAsset.Kind switch
        {
            MediaAssetKind.Audio => TimelineEditingService.CreateAudioLayer(
                SelectedMediaAsset.Model,
                _document.Layers.Count,
                CurrentTime),
            _ => TimelineEditingService.CreateVideoLayer(
                SelectedMediaAsset.Model,
                GetNextLibraryPlacement().X,
                GetNextLibraryPlacement().Y,
                _document.Layers.Count,
                CurrentTime)
        };

        TimelineEditingService.AddLayer(_document, layer);
        RebuildLayers(layer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = SelectedMediaAsset.Kind == MediaAssetKind.Audio
            ? $"{SelectedMediaAsset.Name} added as an audio track"
            : $"{SelectedMediaAsset.Name} placed on stage";
    }

    [RelayCommand]
    private void SelectDrawingTool(string? tool)
    {
        if (!Enum.TryParse<DrawingTool>(tool, true, out var drawingTool))
        {
            return;
        }

        SelectedDrawingTool = drawingTool;
    }

    [RelayCommand]
    private void ResetCanvasViewRotation()
    {
        CanvasViewRotation = 0d;
        StatusMessage = "Stage view rotation reset";
    }

    [RelayCommand]
    private void InsertAvaloniaControl(string? controlKind)
    {
        if (!CanInsertStageItems || !Enum.TryParse<AvaloniaControlKind>(controlKind, true, out var parsedControlKind))
        {
            return;
        }

        AddAvaloniaControlLayer(parsedControlKind, GetNextLibraryPlacement());
    }

    [RelayCommand]
    private void SelectAnimationExchangeFormat(string? formatKey)
    {
        if (!Enum.TryParse<AnimationExchangeFormat>(formatKey, true, out var format))
        {
            return;
        }

        SelectedAnimationExchangeFormat = format;
    }

    [RelayCommand]
    private void RefreshAnimationExchangePreview()
    {
        RefreshAnimationExchangePreviewCore(updateStatus: true);
    }

    [RelayCommand]
    private void InsertSelectedLibraryItem()
    {
        if (SelectedLibraryItem is null || IsEditingSymbol)
        {
            return;
        }

        var placement = GetNextLibraryPlacement();
        var layer = TimelineEditingService.CreateLayerFromLibraryItem(
            SelectedLibraryItem.Model,
            placement.X,
            placement.Y,
            _document.Layers.Count);
        TimelineEditingService.AddLayer(_document, layer);
        RebuildLayers(layer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = $"{SelectedLibraryItem.Name} placed on stage";
    }

    [RelayCommand]
    private void InsertSelectedComponent()
    {
        if (SelectedComponentItem is null || IsEditingSymbol)
        {
            return;
        }

        var placement = GetNextLibraryPlacement();
        var layer = TimelineEditingService.CreateLayerFromLibraryItem(
            SelectedComponentItem.Model,
            placement.X,
            placement.Y,
            _document.Layers.Count);
        TimelineEditingService.AddLayer(_document, layer);
        RebuildLayers(layer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = $"{SelectedComponentItem.Name} inserted from the component library";
    }

    [RelayCommand]
    private void CreateComponentFromSelection()
    {
        if (SelectedLayer is null || IsEditingSymbol)
        {
            return;
        }

        var selectionId = SelectedLayer.Id;
        var componentName = GetUniqueComponentName(SelectedLayer.Name);
        var component = TimelineEditingService.CreateLibraryItemFromLayer(SelectedLayer.Model, componentName, SelectedLibrarySymbolKind);
        component.IsComponent = true;
        component.ComponentCategory = SelectedLibrarySymbolKind == SymbolKind.Button ? "Controls" : "Widgets";
        component.ComponentDescription = $"Reusable {component.SymbolKind} component";
        TimelineEditingService.AddLibraryItem(_document, component);
        ReloadLibraryItems(component.Id);
        ReloadComponentItems(component.Id);
        RebuildLayers(selectionId);
        RecordHistoryIfNeeded();
        StatusMessage = $"{component.Name} added to the component library";
    }

    [RelayCommand]
    private void AddComponentParameter()
    {
        if (!CanEditSelectedComponentParameters || SelectedComponentItem is null)
        {
            return;
        }

        var parameter = new ComponentParameterDefinition
        {
            Name = "parameter",
            Kind = ComponentParameterKind.String,
            BindingTarget = ComponentParameterBindingTarget.Text,
            DefaultValue = string.Empty,
            Description = "Reusable component input"
        };

        SelectedComponentItem.Model.ComponentParameters.Add(parameter);
        ComponentParameterService.NormalizeDefinitions(SelectedComponentItem.Model);
        foreach (var layer in _document.Layers.Where(layer => layer.SourceLibraryItemId == SelectedComponentItem.Id))
        {
            ComponentParameterService.EnsureOverrides(layer, SelectedComponentItem.Model);
        }

        SelectedComponentItem.RefreshMetadata();
        if (SelectedLibraryItem?.Id == SelectedComponentItem.Id)
        {
            ReloadLibraryItems(SelectedComponentItem.Id);
        }
        else
        {
            ReloadLibraryItems(SelectedLibraryItem?.Id);
        }

        ReloadComponentItems(SelectedComponentItem.Id);
        ReloadSelectedComponentParameters(parameter.Id);
        RebuildLayers(SelectedLayer?.Id);
        RefreshSourceMonitorPreview();
        RecordHistoryIfNeeded();
        StatusMessage = "Component parameter added";
    }

    [RelayCommand]
    private void RemoveSelectedComponentParameter()
    {
        if (!CanRemoveSelectedComponentParameter || SelectedComponentItem is null || SelectedComponentParameter is null)
        {
            return;
        }

        var removedName = SelectedComponentParameter.Name;
        SelectedComponentItem.Model.ComponentParameters.RemoveAll(item => item.Id == SelectedComponentParameter.Id);
        ComponentParameterService.NormalizeDefinitions(SelectedComponentItem.Model);
        foreach (var layer in _document.Layers.Where(layer => layer.SourceLibraryItemId == SelectedComponentItem.Id))
        {
            ComponentParameterService.EnsureOverrides(layer, SelectedComponentItem.Model);
        }

        SelectedComponentItem.RefreshMetadata();
        if (SelectedLibraryItem?.Id == SelectedComponentItem.Id)
        {
            ReloadLibraryItems(SelectedComponentItem.Id);
        }
        else
        {
            ReloadLibraryItems(SelectedLibraryItem?.Id);
        }

        ReloadComponentItems(SelectedComponentItem.Id);
        ReloadSelectedComponentParameters();
        RebuildLayers(SelectedLayer?.Id);
        RefreshSourceMonitorPreview();
        RecordHistoryIfNeeded();
        StatusMessage = $"{removedName} removed from the component";
    }

    [RelayCommand]
    private void ResetSelectedComponentOverride()
    {
        if (!CanResetSelectedComponentOverride || SelectedComponentOverrideItem is null)
        {
            return;
        }

        SelectedComponentOverrideItem.Model.Value = SelectedComponentOverrideItem.Definition.DefaultValue ?? string.Empty;
        ReloadSelectedComponentOverrides(SelectedComponentOverrideItem.Model.Name);
        RebuildLayers(SelectedLayer?.Id);
        RefreshSourceMonitorPreview();
        RecordHistoryIfNeeded();
        StatusMessage = $"{SelectedComponentOverrideItem.Name} reset to the component default";
    }

    [RelayCommand]
    private void AddBehavior()
    {
        if (!CanAddBehavior || SelectedLayer is null)
        {
            return;
        }

        var behavior = InteractionBehaviorService.CreateBehavior($"{SelectedLayer.Name} Behavior");
        SelectedLayer.Model.Behaviors.Add(behavior);
        ReloadBehaviors(behavior.Id);
        RecordHistoryIfNeeded();
        StatusMessage = "Behavior added";
    }

    [RelayCommand]
    private void DeleteSelectedBehavior()
    {
        if (!CanDeleteSelectedBehavior || SelectedLayer is null || SelectedBehavior is null)
        {
            return;
        }

        SelectedLayer.Model.Behaviors.RemoveAll(item => item.Id == SelectedBehavior.Id);
        ReloadBehaviors();
        RecordHistoryIfNeeded();
        StatusMessage = "Behavior removed";
    }

    [RelayCommand]
    private void InsertSelectedSnippetIntoFrameAction()
    {
        if (!CanInsertActionScriptSnippetIntoFrame || SelectedActionScriptSnippet is null)
        {
            return;
        }

        FrameActionScriptEditor = AppendActionScriptSnippet(FrameActionScriptEditor, SelectedActionScriptSnippet.Script);
        StatusMessage = $"Inserted frame snippet '{SelectedActionScriptSnippet.Name}'";
    }

    [RelayCommand]
    private void InsertSelectedSnippetIntoBehaviorScript()
    {
        if (!CanInsertActionScriptSnippetIntoBehavior || SelectedActionScriptSnippet is null)
        {
            return;
        }

        SelectedBehaviorScriptEditor = AppendActionScriptSnippet(SelectedBehaviorScriptEditor, SelectedActionScriptSnippet.Script);
        StatusMessage = $"Inserted behavior snippet '{SelectedActionScriptSnippet.Name}'";
    }

    [RelayCommand]
    private void EnterSelectedSymbolEdit()
    {
        if (IsEditingSymbol)
        {
            return;
        }

        var libraryItemId = SelectedLayer?.Model.SourceLibraryItemId ?? SelectedLibraryItem?.Id;
        if (libraryItemId is not Guid selectedLibraryItemId)
        {
            return;
        }

        EnterSymbolEdit(selectedLibraryItemId, ButtonVisualState.Up, null, 0);
        RecordHistoryIfNeeded();
    }

    [RelayCommand]
    private void ExitSymbolEdit()
    {
        if (!IsEditingSymbol)
        {
            return;
        }

        var libraryItemName = EditingLibraryItem?.Name ?? "Symbol";
        PersistEditingSymbolWorkspace();
        var sceneId = _editingSceneId;
        var layerId = _editingSceneLayerId;
        _editingLibraryItemId = null;
        _editingSceneId = null;
        _editingSceneLayerId = null;
        ReloadScenes(sceneId);
        if (layerId is Guid selectedLayerId)
        {
            SelectLayer(selectedLayerId);
        }

        RefreshSymbolEditingProperties();
        RecordHistoryIfNeeded();
        StatusMessage = $"{libraryItemName} returned to scene";
    }

    [RelayCommand]
    private void CreateSymbolFromSelection()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        var selectionId = SelectedLayer.Id;
        var symbolName = GetUniqueLibraryName(SelectedLayer.Name);
        var libraryItem = TimelineEditingService.CreateLibraryItemFromLayer(SelectedLayer.Model, symbolName, SelectedLibrarySymbolKind);
        TimelineEditingService.AddLibraryItem(_document, libraryItem);
        SelectedLayer.Model.SourceLibraryItemId = libraryItem.Id;
        ReloadLibraryItems(libraryItem.Id);
        ReloadComponentItems();
        RebuildLayers(selectionId);
        RecordHistoryIfNeeded();
        StatusMessage = $"{libraryItem.Name} added to library";
    }

    [RelayCommand]
    private void UpdateLinkedSymbolFromSelection()
    {
        if (SelectedLayer?.Model.SourceLibraryItemId is not Guid libraryItemId)
        {
            return;
        }

        var libraryItem = _document.LibraryItems.FirstOrDefault(item => item.Id == libraryItemId);
        if (libraryItem is null)
        {
            return;
        }

        var selectionId = SelectedLayer.Id;
        TimelineEditingService.UpdateLibraryItemFromLayer(libraryItem, SelectedLayer.Model);
        var updatedInstances = TimelineEditingService.SynchronizeLibraryItemInstances(_document, libraryItemId);
        ReloadLibraryItems(libraryItemId);
        ReloadComponentItems(libraryItemId);
        RebuildLayers(selectionId);
        RecordHistoryIfNeeded();
        StatusMessage = updatedInstances == 1
            ? $"Updated {libraryItem.Name} across 1 linked instance"
            : $"Updated {libraryItem.Name} across {updatedInstances} linked instances";
    }

    [RelayCommand]
    private void RelinkSelectionToSelectedLibraryItem()
    {
        if (!CanRelinkSelectionToSelectedLibraryItem || SelectedLayer is null || SelectedLibraryItem is null)
        {
            return;
        }

        var relinkedLayer = LibraryManagementService.RelinkLayerToLibraryItem(SelectedLayer.Model, SelectedLibraryItem.Model);
        if (!ReplaceCurrentLayer(relinkedLayer))
        {
            return;
        }

        RebuildLayers(relinkedLayer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = $"{SelectedLayer.Name} relinked to {SelectedLibraryItem.Name}";
    }

    [RelayCommand]
    private void DuplicateSelection()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        var duplicate = TimelineEditingService.DuplicateLayer(SelectedLayer.Model, _document.Layers.Count);
        duplicate.Defaults.X += 34;
        duplicate.Defaults.Y += 30;
        TimelineEditingService.AddLayer(_document, duplicate);
        RebuildLayers(duplicate.Id);
        RecordHistoryIfNeeded();
        StatusMessage = "Layer duplicated";
    }

    [RelayCommand]
    private void GroupSelectionIntoFolder()
    {
        if (!CanGroupSelectionIntoFolder || SelectedLayer is null)
        {
            return;
        }

        var folder = TimelineEditingService.CreateFolderLayer(GetUniqueLayerName($"{SelectedLayer.Name} Folder"), _document.Layers.Count);
        TimelineEditingService.AddLayer(_document, folder);
        LayerHierarchyService.TrySetParent(_document.Layers, SelectedLayer.Id, folder.Id);
        RebuildLayers(folder.Id);
        RecordHistoryIfNeeded();
        StatusMessage = $"{SelectedLayer.Name} grouped into {folder.Name}";
    }

    [RelayCommand]
    private void RemoveSelectionFromFolder()
    {
        if (!CanRemoveSelectionFromFolder || SelectedLayer is null)
        {
            return;
        }

        LayerHierarchyService.TrySetParent(_document.Layers, SelectedLayer.Id, null);
        RebuildLayers(SelectedLayer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = $"{SelectedLayer.Name} moved to root";
    }

    [RelayCommand]
    private void ToggleSelectedFolderExpanded()
    {
        if (!CanToggleSelectedFolderExpanded || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.IsExpanded = !SelectedLayer.Model.IsExpanded;
        RebuildLayers(SelectedLayer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = SelectedLayer.Model.IsExpanded ? "Folder expanded" : "Folder collapsed";
    }

    [RelayCommand]
    private void DeleteSelection()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        var removedId = SelectedLayer.Id;
        TimelineEditingService.RemoveLayer(_document, removedId);
        var nextSelection = _document.Layers.OrderByDescending(item => item.ZIndex).FirstOrDefault()?.Id;
        RebuildLayers(nextSelection);
        RecordHistoryIfNeeded();
        StatusMessage = "Layer removed";
    }

    [RelayCommand]
    private void BringForward()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        TimelineEditingService.BringForward(_document, SelectedLayer.Id);
        RebuildLayers(SelectedLayer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = "Layer moved forward";
    }

    [RelayCommand]
    private void SendBackward()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        TimelineEditingService.SendBackward(_document, SelectedLayer.Id);
        RebuildLayers(SelectedLayer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = "Layer moved backward";
    }

    [RelayCommand]
    private void AddCurrentKeyframe()
    {
        if (!CanEditPropertyTracks || SelectedLayer is null)
        {
            return;
        }

        var value = FrameTimelineService.SampleProperty(SelectedLayer.Model, SelectedProperty, CurrentTime, SceneFrameRate, TotalFrames);
        var keyframe = TimelineEditingService.SetKeyframe(
            SelectedLayer.Model,
            SelectedProperty,
            Snap(CurrentTime),
            value,
            Duration);

        ReloadTracks();
        SelectedKeyframeId = keyframe.Id;
        RecordHistoryIfNeeded();
        StatusMessage = $"Keyframe added on {GetPropertyTitle(SelectedProperty)}";
    }

    [RelayCommand]
    private void DeleteCurrentKeyframe()
    {
        if (!CanEditPropertyTracks || SelectedLayer is null || SelectedKeyframeId is null)
        {
            return;
        }

        if (TimelineEditingService.RemoveKeyframe(SelectedLayer.Model, SelectedProperty, SelectedKeyframeId.Value))
        {
            SelectedKeyframeId = null;
            ReloadTracks();
            RecordHistoryIfNeeded();
            StatusMessage = "Keyframe removed";
        }
    }

    [RelayCommand]
    private void CaptureShapeKeyframe()
    {
        if (!CanCaptureShapeKeyframe || SelectedLayer is null)
        {
            return;
        }

        var shapeKeyframe = TimelineEditingService.CaptureShapeKeyframe(SelectedLayer.Model, CurrentTime, Duration);
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        OnPropertyChanged(nameof(ShapeTweenSummary));
        OnPropertyChanged(nameof(CanDeleteCurrentShapeKeyframe));
        StatusMessage = $"Morph key captured at {shapeKeyframe.Time:0.00}s";
    }

    [RelayCommand]
    private void DeleteCurrentShapeKeyframe()
    {
        if (!CanDeleteCurrentShapeKeyframe || SelectedLayer is null)
        {
            return;
        }

        var shapeKeyframe = FindCurrentShapeKeyframeModel();
        if (shapeKeyframe is null)
        {
            return;
        }

        if (!TimelineEditingService.RemoveShapeKeyframe(SelectedLayer.Model, shapeKeyframe.Id))
        {
            return;
        }

        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        OnPropertyChanged(nameof(ShapeTweenSummary));
        OnPropertyChanged(nameof(CanDeleteCurrentShapeKeyframe));
        StatusMessage = "Morph key removed";
    }

    [RelayCommand]
    private void AddFrameKeyframe()
    {
        if (!CanEditPropertyTracks || SelectedLayer is null)
        {
            return;
        }

        var frame = CurrentFrame;
        var frameTime = FrameTimelineService.FrameToTime(frame, SceneFrameRate);
        SeekFrame(frame);
        var selectedPropertyKeyframe = CreateFrameSnapshotKeyframes(SelectedLayer.Model, frameTime);
        FrameTimelineService.InsertContentKeyframe(SelectedLayer.Model, frame, TotalFrames);
        SelectedKeyframeId = selectedPropertyKeyframe?.Id;
        SelectionStartFrame = frame;
        SelectionEndFrame = frame;
        RefreshFrameAwareState();
        RecordHistoryIfNeeded();
        StatusMessage = $"Frame key added at {CurrentFrameLabel}";
    }

    [RelayCommand]
    private void AddBlankFrameKeyframe()
    {
        if (!CanEditPropertyTracks || SelectedLayer is null)
        {
            return;
        }

        SeekFrame(CurrentFrame);
        FrameTimelineService.InsertBlankKeyframe(SelectedLayer.Model, CurrentFrame, TotalFrames);
        SelectedKeyframeId = null;
        SelectionStartFrame = CurrentFrame;
        SelectionEndFrame = CurrentFrame;
        RefreshFrameAwareState();
        RecordHistoryIfNeeded();
        StatusMessage = $"Blank key added at {CurrentFrameLabel}";
    }

    [RelayCommand]
    private void ExtendFrameHold()
    {
        if (!CanEditPropertyTracks || SelectedLayer is null)
        {
            return;
        }

        SeekFrame(CurrentFrame);
        var targetFrame = SelectionStartFrame >= 0 && SelectionEndFrame >= 0
            ? Math.Max(SelectionStartFrame, SelectionEndFrame)
            : Math.Min(TotalFrames - 1, CurrentFrame + 1);
        var span = FrameTimelineService.ExtendSpanToFrame(SelectedLayer.Model, CurrentFrame, targetFrame, TotalFrames);
        SelectionStartFrame = span.StartFrame;
        SelectionEndFrame = span.EndFrame;
        RefreshFrameAwareState();
        RecordHistoryIfNeeded();
        StatusMessage = $"Frame hold extended to F{span.EndFrame + 1}";
    }

    [RelayCommand]
    private void CopyFrameRange()
    {
        if (!CanCopyFrameRange || SelectedLayer is null)
        {
            return;
        }

        _frameClipboard = FrameTimelineService.CopyRange(
            SelectedLayer.Model,
            SelectionStartFrame,
            SelectionEndFrame,
            TotalFrames,
            SceneFrameRate);
        RefreshFrameSelectionStateProperties();
        StatusMessage = $"Copied {SelectedFrameRangeSummary}";
    }

    [RelayCommand]
    private void PasteFrameRange()
    {
        if (!CanPasteFrameRange || SelectedLayer is null || _frameClipboard is null)
        {
            return;
        }

        var targetFrame = SelectionStartFrame >= 0 && SelectionEndFrame >= 0
            ? Math.Min(SelectionStartFrame, SelectionEndFrame)
            : CurrentFrame;
        FrameTimelineService.PasteRange(SelectedLayer.Model, targetFrame, _frameClipboard, TotalFrames, SceneFrameRate);
        SelectionStartFrame = targetFrame;
        SelectionEndFrame = Math.Min(TotalFrames - 1, targetFrame + _frameClipboard.FrameCount - 1);
        RefreshFrameAwareState();
        RecordHistoryIfNeeded();
        StatusMessage = $"Pasted frames at F{targetFrame + 1}";
    }

    [RelayCommand]
    private void RazorSelection()
    {
        if (!CanUseEditorialTools || SelectedLayer is null)
        {
            return;
        }

        var mediaAsset = GetSelectedMediaAssetModel();
        var newLayer = EditorialEditingService.RazorSplitLayer(SelectedLayer.Model, mediaAsset, CurrentTime, _document.Layers.Count);
        if (newLayer is null)
        {
            StatusMessage = "Razor split requires the playhead inside a media clip";
            return;
        }

        TimelineEditingService.AddLayer(_document, newLayer);
        RebuildLayers(newLayer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = "Clip split at playhead";
    }

    [RelayCommand]
    private void ApplyEditorialOperation(string? operation)
    {
        if (!CanUseEditorialTools || SelectedLayer is null || string.IsNullOrWhiteSpace(operation))
        {
            return;
        }

        var step = FrameTimelineService.FrameToTime(1, SceneFrameRate);
        var normalizedOperation = operation.Trim().ToLowerInvariant();
        var changed = normalizedOperation switch
        {
            "slip-back" => EditorialEditingService.Slip(SelectedLayer.Model, GetSelectedMediaAssetModel(), -step),
            "slip-forward" => EditorialEditingService.Slip(SelectedLayer.Model, GetSelectedMediaAssetModel(), step),
            "slide-back" => EditorialEditingService.Slide(_document.Layers, SelectedLayer.Id, -step),
            "slide-forward" => EditorialEditingService.Slide(_document.Layers, SelectedLayer.Id, step),
            "ripple-shorter" => EditorialEditingService.Ripple(_document.Layers, SelectedLayer.Id, -step),
            "ripple-longer" => EditorialEditingService.Ripple(_document.Layers, SelectedLayer.Id, step),
            _ => false
        };

        if (!changed)
        {
            StatusMessage = "Editorial operation could not be applied";
            return;
        }

        RebuildLayers(SelectedLayer.Id);
        RefreshAudioMeter();
        RecordHistoryIfNeeded();
        StatusMessage = normalizedOperation switch
        {
            "slip-back" => "Clip slipped earlier",
            "slip-forward" => "Clip slipped later",
            "slide-back" => "Clip slid earlier",
            "slide-forward" => "Clip slid later",
            "ripple-shorter" => "Ripple trim shortened",
            "ripple-longer" => "Ripple trim extended",
            _ => "Editorial operation applied"
        };
    }

    [RelayCommand]
    private void AddOrUpdateFrameLabel()
    {
        if (SelectedScene is null)
        {
            return;
        }

        if (FrameTimelineService.SetFrameLabel(SelectedScene.Model, CurrentFrame, FrameLabelEditor, TotalFrames))
        {
            ReloadFrameLabels();
            RecordHistoryIfNeeded();
            StatusMessage = string.IsNullOrWhiteSpace(FrameLabelEditor)
                ? $"Frame label removed at {CurrentFrameLabel}"
                : $"Frame label saved at {CurrentFrameLabel}";
        }
    }

    [RelayCommand]
    private void RemoveFrameLabel()
    {
        if (SelectedScene is null)
        {
            return;
        }

        if (FrameTimelineService.RemoveFrameLabel(SelectedScene.Model, CurrentFrame))
        {
            ReloadFrameLabels();
            RefreshFrameLabelEditor();
            RecordHistoryIfNeeded();
            StatusMessage = $"Frame label removed at {CurrentFrameLabel}";
        }
    }

    [RelayCommand]
    private void AddOrUpdateSceneMarker()
    {
        if (!CanEditSceneMarkers || SelectedScene is null)
        {
            return;
        }

        if (!SceneTimelineService.AddOrUpdateMarker(
                SelectedScene.Model,
                SelectedSceneMarker?.Id,
                CurrentFrame,
                MarkerNameEditor,
                SelectedMarkerKind,
                MarkerNotesEditor,
                MarkerColorEditor,
                TotalFrames))
        {
            return;
        }

        ReloadSceneMarkers(SelectedSceneMarker?.Id);
        SelectedScene.RefreshMetadata();
        RecordHistoryIfNeeded();
        RefreshSceneStateProperties();
        RefreshMonitorPreviews();
        StatusMessage = $"Marker saved at {CurrentFrameLabel}";
    }

    [RelayCommand]
    private void RemoveSelectedSceneMarker()
    {
        if (!CanRemoveSelectedSceneMarker || SelectedScene is null || SelectedSceneMarker is null)
        {
            return;
        }

        if (!SceneTimelineService.RemoveMarker(SelectedScene.Model, SelectedSceneMarker.Id))
        {
            return;
        }

        ReloadSceneMarkers();
        SelectedScene.RefreshMetadata();
        RecordHistoryIfNeeded();
        RefreshSceneStateProperties();
        RefreshMonitorPreviews();
        StatusMessage = "Marker removed";
    }

    [RelayCommand]
    private void SetSceneWorkAreaFromSelection()
    {
        if (!CanSetSceneWorkArea || SelectedScene is null)
        {
            return;
        }

        SceneTimelineService.SetWorkArea(SelectedScene.Model, SelectionStartFrame, SelectionEndFrame, TotalFrames);
        RefreshSceneTimelineEditors();
        RecordHistoryIfNeeded();
        RefreshSceneStateProperties();
        RefreshMonitorPreviews();
        StatusMessage = $"Work area set to {CurrentSceneWorkAreaLabel}";
    }

    [RelayCommand]
    private void ClearSceneWorkArea()
    {
        if (!CanEditSceneMarkers || SelectedScene is null)
        {
            return;
        }

        SceneTimelineService.ClearWorkArea(SelectedScene.Model, TotalFrames);
        RefreshSceneTimelineEditors();
        RecordHistoryIfNeeded();
        RefreshSceneStateProperties();
        RefreshMonitorPreviews();
        StatusMessage = "Work area reset to full scene range";
    }

    [RelayCommand]
    private void SetSceneInPoint()
    {
        if (!CanEditSceneMarkers || SelectedScene is null)
        {
            return;
        }

        SceneTimelineService.SetInPoint(SelectedScene.Model, CurrentFrame, TotalFrames);
        RefreshSceneTimelineEditors();
        RecordHistoryIfNeeded();
        RefreshSceneStateProperties();
        RefreshMonitorPreviews();
        StatusMessage = $"Scene in point set at {CurrentFrameLabel}";
    }

    [RelayCommand]
    private void SetSceneOutPoint()
    {
        if (!CanEditSceneMarkers || SelectedScene is null)
        {
            return;
        }

        SceneTimelineService.SetOutPoint(SelectedScene.Model, CurrentFrame, TotalFrames);
        RefreshSceneTimelineEditors();
        RecordHistoryIfNeeded();
        RefreshSceneStateProperties();
        RefreshMonitorPreviews();
        StatusMessage = $"Scene out point set at {CurrentFrameLabel}";
    }

    [RelayCommand]
    private void JumpToSceneInPoint()
    {
        if (SelectedScene is null)
        {
            return;
        }

        SeekFrame(CurrentSceneInFrame);
    }

    [RelayCommand]
    private void JumpToSceneOutPoint()
    {
        if (SelectedScene is null)
        {
            return;
        }

        SeekFrame(CurrentSceneOutFrame);
    }

    [RelayCommand]
    private void CaptureVisualState()
    {
        if (!CanEditVisualStates || SelectedLayer is null)
        {
            return;
        }

        var state = VisualStateEditingService.CaptureState(SelectedLayer.Model, VisualStateGroupNameEditor, VisualStateNameEditor, CurrentTime);
        var group = SelectedLayer.Model.VisualStateGroups.First(item => item.States.Any(candidate => candidate.Id == state.Id));
        ReloadVisualStateGroups(group.Id);
        ReloadVisualStates(state.Id);
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = $"Captured state {state.Name}";
    }

    [RelayCommand]
    private void ApplySelectedVisualState()
    {
        if (!CanApplySelectedVisualState || SelectedLayer is null || SelectedVisualStateGroup is null || SelectedVisualState is null)
        {
            return;
        }

        if (!VisualStateEditingService.ApplyState(SelectedLayer.Model, SelectedVisualStateGroup.Id, SelectedVisualState.Id))
        {
            return;
        }

        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        ReloadVisualStateGroups(SelectedVisualStateGroup.Id);
        ReloadVisualStates(SelectedVisualState.Id);
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = $"Applied state {SelectedVisualState.Name}";
    }

    [RelayCommand]
    private void DeleteSelectedVisualState()
    {
        if (!CanRemoveSelectedVisualState || SelectedLayer is null || SelectedVisualStateGroup is null || SelectedVisualState is null)
        {
            return;
        }

        if (!VisualStateEditingService.RemoveState(SelectedLayer.Model, SelectedVisualStateGroup.Id, SelectedVisualState.Id))
        {
            return;
        }

        ReloadVisualStateGroups(SelectedVisualStateGroup.Id);
        RecordHistoryIfNeeded();
        StatusMessage = "Visual state removed";
    }

    [RelayCommand]
    private void DeleteSelectedVisualStateGroup()
    {
        if (!CanRemoveSelectedVisualStateGroup || SelectedLayer is null || SelectedVisualStateGroup is null)
        {
            return;
        }

        if (!VisualStateEditingService.RemoveGroup(SelectedLayer.Model, SelectedVisualStateGroup.Id))
        {
            return;
        }

        ReloadVisualStateGroups();
        RecordHistoryIfNeeded();
        StatusMessage = "Visual state group removed";
    }

    [RelayCommand]
    private void MarkSourceMonitorIn()
    {
        SourceMonitorInTime = SourceMonitorTime;
        StatusMessage = $"Source in set to {SourceMonitorInTime:0.00}s";
    }

    [RelayCommand]
    private void MarkSourceMonitorOut()
    {
        SourceMonitorOutTime = SourceMonitorTime;
        StatusMessage = $"Source out set to {SourceMonitorOutTime:0.00}s";
    }

    [RelayCommand]
    private void ApplyFillPreset(string? fill)
    {
        if (string.IsNullOrWhiteSpace(fill))
        {
            return;
        }

        FillHexEditor = fill;
        if (!CanEditSelection)
        {
            StatusMessage = "Color preset applied";
        }
    }

    [RelayCommand]
    private void ApplyStrokePreset(string? stroke)
    {
        if (string.IsNullOrWhiteSpace(stroke))
        {
            return;
        }

        StrokeHexEditor = stroke;
        if (!CanEditSelection)
        {
            StatusMessage = "Stroke preset applied";
        }
    }

    [RelayCommand]
    private void IncreaseTextSize()
    {
        if (!CanEditTextMenu)
        {
            return;
        }

        InspectorFontSize = Math.Max(8d, InspectorFontSize + 2d);
        StatusMessage = "Text size increased";
    }

    [RelayCommand]
    private void DecreaseTextSize()
    {
        if (!CanEditTextMenu)
        {
            return;
        }

        InspectorFontSize = Math.Max(8d, InspectorFontSize - 2d);
        StatusMessage = "Text size decreased";
    }

    [RelayCommand]
    private void SetTimelineWorkspaceView(string? viewKey)
    {
        if (string.IsNullOrWhiteSpace(viewKey) ||
            !Enum.TryParse<TimelineWorkspaceView>(viewKey, true, out var view) ||
            SelectedTimelineView == view)
        {
            return;
        }

        SelectedTimelineView = view;
        StatusMessage = view == TimelineWorkspaceView.Frames
            ? "Frame exposure sheet focused"
            : "Property curves focused";
    }

    [RelayCommand]
    private void SetOnionSkinCount(string? countKey)
    {
        if (string.IsNullOrWhiteSpace(countKey))
        {
            return;
        }

        var parts = countKey.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var value))
        {
            return;
        }

        value = Math.Clamp(value, 0, 6);

        switch (parts[0].ToLowerInvariant())
        {
            case "before":
            case "prev":
                OnionSkinBefore = value;
                StatusMessage = $"Onion skin previous frames set to {value}";
                break;
            case "after":
            case "next":
                OnionSkinAfter = value;
                StatusMessage = $"Onion skin next frames set to {value}";
                break;
        }
    }

    public void ApplyFrameRulerInteraction(FrameTimelineRulerInteractionKind interactionKind, int frame)
    {
        var normalizedFrame = Math.Clamp(frame, 0, Math.Max(0, TotalFrames - 1));

        switch (interactionKind)
        {
            case FrameTimelineRulerInteractionKind.Playhead:
                SeekFrame(normalizedFrame);
                return;

            case FrameTimelineRulerInteractionKind.InPoint:
                SetSceneInPointFrame(normalizedFrame);
                return;

            case FrameTimelineRulerInteractionKind.OutPoint:
                SetSceneOutPointFrame(normalizedFrame);
                return;

            case FrameTimelineRulerInteractionKind.WorkAreaStart:
                SetSceneWorkAreaStartFrame(normalizedFrame);
                return;

            case FrameTimelineRulerInteractionKind.WorkAreaEnd:
                SetSceneWorkAreaEndFrame(normalizedFrame);
                return;

            case FrameTimelineRulerInteractionKind.OnionBefore:
                OnionSkinBefore = Math.Clamp(CurrentFrame - normalizedFrame, 0, 12);
                if (!_isInteractiveChange)
                {
                    StatusMessage = $"Onion skin previous frames set to {OnionSkinBefore}";
                }
                return;

            case FrameTimelineRulerInteractionKind.OnionAfter:
                OnionSkinAfter = Math.Clamp(normalizedFrame - CurrentFrame, 0, 12);
                if (!_isInteractiveChange)
                {
                    StatusMessage = $"Onion skin next frames set to {OnionSkinAfter}";
                }
                return;
        }
    }

    public void SetSceneInPointFrame(int frame)
    {
        if (!CanEditSceneMarkers || SelectedScene is null)
        {
            return;
        }

        SceneTimelineService.SetInPoint(SelectedScene.Model, frame, TotalFrames);
        RefreshSceneTimelineEditors();
        RecordHistoryIfNeeded();
        RefreshSceneStateProperties();
        if (!_isInteractiveChange)
        {
            RefreshMonitorPreviews();
            SeekFrame(CurrentSceneInFrame);
            StatusMessage = $"Scene in point set at {CurrentSceneInFrameLabel}";
        }
    }

    public void SetSceneOutPointFrame(int frame)
    {
        if (!CanEditSceneMarkers || SelectedScene is null)
        {
            return;
        }

        SceneTimelineService.SetOutPoint(SelectedScene.Model, frame, TotalFrames);
        RefreshSceneTimelineEditors();
        RecordHistoryIfNeeded();
        RefreshSceneStateProperties();
        if (!_isInteractiveChange)
        {
            RefreshMonitorPreviews();
            SeekFrame(CurrentSceneOutFrame);
            StatusMessage = $"Scene out point set at {CurrentSceneOutFrameLabel}";
        }
    }

    public void SetSceneWorkAreaStartFrame(int frame)
    {
        if (!CanEditSceneMarkers || SelectedScene is null)
        {
            return;
        }

        SceneTimelineService.SetWorkAreaStart(SelectedScene.Model, frame, TotalFrames);
        RefreshSceneTimelineEditors();
        RecordHistoryIfNeeded();
        RefreshSceneStateProperties();
        if (!_isInteractiveChange)
        {
            RefreshMonitorPreviews();
            StatusMessage = $"Work area start set to F{CurrentSceneWorkAreaStartFrame + 1}";
        }
    }

    public void SetSceneWorkAreaEndFrame(int frame)
    {
        if (!CanEditSceneMarkers || SelectedScene is null)
        {
            return;
        }

        SceneTimelineService.SetWorkAreaEnd(SelectedScene.Model, frame, TotalFrames);
        RefreshSceneTimelineEditors();
        RecordHistoryIfNeeded();
        RefreshSceneStateProperties();
        if (!_isInteractiveChange)
        {
            RefreshMonitorPreviews();
            StatusMessage = $"Work area end set to F{CurrentSceneWorkAreaEndFrame + 1}";
        }
    }

    public void SetTimelineDockHeightPixels(double height)
    {
        var normalized = Math.Clamp(height, MinTimelineDockHeight, MaxTimelineDockHeight);
        if (Math.Abs(TimelineDockHeight.Value - normalized) < 0.1d)
        {
            return;
        }

        TimelineDockHeight = new GridLength(normalized);
    }

    public void SetCanvasSize(double width, double height, string statusMessage = "Canvas resized")
    {
        if (!CanEditCanvasProperties || SelectedScene is null)
        {
            return;
        }

        var normalizedWidth = Math.Round(SceneEditingService.NormalizeCanvasExtent(width));
        var normalizedHeight = Math.Round(SceneEditingService.NormalizeCanvasExtent(height));
        if (Math.Abs(CanvasWidth - normalizedWidth) < 0.1d &&
            Math.Abs(CanvasHeight - normalizedHeight) < 0.1d)
        {
            return;
        }

        _suppressSceneSurfaceEditor = true;
        CanvasWidth = normalizedWidth;
        CanvasHeight = normalizedHeight;
        _suppressSceneSurfaceEditor = false;

        _document.CanvasWidth = normalizedWidth;
        _document.CanvasHeight = normalizedHeight;
        SelectedScene.Model.CanvasWidth = normalizedWidth;
        SelectedScene.Model.CanvasHeight = normalizedHeight;
        SelectedScene.RefreshMetadata();
        RefreshSceneStateProperties();
        RecordHistoryIfNeeded();
        if (!_isInteractiveChange)
        {
            StatusMessage = statusMessage;
        }
    }

    [RelayCommand]
    private void ApplyCanvasPreset(string? preset)
    {
        if (!TryParseCanvasPreset(preset, out var width, out var height))
        {
            return;
        }

        SetCanvasSize(width, height, $"Canvas preset {width:0} × {height:0} applied");
    }

    [RelayCommand]
    private void ApplyAnimateDocumentTargetDefaults()
    {
        if (IsEditingSymbol)
        {
            return;
        }

        AnimateDocumentProfileService.ApplyTargetDefaults(_document, AnimateDocumentTypeEditor);
        RefreshAnimateDocumentEditor();
        ReloadActionScriptSnippets(SelectedActionScriptSnippet?.Name);
        RefreshSceneStateProperties();
        RefreshPublishValidation();
        RecordHistoryIfNeeded();
        StatusMessage = $"{AnimateDocumentProfileService.GetDisplayName(AnimateDocumentTypeEditor)} defaults applied";
    }

    [RelayCommand]
    private void ToggleWorkspacePanel(string? panelKey)
    {
        if (string.IsNullOrWhiteSpace(panelKey))
        {
            return;
        }

        switch (panelKey.Trim().ToLowerInvariant())
        {
            case "left":
                LeftPanelMode = LeftPanelMode == WorkspacePanelMode.Hidden ? _leftRestoreMode : WorkspacePanelMode.Hidden;
                StatusMessage = LeftPanelMode == WorkspacePanelMode.Hidden
                    ? "Tools dock hidden"
                    : $"Tools dock restored as {GetWorkspacePanelStateLabel(LeftPanelMode)}";
                break;
            case "right":
                RightPanelMode = RightPanelMode == WorkspacePanelMode.Hidden ? _rightRestoreMode : WorkspacePanelMode.Hidden;
                StatusMessage = RightPanelMode == WorkspacePanelMode.Hidden
                    ? "Inspector hidden"
                    : $"Inspector restored as {GetWorkspacePanelStateLabel(RightPanelMode)}";
                break;
            case "timeline":
                TimelinePanelMode = TimelinePanelMode == WorkspacePanelMode.Hidden ? _timelineRestoreMode : WorkspacePanelMode.Hidden;
                StatusMessage = TimelinePanelMode == WorkspacePanelMode.Hidden
                    ? "Timeline hidden"
                    : $"Timeline restored as {GetWorkspacePanelStateLabel(TimelinePanelMode)}";
                break;
        }
    }

    private static bool TryParseCanvasPreset(string? preset, out double width, out double height)
    {
        width = 0;
        height = 0;
        if (string.IsNullOrWhiteSpace(preset))
        {
            return false;
        }

        var normalized = preset.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "hd":
            case "1280x720":
                width = 1280;
                height = 720;
                return true;
            case "fhd":
            case "1920x1080":
                width = 1920;
                height = 1080;
                return true;
            case "square":
            case "1080x1080":
                width = 1080;
                height = 1080;
                return true;
            case "story":
            case "1080x1920":
                width = 1080;
                height = 1920;
                return true;
        }

        var parts = normalized.Split(['x', '×'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2 ||
            !double.TryParse(parts[0], out width) ||
            !double.TryParse(parts[1], out height))
        {
            width = 0;
            height = 0;
            return false;
        }

        return true;
    }

    [RelayCommand]
    private void SetWorkspacePanelMode(string? panelModeKey)
    {
        if (string.IsNullOrWhiteSpace(panelModeKey))
        {
            return;
        }

        var parts = panelModeKey.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !TryParseWorkspacePanelMode(parts[1], out var mode))
        {
            return;
        }

        switch (parts[0].ToLowerInvariant())
        {
            case "left":
                LeftPanelMode = mode;
                break;
            case "right":
                RightPanelMode = mode;
                break;
            case "timeline":
                TimelinePanelMode = mode;
                break;
        }
    }

    [RelayCommand]
    private void RestoreSavedWorkspaceLayout()
    {
        RestoreWorkspaceLayout(updateStatus: true);
    }

    [RelayCommand]
    private void FocusWorkspaceArea(string? areaKey)
    {
        if (string.IsNullOrWhiteSpace(areaKey))
        {
            return;
        }

        switch (areaKey.Trim().ToLowerInvariant())
        {
            case "classic":
                ApplyWorkspacePreset(WorkspaceLayoutPreset.Classic, updateStatus: true);
                break;
            case "stage":
                ApplyWorkspacePreset(WorkspaceLayoutPreset.StageFocus, updateStatus: true);
                break;
            case "timeline":
                ApplyWorkspacePreset(WorkspaceLayoutPreset.TimelineFocus, updateStatus: true);
                break;
            case "tools":
                ApplyCustomWorkspaceFocus(
                    leftMode: WorkspacePanelMode.Docked,
                    rightMode: WorkspacePanelMode.Hidden,
                    timelineMode: WorkspacePanelMode.Overlay,
                    leftDockWidth: Math.Max(244, LeftDockWidth.Value),
                    rightDockWidth: RightDockWidth.Value,
                    timelineDockHeight: Math.Max(440, TimelineDockHeight.Value),
                    "Tools focus workspace applied");
                break;
            case "inspector":
                ApplyCustomWorkspaceFocus(
                    leftMode: WorkspacePanelMode.Hidden,
                    rightMode: WorkspacePanelMode.Docked,
                    timelineMode: WorkspacePanelMode.Overlay,
                    leftDockWidth: LeftDockWidth.Value,
                    rightDockWidth: Math.Max(304, RightDockWidth.Value),
                    timelineDockHeight: Math.Max(440, TimelineDockHeight.Value),
                    "Inspector focus workspace applied");
                break;
        }
    }

    [RelayCommand]
    private void ShowWorkspaceHelp()
    {
        StatusMessage = HelpMenuSummary;
    }

    [RelayCommand]
    private void ShowKeyboardShortcuts()
    {
        StatusMessage = WorkspaceShortcutSummary;
    }

    [RelayCommand]
    private void ShowSelectionContext()
    {
        StatusMessage = SelectedLayer is null ? "No layer selected" : ModifyMenuSummary;
    }

    [RelayCommand]
    private void AlignLeft()
    {
        AlignSelection(x: 0, y: null, "Aligned left");
    }

    [RelayCommand]
    private void AlignCenter()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        AlignSelection((CanvasWidth - SelectedLayer.Width) / 2d, null, "Aligned center");
    }

    [RelayCommand]
    private void AlignRight()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        AlignSelection(CanvasWidth - SelectedLayer.Width, null, "Aligned right");
    }

    [RelayCommand]
    private void AlignTop()
    {
        AlignSelection(null, 0, "Aligned top");
    }

    [RelayCommand]
    private void AlignMiddle()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        AlignSelection(null, (CanvasHeight - SelectedLayer.Height) / 2d, "Aligned middle");
    }

    [RelayCommand]
    private void AlignBottom()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        AlignSelection(null, CanvasHeight - SelectedLayer.Height, "Aligned bottom");
    }

    public void LoadDocument(TimelineDocument document, string label, TimelineDocumentFileFormat format = TimelineDocumentFileFormat.NativeProject)
    {
        StopPlayback(false);
        _document = document;
        _frameClipboard = null;
        _editingLibraryItemId = null;
        _editingSceneId = null;
        _editingSceneLayerId = null;
        _prototypeSnapshot = null;
        _prototypeVariables.Clear();
        IsPrototypeMode = false;
        PrototypeStatusMessage = "Prototype idle";
        _suppressButtonStateEditor = true;
        EditingButtonState = ButtonVisualState.Up;
        _suppressButtonStateEditor = false;
        SelectedDrawingTool = DrawingTool.Select;
        SceneEditingService.EnsureScenes(_document);
        AnimateDocumentProfileService.EnsureSettings(_document);
        LibraryManagementService.EnsureLibraryMetadata(_document);
        PublishProfileService.EnsureProfiles(_document);
        MotionPresetService.EnsureBuiltInPresets(_document);
        DocumentName = document.Name;
        FileLabel = label;
        CurrentDocumentFileFormat = format;
        if (TimelineDocumentFileService.ToAnimationExchangeFormat(format) is { } exchangeFormat)
        {
            SelectedAnimationExchangeFormat = exchangeFormat;
        }

        ReloadLibraryItems();
        ReloadComponentItems();
        ReloadMediaAssets();
        ReloadPublishProfiles();
        ReloadMotionPresets();
        ReloadScenes(document.ActiveSceneId);
        RefreshAnimateDocumentEditor();
        ReloadActionScriptSnippets();
        RefreshFrameSelectionStateProperties();
        RefreshSymbolEditingProperties();
        ResetHistory();
        HistoryEntries.Clear();
        ReloadMovieExplorer();
        OnPropertyChanged(nameof(HistoryPanelSummary));
        RefreshAnimationExchangePreviewCore(updateStatus: false);
        StatusMessage = "Document loaded";
    }

    public void SetDocumentLabel(string label, TimelineDocumentFileFormat? format = null)
    {
        FileLabel = label;
        if (format is not null)
        {
            CurrentDocumentFileFormat = format.Value;
        }
    }

    public void ApplyAnimationExchangeResult(string summary, IEnumerable<AnimationExchangeIssue> issues)
    {
        LastAnimationExchangeSummary = summary?.Trim() ?? string.Empty;
        AnimationExchangeIssues.Clear();
        foreach (var issue in issues)
        {
            AnimationExchangeIssues.Add(issue);
        }

        OnPropertyChanged(nameof(HasAnimationExchangeIssues));
        RefreshAnimationExchangePreviewCore(updateStatus: false);
        StatusMessage = string.IsNullOrWhiteSpace(LastAnimationExchangeSummary)
            ? "Animation interop updated"
            : LastAnimationExchangeSummary;
    }

    public TimelineDocument CreateExportDocumentSnapshot()
    {
        return BuildPersistedDocumentSnapshot();
    }

    private void SeedColorSwatches()
    {
        ColorSwatches.Clear();
        foreach (var swatch in new[]
                 {
                     new ColorSwatchViewModel("Stage Night", "#09111F"),
                     new ColorSwatchViewModel("Stage Indigo", "#182748"),
                     new ColorSwatchViewModel("Signal Cyan", "#24E5C1"),
                     new ColorSwatchViewModel("Vector Aqua", "#61E6FF"),
                     new ColorSwatchViewModel("Accent Blue", "#1F7DFF"),
                     new ColorSwatchViewModel("Solar Orange", "#FF8A4C"),
                     new ColorSwatchViewModel("Warm Gold", "#FFCC4D"),
                     new ColorSwatchViewModel("Studio White", "#F7F5ED"),
                     new ColorSwatchViewModel("Ink Slate", "#243651"),
                     new ColorSwatchViewModel("Fog Gray", "#74839A"),
                     new ColorSwatchViewModel("Rig Violet", "#8B5CF6"),
                     new ColorSwatchViewModel("Danger Coral", "#FF6E6E")
                 })
        {
            ColorSwatches.Add(swatch);
        }
    }

    private void AppendHistoryEntry(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var normalized = message.Trim();
        if (HistoryEntries.Count > 0 && string.Equals(HistoryEntries[0].Message, normalized, StringComparison.Ordinal))
        {
            return;
        }

        HistoryEntries.Insert(0, new HistoryEntryViewModel(normalized, DateTimeOffset.Now));
        while (HistoryEntries.Count > 60)
        {
            HistoryEntries.RemoveAt(HistoryEntries.Count - 1);
        }

        OnPropertyChanged(nameof(HistoryPanelSummary));
    }

    private bool TryFocusMovieExplorerNode(string focusToken)
    {
        var parts = focusToken.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        if (string.Equals(parts[0], "scene", StringComparison.Ordinal) &&
            parts.Length >= 2 &&
            Guid.TryParse(parts[1], out var sceneId))
        {
            return TryFocusScene(sceneId);
        }

        if (string.Equals(parts[0], "layer", StringComparison.Ordinal) &&
            parts.Length >= 3 &&
            Guid.TryParse(parts[1], out var layerSceneId) &&
            Guid.TryParse(parts[2], out var layerId))
        {
            return TryFocusLayer(layerSceneId, layerId);
        }

        if (string.Equals(parts[0], "marker", StringComparison.Ordinal) &&
            parts.Length >= 3 &&
            Guid.TryParse(parts[1], out var markerSceneId) &&
            Guid.TryParse(parts[2], out var markerId))
        {
            return TryFocusMarker(markerSceneId, markerId);
        }

        if (string.Equals(parts[0], "label", StringComparison.Ordinal) &&
            parts.Length >= 3 &&
            Guid.TryParse(parts[1], out var labelSceneId) &&
            Guid.TryParse(parts[2], out var labelId))
        {
            return TryFocusFrameLabel(labelSceneId, labelId);
        }

        if (string.Equals(parts[0], "library", StringComparison.Ordinal) &&
            parts.Length >= 2 &&
            Guid.TryParse(parts[1], out var libraryItemId))
        {
            SelectedLibraryItem = LibraryItems.FirstOrDefault(item => item.Id == libraryItemId);
            return SelectedLibraryItem is not null;
        }

        if (string.Equals(parts[0], "media", StringComparison.Ordinal) &&
            parts.Length >= 2 &&
            Guid.TryParse(parts[1], out var mediaAssetId))
        {
            SelectedMediaAsset = MediaAssets.FirstOrDefault(item => item.Id == mediaAssetId);
            return SelectedMediaAsset is not null;
        }

        if (string.Equals(parts[0], "motion", StringComparison.Ordinal) &&
            parts.Length >= 2 &&
            Guid.TryParse(parts[1], out var motionPresetId))
        {
            SelectedMotionPreset = MotionPresets.FirstOrDefault(item => item.Id == motionPresetId);
            return SelectedMotionPreset is not null;
        }

        if (string.Equals(parts[0], "behavior", StringComparison.Ordinal) &&
            parts.Length >= 4 &&
            Guid.TryParse(parts[1], out var behaviorSceneId) &&
            Guid.TryParse(parts[2], out var behaviorLayerId) &&
            Guid.TryParse(parts[3], out var behaviorId))
        {
            if (!TryFocusLayer(behaviorSceneId, behaviorLayerId))
            {
                return false;
            }

            SelectedBehavior = Behaviors.FirstOrDefault(item => item.Id == behaviorId);
            return SelectedBehavior is not null;
        }

        if (string.Equals(parts[0], "stateGroup", StringComparison.Ordinal) &&
            parts.Length >= 4 &&
            Guid.TryParse(parts[1], out var stateGroupSceneId) &&
            Guid.TryParse(parts[2], out var stateGroupLayerId) &&
            Guid.TryParse(parts[3], out var stateGroupId))
        {
            if (!TryFocusLayer(stateGroupSceneId, stateGroupLayerId))
            {
                return false;
            }

            SelectedVisualStateGroup = VisualStateGroups.FirstOrDefault(item => item.Id == stateGroupId);
            return SelectedVisualStateGroup is not null;
        }

        if (string.Equals(parts[0], "state", StringComparison.Ordinal) &&
            parts.Length >= 5 &&
            Guid.TryParse(parts[1], out var stateSceneId) &&
            Guid.TryParse(parts[2], out var stateLayerId) &&
            Guid.TryParse(parts[3], out var stateParentGroupId) &&
            Guid.TryParse(parts[4], out var stateId))
        {
            if (!TryFocusLayer(stateSceneId, stateLayerId))
            {
                return false;
            }

            SelectedVisualStateGroup = VisualStateGroups.FirstOrDefault(item => item.Id == stateParentGroupId);
            SelectedVisualState = VisualStates.FirstOrDefault(item => item.Id == stateId);
            return SelectedVisualState is not null;
        }

        return false;
    }

    private bool TryFocusScene(Guid sceneId)
    {
        var scene = Scenes.FirstOrDefault(item => item.Id == sceneId);
        if (scene is null)
        {
            return false;
        }

        SelectedScene = scene;
        return true;
    }

    private bool TryFocusLayer(Guid sceneId, Guid layerId)
    {
        if (!TryFocusScene(sceneId))
        {
            return false;
        }

        SelectedLayer = Layers.FirstOrDefault(item => item.Id == layerId);
        return SelectedLayer is not null;
    }

    private bool TryFocusMarker(Guid sceneId, Guid markerId)
    {
        if (!TryFocusScene(sceneId))
        {
            return false;
        }

        SelectedSceneMarker = SceneMarkers.FirstOrDefault(item => item.Id == markerId);
        if (SelectedSceneMarker is null)
        {
            return false;
        }

        Seek(FrameTimelineService.FrameToTime(SelectedSceneMarker.Frame, SceneFrameRate));
        return true;
    }

    private bool TryFocusFrameLabel(Guid sceneId, Guid labelId)
    {
        if (!TryFocusScene(sceneId))
        {
            return false;
        }

        var label = SceneFrameLabels.FirstOrDefault(item => item.Id == labelId);
        if (label is null)
        {
            return false;
        }

        Seek(FrameTimelineService.FrameToTime(label.Frame, SceneFrameRate));
        return true;
    }

    private void ReloadMovieExplorer()
    {
        MovieExplorerNodes.Clear();

        var search = MovieExplorerSearchText.Trim();
        foreach (var node in BuildMovieExplorerNodes(search))
        {
            MovieExplorerNodes.Add(node);
        }

        SelectedMovieExplorerNode = SelectedMovieExplorerNode is null
            ? MovieExplorerNodes.FirstOrDefault()
            : MovieExplorerNodes.SelectMany(FlattenMovieExplorerNodes)
                .FirstOrDefault(item => string.Equals(item.FocusToken, SelectedMovieExplorerNode.FocusToken, StringComparison.Ordinal))
              ?? MovieExplorerNodes.FirstOrDefault();

        OnPropertyChanged(nameof(MovieExplorerSummary));
        OnPropertyChanged(nameof(SelectedMovieExplorerSummary));
        OnPropertyChanged(nameof(CanFocusSelectedMovieExplorerNode));
    }

    private IEnumerable<MovieExplorerNodeViewModel> BuildMovieExplorerNodes(string search)
    {
        foreach (var scene in _document.Scenes.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            var sceneNode = new MovieExplorerNodeViewModel(
                scene.Name,
                "Scene",
                $"{scene.Layers.Count} layers • {scene.Duration:0.##}s • {scene.FrameRate:0.#} fps",
                $"scene:{scene.Id}");

            foreach (var layer in scene.Layers.OrderByDescending(item => item.ZIndex))
            {
                var layerNode = BuildLayerMovieExplorerNode(scene, layer);
                if (layerNode is not null)
                {
                    sceneNode.Children.Add(layerNode);
                }
            }

            foreach (var marker in scene.Markers.OrderBy(item => item.Frame).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                sceneNode.Children.Add(new MovieExplorerNodeViewModel(
                    string.IsNullOrWhiteSpace(marker.Name) ? $"Marker F{marker.Frame + 1}" : marker.Name,
                    "Marker",
                    $"{marker.Kind} • F{marker.Frame + 1}",
                    $"marker:{scene.Id}:{marker.Id}"));
            }

            foreach (var label in scene.FrameLabels.OrderBy(item => item.Frame).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                sceneNode.Children.Add(new MovieExplorerNodeViewModel(
                    string.IsNullOrWhiteSpace(label.Name) ? $"Label F{label.Frame + 1}" : label.Name,
                    "Frame Label",
                    $"F{label.Frame + 1}",
                    $"label:{scene.Id}:{label.Id}"));
            }

            if (MatchesMovieExplorerSearch(sceneNode, search))
            {
                yield return sceneNode;
            }
        }

        var libraryRoot = new MovieExplorerNodeViewModel("Library", "Panel", $"{_document.LibraryItems.Count} symbols");
        foreach (var item in _document.LibraryItems.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            libraryRoot.Children.Add(new MovieExplorerNodeViewModel(
                item.Name,
                "Library Item",
                $"{item.SymbolKind} • {item.FolderPath}",
                $"library:{item.Id}"));
        }

        if (MatchesMovieExplorerSearch(libraryRoot, search))
        {
            yield return libraryRoot;
        }

        var mediaRoot = new MovieExplorerNodeViewModel("Media", "Panel", $"{_document.MediaAssets.Count} assets");
        foreach (var asset in _document.MediaAssets.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            mediaRoot.Children.Add(new MovieExplorerNodeViewModel(
                asset.Name,
                "Media",
                $"{asset.Kind} • {asset.SourceLabel}",
                $"media:{asset.Id}"));
        }

        if (MatchesMovieExplorerSearch(mediaRoot, search))
        {
            yield return mediaRoot;
        }

        var motionRoot = new MovieExplorerNodeViewModel("Motion Presets", "Panel", $"{_document.MotionPresets.Count} presets");
        foreach (var preset in _document.MotionPresets.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            motionRoot.Children.Add(new MovieExplorerNodeViewModel(
                preset.Name,
                "Motion Preset",
                preset.Description ?? string.Empty,
                $"motion:{preset.Id}"));
        }

        if (MatchesMovieExplorerSearch(motionRoot, search))
        {
            yield return motionRoot;
        }
    }

    private MovieExplorerNodeViewModel? BuildLayerMovieExplorerNode(SceneModel scene, TimelineLayer layer)
    {
        var subtitleParts = new List<string>
        {
            layer.Kind.ToString()
        };
        if (!layer.IsVisible)
        {
            subtitleParts.Add("hidden");
        }

        if (layer.IsLocked)
        {
            subtitleParts.Add("locked");
        }

        var layerNode = new MovieExplorerNodeViewModel(
            layer.Name,
            "Layer",
            string.Join(" • ", subtitleParts),
            $"layer:{scene.Id}:{layer.Id}");

        foreach (var behavior in layer.Behaviors.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            layerNode.Children.Add(new MovieExplorerNodeViewModel(
                string.IsNullOrWhiteSpace(behavior.Name) ? behavior.Trigger.ToString() : behavior.Name,
                "Behavior",
                $"{behavior.Trigger} • {behavior.Action}",
                $"behavior:{scene.Id}:{layer.Id}:{behavior.Id}"));
        }

        foreach (var group in layer.VisualStateGroups.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            var groupNode = new MovieExplorerNodeViewModel(
                group.Name,
                "State Group",
                $"{group.States.Count} states",
                $"stateGroup:{scene.Id}:{layer.Id}:{group.Id}");

            foreach (var state in group.States.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                groupNode.Children.Add(new MovieExplorerNodeViewModel(
                    state.Name,
                    "State",
                    "Captured template",
                    $"state:{scene.Id}:{layer.Id}:{group.Id}:{state.Id}"));
            }

            layerNode.Children.Add(groupNode);
        }

        return layerNode;
    }

    private static bool MatchesMovieExplorerSearch(MovieExplorerNodeViewModel node, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return FlattenMovieExplorerNodes(node).Any(item =>
            item.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            item.KindLabel.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            item.Subtitle.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<MovieExplorerNodeViewModel> FlattenMovieExplorerNodes(MovieExplorerNodeViewModel node)
    {
        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var nested in FlattenMovieExplorerNodes(child))
            {
                yield return nested;
            }
        }
    }

    private void RefreshAnimationExchangePreviewCore(bool updateStatus)
    {
        try
        {
            var preview = AnimationExchangePreviewService.BuildPreview(
                CreateExportDocumentSnapshot(),
                SelectedAnimationExchangeFormat);
            AnimationExchangePreviewCode = preview.Code;
            AnimationExchangePreviewSummary = preview.Summary;
            AnimationExchangePreviewVisualSummary = preview.VisualSummary;
            AnimationExchangePreviewFileName = preview.SuggestedFileName;
            AnimationExchangePreviewDocument = preview.PreviewDocument;
            AnimationExchangePreviewIssues.Clear();
            foreach (var issue in preview.Issues)
            {
                AnimationExchangePreviewIssues.Add(issue);
            }

            OnPropertyChanged(nameof(HasAnimationExchangePreviewIssues));
            if (updateStatus)
            {
                StatusMessage = $"{AnimationExchangeService.GetDisplayName(SelectedAnimationExchangeFormat)} preview refreshed";
            }
        }
        catch (Exception ex)
        {
            AnimationExchangePreviewCode = string.Empty;
            AnimationExchangePreviewSummary = "Preview generation failed.";
            AnimationExchangePreviewVisualSummary = "Preview generation failed.";
            AnimationExchangePreviewFileName = string.Empty;
            AnimationExchangePreviewDocument = null;
            AnimationExchangePreviewIssues.Clear();
            AnimationExchangePreviewIssues.Add(new AnimationExchangeIssue
            {
                Severity = AnimationExchangeIssueSeverity.Error,
                Source = "Preview",
                Message = ex.Message
            });
            OnPropertyChanged(nameof(HasAnimationExchangePreviewIssues));
            if (updateStatus)
            {
                StatusMessage = "Animation preview failed";
            }
        }
    }

    public void HandlePrototypeTrigger(Guid? layerId, InteractionTriggerKind trigger)
    {
        if (!IsPrototypeMode || IsEditingSymbol)
        {
            return;
        }

        var sourceLayer = layerId is Guid sourceLayerId
            ? _document.Layers.FirstOrDefault(item => item.Id == sourceLayerId)
            : null;
        if (sourceLayer is not null)
        {
            ApplyPrototypeButtonState(sourceLayer, trigger);
        }

        ExecuteTriggeredBehaviors(sourceLayer?.Id, trigger, null);
        RebuildLayers(sourceLayer?.Id ?? SelectedLayer?.Id);
    }

    public void AddImportedMediaAsset(MediaAsset asset)
    {
        asset.Name = GetUniqueMediaName(asset.Name);
        _document.MediaAssets.Add(asset);
        ReloadMediaAssets(asset.Id);
        RecordHistoryIfNeeded();
        StatusMessage = asset.Kind == MediaAssetKind.Audio
            ? $"{asset.Name} imported into the media bin"
            : $"{asset.Name} imported as a video clip";
    }

    public PublishProfile? CreateSelectedPublishProfileSnapshot()
    {
        return SelectedPublishProfile?.Model is { } profile ? DocumentSerializer.Clone(profile) : null;
    }

    private TimelineDocument BuildPersistedDocumentSnapshot()
    {
        if (!IsEditingSymbol)
        {
            PersistActiveScene();
            return DocumentSerializer.Clone(_document);
        }

        var snapshot = DocumentSerializer.Clone(_document);
        if (_editingLibraryItemId is Guid libraryItemId)
        {
            var libraryItem = snapshot.LibraryItems.FirstOrDefault(item => item.Id == libraryItemId);
            if (libraryItem is not null)
            {
                TimelineEditingService.SetEditableSymbolLayers(
                    libraryItem,
                    _document.Layers,
                    Duration,
                    SceneFrameRate,
                    EditingButtonState);
                TimelineEditingService.SynchronizeLibraryItemInstances(snapshot, libraryItemId);
            }
        }

        var restoreScene = snapshot.Scenes.FirstOrDefault(scene => scene.Id == _editingSceneId)
            ?? snapshot.Scenes.FirstOrDefault(scene => scene.Id == snapshot.ActiveSceneId)
            ?? snapshot.Scenes.FirstOrDefault();
        if (restoreScene is not null)
        {
            SceneEditingService.ApplySceneToDocument(snapshot, restoreScene);
        }

        return snapshot;
    }

    private void EnterPrototypeMode()
    {
        if (IsEditingSymbol)
        {
            return;
        }

        StopPlayback(false);
        _prototypeSnapshot = CaptureSnapshot();
        _prototypeVariables.Clear();
        OnPropertyChanged(nameof(PrototypeVariableSummary));
        IsPrototypeMode = true;
        PrototypeStatusMessage = "Prototype armed";
        StatusMessage = "Prototype preview active";
    }

    private void ExitPrototypeMode()
    {
        var snapshot = _prototypeSnapshot;
        _prototypeSnapshot = null;
        _prototypeVariables.Clear();
        OnPropertyChanged(nameof(PrototypeVariableSummary));
        IsPrototypeMode = false;
        PrototypeStatusMessage = "Prototype idle";
        if (snapshot is not null)
        {
            ApplySnapshot(snapshot);
        }

        StatusMessage = "Prototype preview closed";
    }

    public void SelectLayer(Guid layerId)
    {
        var layer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (layer is not null)
        {
            SelectedLayer = layer;
        }
    }

    public void ToggleTimelineHierarchy(Guid layerId)
    {
        var layer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (layer is null)
        {
            return;
        }

        if (layer.Kind == LayerKind.Folder)
        {
            SelectLayer(layerId);
            ToggleSelectedFolderExpanded();
            return;
        }

        if (_collapsedTimelineLayers.Contains(layerId))
        {
            _collapsedTimelineLayers.Remove(layerId);
            StatusMessage = $"{layer.Name} curves expanded";
        }
        else
        {
            _collapsedTimelineLayers.Add(layerId);
            StatusMessage = $"{layer.Name} curves collapsed";
        }

        ReloadTimelineRows();
    }

    public void ToggleFrameHierarchy(Guid layerId)
    {
        var layer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (layer?.Kind != LayerKind.Folder)
        {
            return;
        }

        SelectLayer(layerId);
        ToggleSelectedFolderExpanded();
    }

    public void SelectFrameRange(Guid layerId, int startFrame, int endFrame)
    {
        if (SelectedLayer?.Id != layerId)
        {
            SelectLayer(layerId);
        }

        SelectedKeyframeId = null;
        _suppressFrameSelectionRefresh = true;
        SelectionStartFrame = Math.Clamp(startFrame, 0, Math.Max(0, TotalFrames - 1));
        SelectionEndFrame = Math.Clamp(endFrame, 0, Math.Max(0, TotalFrames - 1));
        _suppressFrameSelectionRefresh = false;
        UpdateFrameRowSelection();
        RefreshFrameSelectionStateProperties();
        var targetFrame = Math.Min(SelectionStartFrame, SelectionEndFrame);
        if (CurrentFrame != targetFrame)
        {
            SeekFrame(targetFrame);
        }
    }

    public void DeselectKeyframe()
    {
        SelectedKeyframeId = null;
    }

    public void Seek(double time)
    {
        CurrentTime = TimelineMath.Clamp(time, 0, Duration);
    }

    public void SeekFrame(int frame)
    {
        var normalized = Math.Clamp(frame, 0, Math.Max(0, TotalFrames - 1));
        Seek(FrameTimelineService.FrameToTime(normalized, SceneFrameRate));
    }

    public void Scrub(double time)
    {
        Seek(Snap(time));
    }

    public void SelectTrack(AnimatedProperty property)
    {
        SelectedProperty = property;
    }

    public void SelectTrack(Guid layerId, AnimatedProperty property)
    {
        SelectLayer(layerId);
        SelectedProperty = property;
    }

    public void SelectKeyframe(AnimatedProperty property, Guid keyframeId)
    {
        SelectedProperty = property;
        SelectedKeyframeId = keyframeId;
        var track = Tracks.FirstOrDefault(item => item.Property == property);
        var keyframe = track?.Keyframes.FirstOrDefault(item => item.Id == keyframeId);
        if (keyframe is not null)
        {
            Seek(keyframe.Time);
        }
    }

    public void SelectKeyframe(Guid layerId, AnimatedProperty property, Guid keyframeId)
    {
        SelectLayer(layerId);
        SelectKeyframe(property, keyframeId);
    }

    public void MoveKeyframe(AnimatedProperty property, Guid keyframeId, double time)
    {
        if (!CanEditSelection || SelectedLayer is null)
        {
            return;
        }

        var snappedTime = Snap(time);
        var existingKeyframe = SelectedLayer.Model.Tracks
            .FirstOrDefault(track => track.Property == property)?
            .Keyframes
            .FirstOrDefault(candidate => candidate.Id == keyframeId);
        if (existingKeyframe is not null && Math.Abs(existingKeyframe.Time - snappedTime) < 0.0001d)
        {
            if (Math.Abs(CurrentTime - snappedTime) > 0.0001d)
            {
                Seek(snappedTime);
            }

            return;
        }

        if (TimelineEditingService.MoveKeyframe(SelectedLayer.Model, property, keyframeId, snappedTime, Duration))
        {
            SelectedProperty = property;
            SelectedKeyframeId = keyframeId;
            if (_isInteractiveChange)
            {
                RefreshInteractiveTrackState(SelectedLayer.Model, property);
            }
            else
            {
                ReloadTracks();
                RecordHistoryIfNeeded();
            }

            Seek(snappedTime);
            if (!_isInteractiveChange)
            {
                StatusMessage = "Keyframe moved";
            }
        }
    }

    public void MoveKeyframe(Guid layerId, AnimatedProperty property, Guid keyframeId, double time)
    {
        if (SelectedLayer?.Id != layerId)
        {
            SelectLayer(layerId);
        }

        MoveKeyframe(property, keyframeId, time);
    }

    public void AddKeyframeAt(AnimatedProperty property, double time)
    {
        if (!CanEditPropertyTracks || SelectedLayer is null)
        {
            return;
        }

        SelectedProperty = property;
        Seek(time);
        var value = FrameTimelineService.SampleProperty(SelectedLayer.Model, property, CurrentTime, SceneFrameRate, TotalFrames);
        var keyframe = TimelineEditingService.SetKeyframe(SelectedLayer.Model, property, Snap(CurrentTime), value, Duration);
        ReloadTracks();
        SelectedKeyframeId = keyframe.Id;
        RecordHistoryIfNeeded();
        StatusMessage = "Keyframe inserted";
    }

    public void AddKeyframeAt(Guid layerId, AnimatedProperty property, double time)
    {
        SelectLayer(layerId);
        AddKeyframeAt(property, time);
    }

    public void UpdateLayerFrame(Guid layerId, Rect bounds)
    {
        var layer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (layer is null || layer.IsLocked || layer.Kind == LayerKind.Audio)
        {
            return;
        }

        SelectedLayer = layer;
        var shouldCreateKeyframe = ShouldCreateKeyframe();

        if (EditMultipleFramesEnabled && SelectionStartFrame >= 0 && SelectionEndFrame >= 0)
        {
            PowerEditingService.ApplyBoundsAcrossFrames(
                layer.Model,
                bounds.X,
                bounds.Y,
                bounds.Width,
                bounds.Height,
                SelectionStartFrame,
                SelectionEndFrame,
                SceneFrameRate,
                Duration);
        }
        else
        {
            TimelineEditingService.ApplyValue(layer.Model, AnimatedProperty.X, bounds.X, CurrentTime, shouldCreateKeyframe, Duration);
            TimelineEditingService.ApplyValue(layer.Model, AnimatedProperty.Y, bounds.Y, CurrentTime, shouldCreateKeyframe, Duration);
            TimelineEditingService.ApplyValue(layer.Model, AnimatedProperty.Width, bounds.Width, CurrentTime, shouldCreateKeyframe, Duration);
            TimelineEditingService.ApplyValue(layer.Model, AnimatedProperty.Height, bounds.Height, CurrentTime, shouldCreateKeyframe, Duration);
        }

        ReloadPreviewForLayer(layer);
        ReloadTracks();
        RefreshInspector();
        RecordHistoryIfNeeded();
        if (!_isInteractiveChange)
        {
            StatusMessage = "Layer transformed";
        }
    }

    public void UpdatePathPoint(Guid layerId, int pointIndex, Point documentPoint)
    {
        var layer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (layer is null || layer.IsLocked || layer.Kind != LayerKind.Path)
        {
            return;
        }

        SelectedLayer = layer;
        SelectedPathPointIndexEditor = pointIndex;
        var snapshot = FrameTimelineService.SampleLayer(layer.Model, CurrentTime, SceneFrameRate, TotalFrames)
            ?? TimelineInterpolationService.SampleLayer(layer.Model, CurrentTime);
        if (!TimelineEditingService.ApplyPathPoint(
                layer.Model,
                pointIndex,
                new VectorPointModel { X = documentPoint.X, Y = documentPoint.Y },
                snapshot,
                CurrentTime,
                ShouldCreateKeyframe(),
                Duration))
        {
            return;
        }

        ReloadPreviewForLayer(layer);
        RefreshSelectedPathPointEditors();
        RefreshInspector();
        RefreshSelectionStateProperties();
        RecordHistoryIfNeeded();
        OnPropertyChanged(nameof(ShapeTweenSummary));
        OnPropertyChanged(nameof(CanDeleteCurrentShapeKeyframe));
        if (!_isInteractiveChange)
        {
            StatusMessage = "Vector point moved";
        }
    }

    public void SelectPathPoint(Guid layerId, int pointIndex)
    {
        var layer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (layer is null || layer.Kind != LayerKind.Path)
        {
            return;
        }

        SelectedLayer = layer;
        SelectedPathPointIndexEditor = pointIndex;
        RefreshSelectedPathPointEditors();
        RefreshSelectionStateProperties();
        StatusMessage = "Vector point selected";
    }

    public void UpdatePathHandle(Guid layerId, int pointIndex, VectorHandleKind handleKind, Point documentPoint)
    {
        var layer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (layer is null || layer.IsLocked || layer.Kind != LayerKind.Path)
        {
            return;
        }

        SelectedLayer = layer;
        SelectedPathPointIndexEditor = pointIndex;
        var snapshot = FrameTimelineService.SampleLayer(layer.Model, CurrentTime, SceneFrameRate, TotalFrames)
            ?? TimelineInterpolationService.SampleLayer(layer.Model, CurrentTime);
        if (!TimelineEditingService.ApplyPathHandle(
                layer.Model,
                pointIndex,
                handleKind,
                new VectorPointModel { X = documentPoint.X, Y = documentPoint.Y },
                snapshot,
                CurrentTime,
                ShouldCreateKeyframe(),
                Duration))
        {
            return;
        }

        ReloadPreviewForLayer(layer);
        RefreshSelectedPathPointEditors();
        RefreshInspector();
        RefreshSelectionStateProperties();
        RecordHistoryIfNeeded();
        OnPropertyChanged(nameof(ShapeTweenSummary));
        OnPropertyChanged(nameof(CanDeleteCurrentShapeKeyframe));
        if (!_isInteractiveChange)
        {
            StatusMessage = "Bezier handle moved";
        }
    }

    public void UpdatePathPointWidthScale(Guid layerId, int pointIndex, double scale)
    {
        var layer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (layer is null || layer.IsLocked || layer.Kind != LayerKind.Path)
        {
            return;
        }

        SelectedLayer = layer;
        SelectedPathPointIndexEditor = pointIndex;
        if (!TimelineEditingService.SetLocalizedPathPointStrokeWidthScale(
                layer.Model,
                pointIndex,
                scale,
                CurrentTime,
                ShouldCreateKeyframe(),
                Duration))
        {
            return;
        }

        ReloadPreviewForLayer(layer);
        RefreshSelectedPathPointEditors();
        RefreshInspector();
        RefreshSelectionStateProperties();
        RecordHistoryIfNeeded();
        OnPropertyChanged(nameof(ShapeTweenSummary));
        OnPropertyChanged(nameof(CanDeleteCurrentShapeKeyframe));
        if (!_isInteractiveChange)
        {
            StatusMessage = "Stroke width adjusted";
        }
    }

    public void UpdateWarpPin(Guid layerId, Guid pinId, Point normalizedPoint)
    {
        var layer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (layer is null || layer.IsLocked || layer.Kind != LayerKind.Path)
        {
            return;
        }

        if (!LayerDeformationService.MoveWarpPin(layer.Model, pinId, normalizedPoint.X, normalizedPoint.Y))
        {
            return;
        }

        ReloadPreviewForLayer(layer);
        RefreshInspector();
        RecordHistoryIfNeeded();
        if (!_isInteractiveChange)
        {
            StatusMessage = "Warp pin moved";
        }
    }

    public void UpdateRigBoneHandle(Guid layerId, Guid boneId, RigBoneHandleKind handleKind, Point normalizedPoint)
    {
        var layer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (layer is null || layer.IsLocked || layer.Kind != LayerKind.Path)
        {
            return;
        }

        if (!LayerDeformationService.MoveRigBoneHandle(layer.Model, boneId, handleKind, normalizedPoint.X, normalizedPoint.Y))
        {
            return;
        }

        ReloadPreviewForLayer(layer);
        RefreshInspector();
        RecordHistoryIfNeeded();
        if (!_isInteractiveChange)
        {
            StatusMessage = handleKind == RigBoneHandleKind.Start ? "Rig bone start moved" : "Rig bone endpoint moved";
        }
    }

    public void BeginInteractiveChange(InteractiveChangeKind kind = InteractiveChangeKind.Generic)
    {
        _isInteractiveChange = true;
        _interactiveChangeKind = kind;
    }

    public void CommitInteractiveChange(string statusMessage)
    {
        if (!_isInteractiveChange)
        {
            return;
        }

        _isInteractiveChange = false;
        _interactiveChangeKind = InteractiveChangeKind.Generic;
        RefreshEditorStateAfterInteractiveChange();
        RecordHistoryIfNeeded();
        StatusMessage = statusMessage;
    }

    public void AddLayerFromPalette(LayerKind kind, Point position, AvaloniaControlKind? avaloniaControlKind = null)
    {
        if (kind == LayerKind.AvaloniaControl && avaloniaControlKind is AvaloniaControlKind controlKind)
        {
            AddAvaloniaControlLayer(controlKind, position);
            return;
        }

        AddLayer(kind, position);
    }

    public void CreateLayerFromCanvas(DrawingTool tool, Rect bounds, IReadOnlyList<Point> points)
    {
        TimelineLayer? layer = tool switch
        {
            DrawingTool.Rectangle => CreateCanvasShapeLayer(LayerKind.Rectangle, "Vector Card", bounds),
            DrawingTool.Ellipse => CreateCanvasShapeLayer(LayerKind.Ellipse, "Vector Orb", bounds),
            DrawingTool.PolyStar => CreatePolyStarLayer("Signal Star", bounds),
            DrawingTool.Text => CreateCanvasShapeLayer(LayerKind.Text, "Canvas Caption", bounds),
            DrawingTool.Line => CreateVectorStrokeLayer("Vector Line", tool, points, false, Math.Max(2, StrokeThicknessEditor)),
            DrawingTool.Pen => CreateVectorStrokeLayer("Pen Path", tool, points, false, Math.Max(2.2d, StrokeThicknessEditor)),
            DrawingTool.Brush => CreateVectorStrokeLayer("Brush Stroke", tool, points, false, Math.Max(6d, StrokeThicknessEditor)),
            DrawingTool.Pencil => CreateVectorStrokeLayer("Pencil Stroke", tool, points, false, Math.Max(2d, StrokeThicknessEditor)),
            _ => null
        };

        if (layer is null)
        {
            return;
        }

        TimelineEditingService.AddLayer(_document, layer);
        RebuildLayers(layer.Id);
        StatusMessage = $"{layer.Name} added";
    }

    public void ApplyLassoSelection(IReadOnlyList<Guid> layerIds)
    {
        if (layerIds.Count == 0)
        {
            SelectedLayer = null;
            StatusMessage = "Lasso selection cleared";
            return;
        }

        SelectLayer(layerIds[0]);
        StatusMessage = layerIds.Count == 1
            ? $"{SelectedLayer?.Name ?? "Layer"} selected with lasso"
            : $"{SelectedLayer?.Name ?? "Layer"} selected from {layerIds.Count} lasso matches";
    }

    public void ApplyCanvasErase(Guid layerId, Point documentPoint, double radius)
    {
        var layer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (layer is null || layer.IsLocked)
        {
            return;
        }

        SelectedLayer = layer;
        var snapshot = FrameTimelineService.SampleLayer(layer.Model, CurrentTime, SceneFrameRate, TotalFrames)
            ?? TimelineInterpolationService.SampleLayer(layer.Model, CurrentTime);
        var result = ToolAuthoringService.ApplyEraser(
            layer.Model,
            snapshot,
            new VectorPointModel { X = documentPoint.X, Y = documentPoint.Y },
            radius,
            EraserToolModeEditor,
            CurrentTime,
            ShouldCreateKeyframe(),
            Duration);
        if (!result.Changed)
        {
            return;
        }

        if (result.RemoveLayer)
        {
            var removedId = layer.Id;
            TimelineEditingService.RemoveLayer(_document, removedId);
            RebuildLayers(_document.Layers.LastOrDefault()?.Id);
            RecordHistoryIfNeeded();
            StatusMessage = $"{layer.Name} erased";
            return;
        }

        ReloadPreviewForLayer(layer);
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = $"{EraserToolModeEditor} erase applied";
    }

    public void SampleLayerStyle(Guid layerId)
    {
        var sourceLayer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (sourceLayer is null)
        {
            return;
        }

        if (SelectedLayer is null || SelectedLayer.Id == layerId || !CanEditSelection)
        {
            SelectLayer(layerId);
            StatusMessage = $"Style sampled from {sourceLayer.Name}";
            return;
        }

        CopyStyleSurface(sourceLayer.Model.Style, SelectedLayer.Model.Style);
        ReloadPreviewForLayer(SelectedLayer);
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = $"Sampled style from {sourceLayer.Name} into {SelectedLayer.Name}";
    }

    public void ApplyStageStyle(Guid layerId, CanvasStyleApplicationKind applicationKind)
    {
        var targetLayer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (targetLayer is null)
        {
            return;
        }

        var shouldSelectTarget = SelectedLayer is null || SelectedLayer.Id == layerId;

        if (applicationKind == CanvasStyleApplicationKind.Fill)
        {
            var snapshot = FrameTimelineService.SampleLayer(targetLayer.Model, CurrentTime, SceneFrameRate, TotalFrames)
                ?? TimelineInterpolationService.SampleLayer(targetLayer.Model, CurrentTime);
            var decision = PaintBucketToolService.AnalyzeFillTarget(snapshot, PaintBucketGapSizeEditor);
            if (!decision.CanApply)
            {
                StatusMessage = $"Paint Bucket gap is too wide for {PaintBucketGapSizeEditor} closing";
                return;
            }

            ApplyFillSurface(targetLayer, decision.ClosePath, PaintBucketLockFillEditor);
        }
        else
        {
            ApplyStrokeSurface(targetLayer);
        }

        ReloadPreviewForLayer(targetLayer);
        if (shouldSelectTarget)
        {
            SelectLayer(layerId);
        }

        RecordHistoryIfNeeded();
        StatusMessage = applicationKind == CanvasStyleApplicationKind.Fill
            ? PaintBucketLockFillEditor
                ? $"Fill applied to {targetLayer.Name} with Lock Fill"
                : $"Fill applied to {targetLayer.Name}"
            : $"Stroke applied to {targetLayer.Name}";
    }

    private void AddLayer(LayerKind kind, Point position)
    {
        var layer = kind switch
        {
            LayerKind.Rectangle => TimelineEditingService.CreateLayer(kind, "Neon Card", "#24E5C1", string.Empty, position.X, position.Y, _document.Layers.Count),
            LayerKind.Ellipse => TimelineEditingService.CreateLayer(kind, "Solar Orb", "#FF8A4C", string.Empty, position.X, position.Y, _document.Layers.Count),
            LayerKind.Text => TimelineEditingService.CreateLayer(kind, "New Headline", "#F7F5ED", "NEW MOTION", position.X, position.Y, _document.Layers.Count),
            _ => TimelineEditingService.CreateLayer(kind, "Layer", "#FFFFFF", string.Empty, position.X, position.Y, _document.Layers.Count)
        };

        TimelineEditingService.AddLayer(_document, layer);
        RebuildLayers(layer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = $"{layer.Name} added";
    }

    private void AddAvaloniaControlLayer(AvaloniaControlKind controlKind, Point position)
    {
        var layer = TimelineEditingService.CreateAvaloniaControlLayer(
            controlKind,
            GetDefaultAvaloniaControlName(controlKind),
            GetDefaultAvaloniaControlFill(controlKind),
            "#E6F1FF",
            GetDefaultAvaloniaControlText(controlKind),
            position.X,
            position.Y,
            _document.Layers.Count);

        TimelineEditingService.AddLayer(_document, layer);
        RebuildLayers(layer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = $"{layer.Name} added";
    }

    private static string GetDefaultAvaloniaControlName(AvaloniaControlKind controlKind)
    {
        return controlKind switch
        {
            AvaloniaControlKind.Border => "Card Surface",
            AvaloniaControlKind.TextBlock => "Text Block",
            AvaloniaControlKind.TextBox => "Text Input",
            AvaloniaControlKind.CheckBox => "Check Option",
            AvaloniaControlKind.ToggleButton => "Toggle Action",
            AvaloniaControlKind.ProgressBar => "Progress Track",
            AvaloniaControlKind.ComboBox => "Dropdown",
            AvaloniaControlKind.ListBox => "Selectable List",
            AvaloniaControlKind.TabControl => "Tabbed Surface",
            AvaloniaControlKind.Grid => "Grid Layout",
            AvaloniaControlKind.StackPanel => "Stack Layout",
            AvaloniaControlKind.PathIcon => "Icon Visual",
            _ => controlKind.ToString()
        };
    }

    private static string GetDefaultAvaloniaControlText(AvaloniaControlKind controlKind)
    {
        return controlKind switch
        {
            AvaloniaControlKind.Button => "Call To Action",
            AvaloniaControlKind.TextBlock => "Avalonia Visual",
            AvaloniaControlKind.TextBox => "Editable text",
            AvaloniaControlKind.CheckBox => "Enable option",
            AvaloniaControlKind.ToggleButton => "Prototype",
            AvaloniaControlKind.ComboBox => "Selected Item",
            AvaloniaControlKind.ListBox => "Item 1|Item 2|Item 3",
            AvaloniaControlKind.TabControl => "Overview|Settings|Export",
            AvaloniaControlKind.Grid => "2x2 Grid",
            AvaloniaControlKind.StackPanel => "Vertical Stack",
            AvaloniaControlKind.PathIcon => "Icon",
            AvaloniaControlKind.Image => "Image Frame",
            AvaloniaControlKind.Panel => "Layout Host",
            _ => controlKind.ToString()
        };
    }

    private static string AppendActionScriptSnippet(string existing, string snippet)
    {
        var normalizedSnippet = snippet?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedSnippet))
        {
            return existing ?? string.Empty;
        }

        return string.IsNullOrWhiteSpace(existing)
            ? normalizedSnippet
            : $"{existing.TrimEnd()}{Environment.NewLine}{normalizedSnippet}";
    }

    private void ApplyFillSurface(LayerViewModel targetLayer, bool closePathForFill, bool preserveGradientTransform)
    {
        var previousGradientKind = targetLayer.Model.Style.GradientKind;
        var previousGradientAngle = targetLayer.Model.Style.GradientAngle;
        var previousGradientCenterX = targetLayer.Model.Style.GradientCenterX;
        var previousGradientCenterY = targetLayer.Model.Style.GradientCenterY;
        var previousGradientScaleX = targetLayer.Model.Style.GradientScaleX;
        var previousGradientScaleY = targetLayer.Model.Style.GradientScaleY;
        var previousClosedState = targetLayer.Model.Style.IsClosed;
        targetLayer.Model.Style.HasFill = FillEnabledEditor;
        targetLayer.Model.Style.Fill = FillHexEditor;
        targetLayer.Model.Style.UseGradient = UseGradientEditor;
        targetLayer.Model.Style.GradientKind = GradientKindEditor;
        targetLayer.Model.Style.GradientAngle = GradientAngleEditor;
        targetLayer.Model.Style.GradientCenterX = GradientCenterXEditor;
        targetLayer.Model.Style.GradientCenterY = GradientCenterYEditor;
        targetLayer.Model.Style.GradientScaleX = GradientScaleXEditor;
        targetLayer.Model.Style.GradientScaleY = GradientScaleYEditor;
        targetLayer.Model.Style.GradientFrom = GradientFromEditor;
        targetLayer.Model.Style.GradientTo = GradientToEditor;
        if (preserveGradientTransform && targetLayer.Model.Style.UseGradient)
        {
            targetLayer.Model.Style.GradientKind = previousGradientKind;
            targetLayer.Model.Style.GradientAngle = previousGradientAngle;
            targetLayer.Model.Style.GradientCenterX = previousGradientCenterX;
            targetLayer.Model.Style.GradientCenterY = previousGradientCenterY;
            targetLayer.Model.Style.GradientScaleX = previousGradientScaleX;
            targetLayer.Model.Style.GradientScaleY = previousGradientScaleY;
        }

        targetLayer.Model.Style.IsClosed = closePathForFill || previousClosedState;
    }

    private void ApplyStrokeSurface(LayerViewModel targetLayer)
    {
        targetLayer.Model.Style.HasStroke = StrokeEnabledEditor;
        targetLayer.Model.Style.Stroke = StrokeHexEditor;
        targetLayer.Model.Style.StrokeThickness = StrokeThicknessEditor;
        targetLayer.Model.Style.StrokeCapStyle = StrokeCapStyleEditor;
        targetLayer.Model.Style.StrokeJoinStyle = StrokeJoinStyleEditor;
        targetLayer.Model.Style.StrokeMiterLimit = StrokeMiterLimitEditor;
    }

    private static void CopyStyleSurface(LayerStyle source, LayerStyle target)
    {
        target.DrawingMode = source.DrawingMode;
        target.PrimitiveShape = source.PrimitiveShape;
        target.HasFill = source.HasFill;
        target.Fill = source.Fill;
        target.HasStroke = source.HasStroke;
        target.Stroke = source.Stroke;
        target.StrokeThickness = source.StrokeThickness;
        target.StrokeCapStyle = source.StrokeCapStyle;
        target.StrokeJoinStyle = source.StrokeJoinStyle;
        target.StrokeMiterLimit = source.StrokeMiterLimit;
        target.UseGradient = source.UseGradient;
        target.GradientKind = source.GradientKind;
        target.GradientAngle = source.GradientAngle;
        target.GradientCenterX = source.GradientCenterX;
        target.GradientCenterY = source.GradientCenterY;
        target.GradientScaleX = source.GradientScaleX;
        target.GradientScaleY = source.GradientScaleY;
        target.GradientFrom = source.GradientFrom;
        target.GradientTo = source.GradientTo;
        target.CornerRadius = source.CornerRadius;
        target.CornerRadiusTopLeft = source.CornerRadiusTopLeft;
        target.CornerRadiusTopRight = source.CornerRadiusTopRight;
        target.CornerRadiusBottomRight = source.CornerRadiusBottomRight;
        target.CornerRadiusBottomLeft = source.CornerRadiusBottomLeft;
        target.IsClosed = source.IsClosed;
        target.EllipseStartAngle = source.EllipseStartAngle;
        target.EllipseSweepAngle = source.EllipseSweepAngle;
        target.PathPoints = VectorPathService.ClonePoints(source.PathPoints);
        target.PolyStarSides = source.PolyStarSides;
        target.PolyStarInnerRadius = source.PolyStarInnerRadius;
        target.PolyStarIsStar = source.PolyStarIsStar;
    }

    private static string GetDefaultAvaloniaControlFill(AvaloniaControlKind controlKind)
    {
        return controlKind switch
        {
            AvaloniaControlKind.Button => "#1F7DFF",
            AvaloniaControlKind.TextBlock => "#F7F5ED",
            AvaloniaControlKind.TextBox => "#25364B",
            AvaloniaControlKind.CheckBox => "#263953",
            AvaloniaControlKind.ToggleButton => "#8B5CF6",
            AvaloniaControlKind.Slider => "#283D57",
            AvaloniaControlKind.ProgressBar => "#204A62",
            AvaloniaControlKind.ComboBox => "#304860",
            AvaloniaControlKind.ListBox => "#233B54",
            AvaloniaControlKind.TabControl => "#2E355A",
            AvaloniaControlKind.Grid => "#233149",
            AvaloniaControlKind.StackPanel => "#1C3552",
            AvaloniaControlKind.PathIcon => "#1B2A45",
            AvaloniaControlKind.Image => "#3B445A",
            AvaloniaControlKind.Panel => "#1E2D44",
            _ => "#243651"
        };
    }

    private void SetLayerBounds(TimelineLayer layer, Rect bounds)
    {
        foreach (var propertyValue in new[]
                 {
                     (AnimatedProperty.X, bounds.X),
                     (AnimatedProperty.Y, bounds.Y),
                     (AnimatedProperty.Width, bounds.Width),
                     (AnimatedProperty.Height, bounds.Height)
                 })
        {
            TimelineEditingService.ApplyValue(layer, propertyValue.Item1, propertyValue.Item2, 0, true, Duration);
        }
    }

    private void ApplyActiveStyle(TimelineLayer layer, bool keepText)
    {
        layer.Style.DrawingMode = ShapeDrawingModeEditor;
        layer.Style.HasFill = FillEnabledEditor;
        layer.Style.Fill = ResolveFillPreset();
        layer.Style.HasStroke = StrokeEnabledEditor;
        layer.Style.Stroke = ResolveStrokePreset();
        layer.Style.StrokeThickness = Math.Max(1, StrokeThicknessEditor);
        layer.Style.UseGradient = UseGradientEditor;
        layer.Style.GradientKind = GradientKindEditor;
        layer.Style.GradientAngle = GradientAngleEditor;
        layer.Style.GradientCenterX = GradientCenterXEditor;
        layer.Style.GradientCenterY = GradientCenterYEditor;
        layer.Style.GradientScaleX = GradientScaleXEditor;
        layer.Style.GradientScaleY = GradientScaleYEditor;
        layer.Style.GradientFrom = ResolveGradientFromPreset();
        layer.Style.GradientTo = ResolveGradientToPreset();
        layer.Style.StrokeCapStyle = StrokeCapStyleEditor;
        layer.Style.StrokeJoinStyle = StrokeJoinStyleEditor;
        layer.Style.StrokeMiterLimit = Math.Max(1d, StrokeMiterLimitEditor);
        layer.Style.CornerRadius = Math.Max(0d, InspectorCornerRadius);
        layer.Style.CornerRadiusTopLeft = Math.Max(0d, CornerRadiusTopLeftEditor);
        layer.Style.CornerRadiusTopRight = Math.Max(0d, CornerRadiusTopRightEditor);
        layer.Style.CornerRadiusBottomRight = Math.Max(0d, CornerRadiusBottomRightEditor);
        layer.Style.CornerRadiusBottomLeft = Math.Max(0d, CornerRadiusBottomLeftEditor);
        layer.Style.EllipseStartAngle = NormalizeShapeAngle(EllipseStartAngleEditor);
        layer.Style.EllipseSweepAngle = Math.Clamp(EllipseSweepAngleEditor, -360d, 360d);
        layer.Style.PolyStarSides = Math.Clamp(PolyStarSidesEditor, 3, 16);
        layer.Style.PolyStarInnerRadius = TimelineMath.Clamp(PolyStarInnerRadiusEditor, 0.08d, 0.95d);
        layer.Style.PolyStarIsStar = PolyStarIsStarEditor;

        if (!keepText)
        {
            layer.Style.Text = string.Empty;
        }

        PrimitiveShapeService.NormalizeStyle(layer.Style);
    }

    private string ResolveFillPreset()
    {
        return string.IsNullOrWhiteSpace(FillHexEditor) ? "#F7F5ED" : FillHexEditor.Trim();
    }

    private string ResolveStrokePreset()
    {
        return string.IsNullOrWhiteSpace(StrokeHexEditor) ? "#FFFFFF" : StrokeHexEditor.Trim();
    }

    private string ResolveGradientFromPreset()
    {
        return string.IsNullOrWhiteSpace(GradientFromEditor) ? ResolveFillPreset() : GradientFromEditor.Trim();
    }

    private string ResolveGradientToPreset()
    {
        return string.IsNullOrWhiteSpace(GradientToEditor) ? ResolveStrokePreset() : GradientToEditor.Trim();
    }

    private string ResolveTextPreset()
    {
        return string.IsNullOrWhiteSpace(TextEditor) ? "NEW MOTION" : TextEditor;
    }

    private TimelineLayer CreateCanvasShapeLayer(LayerKind kind, string name, Rect bounds)
    {
        var normalizedBounds = kind == LayerKind.Text && (bounds.Width < 48 || bounds.Height < 48)
            ? new Rect(bounds.X, bounds.Y, 320, 92)
            : bounds;
        var text = kind == LayerKind.Text ? ResolveTextPreset() : string.Empty;
        var layer = TimelineEditingService.CreateLayer(kind, name, ResolveFillPreset(), text, normalizedBounds.X, normalizedBounds.Y, _document.Layers.Count);
        SetLayerBounds(layer, normalizedBounds);
        ApplyActiveStyle(layer, keepText: kind == LayerKind.Text);
        layer.Style.PrimitiveShape = kind switch
        {
            LayerKind.Rectangle => PrimitiveShapeType.Rectangle,
            LayerKind.Ellipse => PrimitiveShapeType.Ellipse,
            _ => PrimitiveShapeType.None
        };
        PrimitiveShapeService.NormalizeStyle(layer.Style);
        return layer;
    }

    private TimelineLayer CreatePolyStarLayer(string name, Rect bounds)
    {
        var points = PrimitiveShapeService.CreatePolyStarPoints(PolyStarSidesEditor, PolyStarInnerRadiusEditor, PolyStarIsStarEditor);
        var layer = TimelineEditingService.CreatePathLayer(
            name,
            ResolveFillPreset(),
            ResolveStrokePreset(),
            points,
            isClosed: true,
            _document.Layers.Count,
            Math.Max(1.2d, StrokeThicknessEditor),
            UseGradientEditor,
            ResolveGradientFromPreset(),
            ResolveGradientToPreset(),
            GradientKindEditor,
            GradientAngleEditor,
            FillEnabledEditor,
            StrokeEnabledEditor,
            StrokeCapStyleEditor,
            StrokeJoinStyleEditor,
            StrokeMiterLimitEditor);
        SetLayerBounds(layer, bounds);
        ApplyActiveStyle(layer, keepText: false);
        layer.Style.PrimitiveShape = PrimitiveShapeType.PolyStar;
        layer.Style.IsClosed = true;
        PrimitiveShapeService.UpdatePolyStarGeometry(layer);
        return layer;
    }

    private TimelineLayer CreateVectorStrokeLayer(string name, DrawingTool tool, IReadOnlyList<Point> points, bool isClosed, double strokeThickness)
    {
        var documentPoints = points.Select(point => new VectorPointModel { X = point.X, Y = point.Y }).ToList();
        List<VectorPointModel> sampledPoints = tool switch
        {
            DrawingTool.Brush => VectorPathService.SimplifyStroke(documentPoints, 4d),
            DrawingTool.Pencil => ToolAuthoringService.RefinePencilStroke(documentPoints, PencilToolModeEditor),
            _ => VectorPathService.SimplifyStroke(documentPoints, 8d)
        };
        VectorStrokeProfileService.ApplyProfile(sampledPoints, VectorStrokeProfilePresetEditor, VectorStrokeProfileStrengthEditor, isClosed);
        return TimelineEditingService.CreatePathLayer(
            name,
            ResolveFillPreset(),
            ResolveStrokePreset(),
            sampledPoints,
            isClosed,
            _document.Layers.Count,
            strokeThickness,
            UseGradientEditor,
            GradientFromEditor,
            GradientToEditor,
            GradientKindEditor,
            GradientAngleEditor,
            FillEnabledEditor,
            StrokeEnabledEditor,
            StrokeCapStyleEditor,
            StrokeJoinStyleEditor,
            StrokeMiterLimitEditor);
    }

    private void RebuildLayers(Guid? selectionId)
    {
        LayerParentingService.Normalize(_document.Layers);
        foreach (var documentLayer in _document.Layers)
        {
            LayerDeformationService.Normalize(documentLayer);
        }

        Layers.Clear();
        var libraryLookup = _document.LibraryItems.ToDictionary(item => item.Id);
        var mediaLookup = _document.MediaAssets.ToDictionary(item => item.Id);
        var hierarchy = LayerHierarchyService.Flatten(_document.Layers);
        var worldSnapshots = LayerParentingService.BuildWorldSnapshots(_document.Layers, CurrentTime, SceneFrameRate, TotalFrames);

        foreach (var entry in hierarchy)
        {
            var layer = entry.Layer;
            var layerViewModel = new LayerViewModel(layer);
            layerViewModel.ApplyHierarchyState(entry.Depth, entry.HasChildren);
            if (layer.SourceLibraryItemId is Guid libraryItemId &&
                libraryLookup.TryGetValue(libraryItemId, out var libraryItem))
            {
                layerViewModel.SetLibraryLink(libraryItem.Name, libraryItem.SymbolKind);
            }
            else if (layer.Media.SourceMediaAssetId is Guid mediaAssetId &&
                     mediaLookup.TryGetValue(mediaAssetId, out var mediaAsset))
            {
                layerViewModel.SetMediaLink(mediaAsset.Name, mediaAsset.Kind);
            }

            var frameSample = FrameTimelineService.SampleLayer(layer, CurrentTime, SceneFrameRate, TotalFrames);
            var previewSnapshot = worldSnapshots.GetValueOrDefault(layer.Id)
                                  ?? frameSample
                                  ?? TimelineInterpolationService.SampleLayer(layer, CurrentTime);
            layerViewModel.ApplyPreviewSnapshot(previewSnapshot, frameSample is not null);

            Layers.Add(layerViewModel);
        }

        SelectedLayer = selectionId is null
            ? Layers.FirstOrDefault()
            : Layers.FirstOrDefault(item => item.Id == selectionId.Value) ?? Layers.FirstOrDefault();

        SelectedScene?.RefreshMetadata();
        RefreshSceneStateProperties();
        ReloadFrameRows();
        ReloadTimelineRows();
        ReloadVisualStateGroups();
        ReloadMovieExplorer();
    }

    private bool ReplaceCurrentLayer(TimelineLayer replacement)
    {
        var index = _document.Layers.FindIndex(layer => layer.Id == replacement.Id);
        if (index < 0)
        {
            return false;
        }

        _document.Layers[index] = replacement;
        PersistActiveScene();
        return true;
    }

    private void ReloadScenes(Guid? selectionId = null)
    {
        PersistActiveScene();
        var nextSceneId = selectionId ?? _document.ActiveSceneId ?? _document.Scenes.FirstOrDefault()?.Id;

        _suppressSceneSelection = true;
        Scenes.Clear();
        foreach (var scene in _document.Scenes)
        {
            Scenes.Add(new SceneViewModel(scene)
            {
                IsSelected = nextSceneId == scene.Id
            });
        }

        SelectedScene = nextSceneId is null
            ? Scenes.FirstOrDefault()
            : Scenes.FirstOrDefault(item => item.Id == nextSceneId.Value) ?? Scenes.FirstOrDefault();
        _suppressSceneSelection = false;

        if (SelectedScene is not null)
        {
            ActivateScene(SelectedScene.Model);
            SelectedScene.IsSelected = true;
        }

        RefreshSceneEditor();
        RefreshSceneStateProperties();
        ReloadSceneMarkers();
        RefreshMonitorPreviews();
        ReloadMovieExplorer();
    }

    private void ReloadTracks()
    {
        Tracks.Clear();

        if (SelectedLayer is null || SelectedLayer.Kind == LayerKind.Audio)
        {
            return;
        }

        foreach (var property in Enum.GetValues<AnimatedProperty>())
        {
            var track = new PropertyTrackViewModel(property)
            {
                IsSelected = property == SelectedProperty
            };

            track.LoadFromLayer(SelectedLayer.Model, CurrentTime, SceneFrameRate, TotalFrames, SelectedKeyframeId);
            Tracks.Add(track);
        }

        OnPropertyChanged(nameof(SelectedKeyframeSummary));
        RefreshSelectedKeyframeEditor();
        ReloadTimelineRows();
    }

    private void RefreshInteractiveTrackState(TimelineLayer layer, AnimatedProperty property)
    {
        var track = Tracks.FirstOrDefault(item => item.Property == property);
        if (track is not null)
        {
            track.SyncKeyframesFromLayer(layer, SelectedKeyframeId);
            track.RefreshCurrentValue(layer, CurrentTime, SceneFrameRate, TotalFrames);
        }

        foreach (var row in TimelineRows)
        {
            if (!row.IsTrackRow || row.LayerId != layer.Id || row.Property != property)
            {
                continue;
            }

            row.SyncTrackFromLayer(
                layer,
                CurrentTime,
                SceneFrameRate,
                TotalFrames,
                SelectedLayer?.Id,
                SelectedProperty,
                SelectedKeyframeId);
        }

        OnPropertyChanged(nameof(SelectedKeyframeSummary));
        RefreshSelectedKeyframeEditor();
    }

    private void RefreshEditorStateAfterInteractiveChange()
    {
        RefreshLayerPreviewsAtCurrentTime();
        UpdateTrackPreviewValues();
        UpdateTimelineRowPreviewValues();
        UpdateFrameRowSelection();
        RefreshFrameSelectionStateProperties();
        RefreshAudioMeter();
        RefreshInspector();
        RefreshFrameLabelEditor();
        RefreshProgramMonitorPreview(force: true);
    }

    private void ReloadMotionPresets(Guid? selectionId = null)
    {
        var nextSelectionId = selectionId ?? SelectedMotionPreset?.Id;
        MotionPresets.Clear();

        foreach (var preset in _document.MotionPresets.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            MotionPresets.Add(new MotionPresetViewModel(preset));
        }

        _suppressInspector = true;
        SelectedMotionPreset = nextSelectionId is null
            ? MotionPresets.FirstOrDefault()
            : MotionPresets.FirstOrDefault(item => item.Id == nextSelectionId.Value) ?? MotionPresets.FirstOrDefault();
        if (SelectedMotionPreset is null)
        {
            MotionPresetNameEditor = string.Empty;
            MotionPresetDescriptionEditor = string.Empty;
        }
        else
        {
            MotionPresetNameEditor = SelectedMotionPreset.Name;
            MotionPresetDescriptionEditor = SelectedMotionPreset.Model.Description ?? string.Empty;
        }

        _suppressInspector = false;
        RefreshMotionPresetStateProperties();
        ReloadMovieExplorer();
    }

    private void ReloadLibraryItems(Guid? selectionId = null)
    {
        var nextSelectionId = selectionId ?? SelectedLibraryItem?.Id;
        ReloadLibraryFolderFilters();
        LibraryItems.Clear();

        var filteredItems = LibraryManagementService.FilterItems(
                _document.LibraryItems,
                LibrarySearchText,
                SelectedLibraryFolderFilter)
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var item in filteredItems)
        {
            LibraryItems.Add(new LibraryItemViewModel(item));
        }

        SelectedLibraryItem = nextSelectionId is null
            ? LibraryItems.FirstOrDefault()
            : LibraryItems.FirstOrDefault(item => item.Id == nextSelectionId.Value) ?? LibraryItems.FirstOrDefault();
        RefreshLibraryStateProperties();
        ReloadMovieExplorer();
    }

    private void ReloadLibraryFolderFilters()
    {
        var currentFilter = string.IsNullOrWhiteSpace(SelectedLibraryFolderFilter)
            ? "All folders"
            : SelectedLibraryFolderFilter;
        var folders = _document.LibraryItems
            .Select(item => LibraryManagementService.GetDisplayFolderPath(item.FolderPath))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(folder => folder, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _suppressLibraryFilterSelection = true;
        LibraryFolderFilters.Clear();
        LibraryFolderFilters.Add("All folders");
        foreach (var folder in folders)
        {
            if (string.Equals(folder, "All folders", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            LibraryFolderFilters.Add(folder);
        }

        SelectedLibraryFolderFilter = LibraryFolderFilters.Contains(currentFilter, StringComparer.OrdinalIgnoreCase)
            ? LibraryFolderFilters.First(folder => string.Equals(folder, currentFilter, StringComparison.OrdinalIgnoreCase))
            : "All folders";
        _suppressLibraryFilterSelection = false;
    }

    private void RefreshSelectedLibraryItemEditor()
    {
        _suppressLibraryItemEditor = true;
        LibraryItemNameEditor = SelectedLibraryItem?.Name ?? string.Empty;
        LibraryFolderEditor = SelectedLibraryItem?.Model.FolderPath ?? string.Empty;
        LibraryLinkageIdEditor = SelectedLibraryItem?.Model.LinkageId ?? string.Empty;
        LibraryBaseClassEditor = SelectedLibraryItem?.Model.BaseClassName ?? string.Empty;
        LibraryExportForRuntimeEditor = SelectedLibraryItem?.Model.ExportForRuntimeSharing ?? false;
        LibraryImportForRuntimeEditor = SelectedLibraryItem?.Model.ImportForRuntimeSharing ?? false;
        LibraryExportInFirstFrameEditor = SelectedLibraryItem?.Model.ExportInFirstFrame ?? true;
        LibrarySharedLibraryPathEditor = SelectedLibraryItem?.Model.SharedLibraryPath ?? string.Empty;
        LibraryUpdateAutomaticallyEditor = SelectedLibraryItem?.Model.UpdateAutomatically ?? false;
        LibraryUseScale9GridEditor = SelectedLibraryItem?.Model.UseScale9Grid ?? false;
        LibraryScale9LeftEditor = SelectedLibraryItem?.Model.Scale9Left ?? 16d;
        LibraryScale9TopEditor = SelectedLibraryItem?.Model.Scale9Top ?? 16d;
        LibraryScale9RightEditor = SelectedLibraryItem?.Model.Scale9Right ?? 144d;
        LibraryScale9BottomEditor = SelectedLibraryItem?.Model.Scale9Bottom ?? 84d;
        LibrarySourceAssetPathEditor = SelectedLibraryItem?.Model.SourceAssetPath ?? string.Empty;
        LibraryRegistrationPointXEditor = SelectedLibraryItem?.Model.RegistrationPointX ?? 0d;
        LibraryRegistrationPointYEditor = SelectedLibraryItem?.Model.RegistrationPointY ?? 0d;
        _suppressLibraryItemEditor = false;
    }

    private void ApplySelectedLibraryMetadata(Action<LibraryItem> apply, string statusMessage)
    {
        if (_suppressLibraryItemEditor || SelectedLibraryItem is null)
        {
            return;
        }

        var selectedId = SelectedLibraryItem.Id;
        apply(SelectedLibraryItem.Model);
        SelectedLibraryItem.RefreshMetadata();
        ReloadLibraryItems(selectedId);
        ReloadComponentItems(selectedId);
        RebuildLayers(SelectedLayer?.Id);
        RecordHistoryIfNeeded();
        StatusMessage = statusMessage;
    }

    private void ReloadComponentItems(Guid? selectionId = null)
    {
        var nextSelectionId = selectionId ?? SelectedComponentItem?.Id;
        ComponentItems.Clear();

        foreach (var item in _document.LibraryItems
                     .Where(item => item.IsComponent)
                     .OrderBy(item => item.ComponentCategory, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            ComponentItems.Add(new LibraryItemViewModel(item));
        }

        SelectedComponentItem = nextSelectionId is null
            ? ComponentItems.FirstOrDefault()
            : ComponentItems.FirstOrDefault(item => item.Id == nextSelectionId.Value) ?? ComponentItems.FirstOrDefault();
        OnPropertyChanged(nameof(CanInsertSelectedComponent));
        OnPropertyChanged(nameof(SelectedComponentSummary));
    }

    private void ReloadSelectedComponentParameters(Guid? selectionId = null)
    {
        var nextSelectionId = selectionId ?? SelectedComponentParameter?.Id;
        SelectedComponentParameters.Clear();

        if (SelectedComponentItem is null)
        {
            SelectedComponentParameter = null;
            OnPropertyChanged(nameof(SelectedComponentParameterSummary));
            return;
        }

        ComponentParameterService.NormalizeDefinitions(SelectedComponentItem.Model);
        foreach (var definition in SelectedComponentItem.Model.ComponentParameters
                     .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            SelectedComponentParameters.Add(new ComponentParameterDefinitionViewModel(definition));
        }

        SelectedComponentParameter = nextSelectionId is null
            ? SelectedComponentParameters.FirstOrDefault()
            : SelectedComponentParameters.FirstOrDefault(item => item.Id == nextSelectionId.Value) ?? SelectedComponentParameters.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedComponentParameterSummary));
    }

    private void ReloadSelectedComponentOverrides(string? selectionName = null)
    {
        var nextSelectionName = selectionName ?? SelectedComponentOverrideItem?.Model.Name;
        SelectedComponentOverrideItems.Clear();

        if (SelectedLayer is null ||
            SelectedLayer.Model.SourceLibraryItemId is not Guid libraryItemId ||
            _document.LibraryItems.FirstOrDefault(item => item.Id == libraryItemId && item.IsComponent) is not { } component)
        {
            SelectedComponentOverrideItem = null;
            OnPropertyChanged(nameof(SelectedComponentOverrideSummary));
            return;
        }

        ComponentParameterService.EnsureOverrides(SelectedLayer.Model, component);
        foreach (var definition in component.ComponentParameters)
        {
            var overrideValue = SelectedLayer.Model.ComponentParameterOverrides
                .First(item => string.Equals(item.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
            SelectedComponentOverrideItems.Add(new ComponentParameterOverrideViewModel(definition, overrideValue));
        }

        SelectedComponentOverrideItem = string.IsNullOrWhiteSpace(nextSelectionName)
            ? SelectedComponentOverrideItems.FirstOrDefault()
            : SelectedComponentOverrideItems.FirstOrDefault(item => string.Equals(item.Model.Name, nextSelectionName, StringComparison.OrdinalIgnoreCase))
              ?? SelectedComponentOverrideItems.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedComponentOverrideSummary));
    }

    private void UpdateSelectedComponentParameter(Action<ComponentParameterDefinition> update, string statusMessage)
    {
        if (_suppressComponentParameterEditor || SelectedComponentItem is null || SelectedComponentParameter is null)
        {
            return;
        }

        var component = SelectedComponentItem.Model;
        var selectedDefinition = SelectedComponentParameter.Model;
        var previousName = selectedDefinition.Name;
        var selectedParameterId = selectedDefinition.Id;
        var selectedLayerId = SelectedLayer?.Id;
        update(selectedDefinition);
        ComponentParameterService.NormalizeDefinitions(component);

        var nextDefinition = component.ComponentParameters.FirstOrDefault(item => item.Id == selectedParameterId) ?? selectedDefinition;
        if (!string.Equals(previousName, nextDefinition.Name, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var layer in _document.Layers.Where(layer => layer.SourceLibraryItemId == component.Id))
            {
                var overrideValue = layer.ComponentParameterOverrides.FirstOrDefault(item => string.Equals(item.Name, previousName, StringComparison.OrdinalIgnoreCase));
                if (overrideValue is not null)
                {
                    overrideValue.Name = nextDefinition.Name;
                }

                ComponentParameterService.EnsureOverrides(layer, component);
            }
        }
        else
        {
            foreach (var layer in _document.Layers.Where(layer => layer.SourceLibraryItemId == component.Id))
            {
                ComponentParameterService.EnsureOverrides(layer, component);
            }
        }

        SelectedComponentItem.RefreshMetadata();
        if (SelectedLibraryItem?.Id == component.Id)
        {
            ReloadLibraryItems(component.Id);
        }
        else
        {
            ReloadLibraryItems(SelectedLibraryItem?.Id);
        }

        ReloadComponentItems(component.Id);
        ReloadSelectedComponentParameters(selectedParameterId);
        RebuildLayers(selectedLayerId);
        RefreshSourceMonitorPreview();
        RecordHistoryIfNeeded();
        StatusMessage = statusMessage;
    }

    private void UpdateSelectedComponentOverride(Action<ComponentParameterOverride> update, string statusMessage)
    {
        if (_suppressComponentOverrideEditor || SelectedLayer is null || SelectedComponentOverrideItem is null)
        {
            return;
        }

        update(SelectedComponentOverrideItem.Model);
        SelectedComponentOverrideItem.RefreshMetadata();
        ReloadSelectedComponentOverrides(SelectedComponentOverrideItem.Model.Name);
        RebuildLayers(SelectedLayer.Id);
        RefreshSourceMonitorPreview();
        RecordHistoryIfNeeded();
        StatusMessage = statusMessage;
    }

    private void ReloadMediaAssets(Guid? selectionId = null)
    {
        var nextSelectionId = selectionId ?? SelectedMediaAsset?.Id;
        MediaAssets.Clear();

        foreach (var item in _document.MediaAssets.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            MediaAssets.Add(new MediaAssetViewModel(item));
        }

        SelectedMediaAsset = nextSelectionId is null
            ? MediaAssets.FirstOrDefault()
            : MediaAssets.FirstOrDefault(item => item.Id == nextSelectionId.Value) ?? MediaAssets.FirstOrDefault();
        RefreshMediaStateProperties();
        ReloadMovieExplorer();
    }

    private void ReloadPublishProfiles(Guid? selectionId = null)
    {
        PublishProfileService.EnsureProfiles(_document);
        var nextSelectionId = selectionId ?? SelectedPublishProfile?.Id;
        PublishProfiles.Clear();

        foreach (var profile in _document.PublishProfiles.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            PublishProfiles.Add(new PublishProfileViewModel(profile));
        }

        SelectedPublishProfile = nextSelectionId is null
            ? PublishProfiles.FirstOrDefault()
            : PublishProfiles.FirstOrDefault(item => item.Id == nextSelectionId.Value) ?? PublishProfiles.FirstOrDefault();
        RefreshPublishStateProperties();
    }

    private void ReloadBehaviors(Guid? selectionId = null)
    {
        var nextSelectionId = selectionId ?? SelectedBehavior?.Id;
        Behaviors.Clear();

        if (SelectedLayer?.Model.Behaviors is not { Count: > 0 } behaviorModels)
        {
            SelectedBehavior = null;
            RefreshBehaviorEditorStateProperties();
            return;
        }

        foreach (var behavior in behaviorModels)
        {
            Behaviors.Add(new InteractionBehaviorViewModel(behavior));
        }

        SelectedBehavior = nextSelectionId is null
            ? Behaviors.FirstOrDefault()
            : Behaviors.FirstOrDefault(item => item.Id == nextSelectionId.Value) ?? Behaviors.FirstOrDefault();
        RefreshBehaviorEditorStateProperties();
        ReloadMovieExplorer();
    }

    private void RefreshSelectedBehaviorEditor()
    {
        _suppressBehaviorEditor = true;
        try
        {
            var behavior = SelectedBehavior?.Model;
            SelectedBehaviorNameEditor = behavior?.Name ?? string.Empty;
            SelectedBehaviorEnabledEditor = behavior?.IsEnabled ?? true;
            SelectedBehaviorTriggerEditor = behavior?.Trigger ?? InteractionTriggerKind.PointerClick;
            SelectedBehaviorTriggerArgumentEditor = behavior?.TriggerArgument ?? string.Empty;
            SelectedBehaviorActionEditor = behavior?.Action ?? InteractionActionKind.Play;
            SelectedBehaviorTargetScene = behavior?.TargetSceneId is Guid sceneId
                ? Scenes.FirstOrDefault(item => item.Id == sceneId)
                : null;
            SelectedBehaviorTargetLayer = behavior?.TargetLayerId is Guid layerId
                ? Layers.FirstOrDefault(item => item.Id == layerId)
                : null;
            SelectedBehaviorTargetFrameLabelEditor = behavior?.TargetFrameLabel ?? string.Empty;
            SelectedBehaviorTargetButtonStateEditor = behavior?.TargetButtonState ?? ButtonVisualState.Up;
            SelectedBehaviorBoolValueEditor = behavior?.BoolValue ?? true;
            SelectedBehaviorVariableNameEditor = behavior?.VariableName ?? string.Empty;
            SelectedBehaviorVariableValueEditor = behavior?.VariableValue ?? string.Empty;
            SelectedBehaviorTargetVisualStateGroupEditor = behavior?.TargetVisualStateGroup ?? string.Empty;
            SelectedBehaviorTargetVisualStateEditor = behavior?.TargetVisualState ?? string.Empty;
            SelectedBehaviorScriptEditor = behavior?.Script ?? string.Empty;
            NormalizeSelectedBehaviorVisualStateTargets(recordHistory: false);
        }
        finally
        {
            _suppressBehaviorEditor = false;
        }

        RefreshBehaviorEditorStateProperties();
    }

    private void RefreshBehaviorEditorStateProperties()
    {
        OnPropertyChanged(nameof(CanEditBehaviors));
        OnPropertyChanged(nameof(CanAddBehavior));
        OnPropertyChanged(nameof(CanDeleteSelectedBehavior));
        OnPropertyChanged(nameof(SelectedBehaviorUsesTriggerArgument));
        OnPropertyChanged(nameof(SelectedBehaviorUsesTargetScene));
        OnPropertyChanged(nameof(SelectedBehaviorUsesTargetFrameLabel));
        OnPropertyChanged(nameof(SelectedBehaviorUsesTargetLayer));
        OnPropertyChanged(nameof(SelectedBehaviorUsesButtonState));
        OnPropertyChanged(nameof(SelectedBehaviorUsesBoolValue));
        OnPropertyChanged(nameof(SelectedBehaviorUsesVariable));
        OnPropertyChanged(nameof(SelectedBehaviorUsesVisualState));
        OnPropertyChanged(nameof(AvailableBehaviorVisualStateGroups));
        OnPropertyChanged(nameof(AvailableBehaviorVisualStates));
        OnPropertyChanged(nameof(SelectedBehaviorSummary));
        OnPropertyChanged(nameof(CanEditSelectedBehaviorScript));
        OnPropertyChanged(nameof(SelectedBehaviorScriptSummary));
    }

    private IReadOnlyList<string> GetAvailableBehaviorVisualStates(string? groupName)
    {
        var group = ResolveBehaviorVisualStateTargetLayer()?.VisualStateGroups
            .FirstOrDefault(item => string.Equals(item.Name, groupName, StringComparison.OrdinalIgnoreCase));
        return group?.States
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Name)
            .ToArray() ?? [];
    }

    private void NormalizeSelectedBehaviorVisualStateTargets(bool recordHistory, string? statusMessage = null, bool forceRecordHistory = false)
    {
        if (SelectedBehavior?.Model is not { } behavior || !SelectedBehaviorUsesVisualState)
        {
            OnPropertyChanged(nameof(AvailableBehaviorVisualStateGroups));
            OnPropertyChanged(nameof(AvailableBehaviorVisualStates));
            return;
        }

        var availableGroups = AvailableBehaviorVisualStateGroups;
        var normalizedGroup = SelectedBehaviorTargetVisualStateGroupEditor ?? string.Empty;
        if (availableGroups.Count == 0)
        {
            normalizedGroup = string.Empty;
        }
        else if (!availableGroups.Any(item => string.Equals(item, normalizedGroup, StringComparison.OrdinalIgnoreCase)))
        {
            normalizedGroup = availableGroups[0];
        }

        var availableStates = GetAvailableBehaviorVisualStates(normalizedGroup);
        var normalizedState = SelectedBehaviorTargetVisualStateEditor ?? string.Empty;
        if (availableStates.Count == 0)
        {
            normalizedState = string.Empty;
        }
        else if (!availableStates.Any(item => string.Equals(item, normalizedState, StringComparison.OrdinalIgnoreCase)))
        {
            normalizedState = availableStates[0];
        }

        var editorChanged =
            !string.Equals(SelectedBehaviorTargetVisualStateGroupEditor, normalizedGroup, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(SelectedBehaviorTargetVisualStateEditor, normalizedState, StringComparison.OrdinalIgnoreCase);

        if (editorChanged)
        {
            var previousSuppressState = _suppressBehaviorEditor;
            _suppressBehaviorEditor = true;
            try
            {
                SelectedBehaviorTargetVisualStateGroupEditor = normalizedGroup;
                SelectedBehaviorTargetVisualStateEditor = normalizedState;
            }
            finally
            {
                _suppressBehaviorEditor = previousSuppressState;
            }
        }

        var modelChanged =
            !string.Equals(behavior.TargetVisualStateGroup, normalizedGroup, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(behavior.TargetVisualState, normalizedState, StringComparison.OrdinalIgnoreCase);

        if (modelChanged)
        {
            behavior.TargetVisualStateGroup = normalizedGroup;
            behavior.TargetVisualState = normalizedState;
            SelectedBehavior.RefreshMetadata();
            OnPropertyChanged(nameof(SelectedBehaviorSummary));
        }

        if ((modelChanged || forceRecordHistory) && recordHistory)
        {
            RecordHistoryIfNeeded();
            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                StatusMessage = statusMessage;
            }
        }

        OnPropertyChanged(nameof(AvailableBehaviorVisualStateGroups));
        OnPropertyChanged(nameof(AvailableBehaviorVisualStates));
    }

    private void RefreshPrototypeStateProperties()
    {
        OnPropertyChanged(nameof(IsPrototypeMode));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(PrototypeVariableSummary));
        OnPropertyChanged(nameof(PrototypeSummary));
        OnPropertyChanged(nameof(WorkspaceSummary));
        OnPropertyChanged(nameof(CanInsertSelectedComponent));
        OnPropertyChanged(nameof(CanCreateComponentFromSelection));
        OnPropertyChanged(nameof(CanEditCurrentFrameActionScript));
        OnPropertyChanged(nameof(CanEditSelectedBehaviorScript));
        OnPropertyChanged(nameof(CanEditCanvasProperties));
        OnPropertyChanged(nameof(CanInsertStageItems));
        OnPropertyChanged(nameof(CanUseSelectionActions));
        OnPropertyChanged(nameof(CanEditTextMenu));
        OnPropertyChanged(nameof(CanManageFrameLabels));
        OnPropertyChanged(nameof(ControlMenuSummary));
        OnPropertyChanged(nameof(HelpMenuSummary));
    }

    private void RefreshActionScriptStateProperties()
    {
        OnPropertyChanged(nameof(CanEditCurrentFrameActionScript));
        OnPropertyChanged(nameof(CanEditSelectedBehaviorScript));
        OnPropertyChanged(nameof(CurrentFrameActionSummary));
        OnPropertyChanged(nameof(SelectedBehaviorScriptSummary));
        OnPropertyChanged(nameof(SelectedActionScriptSnippetSummary));
        OnPropertyChanged(nameof(CanInsertActionScriptSnippetIntoFrame));
        OnPropertyChanged(nameof(CanInsertActionScriptSnippetIntoBehavior));
    }

    private void ReloadActionScriptSnippets(string? preferredName = null)
    {
        var selectedName = preferredName ?? SelectedActionScriptSnippet?.Name;
        ActionScriptSnippets.Clear();
        foreach (var snippet in ActionScriptSnippetService.BuildSnippets(_document, SelectedScene?.Model, SelectedLayer?.Model))
        {
            ActionScriptSnippets.Add(new ActionScriptSnippetViewModel(snippet));
        }

        SelectedActionScriptSnippet = string.IsNullOrWhiteSpace(selectedName)
            ? ActionScriptSnippets.FirstOrDefault()
            : ActionScriptSnippets.FirstOrDefault(item => string.Equals(item.Name, selectedName, StringComparison.OrdinalIgnoreCase))
                ?? ActionScriptSnippets.FirstOrDefault();
    }

    private void RefreshSelectedPublishProfileEditor()
    {
        _suppressPublishProfileEditor = true;
        var profile = SelectedPublishProfile?.Model;
        PublishProfileNameEditor = profile?.Name ?? string.Empty;
        PublishOutputKindEditor = profile?.OutputKind ?? PublishOutputKind.PngSequence;
        PublishWidthEditor = profile?.Width ?? Math.Max(1, (int)Math.Round(CanvasWidth));
        PublishHeightEditor = profile?.Height ?? Math.Max(1, (int)Math.Round(CanvasHeight));
        PublishFrameRateEditor = profile?.FrameRate ?? SceneFrameRate;
        PublishPlayAllScenesEditor = profile?.PlayAllScenes ?? false;
        PublishTransparentBackgroundEditor = profile?.TransparentBackground ?? false;
        PublishIncludeSceneGraphEditor = profile?.IncludeSceneGraph ?? true;
        PublishIncludeValidationReportEditor = profile?.IncludeValidationReport ?? true;
        PublishSpriteSheetColumnsEditor = profile?.SpriteSheetColumns ?? 6;
        PublishQualityEditor = profile?.Quality ?? 90;
        _suppressPublishProfileEditor = false;
        RefreshPublishValidation();
    }

    private void UpdateSelectedBehavior(Action<InteractionBehaviorModel> update, string statusMessage, bool recordHistory = true)
    {
        if (_suppressBehaviorEditor || !CanEditBehaviors || SelectedBehavior?.Model is not { } behavior)
        {
            return;
        }

        update(behavior);
        SelectedBehavior.RefreshMetadata();
        if (recordHistory)
        {
            RecordHistoryIfNeeded();
        }

        StatusMessage = statusMessage;
        OnPropertyChanged(nameof(SelectedBehaviorSummary));
    }

    private void UpdateSelectedPublishProfile(Action<PublishProfile> update, string statusMessage)
    {
        if (_suppressPublishProfileEditor || !CanEditPublishProfiles || SelectedPublishProfile?.Model is not { } profile)
        {
            return;
        }

        update(profile);
        SelectedPublishProfile.RefreshMetadata();
        RefreshPublishValidation();
        RecordHistoryIfNeeded();
        StatusMessage = statusMessage;
        OnPropertyChanged(nameof(SelectedPublishProfileSummary));
    }

    private void RefreshPublishValidation()
    {
        PublishValidationIssues.Clear();

        if (SelectedPublishProfile?.Model is not { } profile)
        {
            RefreshPublishStateProperties();
            return;
        }

        var snapshot = BuildPersistedDocumentSnapshot();
        PublishProfileService.EnsureProfiles(snapshot);
        var issues = PublishExportService.Validate(snapshot, profile);
        foreach (var issue in issues)
        {
            PublishValidationIssues.Add(issue);
        }

        RefreshPublishStateProperties();
    }

    private void UpdateTrackPreviewValues()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        foreach (var track in Tracks)
        {
            track.RefreshCurrentValue(SelectedLayer.Model, CurrentTime, SceneFrameRate, TotalFrames);
        }

        OnPropertyChanged(nameof(SelectedKeyframeSummary));
    }

    private void ReloadTimelineRows()
    {
        TimelineRows.Clear();

        foreach (var layer in Layers)
        {
            var headerRow = new TimelineTrackRowViewModel(layer.Id, TimelineTrackRowKind.LayerHeader);
            var isExpanded = layer.Kind == LayerKind.Folder || !_collapsedTimelineLayers.Contains(layer.Id);
            headerRow.ApplyHierarchyState(layer.Depth);
            headerRow.LoadLayerHeaderFromLayer(layer.Model, SelectedLayer?.Id == layer.Id, isExpanded);
            TimelineRows.Add(headerRow);

            if (layer.Kind is LayerKind.Audio or LayerKind.Folder || !isExpanded)
            {
                continue;
            }

            foreach (var group in GetTimelinePropertyGroups())
            {
                var properties = GetPropertiesForGroup(group);
                var keyedPropertyCount = properties.Count(property =>
                    layer.Model.Tracks.Any(track => track.Property == property && track.Keyframes.Count > 1));

                var groupRow = new TimelineTrackRowViewModel(layer.Id, TimelineTrackRowKind.PropertyGroupHeader, groupKind: group);
                groupRow.ApplyHierarchyState(layer.Depth, 1);
                groupRow.LoadGroupHeaderFromLayer(layer.Model, keyedPropertyCount);
                TimelineRows.Add(groupRow);

                foreach (var property in properties)
                {
                    var row = new TimelineTrackRowViewModel(layer.Id, TimelineTrackRowKind.PropertyTrack, property, group);
                    row.ApplyHierarchyState(layer.Depth, 2);
                    row.LoadFromLayer(
                        layer.Model,
                        CurrentTime,
                        SceneFrameRate,
                        TotalFrames,
                        SelectedLayer?.Id,
                        SelectedProperty,
                        SelectedKeyframeId,
                        false);
                    TimelineRows.Add(row);
                }
            }
        }

        OnPropertyChanged(nameof(TimelineSurfaceHeight));
        OnPropertyChanged(nameof(TimelineSurfaceBodyHeight));
    }

    private void UpdateTimelineRowPreviewValues()
    {
        foreach (var row in TimelineRows)
        {
            if (!row.IsTrackRow)
            {
                continue;
            }

            var layer = Layers.FirstOrDefault(item => item.Id == row.LayerId);
            if (layer is not null)
            {
                row.RefreshCurrentValue(layer.Model, CurrentTime, SceneFrameRate, TotalFrames);
            }
        }
    }

    private void UpdateTimelineRowSelection()
    {
        foreach (var row in TimelineRows)
        {
            row.UpdateSelection(SelectedLayer?.Id, SelectedProperty, SelectedKeyframeId);
        }
    }

    private void ReloadFrameRows()
    {
        FrameRows.Clear();
        var mediaLookup = _document.MediaAssets.ToDictionary(item => item.Id);

        foreach (var layer in Layers)
        {
            var row = new LayerFrameTimelineRowViewModel(layer.Id);
            row.LoadFromLayer(
                layer.Model,
                mediaLookup,
                SceneFrameRate,
                TotalFrames,
                SelectedLayer?.Id,
                SelectionStartFrame,
                SelectionEndFrame);
            row.ApplyHierarchyState(layer.Depth, layer.HasChildren, layer.IsExpanded);
            FrameRows.Add(row);
        }

        OnPropertyChanged(nameof(FrameTimelineSurfaceHeight));
        OnPropertyChanged(nameof(FrameTimelineSurfaceBodyHeight));
    }

    private static IReadOnlyList<TimelineTrackGroupKind> GetTimelinePropertyGroups()
    {
        return
        [
            TimelineTrackGroupKind.Layout,
            TimelineTrackGroupKind.Scale,
            TimelineTrackGroupKind.Rotation3D,
            TimelineTrackGroupKind.Appearance
        ];
    }

    private static IReadOnlyList<AnimatedProperty> GetPropertiesForGroup(TimelineTrackGroupKind group)
    {
        return group switch
        {
            TimelineTrackGroupKind.Layout =>
            [
                AnimatedProperty.X,
                AnimatedProperty.Y,
                AnimatedProperty.Width,
                AnimatedProperty.Height
            ],
            TimelineTrackGroupKind.Scale =>
            [
                AnimatedProperty.ScaleX,
                AnimatedProperty.ScaleY
            ],
            TimelineTrackGroupKind.Rotation3D =>
            [
                AnimatedProperty.Rotation,
                AnimatedProperty.SkewX,
                AnimatedProperty.SkewY,
                AnimatedProperty.RotationX,
                AnimatedProperty.RotationY,
                AnimatedProperty.ZDepth
            ],
            _ =>
            [
                AnimatedProperty.Opacity
            ]
        };
    }

    private void ReloadFrameLabels()
    {
        SceneFrameLabels.Clear();

        if (SelectedScene is null)
        {
            RefreshFrameLabelEditor();
            return;
        }

        foreach (var label in SelectedScene.Model.FrameLabels.OrderBy(item => item.Frame))
        {
            SceneFrameLabels.Add(new FrameLabelViewModel(label));
        }

        RefreshFrameLabelEditor();
        RefreshActionScriptStateProperties();
    }

    private void ReloadSceneMarkers(Guid? selectionId = null)
    {
        SceneMarkers.Clear();

        if (SelectedScene is null)
        {
            SelectedSceneMarker = null;
            RefreshSceneTimelineEditors();
            return;
        }

        SceneTimelineService.EnsureTimelineMetadata(SelectedScene.Model, TotalFrames);
        foreach (var marker in SelectedScene.Model.Markers.OrderBy(item => item.Frame).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            SceneMarkers.Add(new SceneMarkerViewModel(marker));
        }

        var nextSelectionId = selectionId
            ?? SelectedSceneMarker?.Id
            ?? SceneMarkers.FirstOrDefault(item => item.Frame == CurrentFrame)?.Id;
        SelectedSceneMarker = nextSelectionId is null
            ? SceneMarkers.FirstOrDefault(item => item.Frame == CurrentFrame)
            : SceneMarkers.FirstOrDefault(item => item.Id == nextSelectionId.Value) ?? SceneMarkers.FirstOrDefault(item => item.Frame == CurrentFrame);
        RefreshSceneTimelineEditors();
        ReloadMovieExplorer();
    }

    private void ReloadVisualStateGroups(Guid? selectionId = null)
    {
        VisualStateGroups.Clear();

        if (SelectedLayer is null)
        {
            SelectedVisualStateGroup = null;
            ReloadVisualStates();
            RefreshVisualStateStateProperties();
            return;
        }

        foreach (var group in SelectedLayer.Model.VisualStateGroups.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            VisualStateGroups.Add(new VisualStateGroupViewModel(group));
        }

        var nextSelectionId = selectionId ?? SelectedVisualStateGroup?.Id;
        SelectedVisualStateGroup = nextSelectionId is null
            ? VisualStateGroups.FirstOrDefault()
            : VisualStateGroups.FirstOrDefault(item => item.Id == nextSelectionId.Value) ?? VisualStateGroups.FirstOrDefault();
        RefreshVisualStateStateProperties();
        ReloadMovieExplorer();
    }

    private void ReloadVisualStates(Guid? selectionId = null)
    {
        VisualStates.Clear();

        if (SelectedVisualStateGroup is null)
        {
            SelectedVisualState = null;
            RefreshVisualStateStateProperties();
            return;
        }

        foreach (var state in SelectedVisualStateGroup.Model.States.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            VisualStates.Add(new VisualStateViewModel(state));
        }

        var activeState = SelectedVisualStateGroup.Model.ActiveStateName;
        var nextSelectionId = selectionId
            ?? SelectedVisualState?.Id
            ?? VisualStates.FirstOrDefault(item => string.Equals(item.Name, activeState, StringComparison.OrdinalIgnoreCase))?.Id;
        SelectedVisualState = nextSelectionId is null
            ? VisualStates.FirstOrDefault(item => string.Equals(item.Name, activeState, StringComparison.OrdinalIgnoreCase)) ?? VisualStates.FirstOrDefault()
            : VisualStates.FirstOrDefault(item => item.Id == nextSelectionId.Value) ?? VisualStates.FirstOrDefault();
        RefreshVisualStateStateProperties();
        ReloadMovieExplorer();
    }

    private void UpdateFrameRowSelection()
    {
        foreach (var row in FrameRows)
        {
            row.UpdateSelection(SelectedLayer?.Id, SelectionStartFrame, SelectionEndFrame);
        }
    }

    private void RefreshFrameAwareState()
    {
        if (SelectionStartFrame >= TotalFrames)
        {
            SelectionStartFrame = TotalFrames - 1;
        }

        if (SelectionEndFrame >= TotalFrames)
        {
            SelectionEndFrame = TotalFrames - 1;
        }

        RefreshLayerPreviewsAtCurrentTime();

        ReloadFrameRows();
        ReloadTracks();
        ReloadTimelineRows();
        RefreshInspector();
        OnPropertyChanged(nameof(TotalFrames));
        OnPropertyChanged(nameof(CurrentFrame));
        OnPropertyChanged(nameof(CurrentFrameLabel));
        OnPropertyChanged(nameof(FrameRateLabel));
        OnPropertyChanged(nameof(FrameTimelineSurfaceWidth));
        OnPropertyChanged(nameof(TimelineSurfaceWidth));
    }

    private void RefreshLayerPreviewsAtCurrentTime()
    {
        foreach (var layer in Layers)
        {
            layer.UpdatePreview(CurrentTime, SceneFrameRate, TotalFrames);
        }
    }

    private void RefreshFrameSelectionStateProperties()
    {
        OnPropertyChanged(nameof(CanCopyFrameRange));
        OnPropertyChanged(nameof(CanPasteFrameRange));
        OnPropertyChanged(nameof(SelectedFrameRangeSummary));
        OnPropertyChanged(nameof(CanSetSceneWorkArea));
        OnPropertyChanged(nameof(EditMultipleFramesSummary));
        OnPropertyChanged(nameof(OnionSkinSummary));
    }

    private void RefreshMotionPresetStateProperties()
    {
        OnPropertyChanged(nameof(CanCaptureMotionPreset));
        OnPropertyChanged(nameof(CanApplySelectedMotionPreset));
        OnPropertyChanged(nameof(CanDeleteSelectedMotionPreset));
        OnPropertyChanged(nameof(MotionPresetSummary));
    }

    private void RefreshSceneTimelineEditors()
    {
        _suppressSceneEditor = true;
        if (SelectedScene is null)
        {
            MarkerNameEditor = string.Empty;
            MarkerNotesEditor = string.Empty;
            MarkerColorEditor = "#57C9FF";
            SelectedMarkerKind = SceneMarkerKind.Cue;
            SceneTransitionKindEditor = SceneTransitionKind.None;
            SceneTransitionDurationEditor = 0.35d;
            SceneTransitionAccentColorEditor = "#000000";
        }
        else
        {
            SceneTimelineService.EnsureTimelineMetadata(SelectedScene.Model, TotalFrames);
            MarkerNameEditor = SelectedSceneMarker?.Name ?? string.Empty;
            MarkerNotesEditor = SelectedSceneMarker?.Notes ?? string.Empty;
            MarkerColorEditor = SelectedSceneMarker?.Model.Color ?? "#57C9FF";
            SelectedMarkerKind = SelectedSceneMarker?.Kind ?? SceneMarkerKind.Cue;
            SceneTransitionKindEditor = SelectedScene.Model.OutgoingTransition.Kind;
            SceneTransitionDurationEditor = SelectedScene.Model.OutgoingTransition.Duration;
            SceneTransitionAccentColorEditor = SelectedScene.Model.OutgoingTransition.AccentColor;
        }

        _suppressSceneEditor = false;
        OnPropertyChanged(nameof(CurrentSceneInFrame));
        OnPropertyChanged(nameof(CurrentSceneOutFrame));
        OnPropertyChanged(nameof(CurrentSceneWorkAreaStartFrame));
        OnPropertyChanged(nameof(CurrentSceneWorkAreaEndFrame));
        OnPropertyChanged(nameof(CurrentSceneInFrameLabel));
        OnPropertyChanged(nameof(CurrentSceneOutFrameLabel));
        OnPropertyChanged(nameof(CurrentSceneWorkAreaLabel));
        OnPropertyChanged(nameof(SelectedSceneMarkerSummary));
        OnPropertyChanged(nameof(SceneTransitionSummary));
    }

    private void RefreshMonitorPreviews()
    {
        RefreshProgramMonitorPreview(force: true);
        RefreshSourceMonitorPreview();
    }

    private void RefreshProgramMonitorPreview(bool force)
    {
        if (!force && IsPlaying)
        {
            var nowTick = Environment.TickCount64;
            if (_lastProgramMonitorRefreshTick >= 0 &&
                nowTick - _lastProgramMonitorRefreshTick < ProgramMonitorPlaybackRefreshIntervalMilliseconds)
            {
                return;
            }

            _lastProgramMonitorRefreshTick = nowTick;
        }
        else
        {
            _lastProgramMonitorRefreshTick = Environment.TickCount64;
        }

        var previewDocument = IsEditingSymbol
            ? BuildPersistedDocumentSnapshot()
            : _document;
        try
        {
            ProgramMonitorBitmap = EditorMonitorPreviewService.RenderProgramMonitor(
                previewDocument,
                PlayAllScenes ? GetCurrentSequenceTime(previewDocument) : CurrentTime,
                PlayAllScenes);
        }
        catch
        {
            ProgramMonitorBitmap = null;
        }

        OnPropertyChanged(nameof(ProgramMonitorSummary));
    }

    private static bool ShouldRefreshPlaybackInterval(ref long lastTick, int intervalMilliseconds)
    {
        var nowTick = Environment.TickCount64;
        if (lastTick >= 0 && nowTick - lastTick < intervalMilliseconds)
        {
            return false;
        }

        lastTick = nowTick;
        return true;
    }

    private bool ShouldRefreshProgramMonitorDuringPlayback()
    {
        if (!UseDockWorkspace)
        {
            return true;
        }

        return DockWorkspace.IsSurfaceActivelyVisible(DockWorkspaceSurfaceIds.ProgramMonitorTool);
    }

    private bool ShouldRefreshTimelinePlaybackValues()
    {
        if (!UseDockWorkspace)
        {
            return SelectedTimelineView == TimelineWorkspaceView.Curves;
        }

        return DockWorkspace.IsSurfaceActivelyVisible(DockWorkspaceSurfaceIds.CurvesTimelineTool);
    }

    private bool ShouldRefreshInspectorDuringPlayback()
    {
        if (SelectedLayer is null)
        {
            return false;
        }

        if (!UseDockWorkspace)
        {
            return IsRightPanelVisible;
        }

        return DockWorkspace.IsSurfaceActivelyVisible(DockWorkspaceSurfaceIds.InspectorTool);
    }

    private bool ShouldRefreshAudioMeterDuringPlayback()
    {
        return SelectedLayer?.Model.Media.SourceMediaAssetId is Guid;
    }

    private void RefreshInspectorPlaybackValues()
    {
        if (_suppressInspector || SelectedLayer is null)
        {
            return;
        }

        var snapshot = FrameTimelineService.SampleLayer(SelectedLayer.Model, CurrentTime, SceneFrameRate, TotalFrames);
        if (snapshot is null)
        {
            return;
        }

        var previousSuppressInspector = _suppressInspector;
        _suppressInspector = true;
        try
        {
            InspectorX = snapshot.Value.X;
            InspectorY = snapshot.Value.Y;
            InspectorWidth = snapshot.Value.Width;
            InspectorHeight = snapshot.Value.Height;
            InspectorScaleX = snapshot.Value.ScaleX;
            InspectorScaleY = snapshot.Value.ScaleY;
            InspectorSkewX = snapshot.Value.SkewX;
            InspectorSkewY = snapshot.Value.SkewY;
            InspectorRotation = snapshot.Value.Rotation;
            InspectorRotationX = snapshot.Value.RotationX;
            InspectorRotationY = snapshot.Value.RotationY;
            InspectorZDepth = snapshot.Value.ZDepth;
            InspectorOpacity = snapshot.Value.Opacity;
            InspectorCornerRadius = snapshot.Value.CornerRadius;
            InspectorFontSize = snapshot.Value.FontSize;
            TextEditor = snapshot.Value.Text;
        }
        finally
        {
            _suppressInspector = previousSuppressInspector;
        }
    }

    private void RefreshSourceMonitorPreview()
    {
        var snapshot = BuildPersistedDocumentSnapshot();
        try
        {
            var preview = EditorMonitorPreviewService.BuildSourcePreview(
                snapshot,
                SelectedLayer?.Model,
                SelectedLibraryItem?.Model ?? SelectedComponentItem?.Model,
                SelectedMediaAsset?.Model,
                SourceMonitorTime);
            SourceMonitorBitmap = preview.Bitmap;
            SourceMonitorTitle = preview.Title;
            SourceMonitorSummary = preview.Summary;
            SourceMonitorDuration = preview.Duration;
            if (preview.Duration <= 0d)
            {
                SourceMonitorInTime = 0d;
                SourceMonitorOutTime = 0d;
            }
            else if (SourceMonitorOutTime <= 0d)
            {
                SourceMonitorOutTime = preview.Duration;
            }
        }
        catch
        {
            SourceMonitorBitmap = null;
            SourceMonitorTitle = "Source unavailable";
            SourceMonitorSummary = "Unable to build source preview.";
            SourceMonitorDuration = 0d;
            SourceMonitorInTime = 0d;
            SourceMonitorOutTime = 0d;
        }

        OnPropertyChanged(nameof(SourceMonitorRangeSummary));
    }

    private void RefreshVisualStateStateProperties()
    {
        OnPropertyChanged(nameof(CanEditVisualStates));
        OnPropertyChanged(nameof(CanApplySelectedVisualState));
        OnPropertyChanged(nameof(CanRemoveSelectedVisualState));
        OnPropertyChanged(nameof(CanRemoveSelectedVisualStateGroup));
        OnPropertyChanged(nameof(SelectedVisualStateSummary));
    }

    private double GetCurrentSequenceTime(TimelineDocument? snapshot = null)
    {
        var document = snapshot ?? _document;
        SceneEditingService.EnsureScenes(document);
        var activeSceneId = SelectedScene?.Id ?? document.ActiveSceneId;
        var offset = 0d;
        for (var index = 0; index < document.Scenes.Count; index++)
        {
            var scene = document.Scenes[index];
            if (scene.Id == activeSceneId)
            {
                return offset + CurrentTime;
            }

            var next = index < document.Scenes.Count - 1 ? document.Scenes[index + 1] : null;
            offset += GetScenePlaybackDuration(scene, UseWorkAreaPlayback) - GetSceneOverlap(scene, next, UseWorkAreaPlayback);
        }

        return offset + CurrentTime;
    }

    private double GetCurrentScenePlaybackStartTime()
    {
        if (SelectedScene is null)
        {
            return 0d;
        }

        SceneTimelineService.EnsureTimelineMetadata(SelectedScene.Model, TotalFrames);
        var range = SceneTimelineService.GetPlaybackRange(SelectedScene.Model, TotalFrames, UseWorkAreaPlayback);
        return FrameTimelineService.FrameToTime(range.StartFrame, SceneFrameRate);
    }

    private double GetCurrentScenePlaybackEndTime()
    {
        if (SelectedScene is null)
        {
            return Duration;
        }

        SceneTimelineService.EnsureTimelineMetadata(SelectedScene.Model, TotalFrames);
        var range = SceneTimelineService.GetPlaybackRange(SelectedScene.Model, TotalFrames, UseWorkAreaPlayback);
        return FrameTimelineService.FrameToTime(range.EndFrame, SceneFrameRate);
    }

    private static double GetScenePlaybackDuration(SceneModel scene, bool useWorkArea)
    {
        var totalFrames = FrameTimelineService.GetTotalFrames(scene.Duration, scene.FrameRate);
        SceneTimelineService.EnsureTimelineMetadata(scene, totalFrames);
        var range = SceneTimelineService.GetPlaybackRange(scene, totalFrames, useWorkArea);
        return Math.Max(0.05d, FrameTimelineService.FrameToTime(range.EndFrame, scene.FrameRate) - FrameTimelineService.FrameToTime(range.StartFrame, scene.FrameRate));
    }

    private static double GetSceneOverlap(SceneModel scene, SceneModel? nextScene, bool useWorkArea)
    {
        if (nextScene is null)
        {
            return 0d;
        }

        return Math.Min(
            SceneTimelineService.GetTransitionOverlap(scene, nextScene),
            Math.Min(GetScenePlaybackDuration(scene, useWorkArea), GetScenePlaybackDuration(nextScene, useWorkArea)));
    }

    private void RefreshFrameLabelEditor()
    {
        var label = FindCurrentFrameLabelModel();
        FrameLabelEditor = label?.Name ?? string.Empty;

        _suppressFrameActionScriptEditor = true;
        FrameActionScriptEditor = label?.Script ?? string.Empty;
        _suppressFrameActionScriptEditor = false;
        RefreshActionScriptStateProperties();
    }

    private FrameLabelModel? FindCurrentFrameLabelModel()
    {
        return IsEditingSymbol || SelectedScene is null
            ? null
            : FrameTimelineService.GetFrameLabel(SelectedScene.Model, CurrentFrame);
    }

    private KeyframeModel? CreateFrameSnapshotKeyframes(TimelineLayer layer, double time)
    {
        if (layer.Kind == LayerKind.Audio)
        {
            return null;
        }

        var frameAwareTime = FrameTimelineService.GetSampleTime(layer, time, SceneFrameRate, TotalFrames);
        KeyframeModel? selectedPropertyKeyframe = null;
        if (layer.Kind == LayerKind.Path)
        {
            TimelineEditingService.CaptureShapeKeyframe(layer, time, Duration);
        }

        foreach (var property in Enum.GetValues<AnimatedProperty>())
        {
            var value = frameAwareTime is null
                ? TimelineInterpolationService.SampleProperty(layer, property, time)
                : FrameTimelineService.SampleProperty(layer, property, time, SceneFrameRate, TotalFrames);
            var keyframe = TimelineEditingService.SetKeyframe(layer, property, time, value, Duration);
            if (property == SelectedProperty)
            {
                selectedPropertyKeyframe = keyframe;
            }
        }

        return selectedPropertyKeyframe;
    }

    private void RefreshInspector()
    {
        _suppressInspector = true;

        if (SelectedLayer is null)
        {
            LayerNameEditor = string.Empty;
            _suppressSymbolInstanceEditor = true;
            SymbolPlaybackModeEditor = SymbolPlaybackMode.SceneTime;
            SymbolPlaybackOffsetEditor = 0;
            SymbolLockedFrameEditor = 0;
            SymbolButtonStateEditor = ButtonVisualState.Up;
            _suppressSymbolInstanceEditor = false;
            InspectorX = 0;
            InspectorY = 0;
            InspectorWidth = 0;
            InspectorHeight = 0;
            InspectorScaleX = 1d;
            InspectorScaleY = 1d;
            InspectorSkewX = 0;
            InspectorSkewY = 0;
            InspectorRotation = 0;
            InspectorRotationX = 0;
            InspectorRotationY = 0;
            InspectorZDepth = 0;
            InspectorOpacity = 1;
            InspectorCornerRadius = 0;
            InspectorFontSize = 48;
            ShapeDrawingModeEditor = SelectedDrawingTool switch
            {
                DrawingTool.Rectangle or DrawingTool.Ellipse or DrawingTool.PolyStar => ShapeDrawingMode.Primitive,
                DrawingTool.Pen or DrawingTool.Brush or DrawingTool.Line or DrawingTool.Pencil => ShapeDrawingMode.Object,
                _ => ShapeDrawingMode.Merge
            };
            CornerRadiusTopLeftEditor = 28d;
            CornerRadiusTopRightEditor = 28d;
            CornerRadiusBottomRightEditor = 28d;
            CornerRadiusBottomLeftEditor = 28d;
            EllipseStartAngleEditor = 0d;
            EllipseSweepAngleEditor = 360d;
            PolyStarSidesEditor = 5;
            PolyStarInnerRadiusEditor = 0.46d;
            PolyStarIsStarEditor = true;
            FillHexEditor = string.Empty;
            StrokeHexEditor = string.Empty;
            FillEnabledEditor = true;
            StrokeEnabledEditor = true;
            StrokeThicknessEditor = 1d;
            UseGradientEditor = false;
            GradientKindEditor = LayerGradientKind.Linear;
            GradientAngleEditor = 45d;
            GradientCenterXEditor = 0.5d;
            GradientCenterYEditor = 0.5d;
            GradientScaleXEditor = 1d;
            GradientScaleYEditor = 1d;
            GradientFromEditor = string.Empty;
            GradientToEditor = string.Empty;
            StrokeCapStyleEditor = LayerStrokeCapStyle.Round;
            StrokeJoinStyleEditor = LayerStrokeJoinStyle.Round;
            StrokeMiterLimitEditor = 3d;
            TextFontFamilyEditor = string.Empty;
            TextBoldEditor = false;
            TextItalicEditor = false;
            TextAlignmentEditor = LayerTextAlignment.Left;
            TextLetterSpacingEditor = 0d;
            TextLineHeightEditor = 0d;
            TextRenderAsHtmlEditor = false;
            TextFieldKindEditor = FlashTextFieldKind.Static;
            TextLineModeEditor = FlashTextLineMode.SingleLine;
            TextSelectableEditor = true;
            TextShowBorderEditor = false;
            TextVariableNameEditor = string.Empty;
            TextMaxCharactersEditor = 0;
            TextPasswordEditor = false;
            TextUseDeviceFontsEditor = false;
            TextAntiAliasModeEditor = FlashTextAntiAliasMode.Animation;
            SelectedPathPointIndexEditor = -1;
            SelectedPathPointHandleModeEditor = VectorHandleMode.Corner;
            SelectedPathPointWidthScaleEditor = 1d;
            AvaloniaControlContentEditor = string.Empty;
            AvaloniaControlSecondaryContentEditor = string.Empty;
            AvaloniaControlIsCheckedEditor = false;
            AvaloniaControlMinimumEditor = 0;
            AvaloniaControlMaximumEditor = 100d;
            AvaloniaControlValueEditor = 50d;
            AvaloniaControlIsIndeterminateEditor = false;
            AvaloniaControlSourceEditor = string.Empty;
            LayerRoleEditor = LayerCompositeRole.Normal;
            LayerBlendModeEditor = LayerBlendMode.Normal;
            MaskLayerCountEditor = 1;
            LayerBlurEditor = 0;
            LayerGlowSizeEditor = 18d;
            LayerGlowOpacityEditor = 0;
            LayerGlowColorEditor = "#FFFFFF";
            LayerShadowBlurEditor = 0;
            LayerShadowOpacityEditor = 0;
            LayerShadowOffsetXEditor = 0;
            LayerShadowOffsetYEditor = 0;
            LayerShadowColorEditor = "#09111F";
            LayerTintColorEditor = "#FFFFFF";
            LayerTintStrengthEditor = 0;
            LayerBrightnessEditor = 0;
            LayerContrastEditor = 0;
            LayerHueShiftEditor = 0;
            LayerBevelTypeEditor = LayerBevelType.Inner;
            LayerBevelSizeEditor = 0d;
            LayerBevelStrengthEditor = 0d;
            LayerBevelAngleEditor = 45d;
            LayerBevelDistanceEditor = 4d;
            LayerBevelHighlightColorEditor = "#FFFFFF";
            LayerBevelShadowColorEditor = "#09111F";
            LayerGradientGlowSizeEditor = 0d;
            LayerGradientGlowStrengthEditor = 0d;
            LayerGradientGlowAngleEditor = 45d;
            LayerGradientGlowDistanceEditor = 0d;
            LayerGradientGlowFromEditor = "#61E6FF";
            LayerGradientGlowToEditor = "#09111F";
            LayerGradientBevelTypeEditor = LayerBevelType.Inner;
            LayerGradientBevelSizeEditor = 0d;
            LayerGradientBevelStrengthEditor = 0d;
            LayerGradientBevelAngleEditor = 45d;
            LayerGradientBevelDistanceEditor = 4d;
            LayerGradientBevelFromEditor = "#FFFFFF";
            LayerGradientBevelToEditor = "#09111F";
            LayerFlashColorEffectModeEditor = FlashColorEffectMode.None;
            LayerFlashAlphaPercentEditor = 100d;
            LayerFlashTintColorEditor = "#FFFFFF";
            LayerFlashTintPercentEditor = 0d;
            LayerFlashBrightnessPercentEditor = 0d;
            LayerFlashRedPercentEditor = 100d;
            LayerFlashGreenPercentEditor = 100d;
            LayerFlashBluePercentEditor = 100d;
            LayerFlashAdvancedAlphaPercentEditor = 100d;
            LayerFlashRedOffsetEditor = 0d;
            LayerFlashGreenOffsetEditor = 0d;
            LayerFlashBlueOffsetEditor = 0d;
            LayerFlashAlphaOffsetEditor = 0d;
            LayerSaturationEditor = 1d;
            LayerParallaxDepthEditor = 1d;
            MediaPlaybackModeEditor = MediaPlaybackMode.Stream;
            MediaStartTimeEditor = 0;
            MediaClipOffsetEditor = 0;
            MediaClipDurationEditor = 1d;
            MediaLoopEditor = false;
            MediaRepeatCountEditor = 1;
            MediaRepeatForeverEditor = false;
            MediaVolumeEditor = 1d;
            MediaGainDbEditor = 0d;
            MediaPanEditor = 0d;
            MediaFadeInEditor = 0d;
            MediaFadeOutEditor = 0d;
            LayerOutlineEditor = false;
            LayerOutlineColorEditor = "#57C9FF";
            LayerCacheAsBitmapEditor = false;
            LayerBitmapCacheBackgroundEditor = "#000000";
            SelectedTransformParentLayer = null;
            SelectedGuideLayer = null;
            GuideOrientToPathEditor = false;
            GuideSnapToPathEditor = false;
            WarpEnabledEditor = false;
            WarpMeshColumnsEditor = 3;
            WarpMeshRowsEditor = 3;
            RigEnabledEditor = false;
            RigPoseNameEditor = "Pose 1";
            RigPoses.Clear();
            SelectedRigPose = null;
            _suppressInspector = false;
            RefreshShapeStyleStateProperties();
            RefreshFlashColorEffectStateProperties();
            RefreshFlashTextFieldStateProperties();
            RefreshGuideBindingStateProperties();
            RefreshTransformParentingStateProperties();
            RefreshAuthoringMetadataStateProperties();
            RefreshWarpRigStateProperties();
            RefreshAudioMeter();
            return;
        }

        LayerNameEditor = SelectedLayer.Name;
        FillHexEditor = SelectedLayer.FillHex;
        StrokeHexEditor = SelectedLayer.StrokeHex;
        FillEnabledEditor = SelectedLayer.Model.Style.HasFill;
        StrokeEnabledEditor = SelectedLayer.Model.Style.HasStroke;
        StrokeThicknessEditor = SelectedLayer.Model.Style.StrokeThickness;
        UseGradientEditor = SelectedLayer.Model.Style.UseGradient;
        GradientKindEditor = SelectedLayer.Model.Style.GradientKind;
        GradientAngleEditor = SelectedLayer.Model.Style.GradientAngle;
        GradientCenterXEditor = SelectedLayer.Model.Style.GradientCenterX;
        GradientCenterYEditor = SelectedLayer.Model.Style.GradientCenterY;
        GradientScaleXEditor = SelectedLayer.Model.Style.GradientScaleX;
        GradientScaleYEditor = SelectedLayer.Model.Style.GradientScaleY;
        GradientFromEditor = SelectedLayer.Model.Style.GradientFrom;
        GradientToEditor = SelectedLayer.Model.Style.GradientTo;
        StrokeCapStyleEditor = SelectedLayer.Model.Style.StrokeCapStyle;
        StrokeJoinStyleEditor = SelectedLayer.Model.Style.StrokeJoinStyle;
        StrokeMiterLimitEditor = SelectedLayer.Model.Style.StrokeMiterLimit;
        ShapeDrawingModeEditor = SelectedLayer.Model.Style.DrawingMode;
        CornerRadiusTopLeftEditor = SelectedLayer.Model.Style.CornerRadiusTopLeft;
        CornerRadiusTopRightEditor = SelectedLayer.Model.Style.CornerRadiusTopRight;
        CornerRadiusBottomRightEditor = SelectedLayer.Model.Style.CornerRadiusBottomRight;
        CornerRadiusBottomLeftEditor = SelectedLayer.Model.Style.CornerRadiusBottomLeft;
        EllipseStartAngleEditor = SelectedLayer.Model.Style.EllipseStartAngle;
        EllipseSweepAngleEditor = SelectedLayer.Model.Style.EllipseSweepAngle;
        PolyStarSidesEditor = SelectedLayer.Model.Style.PolyStarSides;
        PolyStarInnerRadiusEditor = SelectedLayer.Model.Style.PolyStarInnerRadius;
        PolyStarIsStarEditor = SelectedLayer.Model.Style.PolyStarIsStar;
        if (SelectedLayer.Kind != LayerKind.Path || SelectedPathPointIndexEditor >= SelectedLayer.Model.Style.PathPoints.Count)
        {
            SelectedPathPointIndexEditor = -1;
        }

        RefreshSelectedPathPointEditors();
        TextEditor = SelectedLayer.TextContent;
        PathClosedEditor = SelectedLayer.Model.Style.IsClosed;
        InspectorX = SelectedLayer.X;
        InspectorY = SelectedLayer.Y;
        InspectorWidth = SelectedLayer.Width;
        InspectorHeight = SelectedLayer.Height;
        InspectorScaleX = SelectedLayer.ScaleX;
        InspectorScaleY = SelectedLayer.ScaleY;
        InspectorSkewX = SelectedLayer.SkewX;
        InspectorSkewY = SelectedLayer.SkewY;
        InspectorRotation = SelectedLayer.Rotation;
        InspectorRotationX = SelectedLayer.RotationX;
        InspectorRotationY = SelectedLayer.RotationY;
        InspectorZDepth = SelectedLayer.ZDepth;
        InspectorOpacity = SelectedLayer.Opacity;
        InspectorCornerRadius = SelectedLayer.CornerRadius;
        InspectorFontSize = SelectedLayer.FontSize;
        TextFontFamilyEditor = SelectedLayer.Model.Style.TextSettings.FontFamily;
        TextBoldEditor = SelectedLayer.Model.Style.TextSettings.IsBold;
        TextItalicEditor = SelectedLayer.Model.Style.TextSettings.IsItalic;
        TextAlignmentEditor = SelectedLayer.Model.Style.TextSettings.Alignment;
        TextLetterSpacingEditor = SelectedLayer.Model.Style.TextSettings.LetterSpacing;
        TextLineHeightEditor = SelectedLayer.Model.Style.TextSettings.LineHeight;
        TextRenderAsHtmlEditor = SelectedLayer.Model.Style.TextSettings.RenderAsHtml;
        TextFieldKindEditor = SelectedLayer.Model.Style.TextSettings.FieldKind;
        TextLineModeEditor = SelectedLayer.Model.Style.TextSettings.LineMode;
        TextSelectableEditor = SelectedLayer.Model.Style.TextSettings.IsSelectable;
        TextShowBorderEditor = SelectedLayer.Model.Style.TextSettings.ShowBorder;
        TextVariableNameEditor = SelectedLayer.Model.Style.TextSettings.VariableName;
        TextMaxCharactersEditor = SelectedLayer.Model.Style.TextSettings.MaxCharacters;
        TextPasswordEditor = SelectedLayer.Model.Style.TextSettings.IsPassword;
        TextUseDeviceFontsEditor = SelectedLayer.Model.Style.TextSettings.UseDeviceFonts;
        TextAntiAliasModeEditor = SelectedLayer.Model.Style.TextSettings.AntiAliasMode;
        AvaloniaControlContentEditor = SelectedLayer.Model.Style.AvaloniaControl.Content;
        AvaloniaControlSecondaryContentEditor = SelectedLayer.Model.Style.AvaloniaControl.SecondaryContent;
        AvaloniaControlIsCheckedEditor = SelectedLayer.Model.Style.AvaloniaControl.IsChecked;
        AvaloniaControlMinimumEditor = SelectedLayer.Model.Style.AvaloniaControl.Minimum;
        AvaloniaControlMaximumEditor = SelectedLayer.Model.Style.AvaloniaControl.Maximum;
        AvaloniaControlValueEditor = SelectedLayer.Model.Style.AvaloniaControl.Value;
        AvaloniaControlIsIndeterminateEditor = SelectedLayer.Model.Style.AvaloniaControl.IsIndeterminate;
        AvaloniaControlSourceEditor = SelectedLayer.Model.Style.AvaloniaControl.Source;
        LayerRoleEditor = SelectedLayer.Model.Compositing.Role;
        LayerBlendModeEditor = SelectedLayer.Model.Compositing.BlendMode;
        MaskLayerCountEditor = Math.Max(1, SelectedLayer.Model.Compositing.MaskLayerCount);
        LayerBlurEditor = SelectedLayer.Model.Compositing.BlurRadius;
        LayerGlowSizeEditor = SelectedLayer.Model.Compositing.GlowSize;
        LayerGlowOpacityEditor = SelectedLayer.Model.Compositing.GlowOpacity;
        LayerGlowColorEditor = SelectedLayer.Model.Compositing.GlowColor;
        LayerShadowBlurEditor = SelectedLayer.Model.Compositing.ShadowBlur;
        LayerShadowOpacityEditor = SelectedLayer.Model.Compositing.ShadowOpacity;
        LayerShadowOffsetXEditor = SelectedLayer.Model.Compositing.ShadowOffsetX;
        LayerShadowOffsetYEditor = SelectedLayer.Model.Compositing.ShadowOffsetY;
        LayerShadowColorEditor = SelectedLayer.Model.Compositing.ShadowColor;
        LayerTintColorEditor = SelectedLayer.Model.Compositing.TintColor;
        LayerTintStrengthEditor = SelectedLayer.Model.Compositing.TintStrength;
        LayerBrightnessEditor = SelectedLayer.Model.Compositing.Brightness;
        LayerContrastEditor = SelectedLayer.Model.Compositing.Contrast;
        LayerHueShiftEditor = SelectedLayer.Model.Compositing.HueShift;
        LayerBevelTypeEditor = SelectedLayer.Model.Compositing.BevelType;
        LayerBevelSizeEditor = SelectedLayer.Model.Compositing.BevelSize;
        LayerBevelStrengthEditor = SelectedLayer.Model.Compositing.BevelStrength;
        LayerBevelAngleEditor = SelectedLayer.Model.Compositing.BevelAngle;
        LayerBevelDistanceEditor = SelectedLayer.Model.Compositing.BevelDistance;
        LayerBevelHighlightColorEditor = SelectedLayer.Model.Compositing.BevelHighlightColor;
        LayerBevelShadowColorEditor = SelectedLayer.Model.Compositing.BevelShadowColor;
        LayerGradientGlowSizeEditor = SelectedLayer.Model.Compositing.GradientGlowSize;
        LayerGradientGlowStrengthEditor = SelectedLayer.Model.Compositing.GradientGlowStrength;
        LayerGradientGlowAngleEditor = SelectedLayer.Model.Compositing.GradientGlowAngle;
        LayerGradientGlowDistanceEditor = SelectedLayer.Model.Compositing.GradientGlowDistance;
        LayerGradientGlowFromEditor = SelectedLayer.Model.Compositing.GradientGlowFrom;
        LayerGradientGlowToEditor = SelectedLayer.Model.Compositing.GradientGlowTo;
        LayerGradientBevelTypeEditor = SelectedLayer.Model.Compositing.GradientBevelType;
        LayerGradientBevelSizeEditor = SelectedLayer.Model.Compositing.GradientBevelSize;
        LayerGradientBevelStrengthEditor = SelectedLayer.Model.Compositing.GradientBevelStrength;
        LayerGradientBevelAngleEditor = SelectedLayer.Model.Compositing.GradientBevelAngle;
        LayerGradientBevelDistanceEditor = SelectedLayer.Model.Compositing.GradientBevelDistance;
        LayerGradientBevelFromEditor = SelectedLayer.Model.Compositing.GradientBevelFrom;
        LayerGradientBevelToEditor = SelectedLayer.Model.Compositing.GradientBevelTo;
        LayerFlashColorEffectModeEditor = SelectedLayer.Model.Compositing.FlashColorEffectMode;
        LayerFlashAlphaPercentEditor = SelectedLayer.Model.Compositing.FlashAlphaPercent;
        LayerFlashTintColorEditor = SelectedLayer.Model.Compositing.FlashTintColor;
        LayerFlashTintPercentEditor = SelectedLayer.Model.Compositing.FlashTintPercent;
        LayerFlashBrightnessPercentEditor = SelectedLayer.Model.Compositing.FlashBrightnessPercent;
        LayerFlashRedPercentEditor = SelectedLayer.Model.Compositing.FlashRedPercent;
        LayerFlashGreenPercentEditor = SelectedLayer.Model.Compositing.FlashGreenPercent;
        LayerFlashBluePercentEditor = SelectedLayer.Model.Compositing.FlashBluePercent;
        LayerFlashAdvancedAlphaPercentEditor = SelectedLayer.Model.Compositing.FlashAdvancedAlphaPercent;
        LayerFlashRedOffsetEditor = SelectedLayer.Model.Compositing.FlashRedOffset;
        LayerFlashGreenOffsetEditor = SelectedLayer.Model.Compositing.FlashGreenOffset;
        LayerFlashBlueOffsetEditor = SelectedLayer.Model.Compositing.FlashBlueOffset;
        LayerFlashAlphaOffsetEditor = SelectedLayer.Model.Compositing.FlashAlphaOffset;
        LayerSaturationEditor = SelectedLayer.Model.Compositing.Saturation;
        LayerParallaxDepthEditor = SelectedLayer.Model.Compositing.ParallaxDepth;
        MediaPlaybackModeEditor = SelectedLayer.Model.Media.PlaybackMode;
        MediaStartTimeEditor = SelectedLayer.Model.Media.StartTime;
        MediaClipOffsetEditor = SelectedLayer.Model.Media.ClipOffset;
        MediaClipDurationEditor = Math.Max(0.05d, SelectedLayer.Model.Media.ClipDuration);
        MediaLoopEditor = SelectedLayer.Model.Media.Loop;
        MediaRepeatCountEditor = Math.Max(1, SelectedLayer.Model.Media.RepeatCount);
        MediaRepeatForeverEditor = SelectedLayer.Model.Media.RepeatForever || SelectedLayer.Model.Media.Loop;
        MediaVolumeEditor = SelectedLayer.Model.Media.Volume;
        MediaGainDbEditor = SelectedLayer.Model.Media.GainDb;
        MediaPanEditor = SelectedLayer.Model.Media.Pan;
        MediaFadeInEditor = SelectedLayer.Model.Media.FadeInDuration;
        MediaFadeOutEditor = SelectedLayer.Model.Media.FadeOutDuration;
        LayerOutlineEditor = SelectedLayer.Model.ShowAsOutline;
        LayerOutlineColorEditor = SelectedLayer.Model.OutlineColor;
        LayerCacheAsBitmapEditor = SelectedLayer.Model.CacheAsBitmap;
        LayerBitmapCacheBackgroundEditor = SelectedLayer.Model.BitmapCacheBackgroundColor;
        SelectedTransformParentLayer = SelectedLayer.Model.TransformParenting.ParentLayerId is Guid transformParentLayerId
            ? Layers.FirstOrDefault(item => item.Id == transformParentLayerId)
            : null;
        SelectedGuideLayer = SelectedLayer.Model.GuidedByLayerId is Guid guideLayerId
            ? Layers.FirstOrDefault(item => item.Id == guideLayerId)
            : null;
        GuideOrientToPathEditor = SelectedLayer.Model.OrientToGuidePath;
        GuideSnapToPathEditor = SelectedLayer.Model.SnapToGuidePath;
        WarpEnabledEditor = SelectedLayer.Model.Warp.IsEnabled;
        WarpMeshColumnsEditor = SelectedLayer.Model.Warp.MeshColumns;
        WarpMeshRowsEditor = SelectedLayer.Model.Warp.MeshRows;
        RigEnabledEditor = SelectedLayer.Model.Rig.IsEnabled;
        RigPoseNameEditor = SelectedLayer.Model.Rig.Poses.Count == 0
            ? "Pose 1"
            : $"Pose {SelectedLayer.Model.Rig.Poses.Count + 1}";
        ReloadRigPoses();
        RefreshSymbolInstanceEditor();

        _suppressInspector = false;
        RefreshShapeStyleStateProperties();
        RefreshFlashColorEffectStateProperties();
        RefreshFlashTextFieldStateProperties();
        RefreshGuideBindingStateProperties();
        RefreshTransformParentingStateProperties();
        RefreshAuthoringMetadataStateProperties();
        RefreshWarpRigStateProperties();
        RefreshAudioMeter();
    }

    private void ReloadRigPoses()
    {
        RigPoses.Clear();
        if (SelectedLayer is null)
        {
            SelectedRigPose = null;
            return;
        }

        foreach (var pose in SelectedLayer.Model.Rig.Poses.OrderBy(item => item.Time))
        {
            RigPoses.Add(new RigPoseViewModel(pose));
        }

        SelectedRigPose = SelectedRigPose is null
            ? RigPoses.FirstOrDefault()
            : RigPoses.FirstOrDefault(item => item.Id == SelectedRigPose.Id) ?? RigPoses.FirstOrDefault();
    }

    private void UpdateSelectedLayerCompositing(Action<LayerCompositeSettings> update, string statusMessage)
    {
        if (_suppressInspector || !CanEditSelection || SelectedLayer is null)
        {
            return;
        }

        update(SelectedLayer.Model.Compositing);
        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        ReloadTimelineRows();
        RefreshInspector();
        RefreshSelectionStateProperties();
        RecordHistoryIfNeeded();
        StatusMessage = statusMessage;
    }

    private void UpdateSelectedTextMetadata(Action<LayerTextSettings> update, string statusMessage)
    {
        if (_suppressInspector || !CanEditSelection || !SelectedLayerIsText || SelectedLayer is null)
        {
            return;
        }

        update(SelectedLayer.Model.Style.TextSettings);
        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        RefreshFlashTextFieldStateProperties();
        RecordHistoryIfNeeded();
        StatusMessage = statusMessage;
    }

    private void UpdateSelectedShapeAuthoring(Action<LayerStyle> update, string statusMessage)
    {
        if (_suppressInspector || !CanEditSelection || SelectedLayer is null)
        {
            return;
        }

        update(SelectedLayer.Model.Style);
        PrimitiveShapeService.NormalizeStyle(SelectedLayer.Model.Style);
        if (SelectedLayer.Model.Style.PrimitiveShape == PrimitiveShapeType.PolyStar)
        {
            PrimitiveShapeService.UpdatePolyStarGeometry(SelectedLayer.Model);
        }

        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        ReloadTracks();
        RefreshShapeStyleStateProperties();
        RecordHistoryIfNeeded();
        StatusMessage = statusMessage;
    }

    private void UpdateCornerRadiusEditor(double value, Action<double> setEditorValue, Action<LayerStyle, double> applyValue, string statusMessage)
    {
        if (_suppressInspector)
        {
            return;
        }

        var normalized = Math.Max(0d, value);
        if (Math.Abs(normalized - value) > 0.0001d)
        {
            _suppressInspector = true;
            setEditorValue(normalized);
            _suppressInspector = false;
            return;
        }

        if (SelectedLayer is null)
        {
            RefreshShapeStyleStateProperties();
            return;
        }

        UpdateSelectedShapeAuthoring(
            style => applyValue(style, normalized),
            statusMessage);
    }

    private static double NormalizeShapeAngle(double value)
    {
        var normalized = value % 360d;
        return normalized < 0d ? normalized + 360d : normalized;
    }

    private void UpdateSelectedLayerAuthoringMetadata(Action<TimelineLayer> update, string statusMessage)
    {
        if (_suppressInspector || !CanEditSelection || SelectedLayer is null)
        {
            return;
        }

        update(SelectedLayer.Model);
        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        RefreshAuthoringMetadataStateProperties();
        RecordHistoryIfNeeded();
        StatusMessage = statusMessage;
    }

    private void RefreshFlashTextFieldStateProperties()
    {
        OnPropertyChanged(nameof(SelectedTextUsesInteractiveFieldMetadata));
        OnPropertyChanged(nameof(SelectedTextUsesVariableName));
        OnPropertyChanged(nameof(SelectedTextFieldSummary));
        OnPropertyChanged(nameof(SelectedAnimationExchangeSummary));
    }

    private void RefreshShapeStyleStateProperties()
    {
        OnPropertyChanged(nameof(SelectedLayerUsesMiterJoin));
        OnPropertyChanged(nameof(SelectedLayerSupportsPrimitiveCornerRadii));
        OnPropertyChanged(nameof(SelectedLayerSupportsPrimitiveEllipse));
        OnPropertyChanged(nameof(SelectedLayerSupportsPolyStar));
        OnPropertyChanged(nameof(CanEditShapeAuthoring));
        OnPropertyChanged(nameof(CanUseShapeQuickControls));
        OnPropertyChanged(nameof(ShapeAuthoringShowsRectangleOptions));
        OnPropertyChanged(nameof(ShapeAuthoringShowsEllipseOptions));
        OnPropertyChanged(nameof(ShapeAuthoringShowsPolyStarOptions));
        OnPropertyChanged(nameof(ShapeAuthoringSummary));
    }

    private void RefreshFlashColorEffectStateProperties()
    {
        OnPropertyChanged(nameof(SelectedLayerUsesFlashAlphaEffect));
        OnPropertyChanged(nameof(SelectedLayerUsesFlashTintEffect));
        OnPropertyChanged(nameof(SelectedLayerUsesFlashBrightnessEffect));
        OnPropertyChanged(nameof(SelectedLayerUsesFlashAdvancedColorEffect));
        OnPropertyChanged(nameof(FlashColorEffectSummary));
        OnPropertyChanged(nameof(CompositingSummary));
    }

    private void RefreshGuideBindingStateProperties()
    {
        OnPropertyChanged(nameof(CanEditGuideBinding));
        OnPropertyChanged(nameof(AvailableGuideLayers));
        OnPropertyChanged(nameof(HasAvailableGuideLayers));
        OnPropertyChanged(nameof(HasGuideBinding));
        OnPropertyChanged(nameof(GuideBindingSummary));
    }

    private void RefreshTransformParentingStateProperties()
    {
        OnPropertyChanged(nameof(CanEditTransformParenting));
        OnPropertyChanged(nameof(AvailableTransformParentLayers));
        OnPropertyChanged(nameof(HasTransformParent));
        OnPropertyChanged(nameof(TransformParentingSummary));
    }

    private void RefreshAuthoringMetadataStateProperties()
    {
        OnPropertyChanged(nameof(CanEditBitmapCacheMetadata));
        OnPropertyChanged(nameof(LayerAuthoringSummary));
    }

    private void RefreshWarpRigStateProperties()
    {
        OnPropertyChanged(nameof(CanEditWarpRigging));
        OnPropertyChanged(nameof(HasWarpPins));
        OnPropertyChanged(nameof(HasRigBones));
        OnPropertyChanged(nameof(HasRigPoses));
        OnPropertyChanged(nameof(CanApplySelectedRigPose));
        OnPropertyChanged(nameof(CanDeleteSelectedRigPose));
        OnPropertyChanged(nameof(WarpRigSummary));
        OnPropertyChanged(nameof(SelectedRigPoseSummary));
    }

    private void RefreshSelectedPathPointEditors()
    {
        _suppressInspector = true;

        if (SelectedLayer is null || SelectedLayer.Kind != LayerKind.Path)
        {
            SelectedPathPointIndexEditor = -1;
            SelectedPathPointHandleModeEditor = VectorHandleMode.Corner;
            SelectedPathPointWidthScaleEditor = 1d;
            _suppressInspector = false;
            return;
        }

        if (SelectedPathPointIndexEditor < 0 || SelectedPathPointIndexEditor >= SelectedLayer.Model.Style.PathPoints.Count)
        {
            SelectedPathPointHandleModeEditor = VectorHandleMode.Corner;
            SelectedPathPointWidthScaleEditor = 1d;
            _suppressInspector = false;
            return;
        }

        var point = SelectedLayer.Model.Style.PathPoints[SelectedPathPointIndexEditor];
        SelectedPathPointHandleModeEditor = point.HandleMode;
        SelectedPathPointWidthScaleEditor = point.StrokeWidthScale;
        _suppressInspector = false;
    }

    private void UpdateSelectedMediaLayer(Action<LayerMediaSettings> update, string statusMessage)
    {
        if (_suppressInspector || !CanEditSelection || !SelectedLayerIsMedia || SelectedLayer is null)
        {
            return;
        }

        update(SelectedLayer.Model.Media);
        ReloadPreviewForLayer(SelectedLayer);
        ReloadFrameRows();
        RefreshInspector();
        RefreshSelectionStateProperties();
        RefreshAudioMeter();
        RecordHistoryIfNeeded();
        StatusMessage = statusMessage;
    }

    private void RefreshAudioMeter()
    {
        var asset = GetSelectedMediaAssetModel();
        var meter = SelectedLayer is null || asset is null
            ? AudioMeterSnapshot.Silent
            : AudioMixService.BuildMeterSnapshot(SelectedLayer.Model, asset, CurrentTime);
        AudioMeterPeak = meter.Peak;
        AudioMeterRms = meter.Rms;
        AudioMeterLeftGain = meter.LeftGain;
        AudioMeterRightGain = meter.RightGain;
        AudioMeterIsAudible = meter.IsAudible;
        OnPropertyChanged(nameof(AudioMeterSummary));
        OnPropertyChanged(nameof(MediaAudioSummary));
        OnPropertyChanged(nameof(MediaRepeatSummary));
        OnPropertyChanged(nameof(MediaLayerSummary));
    }

    private MediaAsset? GetSelectedMediaAssetModel()
    {
        if (SelectedLayer?.Model.Media.SourceMediaAssetId is not Guid mediaAssetId)
        {
            return null;
        }

        return _document.MediaAssets.FirstOrDefault(item => item.Id == mediaAssetId);
    }

    private void UpdateSelectedAvaloniaControl(Action<AvaloniaControlSettings> update, string statusMessage)
    {
        if (_suppressInspector || !CanEditAvaloniaControl || SelectedLayer is null)
        {
            return;
        }

        update(SelectedLayer.Model.Style.AvaloniaControl);
        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = statusMessage;
    }

    private void ApplyInspectorValue(AnimatedProperty property, double value)
    {
        if (_suppressInspector || IsPlaying || !CanEditPropertyTracks || SelectedLayer is null)
        {
            return;
        }

        var normalized = property switch
        {
            AnimatedProperty.Width or AnimatedProperty.Height => Math.Max(24, value),
            AnimatedProperty.Opacity => TimelineMath.Clamp(value, 0, 1),
            _ => value
        };

        if (EditMultipleFramesEnabled && SelectionStartFrame >= 0 && SelectionEndFrame >= 0)
        {
            PowerEditingService.ApplyValueAcrossFrames(
                SelectedLayer.Model,
                property,
                normalized,
                SelectionStartFrame,
                SelectionEndFrame,
                SceneFrameRate,
                Duration);
        }
        else
        {
            TimelineEditingService.ApplyValue(
                SelectedLayer.Model,
                property,
                normalized,
                CurrentTime,
                ShouldCreateKeyframe(),
                Duration);
        }

        ReloadPreviewForLayer(SelectedLayer);
        ReloadTracks();
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = $"{GetPropertyTitle(property)} updated";
    }

    partial void OnSelectedKeyframeEasingChanged(EasingKind value)
    {
        if (_suppressSelectedKeyframeEditor || !CanEditSelection)
        {
            return;
        }

        var keyframe = FindSelectedKeyframeModel();
        if (keyframe is null)
        {
            return;
        }

        keyframe.Easing = value;
        ReloadTracks();
        RefreshSelectedKeyframeEditor();
        RecordHistoryIfNeeded();
        StatusMessage = "Keyframe easing updated";
    }

    partial void OnCustomEaseControl1XChanged(double value) => UpdateSelectedCustomCurve(
        keyframe => keyframe.ControlPoint1X = TimelineMath.Clamp(value, 0, 1));

    partial void OnCustomEaseControl1YChanged(double value) => UpdateSelectedCustomCurve(
        keyframe => keyframe.ControlPoint1Y = TimelineMath.Clamp(value, 0, 1));

    partial void OnCustomEaseControl2XChanged(double value) => UpdateSelectedCustomCurve(
        keyframe => keyframe.ControlPoint2X = TimelineMath.Clamp(value, 0, 1));

    partial void OnCustomEaseControl2YChanged(double value) => UpdateSelectedCustomCurve(
        keyframe => keyframe.ControlPoint2Y = TimelineMath.Clamp(value, 0, 1));

    private void ReloadPreviewForLayer(LayerViewModel layer)
    {
        layer.RefreshMetadata();
        var frameSample = FrameTimelineService.SampleLayer(layer.Model, CurrentTime, SceneFrameRate, TotalFrames);
        var snapshot = SampleWorldLayerSnapshot(layer.Model, CurrentTime);
        layer.ApplyPreviewSnapshot(snapshot, frameSample is not null);
    }

    private LayerSnapshot SampleWorldLayerSnapshot(TimelineLayer layer, double time)
    {
        return LayerParentingService.SampleWorldSnapshot(_document.Layers, layer, time, SceneFrameRate, TotalFrames)
               ?? (FrameTimelineService.SampleLayer(layer, time, SceneFrameRate, TotalFrames)
                   ?? TimelineInterpolationService.SampleLayer(layer, time));
    }

    private bool ShouldCreateKeyframe()
    {
        return !IsPlaying && (AutoKey || CurrentTime > 0.0001d);
    }

    private double Snap(double time)
    {
        return SnapToGrid ? TimelineMath.Snap(time, 0.1d) : time;
    }

    private EditorStateSnapshot CaptureSnapshot()
    {
        return new EditorStateSnapshot(
            BuildPersistedDocumentSnapshot(),
            SelectedScene?.Id,
            _editingLibraryItemId,
            EditingButtonState,
            SelectedLayer?.Id,
            SelectedProperty,
            SelectedKeyframeId,
            CurrentTime);
    }

    private void ResetHistory()
    {
        var snapshot = CaptureSnapshot();
        _history ??= new UndoRedoStack<EditorStateSnapshot>(snapshot, EditorStateSnapshotComparer.Instance);
        _history.Reset(snapshot);
        UpdateHistoryAvailability();
    }

    private void RefreshSelectedKeyframeEditor()
    {
        _suppressSelectedKeyframeEditor = true;
        var keyframe = FindSelectedKeyframeModel();
        SelectedKeyframeEasing = keyframe?.Easing ?? EasingKind.Linear;
        _suppressSelectedKeyframeEditor = false;
        _suppressCustomEasingEditor = true;
        CustomEaseControl1X = keyframe?.ControlPoint1X ?? 0.25d;
        CustomEaseControl1Y = keyframe?.ControlPoint1Y ?? 0.1d;
        CustomEaseControl2X = keyframe?.ControlPoint2X ?? 0.25d;
        CustomEaseControl2Y = keyframe?.ControlPoint2Y ?? 1d;
        _suppressCustomEasingEditor = false;
        OnPropertyChanged(nameof(HasSelectedCustomEasing));
        OnPropertyChanged(nameof(CanEditCustomEasingCurve));
    }

    private void RefreshSymbolInstanceEditor()
    {
        _suppressSymbolInstanceEditor = true;
        InstanceNameEditor = SelectedLayer?.Model.InstanceName ?? string.Empty;
        SymbolPlaybackModeEditor = SelectedLayer?.Model.SymbolPlaybackMode ?? SymbolPlaybackMode.SceneTime;
        SymbolPlaybackOffsetEditor = SelectedLayer?.Model.SymbolPlaybackOffset ?? 0;
        SymbolLockedFrameEditor = SelectedLayer?.Model.SymbolLockedFrame ?? 0;
        SymbolButtonStateEditor = SelectedLayer?.Model.SymbolButtonState ?? ButtonVisualState.Up;
        _suppressSymbolInstanceEditor = false;
    }

    private KeyframeModel? FindSelectedKeyframeModel()
    {
        if (SelectedLayer is null || SelectedKeyframeId is null)
        {
            return null;
        }

        return SelectedLayer.Model.Tracks
            .SelectMany(track => track.Keyframes)
            .FirstOrDefault(keyframe => keyframe.Id == SelectedKeyframeId.Value);
    }

    private ShapeKeyframeModel? FindCurrentShapeKeyframeModel()
    {
        if (SelectedLayer is null || SelectedLayer.Kind != LayerKind.Path)
        {
            return null;
        }

        return SelectedLayer.Model.ShapeKeyframes
            .FirstOrDefault(keyframe => Math.Abs(keyframe.Time - CurrentTime) < 0.0001d);
    }

    private void UpdateSelectedCustomCurve(Action<KeyframeModel> update)
    {
        if (_suppressCustomEasingEditor || !CanEditCustomEasingCurve)
        {
            return;
        }

        var keyframe = FindSelectedKeyframeModel();
        if (keyframe is null)
        {
            return;
        }

        update(keyframe);
        ReloadTracks();
        RecordHistoryIfNeeded();
        StatusMessage = "Custom easing curve updated";
    }

    private void RecordHistoryIfNeeded()
    {
        if (_isApplyingHistory || _isInteractiveChange || _history is null || IsPrototypeMode)
        {
            return;
        }

        _history.Record(CaptureSnapshot());
        UpdateHistoryAvailability();
        RefreshPublishValidation();
    }

    private void ApplySnapshot(EditorStateSnapshot snapshot)
    {
        StopPlayback(false);
        _isApplyingHistory = true;
        _frameClipboard = null;

        _document = DocumentSerializer.Clone(snapshot.Document);
        SceneEditingService.EnsureScenes(_document);
        LibraryManagementService.EnsureLibraryMetadata(_document);
        PublishProfileService.EnsureProfiles(_document);
        DocumentName = _document.Name;
        ReloadLibraryItems();
        ReloadComponentItems();
        ReloadMediaAssets();
        ReloadPublishProfiles();
        ReloadScenes(snapshot.SelectedSceneId);
        SelectedProperty = snapshot.SelectedProperty;
        SelectedKeyframeId = snapshot.SelectedKeyframeId;
        if (snapshot.EditingLibraryItemId is Guid editingLibraryItemId)
        {
            EnterSymbolEdit(editingLibraryItemId, snapshot.EditingButtonState, snapshot.SelectedLayerId, snapshot.CurrentTime);
        }
        else
        {
            RebuildLayers(snapshot.SelectedLayerId);
            Seek(snapshot.CurrentTime);
            SelectionStartFrame = CurrentFrame;
            SelectionEndFrame = CurrentFrame;
            RefreshInspector();
            RefreshSelectedKeyframeEditor();
            RefreshFrameLabelEditor();
            RefreshFrameSelectionStateProperties();
            RefreshSymbolEditingProperties();
        }

        _isApplyingHistory = false;
    }

    private void UpdateHistoryAvailability()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(HistoryPanelSummary));
    }

    private void StartPlayback()
    {
        var playbackStart = GetCurrentScenePlaybackStartTime();
        var playbackEnd = GetCurrentScenePlaybackEndTime();
        if (CurrentTime < playbackStart || CurrentTime > playbackEnd)
        {
            Seek(playbackStart);
        }

        _playbackOriginTime = CurrentTime;
        _playbackClock.Restart();
        _lastProgramMonitorRefreshTick = -1;
        _lastPlaybackInspectorRefreshTick = -1;
        _lastPlaybackTimelinePreviewRefreshTick = -1;
        _lastPlaybackAudioRefreshTick = -1;
        _playbackTimer.Start();
        IsPlaying = true;
        if (IsPrototypeMode)
        {
            PrototypeStatusMessage = "Timeline playing";
        }

        StatusMessage = "Playback running";
    }

    private void HandlePlaybackTick(object? sender, EventArgs e)
    {
        var previousTime = CurrentTime;
        var elapsed = _playbackClock.Elapsed.TotalSeconds;
        var nextTime = _playbackOriginTime + elapsed;
        var playbackStart = GetCurrentScenePlaybackStartTime();
        var playbackEnd = GetCurrentScenePlaybackEndTime();

        if (nextTime > playbackEnd)
        {
            if (PlayAllScenes && TryAdvancePlaybackScene())
            {
                return;
            }

            if (LoopPlayback)
            {
                _playbackClock.Restart();
                _playbackOriginTime = playbackStart;
                Seek(playbackStart);
                return;
            }

            StopPlayback(true);
            Seek(playbackEnd);
            return;
        }

        var nextFrame = FrameTimelineService.TimeToFrame(nextTime, SceneFrameRate, TotalFrames);
        var quantizedTime = FrameTimelineService.FrameToTime(nextFrame, SceneFrameRate);
        if (Math.Abs(quantizedTime - CurrentTime) < 0.0001d)
        {
            return;
        }

        Seek(quantizedTime);
        if (IsPrototypeMode)
        {
            HandlePrototypeTimelineAdvance(previousTime, CurrentTime);
        }
    }

    private void StopPlayback(bool resetTime)
    {
        _playbackTimer.Stop();
        _playbackClock.Stop();
        _lastProgramMonitorRefreshTick = -1;
        _lastPlaybackInspectorRefreshTick = -1;
        _lastPlaybackTimelinePreviewRefreshTick = -1;
        _lastPlaybackAudioRefreshTick = -1;
        IsPlaying = false;

        if (resetTime)
        {
            Seek(GetCurrentScenePlaybackStartTime());
        }
        else
        {
            RefreshLayerPreviewsAtCurrentTime();
            UpdateTrackPreviewValues();
            UpdateTimelineRowPreviewValues();
            RefreshAudioMeter();
            RefreshInspector();
            RefreshFrameLabelEditor();
            RefreshProgramMonitorPreview(force: true);
        }

        if (IsPrototypeMode)
        {
            PrototypeStatusMessage = "Timeline paused";
        }

        StatusMessage = "Playback stopped";
    }

    private void HandlePrototypeTimelineAdvance(double previousTime, double currentTime)
    {
        if (!IsPrototypeMode || SelectedScene is null || currentTime <= previousTime)
        {
            return;
        }

        var startFrame = FrameTimelineService.TimeToFrame(previousTime, SceneFrameRate, TotalFrames);
        var endFrame = FrameTimelineService.TimeToFrame(currentTime, SceneFrameRate, TotalFrames);
        var labels = SelectedScene.Model.FrameLabels
            .Where(label => label.Frame > startFrame && label.Frame <= endFrame)
            .OrderBy(label => label.Frame)
            .ToList();
        foreach (var label in labels)
        {
            ExecuteTriggeredBehaviors(null, InteractionTriggerKind.FrameLabelReached, label.Name);
            if (!string.IsNullOrWhiteSpace(label.Script))
            {
                ExecuteActionScript(label.Script, null, $"frame label {label.Name}");
            }
            else
            {
                PrototypeStatusMessage = $"Reached label {label.Name}";
            }
        }

        RebuildLayers(SelectedLayer?.Id);
    }

    private void ApplyPrototypeButtonState(TimelineLayer layer, InteractionTriggerKind trigger)
    {
        if (layer.SourceLibraryItemId is not Guid libraryItemId)
        {
            return;
        }

        var libraryItem = _document.LibraryItems.FirstOrDefault(item => item.Id == libraryItemId);
        if (libraryItem?.SymbolKind != SymbolKind.Button)
        {
            return;
        }

        layer.SymbolButtonState = trigger switch
        {
            InteractionTriggerKind.PointerEnter => ButtonVisualState.Over,
            InteractionTriggerKind.PointerLeave => ButtonVisualState.Up,
            InteractionTriggerKind.PointerDown => ButtonVisualState.Down,
            InteractionTriggerKind.PointerUp => ButtonVisualState.Over,
            _ => layer.SymbolButtonState
        };
    }

    private void ExecuteTriggeredBehaviors(Guid? sourceLayerId, InteractionTriggerKind trigger, string? argument)
    {
        if (!IsPrototypeMode)
        {
            return;
        }

        var sourceLayer = sourceLayerId is Guid layerId
            ? _document.Layers.FirstOrDefault(item => item.Id == layerId)
            : null;

        IEnumerable<(InteractionBehaviorModel Behavior, Guid? SourceLayerId)> matches = sourceLayer is null && trigger == InteractionTriggerKind.FrameLabelReached
            ? _document.Layers
                .SelectMany(layer => layer.Behaviors
                    .Where(behavior => InteractionBehaviorService.MatchesTrigger(behavior, trigger, argument))
                    .Select(behavior => (behavior, (Guid?)layer.Id)))
            : sourceLayer?.Behaviors
                .Where(behavior => InteractionBehaviorService.MatchesTrigger(behavior, trigger, argument))
                .Select(behavior => (behavior, sourceLayerId)) ?? [];

        foreach (var (behavior, behaviorSourceLayerId) in matches.ToList())
        {
            ExecuteBehavior(behavior, behaviorSourceLayerId);
        }
    }

    private void ExecuteBehavior(InteractionBehaviorModel behavior, Guid? sourceLayerId)
    {
        var sourceLayer = sourceLayerId is Guid sourceId
            ? _document.Layers.FirstOrDefault(item => item.Id == sourceId)
            : null;

        switch (behavior.Action)
        {
            case InteractionActionKind.Play:
                if (!IsPlaying)
                {
                    StartPlayback();
                }

                PrototypeStatusMessage = "Play action fired";
                break;
            case InteractionActionKind.Stop:
                StopPlayback(false);
                PrototypeStatusMessage = "Stop action fired";
                break;
            case InteractionActionKind.GoToScene:
                if (behavior.TargetSceneId is Guid sceneId)
                {
                    var targetScene = Scenes.FirstOrDefault(item => item.Id == sceneId);
                    if (targetScene is not null)
                    {
                        SelectedScene = targetScene;
                        PrototypeStatusMessage = $"Switched to {targetScene.Name}";
                    }
                }

                break;
            case InteractionActionKind.GoToFrameLabel:
                if (!string.IsNullOrWhiteSpace(behavior.TargetFrameLabel))
                {
                    if (behavior.TargetSceneId is Guid targetSceneId)
                    {
                        var targetScene = Scenes.FirstOrDefault(item => item.Id == targetSceneId);
                        if (targetScene is not null)
                        {
                            SelectedScene = targetScene;
                        }
                    }

                    var label = SelectedScene?.Model.FrameLabels.FirstOrDefault(item =>
                        string.Equals(item.Name, behavior.TargetFrameLabel, StringComparison.OrdinalIgnoreCase));
                    if (label is not null)
                    {
                        SeekFrame(label.Frame);
                        PrototypeStatusMessage = $"Jumped to {label.Name}";
                    }
                }

                break;
            case InteractionActionKind.SetLayerVisibility:
                var visibilityLayer = ResolveBehaviorTargetLayer(behavior, sourceLayer);
                if (visibilityLayer is not null)
                {
                    visibilityLayer.IsVisible = behavior.BoolValue;
                    PrototypeStatusMessage = behavior.BoolValue
                        ? $"{visibilityLayer.Name} shown"
                        : $"{visibilityLayer.Name} hidden";
                }

                break;
            case InteractionActionKind.SetButtonState:
                var buttonLayer = ResolveBehaviorTargetLayer(behavior, sourceLayer);
                if (buttonLayer is not null)
                {
                    buttonLayer.SymbolButtonState = behavior.TargetButtonState;
                    PrototypeStatusMessage = $"{buttonLayer.Name} set to {behavior.TargetButtonState}";
                }

                break;
            case InteractionActionKind.SetVariable:
                if (!string.IsNullOrWhiteSpace(behavior.VariableName))
                {
                    _prototypeVariables[behavior.VariableName] = behavior.VariableValue;
                    OnPropertyChanged(nameof(PrototypeVariableSummary));
                    PrototypeStatusMessage = $"{behavior.VariableName} = {behavior.VariableValue}";
                }

                break;
            case InteractionActionKind.ApplyVisualState:
                {
                    var visualStateLayer = ResolveBehaviorTargetLayer(behavior, sourceLayer);
                    if (visualStateLayer is not null &&
                        VisualStateEditingService.ApplyState(
                            visualStateLayer,
                            behavior.TargetVisualStateGroup,
                            behavior.TargetVisualState))
                    {
                        RefreshPrototypeLayerState(visualStateLayer.Id);
                        PrototypeStatusMessage = $"{visualStateLayer.Name} state {behavior.TargetVisualState}";
                    }
                }

                break;
        }

        if (!string.IsNullOrWhiteSpace(behavior.Script))
        {
            ExecuteActionScript(behavior.Script, sourceLayerId, $"behavior {behavior.Name}");
        }

        OnPropertyChanged(nameof(PrototypeSummary));
    }

    private void ExecuteActionScript(string script, Guid? sourceLayerId, string sourceName)
    {
        var parse = ActionScriptService.Parse(script);
        if (parse.HasErrors)
        {
            PrototypeStatusMessage = $"{sourceName} script error • {parse.Errors[0]}";
            OnPropertyChanged(nameof(PrototypeSummary));
            return;
        }

        foreach (var command in parse.Commands)
        {
            ExecuteActionScriptCommand(command, sourceLayerId);
        }
    }

    private void ExecuteActionScriptCommand(ActionScriptCommand command, Guid? sourceLayerId)
    {
        var sourceLayer = sourceLayerId is Guid sourceId
            ? _document.Layers.FirstOrDefault(item => item.Id == sourceId)
            : null;

        switch (command.Kind)
        {
            case ActionScriptCommandKind.Play:
                if (!IsPlaying)
                {
                    StartPlayback();
                }

                PrototypeStatusMessage = "Script play";
                break;
            case ActionScriptCommandKind.Stop:
                StopPlayback(false);
                PrototypeStatusMessage = "Script stop";
                break;
            case ActionScriptCommandKind.GoToScene:
                {
                    var targetScene = Scenes.FirstOrDefault(item => string.Equals(item.Name, command.Arguments[0], StringComparison.OrdinalIgnoreCase));
                    if (targetScene is not null)
                    {
                        SelectedScene = targetScene;
                        PrototypeStatusMessage = $"Scene {targetScene.Name}";
                    }
                }

                break;
            case ActionScriptCommandKind.GoToLabel:
                {
                    var label = SelectedScene?.Model.FrameLabels.FirstOrDefault(item =>
                        string.Equals(item.Name, command.Arguments[0], StringComparison.OrdinalIgnoreCase));
                    if (label is not null)
                    {
                        SeekFrame(label.Frame);
                        PrototypeStatusMessage = $"Label {label.Name}";
                    }
                }

                break;
            case ActionScriptCommandKind.GoToFrame:
                if (int.TryParse(command.Arguments[0], out var targetFrame))
                {
                    SeekFrame(Math.Max(0, targetFrame - 1));
                    PrototypeStatusMessage = $"Frame {Math.Max(1, targetFrame)}";
                }

                break;
            case ActionScriptCommandKind.ShowLayer:
            case ActionScriptCommandKind.HideLayer:
                {
                    var targetLayer = ResolveActionScriptLayer(command.Arguments[0], sourceLayer);
                    if (targetLayer is not null)
                    {
                        targetLayer.IsVisible = command.Kind == ActionScriptCommandKind.ShowLayer;
                        PrototypeStatusMessage = targetLayer.IsVisible
                            ? $"{targetLayer.Name} shown"
                            : $"{targetLayer.Name} hidden";
                    }
                }

                break;
            case ActionScriptCommandKind.SetVariable:
                _prototypeVariables[command.Arguments[0]] = string.Join(" ", command.Arguments.Skip(1));
                OnPropertyChanged(nameof(PrototypeVariableSummary));
                PrototypeStatusMessage = $"{command.Arguments[0]} = {_prototypeVariables[command.Arguments[0]]}";
                break;
            case ActionScriptCommandKind.SetText:
                {
                    var targetLayer = ResolveActionScriptLayer(command.Arguments[0], sourceLayer);
                    if (targetLayer is not null)
                    {
                        targetLayer.Style.Text = string.Join(" ", command.Arguments.Skip(1));
                        PrototypeStatusMessage = $"{targetLayer.Name} text updated";
                    }
                }

                break;
            case ActionScriptCommandKind.SetButtonState:
                {
                    var targetLayer = ResolveActionScriptLayer(command.Arguments[0], sourceLayer);
                    if (targetLayer is not null &&
                        Enum.TryParse<ButtonVisualState>(command.Arguments[1], ignoreCase: true, out var buttonState))
                    {
                        targetLayer.SymbolButtonState = buttonState;
                        PrototypeStatusMessage = $"{targetLayer.Name} {buttonState}";
                    }
                }

                break;
            case ActionScriptCommandKind.SetVisualState:
                {
                    var targetLayer = ResolveActionScriptLayer(command.Arguments[0], sourceLayer);
                    if (targetLayer is not null &&
                        VisualStateEditingService.ApplyState(targetLayer, command.Arguments[1], command.Arguments[2]))
                    {
                        RefreshPrototypeLayerState(targetLayer.Id);
                        PrototypeStatusMessage = $"{targetLayer.Name} state {command.Arguments[2]}";
                    }
                }

                break;
            case ActionScriptCommandKind.Trace:
                PrototypeStatusMessage = string.Join(" ", command.Arguments);
                break;
        }

        OnPropertyChanged(nameof(PrototypeSummary));
    }

    private TimelineLayer? ResolveBehaviorTargetLayer(InteractionBehaviorModel behavior, TimelineLayer? sourceLayer)
    {
        if (behavior.TargetLayerId is Guid targetLayerId)
        {
            return _document.Layers.FirstOrDefault(item => item.Id == targetLayerId);
        }

        return sourceLayer;
    }

    private TimelineLayer? ResolveBehaviorVisualStateTargetLayer()
    {
        return SelectedBehaviorTargetLayer?.Model ?? SelectedLayer?.Model;
    }

    private TimelineLayer? ResolveActionScriptLayer(string target, TimelineLayer? sourceLayer)
    {
        if (string.Equals(target, "this", StringComparison.OrdinalIgnoreCase))
        {
            return sourceLayer;
        }

        return _document.Layers.FirstOrDefault(layer => string.Equals(layer.Name, target, StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshPrototypeLayerState(Guid layerId)
    {
        var layerViewModel = Layers.FirstOrDefault(item => item.Id == layerId);
        if (layerViewModel is not null)
        {
            ReloadPreviewForLayer(layerViewModel);
        }

        if (SelectedLayer?.Id == layerId)
        {
            ReloadVisualStateGroups(SelectedVisualStateGroup?.Id);
            ReloadVisualStates(SelectedVisualState?.Id);
            RefreshInspector();
        }
    }

    private void AlignSelection(double? x, double? y, string status)
    {
        if (!CanEditSelection || SelectedLayer is null)
        {
            return;
        }

        var targetBounds = new Rect(
            x ?? SelectedLayer.X,
            y ?? SelectedLayer.Y,
            SelectedLayer.Width,
            SelectedLayer.Height);
        UpdateLayerFrame(SelectedLayer.Id, targetBounds);
        StatusMessage = status;
    }

    private static string GetPropertyTitle(AnimatedProperty property)
    {
        return property switch
        {
            AnimatedProperty.X => "Position X",
            AnimatedProperty.Y => "Position Y",
            AnimatedProperty.Width => "Width",
            AnimatedProperty.Height => "Height",
            AnimatedProperty.ScaleX => "Scale X",
            AnimatedProperty.ScaleY => "Scale Y",
            AnimatedProperty.SkewX => "Skew X",
            AnimatedProperty.SkewY => "Skew Y",
            AnimatedProperty.Rotation => "Rotation",
            AnimatedProperty.RotationX => "Rotation X",
            AnimatedProperty.RotationY => "Rotation Y",
            AnimatedProperty.ZDepth => "Z Depth",
            AnimatedProperty.Opacity => "Opacity",
            _ => property.ToString()
        };
    }

    [RelayCommand]
    private void ToggleSelectedLayerVisibility()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.IsVisible = !SelectedLayer.Model.IsVisible;
        SelectedLayer.RefreshMetadata();
        ReloadFrameRows();
        ReloadTimelineRows();
        RecordHistoryIfNeeded();
        StatusMessage = SelectedLayer.Model.IsVisible ? "Layer shown" : "Layer hidden";
        RefreshSelectionStateProperties();
    }

    [RelayCommand]
    private void ToggleSelectedLayerLock()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.IsLocked = !SelectedLayer.Model.IsLocked;
        SelectedLayer.RefreshMetadata();
        ReloadFrameRows();
        ReloadTracks();
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = SelectedLayer.Model.IsLocked ? "Layer locked" : "Layer unlocked";
        RefreshSelectionStateProperties();
    }

    [RelayCommand]
    private void ToggleSelectedLayerMute()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.IsMuted = !SelectedLayer.Model.IsMuted;
        SelectedLayer.RefreshMetadata();
        RefreshAudioMeter();
        RefreshMonitorPreviews();
        ReloadFrameRows();
        RebuildLayers(SelectedLayer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = SelectedLayer.Model.IsMuted ? "Layer muted" : "Layer unmuted";
        RefreshSelectionStateProperties();
    }

    [RelayCommand]
    private void ToggleSelectedLayerSolo()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.IsSolo = !SelectedLayer.Model.IsSolo;
        SelectedLayer.RefreshMetadata();
        RefreshAudioMeter();
        RefreshMonitorPreviews();
        RebuildLayers(SelectedLayer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = SelectedLayer.Model.IsSolo ? "Layer soloed" : "Layer unsoloed";
        RefreshSelectionStateProperties();
    }

    [RelayCommand]
    private void ClearGuideBinding()
    {
        if (!CanEditGuideBinding || SelectedLayer is null)
        {
            return;
        }

        _suppressInspector = true;
        SelectedGuideLayer = null;
        GuideOrientToPathEditor = false;
        GuideSnapToPathEditor = false;
        _suppressInspector = false;

        SelectedLayer.Model.GuidedByLayerId = null;
        SelectedLayer.Model.OrientToGuidePath = false;
        SelectedLayer.Model.SnapToGuidePath = false;
        SelectedLayer.RefreshMetadata();
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Guide binding cleared";
        RefreshGuideBindingStateProperties();
    }

    [RelayCommand]
    private void ClearTransformParent()
    {
        if (!CanEditTransformParenting || SelectedLayer is null)
        {
            return;
        }

        _suppressInspector = true;
        SelectedTransformParentLayer = null;
        _suppressInspector = false;

        SelectedLayer.Model.TransformParenting = new TransformParentingSettings();
        SelectedLayer.RefreshMetadata();
        RebuildLayers(SelectedLayer.Id);
        RecordHistoryIfNeeded();
        StatusMessage = "Transform parent cleared";
        RefreshTransformParentingStateProperties();
    }

    [RelayCommand]
    private void EnsureDefaultWarpPins()
    {
        if (!CanEditWarpRigging || SelectedLayer is null)
        {
            return;
        }

        LayerDeformationService.EnsureDefaultWarpPins(SelectedLayer.Model);
        _suppressInspector = true;
        WarpEnabledEditor = true;
        WarpMeshColumnsEditor = SelectedLayer.Model.Warp.MeshColumns;
        WarpMeshRowsEditor = SelectedLayer.Model.Warp.MeshRows;
        _suppressInspector = false;
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Default warp pins created";
        RefreshWarpRigStateProperties();
    }

    [RelayCommand]
    private void EnsureDefaultRig()
    {
        if (!CanEditWarpRigging || SelectedLayer is null)
        {
            return;
        }

        LayerDeformationService.EnsureDefaultRig(SelectedLayer.Model);
        _suppressInspector = true;
        RigEnabledEditor = true;
        _suppressInspector = false;
        ReloadRigPoses();
        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = "Default rig created";
        RefreshWarpRigStateProperties();
    }

    [RelayCommand]
    private void CaptureRigPose()
    {
        if (!CanEditWarpRigging || SelectedLayer is null)
        {
            return;
        }

        if (!LayerDeformationService.CapturePose(SelectedLayer.Model, RigPoseNameEditor, CurrentTime))
        {
            return;
        }

        ReloadRigPoses();
        SelectedRigPose = RigPoses.LastOrDefault();
        RecordHistoryIfNeeded();
        StatusMessage = $"{SelectedRigPose?.Name ?? "Pose"} captured";
        RefreshWarpRigStateProperties();
    }

    [RelayCommand]
    private void ApplySelectedRigPose()
    {
        if (!CanEditWarpRigging || SelectedLayer is null || SelectedRigPose is null)
        {
            return;
        }

        if (!LayerDeformationService.ApplyPose(SelectedLayer.Model, SelectedRigPose.Id))
        {
            return;
        }

        ReloadPreviewForLayer(SelectedLayer);
        RecordHistoryIfNeeded();
        StatusMessage = $"{SelectedRigPose.Name} applied";
        RefreshWarpRigStateProperties();
    }

    [RelayCommand]
    private void DeleteSelectedRigPose()
    {
        if (!CanEditWarpRigging || SelectedLayer is null || SelectedRigPose is null)
        {
            return;
        }

        var deletedName = SelectedRigPose.Name;
        if (!LayerDeformationService.DeletePose(SelectedLayer.Model, SelectedRigPose.Id))
        {
            return;
        }

        ReloadRigPoses();
        SelectedRigPose = RigPoses.FirstOrDefault();
        RecordHistoryIfNeeded();
        StatusMessage = $"{deletedName} deleted";
        RefreshWarpRigStateProperties();
    }

    private void RefreshSelectionStateProperties()
    {
        OnPropertyChanged(nameof(CanEditSelection));
        OnPropertyChanged(nameof(CanEditPropertyTracks));
        OnPropertyChanged(nameof(CanEditStyleSurface));
        OnPropertyChanged(nameof(CanEditPathPoints));
        OnPropertyChanged(nameof(HasSelectedPathPoint));
        OnPropertyChanged(nameof(CanInsertPathPointAfterSelected));
        OnPropertyChanged(nameof(CanDeleteSelectedPathPoint));
        OnPropertyChanged(nameof(CanReverseSelectedPath));
        OnPropertyChanged(nameof(CanApplySelectedStrokeProfile));
        OnPropertyChanged(nameof(CanOptimizeSelectedPath));
        OnPropertyChanged(nameof(CanEditGradientTransform));
        OnPropertyChanged(nameof(CanAlignSelection));
        OnPropertyChanged(nameof(CanEditAvaloniaControl));
        OnPropertyChanged(nameof(CanCaptureShapeKeyframe));
        OnPropertyChanged(nameof(CanDeleteCurrentShapeKeyframe));
        OnPropertyChanged(nameof(CanToggleLayerState));
        OnPropertyChanged(nameof(CanCreateFolder));
        OnPropertyChanged(nameof(CanGroupSelectionIntoFolder));
        OnPropertyChanged(nameof(CanRemoveSelectionFromFolder));
        OnPropertyChanged(nameof(CanToggleSelectedFolderExpanded));
        OnPropertyChanged(nameof(CanUseEditorialTools));
        OnPropertyChanged(nameof(CanDeleteSelectedKeyframe));
        OnPropertyChanged(nameof(CanEditCustomEasingCurve));
        OnPropertyChanged(nameof(CanCaptureMotionPreset));
        OnPropertyChanged(nameof(CanApplySelectedMotionPreset));
        OnPropertyChanged(nameof(CanDeleteSelectedMotionPreset));
        OnPropertyChanged(nameof(SelectedLayerId));
        OnPropertyChanged(nameof(SelectedLayerIsPath));
        OnPropertyChanged(nameof(SelectedLayerIsFolder));
        OnPropertyChanged(nameof(SelectedLayerIsAvaloniaControl));
        OnPropertyChanged(nameof(SelectedLayerIsMask));
        OnPropertyChanged(nameof(SelectedLayerIsGuide));
        OnPropertyChanged(nameof(SelectedLayerIsCamera));
        OnPropertyChanged(nameof(SelectedLayerIsVideo));
        OnPropertyChanged(nameof(SelectedLayerIsAudio));
        OnPropertyChanged(nameof(SelectedLayerIsMedia));
        OnPropertyChanged(nameof(SelectedLayerIsSymbolInstance));
        OnPropertyChanged(nameof(SelectedLayerIsComponentInstance));
        OnPropertyChanged(nameof(CanEditSelectedLayerInstanceMetadata));
        OnPropertyChanged(nameof(CanEditSelectedComponentOverrides));
        OnPropertyChanged(nameof(CanResetSelectedComponentOverride));
        OnPropertyChanged(nameof(CanEditGuideBinding));
        OnPropertyChanged(nameof(CanEditTransformParenting));
        OnPropertyChanged(nameof(CanEditWarpRigging));
        OnPropertyChanged(nameof(CanEditBitmapCacheMetadata));
        OnPropertyChanged(nameof(SelectedLayerSupportsCornerRadius));
        OnPropertyChanged(nameof(SelectedLayerSupportsPrimitiveCornerRadii));
        OnPropertyChanged(nameof(SelectedLayerSupportsPrimitiveEllipse));
        OnPropertyChanged(nameof(SelectedLayerSupportsPolyStar));
        OnPropertyChanged(nameof(CanEditShapeAuthoring));
        OnPropertyChanged(nameof(CanUseShapeQuickControls));
        OnPropertyChanged(nameof(ShapeAuthoringShowsRectangleOptions));
        OnPropertyChanged(nameof(ShapeAuthoringShowsEllipseOptions));
        OnPropertyChanged(nameof(ShapeAuthoringShowsPolyStarOptions));
        OnPropertyChanged(nameof(SelectedSymbolInstanceIsButton));
        OnPropertyChanged(nameof(SelectedLayerVisibilityLabel));
        OnPropertyChanged(nameof(SelectedLayerLockLabel));
        OnPropertyChanged(nameof(SelectedLayerMuteLabel));
        OnPropertyChanged(nameof(SelectedLayerSoloLabel));
        OnPropertyChanged(nameof(SelectedLayerExpandLabel));
        OnPropertyChanged(nameof(SelectedLayerStateLabel));
        OnPropertyChanged(nameof(SelectionHeadline));
        OnPropertyChanged(nameof(CanUseSelectionActions));
        OnPropertyChanged(nameof(CanEditTextMenu));
        OnPropertyChanged(nameof(ModifyMenuHeader));
        OnPropertyChanged(nameof(ModifyMenuSummary));
        OnPropertyChanged(nameof(TextMenuSummary));
        OnPropertyChanged(nameof(ShapeTweenSummary));
        OnPropertyChanged(nameof(CompositingSummary));
        OnPropertyChanged(nameof(MediaLayerSummary));
        OnPropertyChanged(nameof(MediaRepeatSummary));
        OnPropertyChanged(nameof(LayerHierarchySummary));
        OnPropertyChanged(nameof(GuideBindingSummary));
        OnPropertyChanged(nameof(TransformParentingSummary));
        OnPropertyChanged(nameof(LayerAuthoringSummary));
        OnPropertyChanged(nameof(WarpRigSummary));
        OnPropertyChanged(nameof(SelectedRigPoseSummary));
        OnPropertyChanged(nameof(EditMultipleFramesSummary));
        OnPropertyChanged(nameof(MediaAudioSummary));
        OnPropertyChanged(nameof(AudioMeterSummary));
        OnPropertyChanged(nameof(ShapeAuthoringSummary));
        OnPropertyChanged(nameof(SelectedPathPointSummary));
        OnPropertyChanged(nameof(MotionPresetSummary));
        OnPropertyChanged(nameof(OnionSkinSummary));
        OnPropertyChanged(nameof(SelectedComponentOverrideSummary));
        OnPropertyChanged(nameof(CanEditVisualStates));
        OnPropertyChanged(nameof(CanApplySelectedVisualState));
        OnPropertyChanged(nameof(CanRemoveSelectedVisualState));
        OnPropertyChanged(nameof(CanRemoveSelectedVisualStateGroup));
        OnPropertyChanged(nameof(SelectedVisualStateSummary));
        RefreshFlashTextFieldStateProperties();
        RefreshGuideBindingStateProperties();
        RefreshTransformParentingStateProperties();
        RefreshAuthoringMetadataStateProperties();
        RefreshWarpRigStateProperties();
        RefreshBehaviorEditorStateProperties();
        RefreshFrameSelectionStateProperties();
        RefreshMotionPresetStateProperties();
    }

    private void RefreshLibraryStateProperties()
    {
        OnPropertyChanged(nameof(CanInsertFromLibrary));
        OnPropertyChanged(nameof(CanEditSelectedLibraryItem));
        OnPropertyChanged(nameof(CanInsertSelectedComponent));
        OnPropertyChanged(nameof(CanEditSelectedComponentParameters));
        OnPropertyChanged(nameof(CanRemoveSelectedComponentParameter));
        OnPropertyChanged(nameof(CanCreateSymbolFromSelection));
        OnPropertyChanged(nameof(CanCreateComponentFromSelection));
        OnPropertyChanged(nameof(CanUpdateLinkedSymbol));
        OnPropertyChanged(nameof(CanRelinkSelectionToSelectedLibraryItem));
        OnPropertyChanged(nameof(CanEnterSymbolEdit));
        OnPropertyChanged(nameof(SelectedLibrarySummary));
        OnPropertyChanged(nameof(SelectedComponentSummary));
        OnPropertyChanged(nameof(SelectedComponentParameterSummary));
    }

    private void RefreshMediaStateProperties()
    {
        OnPropertyChanged(nameof(CanInsertSelectedMediaAsset));
        OnPropertyChanged(nameof(SelectedMediaSummary));
    }

    private void RefreshPublishStateProperties()
    {
        OnPropertyChanged(nameof(CanEditPublishProfiles));
        OnPropertyChanged(nameof(CanDeletePublishProfile));
        OnPropertyChanged(nameof(CanPreviewSelectedPublishProfile));
        OnPropertyChanged(nameof(CanExportSelectedPublishProfile));
        OnPropertyChanged(nameof(SelectedPublishProfileSummary));
        OnPropertyChanged(nameof(HasPublishErrors));
        OnPropertyChanged(nameof(PublishOutputIsSpriteSheet));
        OnPropertyChanged(nameof(PublishOutputRequiresDirectory));
        OnPropertyChanged(nameof(PublishOutputIsSingleFile));
    }

    private void RefreshSymbolEditingProperties()
    {
        OnPropertyChanged(nameof(IsEditingSymbol));
        OnPropertyChanged(nameof(EditingLibraryItem));
        OnPropertyChanged(nameof(EditingSymbolIsButton));
        OnPropertyChanged(nameof(CanAddCameraLayer));
        OnPropertyChanged(nameof(CanInsertSelectedMediaAsset));
        OnPropertyChanged(nameof(CanInsertSelectedComponent));
        OnPropertyChanged(nameof(CanCreateComponentFromSelection));
        OnPropertyChanged(nameof(CanEditPublishProfiles));
        OnPropertyChanged(nameof(CanPreviewSelectedPublishProfile));
        OnPropertyChanged(nameof(CanExportSelectedPublishProfile));
        OnPropertyChanged(nameof(CanEnterSymbolEdit));
        OnPropertyChanged(nameof(CanExitSymbolEdit));
        OnPropertyChanged(nameof(CanEditScenes));
        OnPropertyChanged(nameof(CanEditCanvasProperties));
        OnPropertyChanged(nameof(CanTogglePrototypeModeMenu));
        OnPropertyChanged(nameof(CanManageFrameLabels));
        OnPropertyChanged(nameof(WorkspaceSummary));
        OnPropertyChanged(nameof(HelpMenuSummary));
    }

    private void RefreshSceneStateProperties()
    {
        RefreshAnimateDocumentCompatibilityIssues();
        OnPropertyChanged(nameof(CanDeleteScene));
        OnPropertyChanged(nameof(CanMoveSceneBackward));
        OnPropertyChanged(nameof(CanMoveSceneForward));
        OnPropertyChanged(nameof(CanAddCameraLayer));
        OnPropertyChanged(nameof(CanInsertSelectedMediaAsset));
        OnPropertyChanged(nameof(CanInsertSelectedComponent));
        OnPropertyChanged(nameof(CanEditScenes));
        OnPropertyChanged(nameof(CanEditCanvasProperties));
        OnPropertyChanged(nameof(CanManageFrameLabels));
        OnPropertyChanged(nameof(SelectedSceneSummary));
        OnPropertyChanged(nameof(CanvasSceneSummary));
        OnPropertyChanged(nameof(CanvasBackgroundSummary));
        OnPropertyChanged(nameof(ControlMenuSummary));
        OnPropertyChanged(nameof(WorkspaceSummary));
        OnPropertyChanged(nameof(HelpMenuSummary));
        OnPropertyChanged(nameof(CanEditSceneMarkers));
        OnPropertyChanged(nameof(CanRemoveSelectedSceneMarker));
        OnPropertyChanged(nameof(CurrentSceneInFrame));
        OnPropertyChanged(nameof(CurrentSceneOutFrame));
        OnPropertyChanged(nameof(CurrentSceneWorkAreaStartFrame));
        OnPropertyChanged(nameof(CurrentSceneWorkAreaEndFrame));
        OnPropertyChanged(nameof(CurrentSceneInFrameLabel));
        OnPropertyChanged(nameof(CurrentSceneOutFrameLabel));
        OnPropertyChanged(nameof(CurrentSceneWorkAreaLabel));
        OnPropertyChanged(nameof(SceneTransitionSummary));
        OnPropertyChanged(nameof(AnimateDocumentTypeLabel));
        OnPropertyChanged(nameof(AnimateDocumentSummary));
        OnPropertyChanged(nameof(AnimateDocumentCompatibilitySummary));
        OnPropertyChanged(nameof(HasAnimateDocumentCompatibilityIssues));
        OnPropertyChanged(nameof(AnimateDocumentConversionSummary));
        OnPropertyChanged(nameof(AnimateDocumentConvertButtonLabel));
        OnPropertyChanged(nameof(AnimateDocumentOptionsSummary));
        OnPropertyChanged(nameof(DocumentFileSummary));
    }

    private void RefreshAnimateDocumentCompatibilityIssues()
    {
        AnimateDocumentCompatibilityIssues.Clear();
        foreach (var issue in AnimateDocumentProfileService.ValidateCompatibility(BuildPersistedDocumentSnapshot()))
        {
            AnimateDocumentCompatibilityIssues.Add(issue);
        }
    }

    private void RefreshAnimateDocumentEditor()
    {
        _suppressAnimateDocumentEditor = true;
        AnimateDocumentProfileService.EnsureSettings(_document);
        AnimateDocumentTypeEditor = _document.Animate.Type;
        AnimateDocumentUnitsEditor = _document.Animate.Units;
        AnimateDocumentResponsiveEditor = _document.Animate.IsResponsive;
        AnimateDocumentExternalScriptEditor = _document.Animate.UseExternalScriptFile;
        AnimateDocumentWebFontsEditor = _document.Animate.UseWebFonts;
        AnimateDocumentAdvancedLayersEditor = _document.Animate.UseAdvancedLayers;
        AnimateDocumentEnable3DEditor = _document.Animate.Enable3DTransforms;
        AnimateDocumentPerspectiveAngleEditor = _document.Animate.PerspectiveAngle;
        AnimateDocumentVanishingPointXEditor = _document.Animate.VanishingPointX;
        AnimateDocumentVanishingPointYEditor = _document.Animate.VanishingPointY;
        AnimateDocumentTemplateNameEditor = _document.Animate.TemplateName;
        _suppressAnimateDocumentEditor = false;
    }

    private void RefreshWorkspaceLayoutProperties()
    {
        OnPropertyChanged(nameof(LeftPanelColumnWidth));
        OnPropertyChanged(nameof(LeftSplitterColumnWidth));
        OnPropertyChanged(nameof(RightPanelColumnWidth));
        OnPropertyChanged(nameof(RightSplitterColumnWidth));
        OnPropertyChanged(nameof(TimelinePanelRowHeight));
        OnPropertyChanged(nameof(TimelineSplitterRowHeight));
        OnPropertyChanged(nameof(LeftPanelColumnSpan));
        OnPropertyChanged(nameof(LeftPanelHostWidth));
        OnPropertyChanged(nameof(LeftPanelHorizontalAlignment));
        OnPropertyChanged(nameof(LeftPanelHostMargin));
        OnPropertyChanged(nameof(LeftPanelZIndex));
        OnPropertyChanged(nameof(IsLeftPanelVisible));
        OnPropertyChanged(nameof(RightPanelGridColumn));
        OnPropertyChanged(nameof(RightPanelColumnSpan));
        OnPropertyChanged(nameof(RightPanelHostWidth));
        OnPropertyChanged(nameof(RightPanelHorizontalAlignment));
        OnPropertyChanged(nameof(RightPanelHostMargin));
        OnPropertyChanged(nameof(RightPanelZIndex));
        OnPropertyChanged(nameof(IsRightPanelVisible));
        OnPropertyChanged(nameof(TimelinePanelGridRow));
        OnPropertyChanged(nameof(TimelinePanelRowSpan));
        OnPropertyChanged(nameof(TimelinePanelHostHeight));
        OnPropertyChanged(nameof(TimelinePanelVerticalAlignment));
        OnPropertyChanged(nameof(TimelinePanelHostMargin));
        OnPropertyChanged(nameof(TimelinePanelZIndex));
        OnPropertyChanged(nameof(IsTimelinePanelVisible));
        OnPropertyChanged(nameof(LeftPanelToggleLabel));
        OnPropertyChanged(nameof(RightPanelToggleLabel));
        OnPropertyChanged(nameof(TimelinePanelToggleLabel));
        OnPropertyChanged(nameof(WorkspaceLayoutLabel));
        OnPropertyChanged(nameof(WorkspaceLayoutSummary));
        OnPropertyChanged(nameof(IsLeftPanelCollapsedHandleVisible));
        OnPropertyChanged(nameof(IsRightPanelCollapsedHandleVisible));
        OnPropertyChanged(nameof(IsTimelinePanelCollapsedHandleVisible));
        OnPropertyChanged(nameof(HasHiddenWorkspacePanels));
        OnPropertyChanged(nameof(IsLeftPanelDocked));
        OnPropertyChanged(nameof(IsLeftPanelOverlay));
        OnPropertyChanged(nameof(IsLeftPanelHidden));
        OnPropertyChanged(nameof(IsRightPanelDocked));
        OnPropertyChanged(nameof(IsRightPanelOverlay));
        OnPropertyChanged(nameof(IsRightPanelHidden));
        OnPropertyChanged(nameof(IsTimelinePanelDocked));
        OnPropertyChanged(nameof(IsTimelinePanelOverlay));
        OnPropertyChanged(nameof(IsTimelinePanelHidden));
    }

    private void ApplyWorkspacePreset(WorkspaceLayoutPreset preset, bool updateStatus)
    {
        _suppressWorkspacePreset = true;
        SelectedWorkspacePreset = preset;

        switch (preset)
        {
            case WorkspaceLayoutPreset.Classic:
                LeftDockWidth = new GridLength(248);
                RightDockWidth = new GridLength(296);
                TimelineDockHeight = new GridLength(460);
                LeftPanelMode = WorkspacePanelMode.Docked;
                RightPanelMode = WorkspacePanelMode.Docked;
                TimelinePanelMode = WorkspacePanelMode.Docked;
                _leftRestoreMode = WorkspacePanelMode.Docked;
                _rightRestoreMode = WorkspacePanelMode.Docked;
                _timelineRestoreMode = WorkspacePanelMode.Docked;
                break;
            case WorkspaceLayoutPreset.StageFocus:
                LeftDockWidth = new GridLength(236);
                RightDockWidth = new GridLength(284);
                TimelineDockHeight = new GridLength(380);
                LeftPanelMode = WorkspacePanelMode.Overlay;
                RightPanelMode = WorkspacePanelMode.Overlay;
                TimelinePanelMode = WorkspacePanelMode.Overlay;
                _leftRestoreMode = WorkspacePanelMode.Overlay;
                _rightRestoreMode = WorkspacePanelMode.Overlay;
                _timelineRestoreMode = WorkspacePanelMode.Overlay;
                break;
            case WorkspaceLayoutPreset.TimelineFocus:
                LeftDockWidth = new GridLength(236);
                RightDockWidth = new GridLength(292);
                TimelineDockHeight = new GridLength(560);
                LeftPanelMode = WorkspacePanelMode.Hidden;
                RightPanelMode = WorkspacePanelMode.Overlay;
                TimelinePanelMode = WorkspacePanelMode.Docked;
                _leftRestoreMode = WorkspacePanelMode.Docked;
                _rightRestoreMode = WorkspacePanelMode.Overlay;
                _timelineRestoreMode = WorkspacePanelMode.Docked;
                break;
            case WorkspaceLayoutPreset.Custom:
            default:
                break;
        }

        _suppressWorkspacePreset = false;
        RefreshWorkspaceLayoutProperties();
        if (updateStatus)
        {
            StatusMessage = preset switch
            {
                WorkspaceLayoutPreset.Classic => "Classic workspace applied",
                WorkspaceLayoutPreset.StageFocus => "Stage focus workspace applied",
                WorkspaceLayoutPreset.TimelineFocus => "Timeline focus workspace applied",
                _ => "Custom workspace active"
            };
        }

        PersistWorkspaceLayoutIfReady();
    }

    private void MarkWorkspacePresetCustom()
    {
        if (_suppressWorkspacePreset || SelectedWorkspacePreset == WorkspaceLayoutPreset.Custom)
        {
            return;
        }

        _suppressWorkspacePreset = true;
        SelectedWorkspacePreset = WorkspaceLayoutPreset.Custom;
        _suppressWorkspacePreset = false;
    }

    private void RestoreWorkspaceLayout(bool updateStatus = false)
    {
        ApplyWorkspaceLayoutState(WorkspaceLayoutPersistenceService.Load(), updateStatus);
    }

    private void ApplyCustomWorkspaceFocus(
        WorkspacePanelMode leftMode,
        WorkspacePanelMode rightMode,
        WorkspacePanelMode timelineMode,
        double leftDockWidth,
        double rightDockWidth,
        double timelineDockHeight,
        string statusMessage)
    {
        var state = CaptureWorkspaceLayoutState();
        state.SelectedPreset = WorkspaceLayoutPreset.Custom;
        state.LeftPanelMode = leftMode;
        state.RightPanelMode = rightMode;
        state.TimelinePanelMode = timelineMode;
        state.LeftRestoreMode = leftMode == WorkspacePanelMode.Hidden ? state.LeftRestoreMode : leftMode;
        state.RightRestoreMode = rightMode == WorkspacePanelMode.Hidden ? state.RightRestoreMode : rightMode;
        state.TimelineRestoreMode = timelineMode == WorkspacePanelMode.Hidden ? state.TimelineRestoreMode : timelineMode;
        state.LeftDockWidth = leftDockWidth;
        state.RightDockWidth = rightDockWidth;
        state.TimelineDockHeight = timelineDockHeight;
        ApplyWorkspaceLayoutState(state, updateStatus: false);
        StatusMessage = statusMessage;
    }

    private void ApplyWorkspaceLayoutState(WorkspaceLayoutState state, bool updateStatus)
    {
        _isApplyingWorkspaceLayout = true;
        _suppressWorkspacePreset = true;

        _leftRestoreMode = NormalizeRestoreMode(state.LeftRestoreMode);
        _rightRestoreMode = NormalizeRestoreMode(state.RightRestoreMode);
        _timelineRestoreMode = NormalizeRestoreMode(state.TimelineRestoreMode);

        LeftDockWidth = new GridLength(Math.Max(232, state.LeftDockWidth));
        RightDockWidth = new GridLength(Math.Max(276, state.RightDockWidth));
        TimelineDockHeight = new GridLength(Math.Clamp(state.TimelineDockHeight, MinTimelineDockHeight, MaxTimelineDockHeight));
        SelectedWorkspacePreset = state.SelectedPreset;
        LeftPanelMode = state.LeftPanelMode;
        RightPanelMode = state.RightPanelMode;
        TimelinePanelMode = state.TimelinePanelMode;

        if (LeftPanelMode != WorkspacePanelMode.Hidden)
        {
            _leftRestoreMode = LeftPanelMode;
        }

        if (RightPanelMode != WorkspacePanelMode.Hidden)
        {
            _rightRestoreMode = RightPanelMode;
        }

        if (TimelinePanelMode != WorkspacePanelMode.Hidden)
        {
            _timelineRestoreMode = TimelinePanelMode;
        }

        _suppressWorkspacePreset = false;
        _isApplyingWorkspaceLayout = false;
        RefreshWorkspaceLayoutProperties();
        if (updateStatus)
        {
            StatusMessage = $"Workspace restored • {WorkspaceLayoutSummary}";
        }
    }

    private WorkspaceLayoutState CaptureWorkspaceLayoutState()
    {
        return new WorkspaceLayoutState
        {
            SelectedPreset = SelectedWorkspacePreset,
            LeftPanelMode = LeftPanelMode,
            RightPanelMode = RightPanelMode,
            TimelinePanelMode = TimelinePanelMode,
            LeftRestoreMode = _leftRestoreMode,
            RightRestoreMode = _rightRestoreMode,
            TimelineRestoreMode = _timelineRestoreMode,
            LeftDockWidth = LeftDockWidth.Value,
            RightDockWidth = RightDockWidth.Value,
            TimelineDockHeight = TimelineDockHeight.Value
        };
    }

    private void PersistWorkspaceLayoutIfReady()
    {
        if (!_workspaceLayoutLoaded || _isApplyingWorkspaceLayout)
        {
            return;
        }

        WorkspaceLayoutPersistenceService.Save(CaptureWorkspaceLayoutState());
    }

    private static WorkspacePanelMode NormalizeRestoreMode(WorkspacePanelMode mode)
    {
        return mode == WorkspacePanelMode.Hidden ? WorkspacePanelMode.Docked : mode;
    }

    private static bool TryParseWorkspacePanelMode(string value, out WorkspacePanelMode mode)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "docked":
            case "dock":
                mode = WorkspacePanelMode.Docked;
                return true;
            case "overlay":
            case "over":
                mode = WorkspacePanelMode.Overlay;
                return true;
            case "hidden":
            case "hide":
                mode = WorkspacePanelMode.Hidden;
                return true;
            default:
                mode = WorkspacePanelMode.Docked;
                return false;
        }
    }

    private static string GetWorkspacePanelModeLabel(WorkspacePanelMode mode)
    {
        return mode switch
        {
            WorkspacePanelMode.Docked => "docked",
            WorkspacePanelMode.Overlay => "set to overlay",
            WorkspacePanelMode.Hidden => "hidden",
            _ => mode.ToString()
        };
    }

    private static string GetWorkspacePanelStateLabel(WorkspacePanelMode mode)
    {
        return mode switch
        {
            WorkspacePanelMode.Docked => "docked",
            WorkspacePanelMode.Overlay => "overlay",
            WorkspacePanelMode.Hidden => "hidden",
            _ => mode.ToString()
        };
    }

    private static string GetSelectionStateSuffix(LayerViewModel selectedLayer)
    {
        if (selectedLayer.IsLocked && !selectedLayer.IsVisible)
        {
            return " • Hidden • Locked";
        }

        if (selectedLayer.IsLocked)
        {
            return " • Locked";
        }

        if (!selectedLayer.IsVisible)
        {
            return " • Hidden";
        }

        return string.Empty;
    }

    private Point GetNextLibraryPlacement()
    {
        var index = _document.Layers.Count;
        return new Point(180 + ((index % 4) * 42), 140 + ((index % 5) * 34));
    }

    private string GetUniqueLayerName(string baseName)
    {
        var normalizedBaseName = string.IsNullOrWhiteSpace(baseName)
            ? "Layer"
            : baseName.Trim();
        var candidate = normalizedBaseName;
        var suffix = 2;

        while (_document.Layers.Any(item => string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{normalizedBaseName} {suffix}";
            suffix++;
        }

        return candidate;
    }

    private string GetUniqueLibraryName(string baseName)
    {
        var normalizedBaseName = string.IsNullOrWhiteSpace(baseName)
            ? "New"
            : baseName.Trim();
        var candidate = $"{normalizedBaseName} Symbol";
        var suffix = 2;

        while (_document.LibraryItems.Any(item => string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{normalizedBaseName} Symbol {suffix}";
            suffix++;
        }

        return candidate;
    }

    private string GetUniqueComponentName(string baseName)
    {
        var normalizedBaseName = string.IsNullOrWhiteSpace(baseName)
            ? "Component"
            : baseName.Trim();
        var candidate = $"{normalizedBaseName} Component";
        var suffix = 2;

        while (_document.LibraryItems.Any(item => string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{normalizedBaseName} Component {suffix}";
            suffix++;
        }

        return candidate;
    }

    private string GetUniquePublishProfileName(string baseName)
    {
        var normalizedBaseName = string.IsNullOrWhiteSpace(baseName)
            ? "Publish Profile"
            : baseName.Trim();
        var candidate = normalizedBaseName;
        var suffix = 2;

        while (_document.PublishProfiles.Any(item => string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{normalizedBaseName} {suffix}";
            suffix++;
        }

        return candidate;
    }

    private string GetUniqueSceneName(string baseName)
    {
        var normalizedBaseName = string.IsNullOrWhiteSpace(baseName)
            ? "Scene"
            : baseName.Trim();
        var candidate = normalizedBaseName;
        var suffix = 2;

        while (_document.Scenes.Any(item => string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{normalizedBaseName} {suffix}";
            suffix++;
        }

        return candidate;
    }

    private string GetUniqueMediaName(string baseName)
    {
        var normalizedBaseName = string.IsNullOrWhiteSpace(baseName)
            ? "Media"
            : baseName.Trim();
        var candidate = normalizedBaseName;
        var suffix = 2;

        while (_document.MediaAssets.Any(item => string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{normalizedBaseName} {suffix}";
            suffix++;
        }

        return candidate;
    }

    private void RefreshSceneEditor()
    {
        _suppressSceneEditor = true;
        SceneNameEditor = SelectedScene?.Name ?? string.Empty;
        SceneTransitionKindEditor = SelectedScene?.Model.OutgoingTransition.Kind ?? SceneTransitionKind.None;
        SceneTransitionDurationEditor = SelectedScene?.Model.OutgoingTransition.Duration ?? 0.35d;
        SceneTransitionAccentColorEditor = SelectedScene?.Model.OutgoingTransition.AccentColor ?? "#000000";
        _suppressSceneEditor = false;
    }

    private void PersistActiveScene(SceneModel? scene = null)
    {
        if (IsEditingSymbol)
        {
            return;
        }

        var selectedSceneModel = SelectedScene is not null && _document.Scenes.Any(item => item.Id == SelectedScene.Id)
            ? SelectedScene.Model
            : null;
        var targetScene = scene
            ?? selectedSceneModel
            ?? _document.Scenes.FirstOrDefault(item => item.Id == _document.ActiveSceneId)
            ?? _document.Scenes.FirstOrDefault();
        if (targetScene is null)
        {
            return;
        }

        targetScene.FrameRate = Math.Max(1, SceneFrameRate);
        SceneEditingService.PersistDocumentStateToScene(_document, targetScene);
        SceneTimelineService.EnsureTimelineMetadata(targetScene, FrameTimelineService.GetTotalFrames(targetScene.Duration, targetScene.FrameRate));
    }

    private void ActivateScene(SceneModel scene)
    {
        SceneTimelineService.EnsureTimelineMetadata(scene, FrameTimelineService.GetTotalFrames(scene.Duration, scene.FrameRate));
        SceneEditingService.ApplySceneToDocument(_document, scene);
        _suppressFrameRateEditor = true;
        SceneFrameRate = Math.Max(1, scene.FrameRate);
        _suppressFrameRateEditor = false;
        _suppressSceneSurfaceEditor = true;
        Duration = scene.Duration;
        CanvasWidth = scene.CanvasWidth;
        CanvasHeight = scene.CanvasHeight;
        TransparentStageBackground = scene.TransparentStageBackground;
        BackgroundFrom = scene.BackgroundFrom;
        BackgroundTo = scene.BackgroundTo;
        _suppressSceneSurfaceEditor = false;
        RebuildLayers(scene.Layers.OrderByDescending(item => item.ZIndex).FirstOrDefault()?.Id);
        ReloadFrameLabels();
        ReloadSceneMarkers();
        SelectionStartFrame = 0;
        SelectionEndFrame = 0;
        Seek(FrameTimelineService.FrameToTime(scene.InFrame, scene.FrameRate));
        RefreshFrameAwareState();
        RefreshSymbolEditingProperties();
        RefreshSceneTimelineEditors();
        RefreshMonitorPreviews();
    }

    private void EnterSymbolEdit(Guid libraryItemId, ButtonVisualState buttonState, Guid? selectedLayerId, double targetTime)
    {
        if (_document.LibraryItems.FirstOrDefault(item => item.Id == libraryItemId) is not { } libraryItem)
        {
            return;
        }

        if (!IsEditingSymbol)
        {
            PersistActiveScene();
            _editingSceneId = SelectedScene?.Id;
            _editingSceneLayerId = SelectedLayer?.Id;
        }
        else if (_editingLibraryItemId == libraryItemId && EditingButtonState != buttonState)
        {
            PersistEditingSymbolWorkspace();
        }

        _editingLibraryItemId = libraryItemId;
        _suppressButtonStateEditor = true;
        EditingButtonState = buttonState;
        _suppressButtonStateEditor = false;
        LoadSymbolEditingWorkspace(libraryItem, buttonState, selectedLayerId, targetTime);
        RefreshSymbolEditingProperties();
    }

    private void LoadSymbolEditingWorkspace(LibraryItem libraryItem, ButtonVisualState buttonState, Guid? selectedLayerId, double targetTime)
    {
        _document.Layers = TimelineEditingService.GetEditableSymbolLayers(libraryItem, buttonState);
        _suppressFrameRateEditor = true;
        SceneFrameRate = Math.Max(1, libraryItem.FrameRate);
        _suppressFrameRateEditor = false;
        _suppressSceneSurfaceEditor = true;
        Duration = Math.Max(0.1d, libraryItem.Duration);
        CanvasWidth = Math.Max(CanvasWidth, libraryItem.Template.Defaults.Width + 220);
        CanvasHeight = Math.Max(CanvasHeight, libraryItem.Template.Defaults.Height + 220);
        _suppressSceneSurfaceEditor = false;
        SceneFrameLabels.Clear();
        RefreshFrameLabelEditor();
        RebuildLayers(selectedLayerId ?? _document.Layers.OrderByDescending(item => item.ZIndex).FirstOrDefault()?.Id);
        Seek(targetTime);
        SelectionStartFrame = CurrentFrame;
        SelectionEndFrame = CurrentFrame;
        RefreshFrameAwareState();
        ReloadLibraryItems(libraryItem.Id);
        ReloadComponentItems(libraryItem.Id);
        StatusMessage = $"Editing symbol {libraryItem.Name}";
    }

    private void PersistEditingSymbolWorkspace()
    {
        if (_editingLibraryItemId is not Guid libraryItemId)
        {
            return;
        }

        var libraryItem = _document.LibraryItems.FirstOrDefault(item => item.Id == libraryItemId);
        if (libraryItem is null)
        {
            return;
        }

        TimelineEditingService.SetEditableSymbolLayers(
            libraryItem,
            _document.Layers,
            Duration,
            SceneFrameRate,
            EditingButtonState);
        TimelineEditingService.SynchronizeLibraryItemInstances(_document, libraryItem.Id);
        ReloadLibraryItems(libraryItem.Id);
        ReloadComponentItems(libraryItem.Id);
    }

    private bool TryAdvancePlaybackScene()
    {
        if (IsEditingSymbol || SelectedScene is null)
        {
            return false;
        }

        var currentIndex = Scenes.IndexOf(SelectedScene);
        if (currentIndex < 0)
        {
            return false;
        }

        var nextIndex = currentIndex + 1;
        if (nextIndex >= Scenes.Count)
        {
            if (!LoopPlayback)
            {
                return false;
            }

            nextIndex = 0;
        }

        SelectedScene = Scenes[nextIndex];
        _playbackClock.Restart();
        var playbackStart = GetCurrentScenePlaybackStartTime();
        _playbackOriginTime = playbackStart;
        Seek(playbackStart);
        return true;
    }
}
