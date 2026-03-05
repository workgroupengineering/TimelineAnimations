using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimelineAnimations.App.Models;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly DispatcherTimer _playbackTimer;
    private readonly Stopwatch _playbackClock = new();
    private TimelineDocument _document = SampleProjectFactory.Create();
    private UndoRedoStack<EditorStateSnapshot>? _history;
    private bool _isApplyingHistory;
    private bool _isInteractiveChange;
    private bool _suppressInspector;
    private double _playbackOriginTime;

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
                Kind = LayerKind.Rectangle
            },
            new()
            {
                Title = "Solar Orb",
                Subtitle = "Soft circular light",
                Fill = "#FF8A4C",
                Accent = "#FFE0B8",
                Kind = LayerKind.Ellipse
            },
            new()
            {
                Title = "Headline",
                Subtitle = "Large text caption",
                Fill = "#F7F5ED",
                Accent = "#B6C8FF",
                Kind = LayerKind.Text
            }
        };

        _playbackTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Background, HandlePlaybackTick);
        LoadDocument(SampleProjectFactory.Create(), "Sample Composition");
    }

    public ObservableCollection<LayerViewModel> Layers { get; } = [];

    public ObservableCollection<PropertyTrackViewModel> Tracks { get; } = [];

    public ObservableCollection<PaletteItemViewModel> PaletteItems { get; }

    public TimelineDocument Document => _document;

    public string CurrentTimeLabel => $"{CurrentTime:0.00}s";

    public string DurationLabel => $"{Duration:0.00}s";

    public string CanvasSizeLabel => $"{CanvasWidth:0} × {CanvasHeight:0}";

    public double TimelineSurfaceWidth => Math.Max(920, (Duration * TimelineZoom) + 200);

    public bool CanUndo => _history?.CanUndo == true;

    public bool CanRedo => _history?.CanRedo == true;

    public bool SelectedLayerIsText => SelectedLayer?.Kind == LayerKind.Text;

    public string SelectionHeadline => SelectedLayer is null
        ? "Nothing selected"
        : $"{SelectedLayer.Name} • {SelectedLayer.KindLabel}";

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
                : $"{track!.Title} @ {keyframe.TimeLabel} • {keyframe.ValueLabel}";
        }
    }

    [ObservableProperty]
    private string documentName = "Sample Composition";

    [ObservableProperty]
    private string fileLabel = "Unsaved";

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
    private LayerViewModel? selectedLayer;

    [ObservableProperty]
    private AnimatedProperty selectedProperty = AnimatedProperty.X;

    [ObservableProperty]
    private Guid? selectedKeyframeId;

    [ObservableProperty]
    private double currentTime;

    [ObservableProperty]
    private double canvasZoom = 0.74d;

    [ObservableProperty]
    private double timelineZoom = 150d;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private bool loopPlayback = true;

    [ObservableProperty]
    private bool autoKey = true;

    [ObservableProperty]
    private bool snapToGrid = true;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private string layerNameEditor = string.Empty;

    [ObservableProperty]
    private string fillHexEditor = "#FFFFFF";

    [ObservableProperty]
    private string textEditor = string.Empty;

    [ObservableProperty]
    private double inspectorX;

    [ObservableProperty]
    private double inspectorY;

    [ObservableProperty]
    private double inspectorWidth;

    [ObservableProperty]
    private double inspectorHeight;

    [ObservableProperty]
    private double inspectorRotation;

    [ObservableProperty]
    private double inspectorOpacity = 1;

    [ObservableProperty]
    private double inspectorCornerRadius = 28;

    [ObservableProperty]
    private double inspectorFontSize = 48;

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
        RefreshInspector();
        OnPropertyChanged(nameof(SelectedLayerIsText));
        OnPropertyChanged(nameof(SelectionHeadline));
        OnPropertyChanged(nameof(SelectedKeyframeSummary));
    }

    partial void OnSelectedPropertyChanged(AnimatedProperty value)
    {
        foreach (var track in Tracks)
        {
            track.IsSelected = track.Property == value;
        }

        OnPropertyChanged(nameof(SelectedKeyframeSummary));
    }

    partial void OnSelectedKeyframeIdChanged(Guid? value)
    {
        foreach (var track in Tracks)
        {
            track.UpdateKeyframeSelection(value);
        }

        OnPropertyChanged(nameof(SelectedKeyframeSummary));
    }

    partial void OnCurrentTimeChanged(double value)
    {
        foreach (var layer in Layers)
        {
            layer.UpdatePreview(value);
        }

        UpdateTrackPreviewValues();
        RefreshInspector();
        OnPropertyChanged(nameof(CurrentTimeLabel));
    }

    partial void OnTimelineZoomChanged(double value)
    {
        OnPropertyChanged(nameof(TimelineSurfaceWidth));
    }

    partial void OnDurationChanged(double value)
    {
        OnPropertyChanged(nameof(DurationLabel));
        OnPropertyChanged(nameof(TimelineSurfaceWidth));
    }

    partial void OnCanvasWidthChanged(double value)
    {
        OnPropertyChanged(nameof(CanvasSizeLabel));
    }

    partial void OnCanvasHeightChanged(double value)
    {
        OnPropertyChanged(nameof(CanvasSizeLabel));
    }

    partial void OnLayerNameEditorChanged(string value)
    {
        if (_suppressInspector || SelectedLayer is null || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        SelectedLayer.Model.Name = value.Trim();
        SelectedLayer.RefreshMetadata();
        RecordHistoryIfNeeded();
        StatusMessage = "Layer renamed";
        OnPropertyChanged(nameof(SelectionHeadline));
    }

    partial void OnFillHexEditorChanged(string value)
    {
        if (_suppressInspector || SelectedLayer is null || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        SelectedLayer.Model.Style.Fill = value.Trim();
        SelectedLayer.RefreshMetadata();
        SelectedLayer.UpdatePreview(CurrentTime);
        RecordHistoryIfNeeded();
        StatusMessage = "Fill updated";
    }

    partial void OnTextEditorChanged(string value)
    {
        if (_suppressInspector || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.Style.Text = value;
        SelectedLayer.RefreshMetadata();
        SelectedLayer.UpdatePreview(CurrentTime);
        RecordHistoryIfNeeded();
        StatusMessage = "Text updated";
    }

    partial void OnInspectorXChanged(double value) => ApplyInspectorValue(AnimatedProperty.X, value);

    partial void OnInspectorYChanged(double value) => ApplyInspectorValue(AnimatedProperty.Y, value);

    partial void OnInspectorWidthChanged(double value) => ApplyInspectorValue(AnimatedProperty.Width, value);

    partial void OnInspectorHeightChanged(double value) => ApplyInspectorValue(AnimatedProperty.Height, value);

    partial void OnInspectorRotationChanged(double value) => ApplyInspectorValue(AnimatedProperty.Rotation, value);

    partial void OnInspectorOpacityChanged(double value) => ApplyInspectorValue(AnimatedProperty.Opacity, value);

    partial void OnInspectorCornerRadiusChanged(double value)
    {
        if (_suppressInspector || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.Style.CornerRadius = Math.Max(0, value);
        SelectedLayer.RefreshMetadata();
        RecordHistoryIfNeeded();
        StatusMessage = "Corner radius updated";
    }

    partial void OnInspectorFontSizeChanged(double value)
    {
        if (_suppressInspector || SelectedLayer is null)
        {
            return;
        }

        SelectedLayer.Model.Style.FontSize = Math.Max(8, value);
        SelectedLayer.RefreshMetadata();
        RecordHistoryIfNeeded();
        StatusMessage = "Font size updated";
    }

    [RelayCommand]
    private void Undo()
    {
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
        if (_history?.TryRedo(out var snapshot) != true)
        {
            return;
        }

        ApplySnapshot(snapshot);
        UpdateHistoryAvailability();
        StatusMessage = "Redo applied";
    }

    [RelayCommand]
    private void ResetSample()
    {
        LoadDocument(SampleProjectFactory.Create(), "Sample Composition");
        StatusMessage = "Sample composition restored";
    }

    [RelayCommand]
    private void TogglePlayback()
    {
        if (IsPlaying)
        {
            StopPlayback(false);
            return;
        }

        _playbackOriginTime = CurrentTime;
        _playbackClock.Restart();
        _playbackTimer.Start();
        IsPlaying = true;
        StatusMessage = "Playback running";
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
    private void AddText()
    {
        AddLayer(LayerKind.Text, new Point(240, 420));
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
        if (SelectedLayer is null)
        {
            return;
        }

        var value = TimelineInterpolationService.SampleProperty(SelectedLayer.Model, SelectedProperty, CurrentTime);
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
        if (SelectedLayer is null || SelectedKeyframeId is null)
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
    private void ApplyFillPreset(string? fill)
    {
        if (SelectedLayer is null || string.IsNullOrWhiteSpace(fill))
        {
            return;
        }

        SelectedLayer.Model.Style.Fill = fill;
        SelectedLayer.RefreshMetadata();
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = "Color preset applied";
    }

    public void LoadDocument(TimelineDocument document, string label)
    {
        StopPlayback(false);
        _document = document;
        DocumentName = document.Name;
        FileLabel = label;
        Duration = document.Duration;
        CanvasWidth = document.CanvasWidth;
        CanvasHeight = document.CanvasHeight;
        BackgroundFrom = document.BackgroundFrom;
        BackgroundTo = document.BackgroundTo;
        RebuildLayers(document.Layers.OrderByDescending(item => item.ZIndex).FirstOrDefault()?.Id);
        Seek(0);
        ResetHistory();
        StatusMessage = "Document loaded";
    }

    public void SetDocumentLabel(string label)
    {
        FileLabel = label;
    }

    public void SelectLayer(Guid layerId)
    {
        var layer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (layer is not null)
        {
            SelectedLayer = layer;
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

    public void Scrub(double time)
    {
        Seek(Snap(time));
    }

    public void SelectTrack(AnimatedProperty property)
    {
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

    public void MoveKeyframe(AnimatedProperty property, Guid keyframeId, double time)
    {
        if (SelectedLayer is null)
        {
            return;
        }

        if (TimelineEditingService.MoveKeyframe(SelectedLayer.Model, property, keyframeId, Snap(time), Duration))
        {
            ReloadTracks();
            SelectedProperty = property;
            SelectedKeyframeId = keyframeId;
            Seek(Snap(time));
            RecordHistoryIfNeeded();
            if (!_isInteractiveChange)
            {
                StatusMessage = "Keyframe moved";
            }
        }
    }

    public void AddKeyframeAt(AnimatedProperty property, double time)
    {
        if (SelectedLayer is null)
        {
            return;
        }

        SelectedProperty = property;
        Seek(time);
        var value = TimelineInterpolationService.SampleProperty(SelectedLayer.Model, property, CurrentTime);
        var keyframe = TimelineEditingService.SetKeyframe(SelectedLayer.Model, property, Snap(CurrentTime), value, Duration);
        ReloadTracks();
        SelectedKeyframeId = keyframe.Id;
        RecordHistoryIfNeeded();
        StatusMessage = "Keyframe inserted";
    }

    public void UpdateLayerFrame(Guid layerId, Rect bounds)
    {
        var layer = Layers.FirstOrDefault(item => item.Id == layerId);
        if (layer is null)
        {
            return;
        }

        SelectedLayer = layer;
        var shouldCreateKeyframe = ShouldCreateKeyframe();

        TimelineEditingService.ApplyValue(layer.Model, AnimatedProperty.X, bounds.X, CurrentTime, shouldCreateKeyframe, Duration);
        TimelineEditingService.ApplyValue(layer.Model, AnimatedProperty.Y, bounds.Y, CurrentTime, shouldCreateKeyframe, Duration);
        TimelineEditingService.ApplyValue(layer.Model, AnimatedProperty.Width, bounds.Width, CurrentTime, shouldCreateKeyframe, Duration);
        TimelineEditingService.ApplyValue(layer.Model, AnimatedProperty.Height, bounds.Height, CurrentTime, shouldCreateKeyframe, Duration);

        ReloadPreviewForLayer(layer);
        ReloadTracks();
        RefreshInspector();
        RecordHistoryIfNeeded();
        if (!_isInteractiveChange)
        {
            StatusMessage = "Layer transformed";
        }
    }

    public void BeginInteractiveChange()
    {
        _isInteractiveChange = true;
    }

    public void CommitInteractiveChange(string statusMessage)
    {
        if (!_isInteractiveChange)
        {
            return;
        }

        _isInteractiveChange = false;
        RecordHistoryIfNeeded();
        StatusMessage = statusMessage;
    }

    public void AddLayerFromPalette(LayerKind kind, Point position)
    {
        AddLayer(kind, position);
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

    private void RebuildLayers(Guid? selectionId)
    {
        Layers.Clear();

        foreach (var layer in _document.Layers.OrderByDescending(item => item.ZIndex))
        {
            Layers.Add(new LayerViewModel(layer));
        }

        SelectedLayer = selectionId is null
            ? Layers.FirstOrDefault()
            : Layers.FirstOrDefault(item => item.Id == selectionId.Value) ?? Layers.FirstOrDefault();
    }

    private void ReloadTracks()
    {
        Tracks.Clear();

        if (SelectedLayer is null)
        {
            return;
        }

        foreach (var property in Enum.GetValues<AnimatedProperty>())
        {
            var track = new PropertyTrackViewModel(property)
            {
                IsSelected = property == SelectedProperty
            };

            track.LoadFromLayer(SelectedLayer.Model, CurrentTime, SelectedKeyframeId);
            Tracks.Add(track);
        }

        OnPropertyChanged(nameof(SelectedKeyframeSummary));
    }

    private void UpdateTrackPreviewValues()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        foreach (var track in Tracks)
        {
            track.RefreshCurrentValue(SelectedLayer.Model, CurrentTime);
        }

        OnPropertyChanged(nameof(SelectedKeyframeSummary));
    }

    private void RefreshInspector()
    {
        _suppressInspector = true;

        if (SelectedLayer is null)
        {
            LayerNameEditor = string.Empty;
            FillHexEditor = "#FFFFFF";
            TextEditor = string.Empty;
            InspectorX = 0;
            InspectorY = 0;
            InspectorWidth = 0;
            InspectorHeight = 0;
            InspectorRotation = 0;
            InspectorOpacity = 1;
            InspectorCornerRadius = 0;
            InspectorFontSize = 48;
            _suppressInspector = false;
            return;
        }

        LayerNameEditor = SelectedLayer.Name;
        FillHexEditor = SelectedLayer.FillHex;
        TextEditor = SelectedLayer.TextContent;
        InspectorX = SelectedLayer.X;
        InspectorY = SelectedLayer.Y;
        InspectorWidth = SelectedLayer.Width;
        InspectorHeight = SelectedLayer.Height;
        InspectorRotation = SelectedLayer.Rotation;
        InspectorOpacity = SelectedLayer.Opacity;
        InspectorCornerRadius = SelectedLayer.CornerRadius;
        InspectorFontSize = SelectedLayer.FontSize;

        _suppressInspector = false;
    }

    private void ApplyInspectorValue(AnimatedProperty property, double value)
    {
        if (_suppressInspector || SelectedLayer is null)
        {
            return;
        }

        var normalized = property switch
        {
            AnimatedProperty.Width or AnimatedProperty.Height => Math.Max(24, value),
            AnimatedProperty.Opacity => TimelineMath.Clamp(value, 0, 1),
            _ => value
        };

        TimelineEditingService.ApplyValue(
            SelectedLayer.Model,
            property,
            normalized,
            CurrentTime,
            ShouldCreateKeyframe(),
            Duration);

        ReloadPreviewForLayer(SelectedLayer);
        ReloadTracks();
        RefreshInspector();
        RecordHistoryIfNeeded();
        StatusMessage = $"{GetPropertyTitle(property)} updated";
    }

    private void ReloadPreviewForLayer(LayerViewModel layer)
    {
        layer.RefreshMetadata();
        layer.UpdatePreview(CurrentTime);
    }

    private bool ShouldCreateKeyframe()
    {
        return AutoKey || CurrentTime > 0.0001d;
    }

    private double Snap(double time)
    {
        return SnapToGrid ? TimelineMath.Snap(time, 0.1d) : time;
    }

    private EditorStateSnapshot CaptureSnapshot()
    {
        return new EditorStateSnapshot(
            _document,
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

    private void RecordHistoryIfNeeded()
    {
        if (_isApplyingHistory || _isInteractiveChange || _history is null)
        {
            return;
        }

        _history.Record(CaptureSnapshot());
        UpdateHistoryAvailability();
    }

    private void ApplySnapshot(EditorStateSnapshot snapshot)
    {
        StopPlayback(false);
        _isApplyingHistory = true;

        _document = DocumentSerializer.Clone(snapshot.Document);
        DocumentName = _document.Name;
        Duration = _document.Duration;
        CanvasWidth = _document.CanvasWidth;
        CanvasHeight = _document.CanvasHeight;
        BackgroundFrom = _document.BackgroundFrom;
        BackgroundTo = _document.BackgroundTo;
        RebuildLayers(snapshot.SelectedLayerId);
        SelectedProperty = snapshot.SelectedProperty;
        SelectedKeyframeId = snapshot.SelectedKeyframeId;
        Seek(snapshot.CurrentTime);
        RefreshInspector();

        _isApplyingHistory = false;
    }

    private void UpdateHistoryAvailability()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    private void HandlePlaybackTick(object? sender, EventArgs e)
    {
        var elapsed = _playbackClock.Elapsed.TotalSeconds;
        var nextTime = _playbackOriginTime + elapsed;

        if (nextTime > Duration)
        {
            if (LoopPlayback)
            {
                _playbackClock.Restart();
                _playbackOriginTime = 0;
                Seek(0);
                return;
            }

            StopPlayback(true);
            Seek(Duration);
            return;
        }

        Seek(nextTime);
    }

    private void StopPlayback(bool resetTime)
    {
        _playbackTimer.Stop();
        _playbackClock.Stop();
        IsPlaying = false;

        if (resetTime)
        {
            Seek(0);
        }

        StatusMessage = "Playback stopped";
    }

    private static string GetPropertyTitle(AnimatedProperty property)
    {
        return property switch
        {
            AnimatedProperty.X => "Position X",
            AnimatedProperty.Y => "Position Y",
            AnimatedProperty.Width => "Width",
            AnimatedProperty.Height => "Height",
            AnimatedProperty.Rotation => "Rotation",
            AnimatedProperty.Opacity => "Opacity",
            _ => property.ToString()
        };
    }
}
