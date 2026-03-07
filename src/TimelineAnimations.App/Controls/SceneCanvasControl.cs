using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using TimelineAnimations.App.Helpers;
using TimelineAnimations.App.Models;
using TimelineAnimations.App.Services;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Controls;

public sealed class SceneCanvasControl : Control
{
    private const double SurfacePadding = 18;
    private const double HandleSize = 12;
    private const double CanvasResizeHandleSize = 14;
    private const double CanvasResizeHandleLength = 42;
    private const double CanvasResizeSnapIncrement = 20d;
    private const double MinZoomFactor = 0.25d;
    private const double MaxZoomFactor = 8d;
    private const double MinVisibleStageExtent = 96d;
    private InteractionMode _interactionMode;
    private Guid? _activeLayerId;
    private int? _activePathPointIndex;
    private Point _pointerOrigin;
    private Rect _originalBounds;
    private double? _verticalGuide;
    private double? _horizontalGuide;
    private Point _draftOrigin;
    private Point _draftCurrent;
    private List<Point> _draftPoints = [];
    private Guid? _prototypeHoverLayerId;
    private Guid? _prototypePressedLayerId;
    private Vector _viewportPan;
    private Vector _viewportPanOrigin;
    private Size _originalCanvasSize;
    private double _canvasResizeScaleOrigin = 1d;
    private bool _isSpacePressed;

    public static readonly StyledProperty<IReadOnlyList<LayerViewModel>?> LayersProperty =
        AvaloniaProperty.Register<SceneCanvasControl, IReadOnlyList<LayerViewModel>?>(nameof(Layers));

    public static readonly StyledProperty<LayerViewModel?> SelectedLayerProperty =
        AvaloniaProperty.Register<SceneCanvasControl, LayerViewModel?>(nameof(SelectedLayer));

    public static readonly StyledProperty<TimelineDocument?> DocumentProperty =
        AvaloniaProperty.Register<SceneCanvasControl, TimelineDocument?>(nameof(Document));

    public static readonly StyledProperty<double> CanvasZoomProperty =
        AvaloniaProperty.Register<SceneCanvasControl, double>(nameof(CanvasZoom), 1d);

    public static readonly StyledProperty<double> DocumentWidthProperty =
        AvaloniaProperty.Register<SceneCanvasControl, double>(nameof(DocumentWidth), 1280d);

    public static readonly StyledProperty<double> DocumentHeightProperty =
        AvaloniaProperty.Register<SceneCanvasControl, double>(nameof(DocumentHeight), 720d);

    public static readonly StyledProperty<double> DurationProperty =
        AvaloniaProperty.Register<SceneCanvasControl, double>(nameof(Duration), 6d);

    public static readonly StyledProperty<string> BackgroundFromProperty =
        AvaloniaProperty.Register<SceneCanvasControl, string>(nameof(BackgroundFrom), "#09111F");

    public static readonly StyledProperty<string> BackgroundToProperty =
        AvaloniaProperty.Register<SceneCanvasControl, string>(nameof(BackgroundTo), "#182748");

    public static readonly StyledProperty<bool> SnapToGridProperty =
        AvaloniaProperty.Register<SceneCanvasControl, bool>(nameof(SnapToGrid), true);

    public static readonly StyledProperty<double> CurrentTimeProperty =
        AvaloniaProperty.Register<SceneCanvasControl, double>(nameof(CurrentTime));

    public static readonly StyledProperty<DrawingTool> DrawingToolProperty =
        AvaloniaProperty.Register<SceneCanvasControl, DrawingTool>(nameof(DrawingTool), DrawingTool.Select);

    public static readonly StyledProperty<double> FrameRateProperty =
        AvaloniaProperty.Register<SceneCanvasControl, double>(nameof(FrameRate), 24d);

    public static readonly StyledProperty<int> TotalFramesProperty =
        AvaloniaProperty.Register<SceneCanvasControl, int>(nameof(TotalFrames), 1);

    public static readonly StyledProperty<bool> OnionSkinEnabledProperty =
        AvaloniaProperty.Register<SceneCanvasControl, bool>(nameof(OnionSkinEnabled), true);

    public static readonly StyledProperty<int> OnionSkinBeforeProperty =
        AvaloniaProperty.Register<SceneCanvasControl, int>(nameof(OnionSkinBefore), 2);

    public static readonly StyledProperty<int> OnionSkinAfterProperty =
        AvaloniaProperty.Register<SceneCanvasControl, int>(nameof(OnionSkinAfter), 2);

    public static readonly StyledProperty<bool> PrototypeModeProperty =
        AvaloniaProperty.Register<SceneCanvasControl, bool>(nameof(PrototypeMode));

    public static readonly StyledProperty<bool> CanResizeCanvasProperty =
        AvaloniaProperty.Register<SceneCanvasControl, bool>(nameof(CanResizeCanvas), true);

    static SceneCanvasControl()
    {
        AffectsRender<SceneCanvasControl>(
            LayersProperty,
            SelectedLayerProperty,
            DocumentProperty,
            CanvasZoomProperty,
            DocumentWidthProperty,
            DocumentHeightProperty,
            DurationProperty,
            BackgroundFromProperty,
            BackgroundToProperty,
            SnapToGridProperty,
            CurrentTimeProperty,
            DrawingToolProperty,
            FrameRateProperty,
            TotalFramesProperty,
            OnionSkinEnabledProperty,
            OnionSkinBeforeProperty,
            OnionSkinAfterProperty,
            PrototypeModeProperty,
            CanResizeCanvasProperty);
    }

    public SceneCanvasControl()
    {
        ClipToBounds = true;
        Focusable = true;
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, HandleDragOver);
        AddHandler(DragDrop.DropEvent, HandleDrop);
    }

    public event EventHandler<CanvasLayerSelectionRequestedEventArgs>? LayerSelectionRequested;

    public event EventHandler<CanvasLayerTransformRequestedEventArgs>? LayerTransformRequested;

    public event EventHandler<CanvasPaletteDropRequestedEventArgs>? PaletteDropRequested;

    public event EventHandler<CanvasDrawingRequestedEventArgs>? DrawingRequested;

    public event EventHandler<CanvasPathPointMoveRequestedEventArgs>? PathPointMoveRequested;

    public event EventHandler<CanvasResizeRequestedEventArgs>? CanvasResizeRequested;

    public event EventHandler<CanvasInteractionStateChangedEventArgs>? TransformInteractionStateChanged;

    public event EventHandler<CanvasPrototypeTriggerRequestedEventArgs>? PrototypeTriggerRequested;

    public IReadOnlyList<LayerViewModel>? Layers
    {
        get => GetValue(LayersProperty);
        set => SetValue(LayersProperty, value);
    }

    public LayerViewModel? SelectedLayer
    {
        get => GetValue(SelectedLayerProperty);
        set => SetValue(SelectedLayerProperty, value);
    }

    public TimelineDocument? Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public double CanvasZoom
    {
        get => GetValue(CanvasZoomProperty);
        set => SetValue(CanvasZoomProperty, value);
    }

    public double DocumentWidth
    {
        get => GetValue(DocumentWidthProperty);
        set => SetValue(DocumentWidthProperty, value);
    }

    public double DocumentHeight
    {
        get => GetValue(DocumentHeightProperty);
        set => SetValue(DocumentHeightProperty, value);
    }

    public double Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public string BackgroundFrom
    {
        get => GetValue(BackgroundFromProperty);
        set => SetValue(BackgroundFromProperty, value);
    }

    public string BackgroundTo
    {
        get => GetValue(BackgroundToProperty);
        set => SetValue(BackgroundToProperty, value);
    }

    public bool SnapToGrid
    {
        get => GetValue(SnapToGridProperty);
        set => SetValue(SnapToGridProperty, value);
    }

    public double CurrentTime
    {
        get => GetValue(CurrentTimeProperty);
        set => SetValue(CurrentTimeProperty, value);
    }

    public DrawingTool DrawingTool
    {
        get => GetValue(DrawingToolProperty);
        set => SetValue(DrawingToolProperty, value);
    }

    public double FrameRate
    {
        get => GetValue(FrameRateProperty);
        set => SetValue(FrameRateProperty, value);
    }

    public int TotalFrames
    {
        get => GetValue(TotalFramesProperty);
        set => SetValue(TotalFramesProperty, value);
    }

    public bool OnionSkinEnabled
    {
        get => GetValue(OnionSkinEnabledProperty);
        set => SetValue(OnionSkinEnabledProperty, value);
    }

    public int OnionSkinBefore
    {
        get => GetValue(OnionSkinBeforeProperty);
        set => SetValue(OnionSkinBeforeProperty, value);
    }

    public int OnionSkinAfter
    {
        get => GetValue(OnionSkinAfterProperty);
        set => SetValue(OnionSkinAfterProperty, value);
    }

    public bool PrototypeMode
    {
        get => GetValue(PrototypeModeProperty);
        set => SetValue(PrototypeModeProperty, value);
    }

    public bool CanResizeCanvas
    {
        get => GetValue(CanResizeCanvasProperty);
        set => SetValue(CanResizeCanvasProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LayersProperty)
        {
            DetachLayerListeners(change.GetOldValue<IReadOnlyList<LayerViewModel>?>());
            AttachLayerListeners(change.GetNewValue<IReadOnlyList<LayerViewModel>?>());
            InvalidateVisual();
        }

        if (change.Property == PrototypeModeProperty && !change.GetNewValue<bool>())
        {
            _prototypeHoverLayerId = null;
            _prototypePressedLayerId = null;
        }

        if (change.Property == CanvasZoomProperty ||
            change.Property == DocumentWidthProperty ||
            change.Property == DocumentHeightProperty)
        {
            NormalizeViewportPan();
        }
    }

    public void ResetViewport()
    {
        _viewportPan = default;
        CanvasZoom = 1d;
        InvalidateVisual();
    }

    public void ZoomToActualSize()
    {
        var fitScale = GetFitScale();
        if (fitScale <= 0.0001d)
        {
            return;
        }

        _viewportPan = default;
        CanvasZoom = Math.Clamp(1d / fitScale, MinZoomFactor, MaxZoomFactor);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var hostRect = new Rect(Bounds.Size);
        var shellBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            [
                new GradientStop(Color.Parse("#07101D"), 0),
                new GradientStop(Color.Parse("#0B1425"), 0.45),
                new GradientStop(Color.Parse("#0D182B"), 1)
            ]
        };

        context.DrawRectangle(shellBrush, new Pen(new SolidColorBrush(Color.Parse("#223353")), 1), hostRect, 30, 30);

        DrawAmbientGlow(context, hostRect);

        var stageRect = GetStageRect();
        var stageBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.2, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.9, 1, RelativeUnit.Relative),
            GradientStops =
            [
                new GradientStop(ColorHelpers.Parse(BackgroundFrom, "#09111F"), 0),
                new GradientStop(ColorHelpers.Parse(BackgroundTo, "#182748"), 1)
            ]
        };

        context.DrawRectangle(stageBrush, new Pen(new SolidColorBrush(Color.Parse("#3B5C8C")), 1.2), stageRect, 28, 28);

        using (context.PushClip(stageRect))
        {
            var sceneState = BuildSceneState(CurrentTime);
            using (var bitmap = CompositeFrameRenderer.RenderBitmap(sceneState, includeBackground: true))
            {
                context.DrawImage(bitmap, stageRect);
            }

            DrawGrid(context, stageRect);
            DrawOnionSkin(context, stageRect);
            DrawRoleOverlays(context, stageRect, sceneState);
            DrawMotionPath(context, stageRect);
            DrawDraftLayer(context, stageRect);
            DrawActiveGuides(context, stageRect);
        }

        DrawFrameLabel(context, stageRect);
        DrawCanvasResizeHandles(context, stageRect);

        if (SelectedLayer is not null && SelectedLayer.IsRenderable)
        {
            DrawSelection(context, stageRect, SelectedLayer);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var pointer = e.GetPosition(this);
        var stageRect = GetStageRect();
        if (ShouldStartViewportPan(e, this, _isSpacePressed))
        {
            _interactionMode = InteractionMode.PanViewport;
            _pointerOrigin = pointer;
            _viewportPanOrigin = _viewportPan;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        _pointerOrigin = pointer;
        _interactionMode = InteractionMode.None;
        _activeLayerId = null;
        _activePathPointIndex = null;
        ClearGuides();
        ClearDraft();

        if (!PrototypeMode &&
            DrawingTool == DrawingTool.Select &&
            CanResizeCanvas &&
            TryHitCanvasResizeHandle(stageRect, pointer, out var canvasResizeMode))
        {
            _interactionMode = canvasResizeMode;
            _originalCanvasSize = new Size(DocumentWidth, DocumentHeight);
            _canvasResizeScaleOrigin = GetStageScale();
            TransformInteractionStateChanged?.Invoke(this, new CanvasInteractionStateChangedEventArgs(true));
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!stageRect.Contains(pointer))
        {
            return;
        }

        var documentPoint = ToWorldPoint(stageRect, pointer);
        if (PrototypeMode)
        {
            HandlePrototypePointerPressed(stageRect, e, pointer);
            return;
        }

        if (DrawingTool != DrawingTool.Select)
        {
            _draftOrigin = documentPoint;
            _draftCurrent = documentPoint;
            _draftPoints = [documentPoint];
            _interactionMode = GetDrawingInteractionMode(DrawingTool);
            TransformInteractionStateChanged?.Invoke(this, new CanvasInteractionStateChangedEventArgs(true));
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (SelectedLayer is not null &&
            !SelectedLayer.IsLocked &&
            TryHitPathPoint(stageRect, SelectedLayer, pointer, out var pointIndex))
        {
            _interactionMode = InteractionMode.MovePathPoint;
            _activeLayerId = SelectedLayer.Id;
            _activePathPointIndex = pointIndex;
            TransformInteractionStateChanged?.Invoke(this, new CanvasInteractionStateChangedEventArgs(true));
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (SelectedLayer is not null &&
            !SelectedLayer.IsLocked &&
            TryHitResizeHandle(stageRect, SelectedLayer, pointer, out var handleMode))
        {
            _interactionMode = handleMode;
            _activeLayerId = SelectedLayer.Id;
            _originalBounds = new Rect(SelectedLayer.X, SelectedLayer.Y, SelectedLayer.Width, SelectedLayer.Height);
            TransformInteractionStateChanged?.Invoke(this, new CanvasInteractionStateChangedEventArgs(true));
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        var hitLayer = HitTestLayer(stageRect, pointer);
        if (hitLayer is not null)
        {
            LayerSelectionRequested?.Invoke(this, new CanvasLayerSelectionRequestedEventArgs(hitLayer.Id));

            if (hitLayer.IsLocked)
            {
                e.Handled = true;
                return;
            }

            _activeLayerId = hitLayer.Id;
            _originalBounds = new Rect(hitLayer.X, hitLayer.Y, hitLayer.Width, hitLayer.Height);
            _interactionMode = InteractionMode.Move;
            TransformInteractionStateChanged?.Invoke(this, new CanvasInteractionStateChangedEventArgs(true));
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        LayerSelectionRequested?.Invoke(this, new CanvasLayerSelectionRequestedEventArgs(null));
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (PrototypeMode)
        {
            HandlePrototypePointerMoved(e);
            return;
        }

        if (_interactionMode == InteractionMode.None)
        {
            return;
        }

        if (_interactionMode == InteractionMode.PanViewport)
        {
            var delta = e.GetPosition(this) - _pointerOrigin;
            ApplyViewportPan(_viewportPanOrigin + delta);
            e.Handled = true;
            return;
        }

        if (IsCanvasResizeInteraction(_interactionMode))
        {
            var delta = e.GetPosition(this) - _pointerOrigin;
            var documentScale = Math.Max(0.05d, _canvasResizeScaleOrigin);
            var nextWidth = _originalCanvasSize.Width;
            var nextHeight = _originalCanvasSize.Height;

            switch (_interactionMode)
            {
                case InteractionMode.ResizeCanvasRight:
                    nextWidth = _originalCanvasSize.Width + (delta.X / documentScale);
                    break;
                case InteractionMode.ResizeCanvasBottom:
                    nextHeight = _originalCanvasSize.Height + (delta.Y / documentScale);
                    break;
                case InteractionMode.ResizeCanvasBottomRight:
                    nextWidth = _originalCanvasSize.Width + (delta.X / documentScale);
                    nextHeight = _originalCanvasSize.Height + (delta.Y / documentScale);
                    break;
            }

            CanvasResizeRequested?.Invoke(
                this,
                new CanvasResizeRequestedEventArgs(
                    NormalizeCanvasExtent(nextWidth),
                    NormalizeCanvasExtent(nextHeight)));
            e.Handled = true;
            return;
        }

        var stageRect = GetStageRect();
        var activeLayer = _activeLayerId is Guid activeLayerId
            ? Layers?.FirstOrDefault(layer => layer.Id == activeLayerId)
            : SelectedLayer;
        var documentPoint = ToWorldPoint(stageRect, e.GetPosition(this), activeLayer);

        if (IsDrawingInteraction(_interactionMode))
        {
            _draftCurrent = documentPoint;
            if (_interactionMode is InteractionMode.DrawPen or InteractionMode.DrawBrush)
            {
                var minimumDistance = _interactionMode == InteractionMode.DrawBrush ? 4d : 9d;
                if (_draftPoints.Count == 0 || Distance(_draftPoints[^1], documentPoint) >= minimumDistance)
                {
                    _draftPoints.Add(documentPoint);
                }
            }

            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_interactionMode == InteractionMode.MovePathPoint && _activeLayerId is Guid pathLayerId && _activePathPointIndex is int pointIndex)
        {
            if (activeLayer is not null)
            {
                var snapshot = GetDisplaySnapshot(activeLayer, CurrentTime);
                var bounds = new Rect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height);
                var localPoint = LayerTransformHelper.InverseTransformPoint(
                    documentPoint,
                    bounds,
                    snapshot,
                    ResolveLibraryItem(activeLayer.Model.SourceLibraryItemId));
                PathPointMoveRequested?.Invoke(this, new CanvasPathPointMoveRequestedEventArgs(pathLayerId, pointIndex, localPoint));
            }
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_activeLayerId is null)
        {
            return;
        }

        var pointerOrigin = ToWorldPoint(stageRect, _pointerOrigin, activeLayer);
        var documentDelta = documentPoint - pointerOrigin;
        var nextBounds = _originalBounds;

        switch (_interactionMode)
        {
            case InteractionMode.Move:
                nextBounds = nextBounds.Translate(documentDelta);
                var moveSnap = CanvasSnapService.SnapMove(
                    nextBounds.X,
                    nextBounds.Y,
                    nextBounds.Width,
                    nextBounds.Height,
                    DocumentWidth,
                    DocumentHeight,
                    SnapToGrid);
                nextBounds = new Rect(moveSnap.X, moveSnap.Y, moveSnap.Width, moveSnap.Height);
                _verticalGuide = moveSnap.VerticalGuide;
                _horizontalGuide = moveSnap.HorizontalGuide;
                break;
            case InteractionMode.ResizeBottomRight:
                nextBounds = new Rect(
                    nextBounds.X,
                    nextBounds.Y,
                    Math.Max(24, nextBounds.Width + documentDelta.X),
                    Math.Max(24, nextBounds.Height + documentDelta.Y));
                ApplyResizeSnapping(ref nextBounds);
                break;
            case InteractionMode.ResizeTopLeft:
                nextBounds = NormalizeRect(
                    nextBounds.X + documentDelta.X,
                    nextBounds.Y + documentDelta.Y,
                    nextBounds.Width - documentDelta.X,
                    nextBounds.Height - documentDelta.Y);
                ApplyResizeSnapping(ref nextBounds);
                break;
            case InteractionMode.ResizeTopRight:
                nextBounds = NormalizeRect(
                    nextBounds.X,
                    nextBounds.Y + documentDelta.Y,
                    nextBounds.Width + documentDelta.X,
                    nextBounds.Height - documentDelta.Y);
                ApplyResizeSnapping(ref nextBounds);
                break;
            case InteractionMode.ResizeBottomLeft:
                nextBounds = NormalizeRect(
                    nextBounds.X + documentDelta.X,
                    nextBounds.Y,
                    nextBounds.Width - documentDelta.X,
                    nextBounds.Height + documentDelta.Y);
                ApplyResizeSnapping(ref nextBounds);
                break;
        }

        nextBounds = ClampToDocument(nextBounds);
        LayerTransformRequested?.Invoke(this, new CanvasLayerTransformRequestedEventArgs(_activeLayerId.Value, nextBounds));
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (PrototypeMode)
        {
            HandlePrototypePointerReleased(e);
            return;
        }

        var interactionMode = _interactionMode;
        var wasInteracting = interactionMode != InteractionMode.None;
        var completionMessage = "Layer transformed";

        if (IsDrawingInteraction(interactionMode))
        {
            var drawing = BuildDrawingRequest();
            ClearDraft();
            if (drawing is not null)
            {
                DrawingRequested?.Invoke(this, drawing);
                completionMessage = "Layer drawn";
            }
        }
        else if (interactionMode == InteractionMode.MovePathPoint)
        {
            completionMessage = "Vector point moved";
        }
        else if (IsCanvasResizeInteraction(interactionMode))
        {
            completionMessage = "Canvas resized";
        }

        _interactionMode = InteractionMode.None;
        _activeLayerId = null;
        _activePathPointIndex = null;
        ClearGuides();
        if (wasInteracting && interactionMode != InteractionMode.PanViewport)
        {
            TransformInteractionStateChanged?.Invoke(this, new CanvasInteractionStateChangedEventArgs(false, completionMessage));
        }

        e.Pointer.Capture(null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var pointer = e.GetPosition(this);
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            var stageRect = GetStageRect();
            var viewportPoint = ToViewportPoint(stageRect, pointer);
            var nextZoom = Math.Clamp(CanvasZoom * Math.Pow(1.12d, e.Delta.Y), MinZoomFactor, MaxZoomFactor);
            ZoomAround(pointer, viewportPoint, nextZoom);
        }
        else
        {
            var horizontalDelta = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? e.Delta.Y : e.Delta.X;
            var verticalDelta = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 0d : e.Delta.Y;
            ApplyViewportPan(_viewportPan + new Vector(horizontalDelta * 42d, verticalDelta * 42d));
        }

        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Space)
        {
            _isSpacePressed = true;
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (e.Key == Key.Space)
        {
            _isSpacePressed = false;
            e.Handled = true;
        }
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        _isSpacePressed = false;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        if (!PrototypeMode || _prototypeHoverLayerId is not Guid hoverLayerId)
        {
            return;
        }

        _prototypeHoverLayerId = null;
        PrototypeTriggerRequested?.Invoke(this, new CanvasPrototypeTriggerRequestedEventArgs(hoverLayerId, InteractionTriggerKind.PointerLeave));
        InvalidateVisual();
    }

    private void HandlePrototypePointerPressed(Rect stageRect, PointerPressedEventArgs e, Point pointer)
    {
        var hitLayer = HitTestLayer(stageRect, pointer);
        if (hitLayer is null)
        {
            _prototypePressedLayerId = null;
            return;
        }

        _prototypePressedLayerId = hitLayer.Id;
        PrototypeTriggerRequested?.Invoke(this, new CanvasPrototypeTriggerRequestedEventArgs(hitLayer.Id, InteractionTriggerKind.PointerDown));
        e.Pointer.Capture(this);
        e.Handled = true;
        InvalidateVisual();
    }

    private void HandlePrototypePointerMoved(PointerEventArgs e)
    {
        var point = e.GetPosition(this);
        var stageRect = GetStageRect();
        var hitLayerId = stageRect.Contains(point) ? HitTestLayer(stageRect, point)?.Id : null;
        if (hitLayerId == _prototypeHoverLayerId)
        {
            return;
        }

        if (_prototypeHoverLayerId is Guid previousHoverLayerId)
        {
            PrototypeTriggerRequested?.Invoke(this, new CanvasPrototypeTriggerRequestedEventArgs(previousHoverLayerId, InteractionTriggerKind.PointerLeave));
        }

        _prototypeHoverLayerId = hitLayerId;
        if (hitLayerId is Guid hoverLayerId)
        {
            PrototypeTriggerRequested?.Invoke(this, new CanvasPrototypeTriggerRequestedEventArgs(hoverLayerId, InteractionTriggerKind.PointerEnter));
        }

        e.Handled = true;
        InvalidateVisual();
    }

    private void HandlePrototypePointerReleased(PointerReleasedEventArgs e)
    {
        var stageRect = GetStageRect();
        var pointer = e.GetPosition(this);
        var hitLayerId = stageRect.Contains(pointer) ? HitTestLayer(stageRect, pointer)?.Id : null;
        if (_prototypePressedLayerId is Guid pressedLayerId)
        {
            PrototypeTriggerRequested?.Invoke(this, new CanvasPrototypeTriggerRequestedEventArgs(pressedLayerId, InteractionTriggerKind.PointerUp));
            if (hitLayerId == pressedLayerId)
            {
                PrototypeTriggerRequested?.Invoke(this, new CanvasPrototypeTriggerRequestedEventArgs(pressedLayerId, InteractionTriggerKind.PointerClick));
            }
        }

        _prototypePressedLayerId = null;
        e.Pointer.Capture(null);
        e.Handled = true;
        InvalidateVisual();
    }

    private void DrawAmbientGlow(DrawingContext context, Rect hostRect)
    {
        var cyanGlow = new SolidColorBrush(Color.Parse("#1435F0D0"));
        var warmGlow = new SolidColorBrush(Color.Parse("#18FF8A4C"));
        context.DrawEllipse(cyanGlow, null, new Point(hostRect.Width * 0.24, hostRect.Height * 0.22), 170, 120);
        context.DrawEllipse(warmGlow, null, new Point(hostRect.Width * 0.84, hostRect.Height * 0.76), 160, 140);
    }

    private void DrawGrid(DrawingContext context, Rect stageRect)
    {
        var scale = GetStageScale();
        var minorPen = new Pen(new SolidColorBrush(Color.Parse("#13253C")), 1);
        var majorPen = new Pen(new SolidColorBrush(Color.Parse("#1F3555")), 1);

        for (var x = 0d; x <= DocumentWidth; x += 80)
        {
            var screenX = stageRect.X + (x * scale);
            var pen = Math.Abs(x % 320) < 0.001 ? majorPen : minorPen;
            context.DrawLine(pen, new Point(screenX, stageRect.Y), new Point(screenX, stageRect.Bottom));
        }

        for (var y = 0d; y <= DocumentHeight; y += 80)
        {
            var screenY = stageRect.Y + (y * scale);
            var pen = Math.Abs(y % 320) < 0.001 ? majorPen : minorPen;
            context.DrawLine(pen, new Point(stageRect.X, screenY), new Point(stageRect.Right, screenY));
        }
    }

    private void DrawLayer(DrawingContext context, Rect stageRect, LayerViewModel layer)
    {
        var snapshot = GetDisplaySnapshot(layer, CurrentTime);
        DrawLayerSnapshot(context, stageRect, layer.Kind, snapshot, tint: null, opacityScale: 1, layer.Model.SourceLibraryItemId);
    }

    private void DrawOnionSkin(DrawingContext context, Rect stageRect)
    {
        if (!OnionSkinEnabled || Layers is null || TotalFrames <= 1 || FrameRate <= 0)
        {
            return;
        }

        var currentFrame = FrameTimelineService.TimeToFrame(CurrentTime, FrameRate, TotalFrames);
        DrawGhostFrames(context, stageRect, currentFrame, OnionSkinBefore, -1, Color.Parse("#FF8A4C"));
        DrawGhostFrames(context, stageRect, currentFrame, OnionSkinAfter, 1, Color.Parse("#61E6FF"));
    }

    private void DrawGhostFrames(DrawingContext context, Rect stageRect, int currentFrame, int count, int direction, Color tint)
    {
        if (Layers is null || count <= 0)
        {
            return;
        }

        var activeCamera = GetActiveCameraSnapshot(CurrentTime);
        for (var offset = count; offset >= 1; offset--)
        {
            var frame = currentFrame + (offset * direction);
            if (frame < 0 || frame >= TotalFrames)
            {
                continue;
            }

            var time = FrameTimelineService.FrameToTime(frame, FrameRate);
            var opacityScale = 0.08 + (((count - offset) + 1) * 0.06);

            foreach (var sample in GetRenderSamples(time))
            {
                if (sample.Role is LayerCompositeRole.Guide or LayerCompositeRole.Camera or LayerCompositeRole.Mask)
                {
                    continue;
                }

                var displaySnapshot = CompositeFrameRenderer.TransformSnapshot(
                    sample.Snapshot,
                    activeCamera,
                    DocumentWidth,
                    DocumentHeight,
                    sample.ParallaxDepth);
                DrawLayerSnapshot(context, stageRect, sample.Kind, displaySnapshot, tint, opacityScale, sample.SourceLibraryItemId);
            }
        }
    }

    private void DrawLayerSnapshot(DrawingContext context, Rect stageRect, LayerKind kind, LayerSnapshot snapshot, Color? tint, double opacityScale, Guid? sourceLibraryItemId = null)
    {
        var rect = ToScreenRect(stageRect, new Rect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height));
        var sourceLibraryItem = ResolveLibraryItem(sourceLibraryItemId);
        var fillBase = ColorHelpers.ApplyCompositeColorEffects(ColorHelpers.Parse(snapshot.Fill, "#FFFFFF"), snapshot.Compositing);
        var strokeBase = ColorHelpers.ApplyCompositeColorEffects(ColorHelpers.Parse(snapshot.Stroke, "#FFFFFF"), snapshot.Compositing);
        var gradientFrom = ColorHelpers.ApplyCompositeColorEffects(ColorHelpers.Parse(snapshot.GradientFrom, snapshot.Fill), snapshot.Compositing);
        var gradientTo = ColorHelpers.ApplyCompositeColorEffects(ColorHelpers.Parse(snapshot.GradientTo, snapshot.Stroke), snapshot.Compositing);
        if (tint is Color tintColor)
        {
            fillBase = Blend(fillBase, tintColor, 0.54);
            strokeBase = Blend(strokeBase, tintColor, 0.48);
            gradientFrom = Blend(gradientFrom, tintColor, 0.54);
            gradientTo = Blend(gradientTo, tintColor, 0.48);
        }

        var isOutlineMode = snapshot.ShowAsOutline;
        var outlineBase = ColorHelpers.ApplyCompositeColorEffects(ColorHelpers.Parse(snapshot.OutlineColor, snapshot.Stroke), snapshot.Compositing);
        if (isOutlineMode)
        {
            fillBase = outlineBase;
            strokeBase = outlineBase;
            gradientFrom = outlineBase;
            gradientTo = outlineBase;
        }

        var opacity = Math.Clamp(snapshot.Opacity * opacityScale, 0, 1);
        var fillColor = ApplyAlpha(fillBase, (byte)(opacity * 255));
        var glowBrush = new SolidColorBrush(ApplyAlpha(fillColor, (byte)Math.Clamp(28 * opacityScale, 0, 255)));
        var fillBrush = isOutlineMode && kind is not LayerKind.Text
            ? new SolidColorBrush(Colors.Transparent)
            : CreateFillBrush(snapshot, rect, fillColor, gradientFrom, gradientTo, opacity);
        var strokeBrush = new SolidColorBrush(ApplyAlpha(strokeBase, (byte)(opacity * 220)));
        var strokePen = LayerStyleRenderHelper.CreateStrokePen(snapshot, strokeBrush, Math.Max(1, snapshot.StrokeThickness * GetStageScale()));

        switch (kind)
        {
            case LayerKind.Rectangle:
                if (!isOutlineMode)
                {
                    context.DrawRectangle(glowBrush, null, rect.Inflate(10), snapshot.CornerRadius + 10, snapshot.CornerRadius + 10);
                }
                using (PushLayerTransform(context, rect, snapshot, sourceLibraryItem))
                {
                    context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                }

                return;
            case LayerKind.Ellipse:
                if (!isOutlineMode)
                {
                    context.DrawRectangle(glowBrush, null, rect.Inflate(10), snapshot.CornerRadius + 10, snapshot.CornerRadius + 10);
                }
                using (PushLayerTransform(context, rect, snapshot, sourceLibraryItem))
                {
                    context.DrawEllipse(fillBrush, strokePen, rect.Center, rect.Width / 2, rect.Height / 2);
                }

                return;
            case LayerKind.Path:
                DrawPathSnapshot(context, stageRect, rect, snapshot, fillBrush, strokeBrush, strokePen, tint, opacityScale, sourceLibraryItem);
                return;
            case LayerKind.AvaloniaControl:
                DrawAvaloniaControlSnapshot(context, rect, snapshot, fillBrush, strokePen, fillColor, strokeBase, opacity, sourceLibraryItem);
                return;
            case LayerKind.Video:
                using (PushLayerTransform(context, rect, snapshot, sourceLibraryItem))
                {
                    MediaAssetRenderService.DrawSyntheticVideoFrame(
                        context,
                        rect,
                        snapshot,
                        "VIDEO",
                        ColorHelpers.ToHex(fillBase),
                        ColorHelpers.ToHex(strokeBase),
                        opacity,
                        renderAsMask: false);
                }

                return;
            case LayerKind.Audio:
                var waveformRect = new Rect(rect.X, rect.Center.Y - 18, rect.Width, 36);
                var waveformBrush = new SolidColorBrush(ApplyAlpha(strokeBase, (byte)(opacity * 190)));
                context.DrawRectangle(
                    new SolidColorBrush(ApplyAlpha(fillBase, (byte)(opacity * 64))),
                    strokePen,
                    waveformRect,
                    12,
                    12);

                var barCount = Math.Max(8, (int)Math.Round(rect.Width / 16d));
                var step = (waveformRect.Width - 20d) / Math.Max(1, barCount);
                for (var barIndex = 0; barIndex < barCount; barIndex++)
                {
                    var normalized = Math.Sin((barIndex / (double)Math.Max(1, barCount - 1)) * Math.PI);
                    var barHeight = Math.Max(4d, normalized * waveformRect.Height * 0.7d);
                    var barWidth = Math.Max(2d, step * 0.45d);
                    var barX = waveformRect.X + 10d + (barIndex * step);
                    var barRect = new Rect(barX, waveformRect.Center.Y - (barHeight / 2d), barWidth, barHeight);
                    context.DrawRectangle(waveformBrush, null, barRect, 2, 2);
                }

                return;
        }

        var plateColor = tint is null ? Color.Parse("#102038") : Blend(Color.Parse("#102038"), tint.Value, 0.4);
        var textPlate = new SolidColorBrush(ApplyAlpha(plateColor, (byte)(opacity * 180)));
        if (!isOutlineMode)
        {
            context.DrawRectangle(textPlate, strokePen, rect, 24, 24);
        }
        using (PushLayerTransform(context, rect, snapshot, sourceLibraryItem))
        {
            DrawTextBlock(context, rect, snapshot.Text, fillBrush, snapshot.FontSize * GetStageScale(), snapshot.TextSettings);
        }
    }

    private void DrawTextBlock(DrawingContext context, Rect rect, string text, IBrush brush, double fontSize, LayerTextSettings? textSettings = null)
    {
        var settings = textSettings ?? new LayerTextSettings();
        var contentRect = DrawFlashTextFieldChrome(context, rect, settings);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var typeface = BuildTypeface(settings);
        var lines = GetRenderedTextLines(text, settings);
        var measuredLines = lines
            .Select(line => new FormattedText(
                line,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                Math.Max(14d, fontSize),
                brush))
            .ToList();
        var lineHeight = settings.LineHeight > 0d
            ? settings.LineHeight * GetStageScale()
            : measuredLines.Count == 0 ? Math.Max(14d, fontSize) : measuredLines.Max(item => item.Height);
        var totalHeight = measuredLines.Count == 0 ? 0d : (measuredLines.Count * lineHeight);
        var y = contentRect.Y + Math.Max(12d, (contentRect.Height - totalHeight) / 2d);

        for (var index = 0; index < measuredLines.Count; index++)
        {
            var formattedLine = measuredLines[index];
            var x = settings.Alignment switch
            {
                LayerTextAlignment.Center => contentRect.X + Math.Max(10d, (contentRect.Width - formattedLine.Width) / 2d),
                LayerTextAlignment.Right => contentRect.Right - formattedLine.Width - 18d,
                _ => contentRect.X + 18d
            };

            if (Math.Abs(settings.LetterSpacing) > 0.001d)
            {
                DrawSpacedText(context, lines[index], brush, typeface, Math.Max(14d, fontSize), new Point(x, y), settings.LetterSpacing * GetStageScale());
            }
            else
            {
                context.DrawText(formattedLine, new Point(x, y));
            }

            y += lineHeight;
        }
    }

    private Rect DrawFlashTextFieldChrome(DrawingContext context, Rect rect, LayerTextSettings settings)
    {
        if (settings.FieldKind == FlashTextFieldKind.Static)
        {
            return rect;
        }

        var borderColor = settings.ShowBorder
            ? Color.Parse("#8EEAFF")
            : Color.Parse("#44566F");
        var fillColor = settings.FieldKind == FlashTextFieldKind.Input
            ? Color.Parse("#102744")
            : Color.Parse("#0D1B31");
        var borderPen = new Pen(new SolidColorBrush(ApplyAlpha(borderColor, settings.ShowBorder ? (byte)192 : (byte)112)), Math.Max(1.2d, 1.2d * GetStageScale()));
        var fillBrush = new SolidColorBrush(ApplyAlpha(fillColor, settings.ShowBorder ? (byte)68 : (byte)34));
        var chromeRect = rect.Deflate(4d);
        context.DrawRectangle(fillBrush, borderPen, chromeRect, 12d, 12d);

        if (!string.IsNullOrWhiteSpace(settings.VariableName))
        {
            var label = new FormattedText(
                settings.VariableName,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Medium),
                Math.Max(10d, 10d * GetStageScale()),
                new SolidColorBrush(ApplyAlpha(Color.Parse("#B6D7FF"), 176)));
            context.DrawText(label, new Point(chromeRect.X + 10d, chromeRect.Bottom - label.Height - 6d));
        }

        return chromeRect.Deflate(8d);
    }

    private static string[] GetRenderedTextLines(string text, LayerTextSettings settings)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        if (settings.MaxCharacters > 0 && normalized.Length > settings.MaxCharacters)
        {
            normalized = normalized[..settings.MaxCharacters];
        }

        if (settings.IsPassword)
        {
            normalized = new string([.. normalized.Select(character => character == '\n' ? '\n' : '•')]);
        }

        if (settings.LineMode == FlashTextLineMode.SingleLine)
        {
            normalized = normalized.Replace('\n', ' ');
        }

        return normalized.Split('\n');
    }

    private void DrawAvaloniaControlSnapshot(
        DrawingContext context,
        Rect rect,
        LayerSnapshot snapshot,
        IBrush fillBrush,
        IPen strokePen,
        Color fillColor,
        Color strokeBase,
        double opacity,
        LibraryItem? sourceLibraryItem)
    {
        var settings = snapshot.AvaloniaControl;
        var textBrush = new SolidColorBrush(ApplyAlpha(Color.Parse("#F5F7FA"), (byte)(Math.Clamp(opacity, 0d, 1d) * 255)));
        var mutedBrush = new SolidColorBrush(ApplyAlpha(Blend(strokeBase, Color.Parse("#F5F7FA"), 0.35d), (byte)(Math.Clamp(opacity, 0d, 1d) * 210)));
        using var _ = PushLayerTransform(context, rect, snapshot, sourceLibraryItem);

        switch (settings.Kind)
        {
            case AvaloniaControlKind.Button:
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(fillColor, 42)), null, rect.Inflate(8), snapshot.CornerRadius + 8, snapshot.CornerRadius + 8);
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                DrawCenteredText(context, rect, string.IsNullOrWhiteSpace(settings.Content) ? "Button" : settings.Content, textBrush, Math.Max(14, snapshot.FontSize * 0.45d));
                return;
            case AvaloniaControlKind.TextBlock:
                DrawTextBlock(context, rect, string.IsNullOrWhiteSpace(settings.Content) ? snapshot.Text : settings.Content, textBrush, Math.Max(18, snapshot.FontSize));
                return;
            case AvaloniaControlKind.TextBox:
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                DrawTextBlock(context, rect, string.IsNullOrWhiteSpace(settings.Content) ? "Type here" : settings.Content, textBrush, Math.Max(14, snapshot.FontSize * 0.45d));
                if (!string.IsNullOrWhiteSpace(settings.SecondaryContent))
                {
                    var watermarkRect = new Rect(rect.X + 20, rect.Bottom - 22, rect.Width - 40, 16);
                    DrawTextBlock(context, watermarkRect, settings.SecondaryContent, mutedBrush, 11);
                }

                return;
            case AvaloniaControlKind.CheckBox:
            {
                var boxSize = Math.Min(24, rect.Height - 10);
                var boxRect = new Rect(rect.X + 8, rect.Center.Y - (boxSize / 2d), boxSize, boxSize);
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(fillColor, 48)), strokePen, boxRect, 6, 6);
                if (settings.IsChecked)
                {
                    var checkPen = new Pen(textBrush, 2.2d);
                    context.DrawLine(checkPen, new Point(boxRect.X + 5, boxRect.Center.Y), new Point(boxRect.X + 10, boxRect.Bottom - 6));
                    context.DrawLine(checkPen, new Point(boxRect.X + 10, boxRect.Bottom - 6), new Point(boxRect.Right - 5, boxRect.Y + 6));
                }

                var labelRect = new Rect(boxRect.Right + 10, rect.Y, rect.Width - boxSize - 24, rect.Height);
                DrawTextBlock(context, labelRect, string.IsNullOrWhiteSpace(settings.Content) ? "CheckBox" : settings.Content, textBrush, 14);
                return;
            }
            case AvaloniaControlKind.ToggleButton:
            {
                var trackRect = new Rect(rect.X + 10, rect.Center.Y - 13, 52, 26);
                var knobRect = new Rect(
                    settings.IsChecked ? trackRect.Right - 22 : trackRect.X + 2,
                    trackRect.Y + 2,
                    20,
                    20);
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(fillColor, settings.IsChecked ? (byte)168 : (byte)72)), strokePen, trackRect, 13, 13);
                context.DrawEllipse(textBrush, null, knobRect.Center, knobRect.Width / 2d, knobRect.Height / 2d);
                var labelRect = new Rect(trackRect.Right + 10, rect.Y, Math.Max(40, rect.Width - 74), rect.Height);
                DrawTextBlock(context, labelRect, string.IsNullOrWhiteSpace(settings.Content) ? "Toggle" : settings.Content, textBrush, 14);
                return;
            }
            case AvaloniaControlKind.Slider:
            {
                var trackRect = new Rect(rect.X + 14, rect.Center.Y - 2, Math.Max(28, rect.Width - 28), 4);
                var normalized = NormalizeRange(settings.Value, settings.Minimum, settings.Maximum);
                var knobX = trackRect.X + (trackRect.Width * normalized);
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(strokeBase, 96)), null, trackRect, 2, 2);
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(fillColor, 210)), null, new Rect(trackRect.X, trackRect.Y, Math.Max(6, knobX - trackRect.X), trackRect.Height), 2, 2);
                context.DrawEllipse(fillBrush, strokePen, new Point(knobX, trackRect.Center.Y), 9, 9);
                return;
            }
            case AvaloniaControlKind.ProgressBar:
            {
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(strokeBase, 72)), strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var normalized = NormalizeRange(settings.Value, settings.Minimum, settings.Maximum);
                var fillRect = new Rect(rect.X + 2, rect.Y + 2, Math.Max(8, (rect.Width - 4) * normalized), Math.Max(8, rect.Height - 4));
                context.DrawRectangle(fillBrush, null, fillRect, Math.Max(4, snapshot.CornerRadius - 2), Math.Max(4, snapshot.CornerRadius - 2));
                return;
            }
            case AvaloniaControlKind.ComboBox:
            {
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var chevronPen = new Pen(textBrush, 1.8d);
                var arrowCenterX = rect.Right - 20d;
                var arrowCenterY = rect.Center.Y;
                context.DrawLine(chevronPen, new Point(arrowCenterX - 6d, arrowCenterY - 3d), new Point(arrowCenterX, arrowCenterY + 3d));
                context.DrawLine(chevronPen, new Point(arrowCenterX, arrowCenterY + 3d), new Point(arrowCenterX + 6d, arrowCenterY - 3d));
                DrawTextBlock(context, new Rect(rect.X + 14d, rect.Y, rect.Width - 42d, rect.Height), string.IsNullOrWhiteSpace(settings.Content) ? "Selected Item" : settings.Content, textBrush, 14d);
                return;
            }
            case AvaloniaControlKind.ListBox:
            {
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var items = SplitControlItems(settings.Content, "Item 1", "Item 2", "Item 3");
                var rowHeight = Math.Max(22d, (rect.Height - 16d) / Math.Max(1, items.Count));
                for (var index = 0; index < items.Count; index++)
                {
                    var rowRect = new Rect(rect.X + 8d, rect.Y + 8d + (index * rowHeight), rect.Width - 16d, rowHeight - 4d);
                    if (index == 0)
                    {
                        context.DrawRectangle(new SolidColorBrush(ApplyAlpha(fillColor, 86)), null, rowRect, 8d, 8d);
                    }

                    DrawTextBlock(context, rowRect, items[index], textBrush, 13d);
                }

                return;
            }
            case AvaloniaControlKind.TabControl:
            {
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var tabs = SplitControlItems(settings.Content, "Overview", "Settings", "Export");
                var active = string.IsNullOrWhiteSpace(settings.SecondaryContent) ? tabs[0] : settings.SecondaryContent;
                var tabWidth = Math.Max(54d, (rect.Width - 16d) / Math.Max(1, tabs.Count));
                for (var index = 0; index < tabs.Count; index++)
                {
                    var tabRect = new Rect(rect.X + 8d + (index * tabWidth), rect.Y + 8d, tabWidth - 4d, 28d);
                    var tabFill = string.Equals(tabs[index], active, StringComparison.OrdinalIgnoreCase)
                        ? new SolidColorBrush(ApplyAlpha(fillColor, 96))
                        : new SolidColorBrush(ApplyAlpha(strokeBase, 44));
                    context.DrawRectangle(tabFill, null, tabRect, 9d, 9d);
                    DrawCenteredText(context, tabRect, tabs[index], textBrush, 12d);
                }

                var bodyRect = new Rect(rect.X + 8d, rect.Y + 44d, rect.Width - 16d, Math.Max(28d, rect.Height - 52d));
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(strokeBase, 34)), null, bodyRect, Math.Max(8d, snapshot.CornerRadius - 4d), Math.Max(8d, snapshot.CornerRadius - 4d));
                DrawCenteredText(context, bodyRect, $"{active} content", mutedBrush, 12d);
                return;
            }
            case AvaloniaControlKind.Grid:
            {
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var cellPen = new Pen(new SolidColorBrush(ApplyAlpha(strokeBase, 92)), 1.2d);
                var verticalMid = rect.X + (rect.Width / 2d);
                var horizontalMid = rect.Y + (rect.Height / 2d);
                context.DrawLine(cellPen, new Point(verticalMid, rect.Y + 10d), new Point(verticalMid, rect.Bottom - 10d));
                context.DrawLine(cellPen, new Point(rect.X + 10d, horizontalMid), new Point(rect.Right - 10d, horizontalMid));
                DrawCenteredText(context, rect, string.IsNullOrWhiteSpace(settings.Content) ? "2x2 Grid" : settings.Content, mutedBrush, 12d);
                return;
            }
            case AvaloniaControlKind.StackPanel:
            {
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var items = SplitControlItems(settings.SecondaryContent, "Item A", "Item B", "Item C");
                var rowHeight = Math.Max(22d, (rect.Height - 18d) / Math.Max(1, items.Count));
                for (var index = 0; index < items.Count; index++)
                {
                    var rowRect = new Rect(rect.X + 10d, rect.Y + 8d + (index * rowHeight), rect.Width - 20d, rowHeight - 6d);
                    context.DrawRectangle(new SolidColorBrush(ApplyAlpha(fillColor, (byte)(index == 0 ? 88 : 52))), null, rowRect, 7d, 7d);
                    DrawTextBlock(context, rowRect, items[index], textBrush, 12d);
                }

                return;
            }
            case AvaloniaControlKind.PathIcon:
            {
                context.DrawRectangle(new SolidColorBrush(ApplyAlpha(fillColor, 36)), strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var iconPoints = ParseControlPathGeometry(settings.SecondaryContent)
                    .Select(point => new Point(rect.X + (point.X * rect.Width), rect.Y + (point.Y * rect.Height)))
                    .ToList();
                var iconGeometry = BuildPathGeometry(iconPoints, true);
                if (iconGeometry is not null)
                {
                    context.DrawGeometry(textBrush, null, iconGeometry);
                }
                else
                {
                    DrawCenteredText(context, rect, string.IsNullOrWhiteSpace(settings.Content) ? "Icon" : settings.Content, textBrush, 14d);
                }

                return;
            }
            case AvaloniaControlKind.Image:
            {
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                var iconPen = new Pen(mutedBrush, 1.8d);
                context.DrawLine(iconPen, new Point(rect.X + 18, rect.Bottom - 18), new Point(rect.Center.X - 8, rect.Center.Y));
                context.DrawLine(iconPen, new Point(rect.Center.X - 8, rect.Center.Y), new Point(rect.Right - 18, rect.Bottom - 34));
                context.DrawEllipse(mutedBrush, null, new Point(rect.Right - 28, rect.Y + 28), 9, 9);
                DrawCenteredText(context, rect, string.IsNullOrWhiteSpace(settings.Source) ? "Image" : settings.Source, textBrush, 12);
                return;
            }
            case AvaloniaControlKind.Panel:
            case AvaloniaControlKind.Border:
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                if (!string.IsNullOrWhiteSpace(settings.Content))
                {
                    var labelRect = new Rect(rect.X + 16, rect.Y + 10, rect.Width - 32, 22);
                    DrawTextBlock(context, labelRect, settings.Content, textBrush, 12);
                }

                return;
            default:
                context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                return;
        }
    }

    private static double NormalizeRange(double value, double minimum, double maximum)
    {
        var range = Math.Max(0.0001d, maximum - minimum);
        return TimelineMath.Clamp((value - minimum) / range, 0d, 1d);
    }

    private static List<string> SplitControlItems(string? value, params string[] fallback)
    {
        var items = string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(['|', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        return items.Count > 0 ? items : [.. fallback];
    }

    private static IReadOnlyList<Point> ParseControlPathGeometry(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return
            [
                new Point(0.5d, 0.08d),
                new Point(0.88d, 0.5d),
                new Point(0.5d, 0.92d),
                new Point(0.12d, 0.5d)
            ];
        }

        var values = System.Text.RegularExpressions.Regex.Matches(data, @"[-+]?\d*\.?\d+")
            .Select(match => double.TryParse(match.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : double.NaN)
            .Where(value => !double.IsNaN(value))
            .ToList();
        if (values.Count < 4)
        {
            return [];
        }

        var points = new List<Point>();
        for (var index = 0; index + 1 < values.Count; index += 2)
        {
            points.Add(new Point(values[index], values[index + 1]));
        }

        return points;
    }

    private static void DrawCenteredText(DrawingContext context, Rect rect, string text, IBrush brush, double fontSize)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default),
            Math.Max(11, fontSize),
            brush);
        var point = new Point(
            rect.X + Math.Max(8, (rect.Width - formattedText.Width) / 2d),
            rect.Y + Math.Max(6, (rect.Height - formattedText.Height) / 2d));
        context.DrawText(formattedText, point);
    }

    private static Typeface BuildTypeface(LayerTextSettings settings)
    {
        var family = settings.UseDeviceFonts || string.IsNullOrWhiteSpace(settings.FontFamily)
            ? FontFamily.Default
            : new FontFamily(settings.FontFamily);
        return new Typeface(
            family,
            settings.IsItalic ? FontStyle.Italic : FontStyle.Normal,
            settings.IsBold ? FontWeight.Bold : FontWeight.Normal);
    }

    private static void DrawSpacedText(
        DrawingContext context,
        string text,
        IBrush brush,
        Typeface typeface,
        double fontSize,
        Point origin,
        double letterSpacing)
    {
        var x = origin.X;
        foreach (var character in text)
        {
            var glyph = new FormattedText(
                character.ToString(),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                brush);
            context.DrawText(glyph, new Point(x, origin.Y));
            x += glyph.Width + letterSpacing;
        }
    }

    private void DrawFrameLabel(DrawingContext context, Rect stageRect)
    {
        var fill = new SolidColorBrush(Color.Parse("#B9C7E8"));
        var label = new FormattedText(
            $"{DocumentWidth:0} × {DocumentHeight:0} stage",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default),
            13,
            fill);

        context.DrawText(label, new Point(stageRect.X + 16, stageRect.Bottom - 28));
    }

    private void DrawCanvasResizeHandles(DrawingContext context, Rect stageRect)
    {
        if (!CanResizeCanvas || PrototypeMode)
        {
            return;
        }

        var accent = DrawingTool == DrawingTool.Select ? Color.Parse("#9BFFF0") : Color.Parse("#677D9F");
        var fill = new SolidColorBrush(Color.FromArgb(192, 9, 16, 28));
        var pen = new Pen(new SolidColorBrush(accent), 1.4d);

        foreach (var rect in GetCanvasResizeHandleRects(stageRect))
        {
            context.DrawRectangle(fill, pen, rect, 5d, 5d);
        }
    }

    private void DrawSelection(DrawingContext context, Rect stageRect, LayerViewModel layer)
    {
        if (layer.Model.Compositing.Role == LayerCompositeRole.Camera)
        {
            var cameraPen = new Pen(new SolidColorBrush(Color.Parse("#FFD166")), 2.4, dashStyle: new DashStyle([8, 5], 0));
            context.DrawRectangle(null, cameraPen, stageRect.Deflate(10), 20, 20);
            return;
        }

        var snapshot = GetDisplaySnapshot(layer, CurrentTime);
        var rect = ToScreenRect(stageRect, new Rect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height));
        var sourceLibraryItem = ResolveLibraryItem(layer.Model.SourceLibraryItemId);
        var transformedCorners = LayerTransformHelper.GetTransformedCorners(rect, snapshot, sourceLibraryItem);
        var borderColor = GetRoleColor(layer.Model.Compositing.Role, layer.IsLocked);
        var borderPen = new Pen(new SolidColorBrush(borderColor), 2, dashStyle: new DashStyle([6, 4], 0));
        using (PushLayerTransform(context, rect, snapshot, sourceLibraryItem))
        {
            context.DrawRectangle(null, borderPen, rect, layer.CornerRadius, layer.CornerRadius);
        }

        if (layer.IsLocked)
        {
            return;
        }

        if (layer.Kind == LayerKind.Path && layer.Model.SourceLibraryItemId is null)
        {
            DrawPathPointHandles(context, stageRect, layer, snapshot, borderColor);
        }

        foreach (var handle in GetHandleRects(transformedCorners))
        {
            context.DrawRectangle(new SolidColorBrush(Color.Parse("#09101C")), new Pen(new SolidColorBrush(Color.Parse("#9BFFF0")), 1.2), handle, 4, 4);
        }
    }

    private void DrawMotionPath(DrawingContext context, Rect stageRect)
    {
        if (SelectedLayer is null ||
            !SelectedLayer.IsRenderable ||
            SelectedLayer.Model.Compositing.Role == LayerCompositeRole.Camera ||
            Duration <= 0 ||
            FrameRate <= 0)
        {
            return;
        }

        var activeCamera = GetActiveCameraSnapshot(CurrentTime);
        var motionPoints = new List<Point>();
        var samples = Math.Max(18, (int)Math.Ceiling(Duration * 22));
        for (var sampleIndex = 0; sampleIndex <= samples; sampleIndex++)
        {
            var time = Math.Min(Duration, Duration * (sampleIndex / (double)samples));
            var snapshot = FrameTimelineService.SampleLayer(SelectedLayer.Model, time, FrameRate, TotalFrames)
                ?? TimelineInterpolationService.SampleLayer(SelectedLayer.Model, time);
            snapshot = CompositeFrameRenderer.TransformSnapshot(
                snapshot,
                activeCamera,
                DocumentWidth,
                DocumentHeight,
                SelectedLayer.Model.Compositing.ParallaxDepth);
            motionPoints.Add(GetMotionAnchorPoint(SelectedLayer, snapshot));
        }

        if (motionPoints.Count < 2)
        {
            return;
        }

        var geometry = BuildPathGeometry(stageRect, motionPoints, isClosed: false);
        if (geometry is null)
        {
            return;
        }

        var pathColor = SelectedLayer.IsLocked ? Color.Parse("#75839F") : Color.Parse("#9BFFF0");
        var glowPen = new Pen(new SolidColorBrush(Color.FromArgb(54, pathColor.R, pathColor.G, pathColor.B)), 7.4, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        var pathPen = new Pen(new SolidColorBrush(Color.FromArgb(205, pathColor.R, pathColor.G, pathColor.B)), 2, dashStyle: new DashStyle([7, 5], 0), lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        context.DrawGeometry(null, glowPen, geometry);
        context.DrawGeometry(null, pathPen, geometry);

        foreach (var time in GetMotionKeyTimes(SelectedLayer.Model))
        {
            var snapshot = FrameTimelineService.SampleLayer(SelectedLayer.Model, time, FrameRate, TotalFrames)
                ?? TimelineInterpolationService.SampleLayer(SelectedLayer.Model, time);
            snapshot = CompositeFrameRenderer.TransformSnapshot(
                snapshot,
                activeCamera,
                DocumentWidth,
                DocumentHeight,
                SelectedLayer.Model.Compositing.ParallaxDepth);
            var anchor = GetMotionAnchorPoint(SelectedLayer, snapshot);
            var center = ToScreenPoint(stageRect, new VectorPointModel { X = anchor.X, Y = anchor.Y });
            context.DrawEllipse(new SolidColorBrush(Color.Parse("#09101C")), new Pen(new SolidColorBrush(Color.Parse("#FFB685")), 1.5), center, 4.8, 4.8);
        }

        var currentSnapshot = FrameTimelineService.SampleLayer(SelectedLayer.Model, CurrentTime, FrameRate, TotalFrames)
            ?? TimelineInterpolationService.SampleLayer(SelectedLayer.Model, CurrentTime);
        currentSnapshot = CompositeFrameRenderer.TransformSnapshot(
            currentSnapshot,
            activeCamera,
            DocumentWidth,
            DocumentHeight,
            SelectedLayer.Model.Compositing.ParallaxDepth);
        var currentAnchor = GetMotionAnchorPoint(SelectedLayer, currentSnapshot);
        var currentPoint = ToScreenPoint(stageRect, new VectorPointModel { X = currentAnchor.X, Y = currentAnchor.Y });
        context.DrawEllipse(new SolidColorBrush(Color.Parse("#9BFFF0")), new Pen(new SolidColorBrush(Color.Parse("#09101C")), 1.2), currentPoint, 6.4, 6.4);
    }

    private void DrawActiveGuides(DrawingContext context, Rect stageRect)
    {
        if (_verticalGuide is null && _horizontalGuide is null)
        {
            return;
        }

        var guidePen = new Pen(new SolidColorBrush(Color.Parse("#9BFFF0")), 1.4, dashStyle: new DashStyle([4, 4], 0));

        if (_verticalGuide is double verticalGuide)
        {
            var point = WorldToScreenPoint(stageRect, new Point(verticalGuide, 0), GetSelectedParallaxDepth());
            var x = point.X;
            context.DrawLine(guidePen, new Point(x, stageRect.Y), new Point(x, stageRect.Bottom));
        }

        if (_horizontalGuide is double horizontalGuide)
        {
            var point = WorldToScreenPoint(stageRect, new Point(0, horizontalGuide), GetSelectedParallaxDepth());
            var y = point.Y;
            context.DrawLine(guidePen, new Point(stageRect.X, y), new Point(stageRect.Right, y));
        }
    }

    private Rect GetStageRect()
    {
        var scale = GetStageScale();
        var centeredRect = GetCenteredStageRect(scale);
        var pan = GetClampedViewportPan(scale);
        return new Rect(centeredRect.X + pan.X, centeredRect.Y + pan.Y, centeredRect.Width, centeredRect.Height);
    }

    private LayerViewModel? HitTestLayer(Rect stageRect, Point point)
    {
        if (Layers is null)
        {
            return null;
        }

        foreach (var layer in Layers.OrderByDescending(item => item.ZIndex))
        {
            if (!layer.IsRenderable || layer.Model.Compositing.Role == LayerCompositeRole.Camera)
            {
                continue;
            }

            var snapshot = GetDisplaySnapshot(layer, CurrentTime);
            var bounds = new Rect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height);
            var viewportPoint = ToViewportPoint(stageRect, point);
            var localPoint = LayerTransformHelper.InverseTransformPoint(
                viewportPoint,
                bounds,
                snapshot,
                ResolveLibraryItem(layer.Model.SourceLibraryItemId));
            if (bounds.Contains(localPoint))
            {
                return layer;
            }
        }

        return null;
    }

    private bool TryHitPathPoint(Rect stageRect, LayerViewModel layer, Point pointer, out int pointIndex)
    {
        if (layer.Kind != LayerKind.Path || !layer.IsRenderable || layer.Model.SourceLibraryItemId is not null)
        {
            pointIndex = -1;
            return false;
        }

        var snapshot = GetDisplaySnapshot(layer, CurrentTime);
        var bounds = new Rect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height);
        var sourceLibraryItem = ResolveLibraryItem(layer.Model.SourceLibraryItemId);
        var points = VectorPathService.GetAbsolutePoints(snapshot);
        for (var index = 0; index < points.Count; index++)
        {
            var transformedPoint = LayerTransformHelper.TransformPoint(
                new Point(points[index].X, points[index].Y),
                bounds,
                snapshot,
                sourceLibraryItem);
            var screenPoint = ToScreenPoint(stageRect, new VectorPointModel { X = transformedPoint.X, Y = transformedPoint.Y });
            var hitRect = new Rect(screenPoint.X - 8, screenPoint.Y - 8, 16, 16);
            if (hitRect.Contains(pointer))
            {
                pointIndex = index;
                return true;
            }
        }

        pointIndex = -1;
        return false;
    }

    private bool TryHitResizeHandle(Rect stageRect, LayerViewModel layer, Point point, out InteractionMode mode)
    {
        var snapshot = GetDisplaySnapshot(layer, CurrentTime);
        var rect = ToScreenRect(stageRect, new Rect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height));
        var handles = GetHandleRects(
            LayerTransformHelper.GetTransformedCorners(
                rect,
                snapshot,
                ResolveLibraryItem(layer.Model.SourceLibraryItemId))).ToArray();

        if (handles[0].Contains(point))
        {
            mode = InteractionMode.ResizeTopLeft;
            return true;
        }

        if (handles[1].Contains(point))
        {
            mode = InteractionMode.ResizeTopRight;
            return true;
        }

        if (handles[2].Contains(point))
        {
            mode = InteractionMode.ResizeBottomLeft;
            return true;
        }

        if (handles[3].Contains(point))
        {
            mode = InteractionMode.ResizeBottomRight;
            return true;
        }

        mode = InteractionMode.None;
        return false;
    }

    private bool TryHitCanvasResizeHandle(Rect stageRect, Point point, out InteractionMode mode)
    {
        var handles = GetCanvasResizeHandleRects(stageRect).ToArray();
        if (handles[0].Contains(point))
        {
            mode = InteractionMode.ResizeCanvasBottomRight;
            return true;
        }

        if (handles[1].Contains(point))
        {
            mode = InteractionMode.ResizeCanvasRight;
            return true;
        }

        if (handles[2].Contains(point))
        {
            mode = InteractionMode.ResizeCanvasBottom;
            return true;
        }

        mode = InteractionMode.None;
        return false;
    }

    private IEnumerable<Rect> GetHandleRects(Rect rect)
    {
        var half = HandleSize / 2;
        yield return new Rect(rect.TopLeft.X - half, rect.TopLeft.Y - half, HandleSize, HandleSize);
        yield return new Rect(rect.TopRight.X - half, rect.TopRight.Y - half, HandleSize, HandleSize);
        yield return new Rect(rect.BottomLeft.X - half, rect.BottomLeft.Y - half, HandleSize, HandleSize);
        yield return new Rect(rect.BottomRight.X - half, rect.BottomRight.Y - half, HandleSize, HandleSize);
    }

    private IEnumerable<Rect> GetHandleRects(IReadOnlyList<Point> corners)
    {
        var half = HandleSize / 2d;
        for (var index = 0; index < corners.Count; index++)
        {
            var corner = corners[index];
            yield return new Rect(corner.X - half, corner.Y - half, HandleSize, HandleSize);
        }
    }

    private IEnumerable<Rect> GetCanvasResizeHandleRects(Rect stageRect)
    {
        yield return new Rect(
            stageRect.Right - (CanvasResizeHandleSize * 0.5d),
            stageRect.Bottom - (CanvasResizeHandleSize * 0.5d),
            CanvasResizeHandleSize,
            CanvasResizeHandleSize);
        yield return new Rect(
            stageRect.Right - (CanvasResizeHandleSize * 0.5d),
            stageRect.Center.Y - (CanvasResizeHandleLength / 2d),
            CanvasResizeHandleSize,
            CanvasResizeHandleLength);
        yield return new Rect(
            stageRect.Center.X - (CanvasResizeHandleLength / 2d),
            stageRect.Bottom - (CanvasResizeHandleSize * 0.5d),
            CanvasResizeHandleLength,
            CanvasResizeHandleSize);
    }

    private Rect ToScreenRect(Rect stageRect, Rect documentRect)
    {
        var scale = GetStageScale();
        return new Rect(
            stageRect.X + (documentRect.X * scale),
            stageRect.Y + (documentRect.Y * scale),
            documentRect.Width * scale,
            documentRect.Height * scale);
    }

    private Point ToScreenPoint(Rect stageRect, VectorPointModel point)
    {
        var scale = GetStageScale();
        return new Point(
            stageRect.X + (point.X * scale),
            stageRect.Y + (point.Y * scale));
    }

    private Point ToViewportPoint(Rect stageRect, Point point)
    {
        var scale = GetStageScale();
        return new Point(
            TimelineAnimations.Core.Services.TimelineMath.Clamp((point.X - stageRect.X) / scale, 0, DocumentWidth),
            TimelineAnimations.Core.Services.TimelineMath.Clamp((point.Y - stageRect.Y) / scale, 0, DocumentHeight));
    }

    private Point ToWorldPoint(Rect stageRect, Point point, LayerViewModel? layer = null)
    {
        var viewportPoint = ToViewportPoint(stageRect, point);
        if (layer?.Model.Compositing.Role == LayerCompositeRole.Camera)
        {
            return viewportPoint;
        }

        var worldPoint = CompositeFrameRenderer.InverseTransformPoint(
            viewportPoint,
            GetActiveCameraSnapshot(CurrentTime),
            DocumentWidth,
            DocumentHeight,
            layer?.Model.Compositing.ParallaxDepth ?? 1d);
        return new Point(
            TimelineAnimations.Core.Services.TimelineMath.Clamp(worldPoint.X, 0, DocumentWidth),
            TimelineAnimations.Core.Services.TimelineMath.Clamp(worldPoint.Y, 0, DocumentHeight));
    }

    private Rect NormalizeRect(double x, double y, double width, double height)
    {
        var normalizedWidth = Math.Max(24, width);
        var normalizedHeight = Math.Max(24, height);
        return new Rect(x, y, normalizedWidth, normalizedHeight);
    }

    private Rect ClampToDocument(Rect rect)
    {
        var width = Math.Min(rect.Width, DocumentWidth);
        var height = Math.Min(rect.Height, DocumentHeight);
        var x = TimelineAnimations.Core.Services.TimelineMath.Clamp(rect.X, 0, DocumentWidth - width);
        var y = TimelineAnimations.Core.Services.TimelineMath.Clamp(rect.Y, 0, DocumentHeight - height);
        return new Rect(x, y, width, height);
    }

    private void ApplyResizeSnapping(ref Rect bounds)
    {
        var snapResult = CanvasSnapService.SnapResize(
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            DocumentWidth,
            DocumentHeight,
            SnapToGrid);
        bounds = new Rect(snapResult.X, snapResult.Y, snapResult.Width, snapResult.Height);
        _verticalGuide = snapResult.VerticalGuide;
        _horizontalGuide = snapResult.HorizontalGuide;
    }

    private void ClearGuides()
    {
        _verticalGuide = null;
        _horizontalGuide = null;
    }

    private void ClearDraft()
    {
        _draftPoints = [];
    }

    private IDisposable PushLayerTransform(DrawingContext context, Rect rect, LayerSnapshot snapshot, LibraryItem? sourceLibraryItem)
    {
        if (!LayerTransformHelper.TryCreateMatrix(rect, snapshot, sourceLibraryItem, out var matrix))
        {
            return Disposable.Empty;
        }

        return context.PushTransform(matrix);
    }

    private void DrawPathSnapshot(
        DrawingContext context,
        Rect stageRect,
        Rect rect,
        LayerSnapshot snapshot,
        IBrush fillBrush,
        SolidColorBrush strokeBrush,
        IPen strokePen,
        Color? tint,
        double opacityScale,
        LibraryItem? sourceLibraryItem)
    {
        var geometry = BuildPathGeometry(stageRect, snapshot);
        if (geometry is null)
        {
            return;
        }

        var glowColor = tint is Color tintColor
            ? Blend(ColorHelpers.Parse(snapshot.Stroke, "#FFFFFF"), tintColor, 0.35)
            : ColorHelpers.Parse(snapshot.Stroke, "#FFFFFF");
        var glowPen = new Pen(
            new SolidColorBrush(ApplyAlpha(glowColor, (byte)Math.Clamp(52 * opacityScale, 0, 255))),
            Math.Max(8, (snapshot.StrokeThickness + 10) * GetStageScale()),
            lineCap: strokePen.LineCap,
            lineJoin: strokePen.LineJoin,
            miterLimit: strokePen.MiterLimit);
        using (PushLayerTransform(context, rect, snapshot, sourceLibraryItem))
        {
            if (snapshot.HasStroke)
            {
                context.DrawGeometry(null, glowPen, geometry);
            }

            context.DrawGeometry(snapshot.IsClosed ? fillBrush : null, strokePen, geometry);
        }
    }

    private void DrawDraftLayer(DrawingContext context, Rect stageRect)
    {
        if (!IsDrawingInteraction(_interactionMode))
        {
            return;
        }

        var accent = Color.Parse("#9BFFF0");
        var previewFill = new SolidColorBrush(Color.FromArgb(42, accent.R, accent.G, accent.B));
        var previewPen = new Pen(new SolidColorBrush(accent), 1.8, dashStyle: new DashStyle([7, 5], 0), lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

        if (_interactionMode is InteractionMode.DrawRectangle or InteractionMode.DrawEllipse or InteractionMode.DrawText)
        {
            var rect = ToScreenRect(stageRect, GetDraftBounds());
            if (_interactionMode == InteractionMode.DrawEllipse)
            {
                context.DrawEllipse(previewFill, previewPen, rect.Center, rect.Width / 2, rect.Height / 2);
                return;
            }

            context.DrawRectangle(previewFill, previewPen, rect, 22, 22);
            if (_interactionMode == InteractionMode.DrawText)
            {
                DrawTextBlock(context, rect, "TEXT", new SolidColorBrush(accent), 24);
            }

            return;
        }

        var points = GetDraftDocumentPoints();
        if (points.Count < 2)
        {
            return;
        }

        var geometry = BuildPathGeometry(stageRect, points, isClosed: false);
        if (geometry is null)
        {
            return;
        }

        context.DrawGeometry(null, previewPen, geometry);
        foreach (var point in points)
        {
            var screenPoint = ToScreenPoint(stageRect, new VectorPointModel { X = point.X, Y = point.Y });
            context.DrawEllipse(new SolidColorBrush(Color.Parse("#09101C")), new Pen(new SolidColorBrush(accent), 1.1), screenPoint, 4.5, 4.5);
        }
    }

    private void DrawPathPointHandles(DrawingContext context, Rect stageRect, LayerViewModel layer, LayerSnapshot snapshot, Color borderColor)
    {
        var bounds = new Rect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height);
        var sourceLibraryItem = ResolveLibraryItem(layer.Model.SourceLibraryItemId);
        foreach (var point in VectorPathService.GetAbsolutePoints(snapshot))
        {
            var transformedPoint = LayerTransformHelper.TransformPoint(
                new Point(point.X, point.Y),
                bounds,
                snapshot,
                sourceLibraryItem);
            var screenPoint = ToScreenPoint(stageRect, new VectorPointModel { X = transformedPoint.X, Y = transformedPoint.Y });
            context.DrawEllipse(new SolidColorBrush(Color.Parse("#09101C")), new Pen(new SolidColorBrush(borderColor), 1.4), screenPoint, 5.5, 5.5);
        }
    }

    private IBrush CreateFillBrush(LayerSnapshot snapshot, Rect rect, Color solidFill, Color gradientFrom, Color gradientTo, double opacity)
    {
        return LayerStyleRenderHelper.CreateFillBrush(snapshot, solidFill, gradientFrom, gradientTo, opacity);
    }

    private StreamGeometry? BuildPathGeometry(Rect stageRect, LayerSnapshot snapshot)
    {
        var points = VectorPathService.GetAbsolutePoints(snapshot)
            .Select(point => ToScreenPoint(stageRect, point))
            .ToList();
        return BuildPathGeometry(points, snapshot.IsClosed);
    }

    private StreamGeometry? BuildPathGeometry(Rect stageRect, IReadOnlyList<Point> documentPoints, bool isClosed)
    {
        var points = documentPoints
            .Select(point => ToScreenPoint(stageRect, new VectorPointModel { X = point.X, Y = point.Y }))
            .ToList();
        return BuildPathGeometry(points, isClosed);
    }

    private static StreamGeometry? BuildPathGeometry(IReadOnlyList<Point> points, bool isClosed)
    {
        if (points.Count < 2)
        {
            return null;
        }

        var geometry = new StreamGeometry();
        using var geometryContext = geometry.Open();
        geometryContext.BeginFigure(points[0], isFilled: isClosed);
        foreach (var point in points.Skip(1))
        {
            geometryContext.LineTo(point);
        }

        geometryContext.EndFigure(isClosed);
        return geometry;
    }

    private static double Distance(Point left, Point right)
    {
        var delta = right - left;
        return Math.Sqrt((delta.X * delta.X) + (delta.Y * delta.Y));
    }

    private LayerSnapshot CreateLayerSnapshot(LayerViewModel layer)
    {
        var style = layer.Model.Style;
        return new LayerSnapshot(
            layer.X,
            layer.Y,
            layer.Width,
            layer.Height,
            layer.ScaleX,
            layer.ScaleY,
            layer.SkewX,
            layer.SkewY,
            layer.Rotation,
            layer.Opacity,
            layer.CornerRadius,
            layer.FillHex,
            layer.StrokeHex,
            layer.TextContent,
            layer.FontSize,
            style.StrokeThickness,
            style.UseGradient,
            style.GradientFrom,
            style.GradientTo,
            style.IsClosed,
            layer.Model.ShowAsOutline,
            layer.Model.OutlineColor,
            VectorPathService.ClonePoints(style.PathPoints),
            style.AvaloniaControl.Clone(),
            layer.Model.Compositing.Clone(),
            style.TextSettings.Clone());
    }

    private Rect GetDraftBounds()
    {
        var left = Math.Min(_draftOrigin.X, _draftCurrent.X);
        var top = Math.Min(_draftOrigin.Y, _draftCurrent.Y);
        var width = Math.Max(24, Math.Abs(_draftCurrent.X - _draftOrigin.X));
        var height = Math.Max(24, Math.Abs(_draftCurrent.Y - _draftOrigin.Y));
        return ClampToDocument(new Rect(left, top, width, height));
    }

    private IReadOnlyList<Point> GetDraftDocumentPoints()
    {
        return _interactionMode switch
        {
            InteractionMode.DrawLine => [_draftOrigin, _draftCurrent],
            InteractionMode.DrawPen or InteractionMode.DrawBrush => _draftPoints.Count > 1 ? [.. _draftPoints] : [_draftOrigin, _draftCurrent],
            _ => [_draftOrigin, _draftCurrent]
        };
    }

    private CanvasDrawingRequestedEventArgs? BuildDrawingRequest()
    {
        if (!IsDrawingInteraction(_interactionMode))
        {
            return null;
        }

        var bounds = GetDraftBounds();
        var points = GetDraftDocumentPoints();
        var rawWidth = Math.Abs(_draftCurrent.X - _draftOrigin.X);
        var rawHeight = Math.Abs(_draftCurrent.Y - _draftOrigin.Y);

        if (_interactionMode is InteractionMode.DrawPen or InteractionMode.DrawBrush)
        {
            if (points.Count < 2)
            {
                return null;
            }

            return new CanvasDrawingRequestedEventArgs(DrawingTool, bounds, points);
        }

        if (_interactionMode == InteractionMode.DrawLine)
        {
            if (Distance(_draftOrigin, _draftCurrent) < 4)
            {
                return null;
            }

            return new CanvasDrawingRequestedEventArgs(DrawingTool, bounds, points);
        }

        if (_interactionMode == InteractionMode.DrawText && rawWidth < 8 && rawHeight < 8)
        {
            return new CanvasDrawingRequestedEventArgs(DrawingTool, new Rect(_draftOrigin.X, _draftOrigin.Y, 320, 92), points);
        }

        if (rawWidth < 4 || rawHeight < 4)
        {
            return null;
        }

        return new CanvasDrawingRequestedEventArgs(DrawingTool, bounds, points);
    }

    private static bool IsDrawingInteraction(InteractionMode mode)
    {
        return mode is InteractionMode.DrawRectangle or
            InteractionMode.DrawEllipse or
            InteractionMode.DrawText or
            InteractionMode.DrawLine or
            InteractionMode.DrawPen or
            InteractionMode.DrawBrush;
    }

    private static bool IsCanvasResizeInteraction(InteractionMode mode)
    {
        return mode is InteractionMode.ResizeCanvasRight or
            InteractionMode.ResizeCanvasBottom or
            InteractionMode.ResizeCanvasBottomRight;
    }

    private static InteractionMode GetDrawingInteractionMode(DrawingTool tool)
    {
        return tool switch
        {
            DrawingTool.Rectangle => InteractionMode.DrawRectangle,
            DrawingTool.Ellipse => InteractionMode.DrawEllipse,
            DrawingTool.Text => InteractionMode.DrawText,
            DrawingTool.Line => InteractionMode.DrawLine,
            DrawingTool.Pen => InteractionMode.DrawPen,
            DrawingTool.Brush => InteractionMode.DrawBrush,
            _ => InteractionMode.None
        };
    }

    private double NormalizeCanvasExtent(double extent)
    {
        var normalized = SceneEditingService.NormalizeCanvasExtent(extent);
        if (!SnapToGrid)
        {
            return SceneEditingService.NormalizeCanvasExtent(Math.Round(normalized));
        }

        var snapped = Math.Round(normalized / CanvasResizeSnapIncrement) * CanvasResizeSnapIncrement;
        return SceneEditingService.NormalizeCanvasExtent(snapped);
    }

    private static IReadOnlyList<double> GetMotionKeyTimes(TimelineLayer layer)
    {
        return
        [
            .. layer.Tracks
                .Where(track => track.Property is AnimatedProperty.X or AnimatedProperty.Y)
                .SelectMany(track => track.Keyframes)
                .Select(keyframe => keyframe.Time)
                .Distinct()
                .OrderBy(time => time)
        ];
    }

    private Point GetMotionAnchorPoint(LayerViewModel layer, LayerSnapshot snapshot)
    {
        var rect = new Rect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height);
        return LayerTransformHelper.ResolveOrigin(rect, ResolveLibraryItem(layer.Model.SourceLibraryItemId));
    }

    private static bool ShouldStartViewportPan(PointerPressedEventArgs e, Control control, bool isSpacePressed)
    {
        var properties = e.GetCurrentPoint(control).Properties;
        return properties.IsMiddleButtonPressed || (isSpacePressed && properties.IsLeftButtonPressed);
    }

    private double GetFitScale()
    {
        var availableWidth = Math.Max(120d, Bounds.Width - (SurfacePadding * 2d));
        var availableHeight = Math.Max(120d, Bounds.Height - (SurfacePadding * 2d));
        if (DocumentWidth <= 0 || DocumentHeight <= 0)
        {
            return 1d;
        }

        return Math.Max(0.05d, Math.Min(availableWidth / DocumentWidth, availableHeight / DocumentHeight));
    }

    private double GetStageScale()
    {
        return Math.Max(0.05d, GetFitScale() * CanvasZoom);
    }

    private Rect GetCenteredStageRect(double scale)
    {
        var width = DocumentWidth * scale;
        var height = DocumentHeight * scale;
        return new Rect((Bounds.Width - width) / 2d, (Bounds.Height - height) / 2d, width, height);
    }

    private Vector GetClampedViewportPan(double scale, Vector? candidatePan = null)
    {
        var centeredRect = GetCenteredStageRect(scale);
        var pan = candidatePan ?? _viewportPan;
        var minX = GetMinimumStageOrigin(centeredRect.Width, Bounds.Width);
        var maxX = GetMaximumStageOrigin(centeredRect.Width, Bounds.Width);
        var minY = GetMinimumStageOrigin(centeredRect.Height, Bounds.Height);
        var maxY = GetMaximumStageOrigin(centeredRect.Height, Bounds.Height);
        var stageX = TimelineAnimations.Core.Services.TimelineMath.Clamp(centeredRect.X + pan.X, minX, maxX);
        var stageY = TimelineAnimations.Core.Services.TimelineMath.Clamp(centeredRect.Y + pan.Y, minY, maxY);
        return new Vector(stageX - centeredRect.X, stageY - centeredRect.Y);
    }

    private static double GetMinimumStageOrigin(double stageExtent, double hostExtent)
    {
        var visibleExtent = Math.Min(MinVisibleStageExtent, stageExtent);
        return stageExtent <= hostExtent - (SurfacePadding * 2d)
            ? SurfacePadding
            : SurfacePadding + visibleExtent - stageExtent;
    }

    private static double GetMaximumStageOrigin(double stageExtent, double hostExtent)
    {
        var visibleExtent = Math.Min(MinVisibleStageExtent, stageExtent);
        return stageExtent <= hostExtent - (SurfacePadding * 2d)
            ? Math.Max(SurfacePadding, hostExtent - SurfacePadding - stageExtent)
            : hostExtent - SurfacePadding - visibleExtent;
    }

    private void NormalizeViewportPan()
    {
        _viewportPan = GetClampedViewportPan(GetStageScale());
    }

    private void ApplyViewportPan(Vector nextPan)
    {
        _viewportPan = GetClampedViewportPan(GetStageScale(), nextPan);
        InvalidateVisual();
    }

    private void ZoomAround(Point pointer, Point viewportPoint, double nextZoom)
    {
        if (Math.Abs(nextZoom - CanvasZoom) < 0.0001d)
        {
            return;
        }

        var fitScale = GetFitScale();
        var nextScale = Math.Max(0.05d, fitScale * nextZoom);
        var centeredRect = GetCenteredStageRect(nextScale);
        var desiredStageX = pointer.X - (viewportPoint.X * nextScale);
        var desiredStageY = pointer.Y - (viewportPoint.Y * nextScale);
        CanvasZoom = nextZoom;
        _viewportPan = GetClampedViewportPan(nextScale, new Vector(desiredStageX - centeredRect.X, desiredStageY - centeredRect.Y));
        InvalidateVisual();
    }

    private void HandleDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = TryParsePaletteDrop(e.DataTransfer.TryGetText(), out _, out _)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void HandleDrop(object? sender, DragEventArgs e)
    {
        if (!TryParsePaletteDrop(e.DataTransfer.TryGetText(), out var kind, out var controlKind))
        {
            return;
        }

        var documentPosition = ToWorldPoint(GetStageRect(), e.GetPosition(this));
        PaletteDropRequested?.Invoke(this, new CanvasPaletteDropRequestedEventArgs(kind, documentPosition, controlKind));
        e.Handled = true;
    }

    private static bool TryParsePaletteDrop(string? text, out LayerKind kind, out AvaloniaControlKind? controlKind)
    {
        kind = default;
        controlKind = null;

        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith("palette:", StringComparison.Ordinal))
        {
            return false;
        }

        var parts = text.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 3 && string.Equals(parts[1], "shape", StringComparison.OrdinalIgnoreCase))
        {
            return Enum.TryParse(parts[2], true, out kind);
        }

        if (parts.Length >= 3 && string.Equals(parts[1], "control", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse(parts[2], true, out AvaloniaControlKind parsedControlKind))
            {
                return false;
            }

            kind = LayerKind.AvaloniaControl;
            controlKind = parsedControlKind;
            return true;
        }

        return Enum.TryParse(text["palette:".Length..], true, out kind);
    }

    private void AttachLayerListeners(IReadOnlyList<LayerViewModel>? layers)
    {
        if (layers is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += HandleLayersCollectionChanged;
        }

        if (layers is null)
        {
            return;
        }

        foreach (var layer in layers)
        {
            layer.PropertyChanged += HandleLayerPropertyChanged;
        }
    }

    private void DetachLayerListeners(IReadOnlyList<LayerViewModel>? layers)
    {
        if (layers is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged -= HandleLayersCollectionChanged;
        }

        if (layers is null)
        {
            return;
        }

        foreach (var layer in layers)
        {
            layer.PropertyChanged -= HandleLayerPropertyChanged;
        }
    }

    private void HandleLayersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (LayerViewModel layer in e.OldItems)
            {
                layer.PropertyChanged -= HandleLayerPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (LayerViewModel layer in e.NewItems)
            {
                layer.PropertyChanged += HandleLayerPropertyChanged;
            }
        }

        InvalidateVisual();
    }

    private void HandleLayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private static Color ApplyAlpha(Color color, byte alpha)
    {
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static Color Blend(Color baseColor, Color tint, double amount)
    {
        var normalized = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            baseColor.A,
            (byte)(baseColor.R + ((tint.R - baseColor.R) * normalized)),
            (byte)(baseColor.G + ((tint.G - baseColor.G) * normalized)),
            (byte)(baseColor.B + ((tint.B - baseColor.B) * normalized)));
    }

    private IReadOnlyList<RenderableLayerSample> GetRenderSamples(double time)
    {
        if (Layers is null)
        {
            return [];
        }

        if (Document is null)
        {
            return
            [
                .. Layers
                    .Where(layer => layer.IsRenderable)
                    .Select(layer => new RenderableLayerSample
                    {
                        SourceLayerId = layer.Id,
                        SourceLibraryItemId = layer.Model.SourceLibraryItemId,
                        Kind = layer.Kind,
                        ZIndex = layer.ZIndex,
                        Snapshot = new LayerSnapshot(
                            layer.X,
                            layer.Y,
                            layer.Width,
                            layer.Height,
                            layer.ScaleX,
                            layer.ScaleY,
                            layer.SkewX,
                            layer.SkewY,
                            layer.Rotation,
                            layer.Opacity,
                            layer.CornerRadius,
                            layer.FillHex,
                            layer.StrokeHex,
                            layer.TextContent,
                            layer.FontSize,
                            layer.Model.Style.StrokeThickness,
                            layer.Model.Style.UseGradient,
                            layer.Model.Style.GradientFrom,
                            layer.Model.Style.GradientTo,
                            layer.Model.Style.IsClosed,
                            layer.Model.ShowAsOutline,
                            layer.Model.OutlineColor,
                            VectorPathService.ClonePoints(layer.Model.Style.PathPoints),
                            layer.Model.Style.AvaloniaControl.Clone(),
                            layer.Model.Compositing.Clone(),
                            layer.Model.Style.TextSettings.Clone())
                    })
            ];
        }

        return SymbolRenderService.BuildRenderSamples(
            Document,
            Layers.Select(layer => layer.Model).ToList(),
            time,
            Duration,
            FrameRate);
    }

    private CompositeSceneState BuildSceneState(double time)
    {
        var samples = GetRenderSamples(time);
        return new CompositeSceneState(
            DocumentWidth,
            DocumentHeight,
            BackgroundFrom,
            BackgroundTo,
            Document?.LibraryItems.ToDictionary(item => item.Id) ?? new Dictionary<Guid, LibraryItem>(),
            Document?.MediaAssets.ToDictionary(item => item.Id) ?? new Dictionary<Guid, MediaAsset>(),
            samples,
            CompositeFrameRenderer.ResolveActiveCamera(samples));
    }

    private LayerSnapshot? GetActiveCameraSnapshot(double time)
    {
        return BuildSceneState(time).ActiveCamera;
    }

    private LayerSnapshot GetDisplaySnapshot(LayerViewModel layer, double time)
    {
        var snapshot = time == CurrentTime
            ? CreateLayerSnapshot(layer)
            : FrameTimelineService.SampleLayer(layer.Model, time, FrameRate, TotalFrames)
                ?? TimelineInterpolationService.SampleLayer(layer.Model, time);
        return TransformSnapshotForStage(layer.Model, snapshot, time);
    }

    private LayerSnapshot TransformSnapshotForStage(TimelineLayer layer, LayerSnapshot snapshot, double time)
    {
        if (layer.Compositing.Role == LayerCompositeRole.Camera)
        {
            return snapshot;
        }

        return CompositeFrameRenderer.TransformSnapshot(
            snapshot,
            GetActiveCameraSnapshot(time),
            DocumentWidth,
            DocumentHeight,
            layer.Compositing.ParallaxDepth);
    }

    private Point WorldToScreenPoint(Rect stageRect, Point point, double parallaxDepth)
    {
        var viewport = CompositeFrameRenderer.TransformPoint(
            point,
            GetActiveCameraSnapshot(CurrentTime),
            DocumentWidth,
            DocumentHeight,
            parallaxDepth);
        return ToScreenPoint(stageRect, new VectorPointModel { X = viewport.X, Y = viewport.Y });
    }

    private double GetSelectedParallaxDepth()
    {
        return SelectedLayer?.Model.Compositing.ParallaxDepth ?? 1d;
    }

    private void DrawRoleOverlays(DrawingContext context, Rect stageRect, CompositeSceneState sceneState)
    {
        foreach (var sample in sceneState.Samples)
        {
            if (sample.Role == LayerCompositeRole.Guide)
            {
                var snapshot = CompositeFrameRenderer.TransformSnapshot(sample.Snapshot, sceneState.ActiveCamera, DocumentWidth, DocumentHeight, sample.ParallaxDepth);
                DrawLayerSnapshot(context, stageRect, sample.Kind, snapshot, Color.Parse("#61E6FF"), 0.28d, sample.SourceLibraryItemId);
                continue;
            }

            if (sample.Role == LayerCompositeRole.Mask)
            {
                var snapshot = CompositeFrameRenderer.TransformSnapshot(sample.Snapshot, sceneState.ActiveCamera, DocumentWidth, DocumentHeight, sample.ParallaxDepth);
                DrawLayerSnapshot(context, stageRect, sample.Kind, snapshot, Color.Parse("#FFD166"), 0.24d, sample.SourceLibraryItemId);
            }
        }
    }

    private static Color GetRoleColor(LayerCompositeRole role, bool isLocked)
    {
        if (isLocked)
        {
            return Color.Parse("#FFB685");
        }

        return role switch
        {
            LayerCompositeRole.Mask => Color.Parse("#FFD166"),
            LayerCompositeRole.Guide => Color.Parse("#61E6FF"),
            LayerCompositeRole.Camera => Color.Parse("#FFD166"),
            _ => Color.Parse("#9BFFF0")
        };
    }

    private LibraryItem? ResolveLibraryItem(Guid? sourceLibraryItemId)
    {
        return Document is not null &&
               sourceLibraryItemId is Guid libraryItemId
            ? Document.LibraryItems.FirstOrDefault(item => item.Id == libraryItemId)
            : null;
    }

    private enum InteractionMode
    {
        None,
        PanViewport,
        Move,
        MovePathPoint,
        ResizeTopLeft,
        ResizeTopRight,
        ResizeBottomLeft,
        ResizeBottomRight,
        ResizeCanvasRight,
        ResizeCanvasBottom,
        ResizeCanvasBottomRight,
        DrawRectangle,
        DrawEllipse,
        DrawText,
        DrawLine,
        DrawPen,
        DrawBrush
    }

    private sealed class Disposable(Action? dispose) : IDisposable
    {
        public static readonly IDisposable Empty = new Disposable(null);

        public void Dispose()
        {
            dispose?.Invoke();
        }
    }
}
