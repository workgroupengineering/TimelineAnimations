using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
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
    private const double SurfacePadding = 26;
    private const double HandleSize = 12;
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

    public static readonly StyledProperty<IReadOnlyList<LayerViewModel>?> LayersProperty =
        AvaloniaProperty.Register<SceneCanvasControl, IReadOnlyList<LayerViewModel>?>(nameof(Layers));

    public static readonly StyledProperty<LayerViewModel?> SelectedLayerProperty =
        AvaloniaProperty.Register<SceneCanvasControl, LayerViewModel?>(nameof(SelectedLayer));

    public static readonly StyledProperty<TimelineDocument?> DocumentProperty =
        AvaloniaProperty.Register<SceneCanvasControl, TimelineDocument?>(nameof(Document));

    public static readonly StyledProperty<double> CanvasZoomProperty =
        AvaloniaProperty.Register<SceneCanvasControl, double>(nameof(CanvasZoom), 0.74d);

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
            PrototypeModeProperty);
    }

    public SceneCanvasControl()
    {
        ClipToBounds = true;
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, HandleDragOver);
        AddHandler(DragDrop.DropEvent, HandleDrop);
    }

    public event EventHandler<CanvasLayerSelectionRequestedEventArgs>? LayerSelectionRequested;

    public event EventHandler<CanvasLayerTransformRequestedEventArgs>? LayerTransformRequested;

    public event EventHandler<CanvasPaletteDropRequestedEventArgs>? PaletteDropRequested;

    public event EventHandler<CanvasDrawingRequestedEventArgs>? DrawingRequested;

    public event EventHandler<CanvasPathPointMoveRequestedEventArgs>? PathPointMoveRequested;

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

        if (SelectedLayer is not null && SelectedLayer.IsRenderable)
        {
            DrawSelection(context, stageRect, SelectedLayer);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var stageRect = GetStageRect();
        var pointer = e.GetPosition(this);
        var documentPoint = ToWorldPoint(stageRect, pointer);
        _pointerOrigin = pointer;
        _interactionMode = InteractionMode.None;
        _activeLayerId = null;
        _activePathPointIndex = null;
        ClearGuides();
        ClearDraft();

        if (!stageRect.Contains(pointer))
        {
            return;
        }

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
            PathPointMoveRequested?.Invoke(this, new CanvasPathPointMoveRequestedEventArgs(pathLayerId, pointIndex, documentPoint));
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

        _interactionMode = InteractionMode.None;
        _activeLayerId = null;
        _activePathPointIndex = null;
        ClearGuides();
        if (wasInteracting)
        {
            TransformInteractionStateChanged?.Invoke(this, new CanvasInteractionStateChangedEventArgs(false, completionMessage));
        }

        e.Pointer.Capture(null);
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
        var minorPen = new Pen(new SolidColorBrush(Color.Parse("#13253C")), 1);
        var majorPen = new Pen(new SolidColorBrush(Color.Parse("#1F3555")), 1);

        for (var x = 0d; x <= DocumentWidth; x += 80)
        {
            var screenX = stageRect.X + (x * CanvasZoom);
            var pen = Math.Abs(x % 320) < 0.001 ? majorPen : minorPen;
            context.DrawLine(pen, new Point(screenX, stageRect.Y), new Point(screenX, stageRect.Bottom));
        }

        for (var y = 0d; y <= DocumentHeight; y += 80)
        {
            var screenY = stageRect.Y + (y * CanvasZoom);
            var pen = Math.Abs(y % 320) < 0.001 ? majorPen : minorPen;
            context.DrawLine(pen, new Point(stageRect.X, screenY), new Point(stageRect.Right, screenY));
        }
    }

    private void DrawLayer(DrawingContext context, Rect stageRect, LayerViewModel layer)
    {
        var snapshot = GetDisplaySnapshot(layer, CurrentTime);
        DrawLayerSnapshot(context, stageRect, layer.Kind, snapshot, tint: null, opacityScale: 1);
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
                DrawLayerSnapshot(context, stageRect, sample.Kind, displaySnapshot, tint, opacityScale);
            }
        }
    }

    private void DrawLayerSnapshot(DrawingContext context, Rect stageRect, LayerKind kind, LayerSnapshot snapshot, Color? tint, double opacityScale)
    {
        var rect = ToScreenRect(stageRect, new Rect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height));
        var fillBase = ColorHelpers.Parse(snapshot.Fill, "#FFFFFF");
        var strokeBase = ColorHelpers.Parse(snapshot.Stroke, "#FFFFFF");
        var gradientFrom = ColorHelpers.Parse(snapshot.GradientFrom, snapshot.Fill);
        var gradientTo = ColorHelpers.Parse(snapshot.GradientTo, snapshot.Stroke);
        if (tint is Color tintColor)
        {
            fillBase = Blend(fillBase, tintColor, 0.54);
            strokeBase = Blend(strokeBase, tintColor, 0.48);
            gradientFrom = Blend(gradientFrom, tintColor, 0.54);
            gradientTo = Blend(gradientTo, tintColor, 0.48);
        }

        var opacity = Math.Clamp(snapshot.Opacity * opacityScale, 0, 1);
        var fillColor = ApplyAlpha(fillBase, (byte)(opacity * 255));
        var glowBrush = new SolidColorBrush(ApplyAlpha(fillColor, (byte)Math.Clamp(28 * opacityScale, 0, 255)));
        var fillBrush = CreateFillBrush(snapshot, rect, fillColor, gradientFrom, gradientTo, opacity);
        var strokeBrush = new SolidColorBrush(ApplyAlpha(strokeBase, (byte)(opacity * 220)));
        var strokePen = new Pen(strokeBrush, Math.Max(1, snapshot.StrokeThickness * CanvasZoom));

        switch (kind)
        {
            case LayerKind.Rectangle:
                context.DrawRectangle(glowBrush, null, rect.Inflate(10), snapshot.CornerRadius + 10, snapshot.CornerRadius + 10);
                using (PushLayerRotation(context, rect, snapshot.Rotation))
                {
                    context.DrawRectangle(fillBrush, strokePen, rect, snapshot.CornerRadius, snapshot.CornerRadius);
                }

                return;
            case LayerKind.Ellipse:
                context.DrawRectangle(glowBrush, null, rect.Inflate(10), snapshot.CornerRadius + 10, snapshot.CornerRadius + 10);
                using (PushLayerRotation(context, rect, snapshot.Rotation))
                {
                    context.DrawEllipse(fillBrush, strokePen, rect.Center, rect.Width / 2, rect.Height / 2);
                }

                return;
            case LayerKind.Path:
                DrawPathSnapshot(context, stageRect, rect, snapshot, fillBrush, strokeBrush, tint, opacityScale);
                return;
            case LayerKind.Video:
                using (PushLayerRotation(context, rect, snapshot.Rotation))
                {
                    MediaAssetRenderService.DrawSyntheticVideoFrame(
                        context,
                        rect,
                        snapshot,
                        "VIDEO",
                        snapshot.Fill,
                        snapshot.Stroke,
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
        context.DrawRectangle(textPlate, strokePen, rect, 24, 24);
        DrawTextBlock(context, rect, snapshot.Text, fillBrush, snapshot.FontSize * CanvasZoom);
    }

    private void DrawTextBlock(DrawingContext context, Rect rect, string text, IBrush brush, double fontSize)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default),
            Math.Max(14, fontSize),
            brush);

        var point = new Point(rect.X + 22, rect.Y + Math.Max(16, (rect.Height - formattedText.Height) / 2));
        context.DrawText(formattedText, point);
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
        var borderColor = GetRoleColor(layer.Model.Compositing.Role, layer.IsLocked);
        var borderPen = new Pen(new SolidColorBrush(borderColor), 2, dashStyle: new DashStyle([6, 4], 0));
        context.DrawRectangle(null, borderPen, rect, layer.CornerRadius, layer.CornerRadius);

        if (layer.IsLocked)
        {
            return;
        }

        if (layer.Kind == LayerKind.Path && layer.Model.SourceLibraryItemId is null)
        {
            DrawPathPointHandles(context, stageRect, snapshot, borderColor);
        }

        foreach (var handle in GetHandleRects(rect))
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
            motionPoints.Add(new Point(snapshot.X + (snapshot.Width / 2d), snapshot.Y + (snapshot.Height / 2d)));
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
            var center = ToScreenPoint(stageRect, new VectorPointModel
            {
                X = snapshot.X + (snapshot.Width / 2d),
                Y = snapshot.Y + (snapshot.Height / 2d)
            });
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
        var currentPoint = ToScreenPoint(stageRect, new VectorPointModel
        {
            X = currentSnapshot.X + (currentSnapshot.Width / 2d),
            Y = currentSnapshot.Y + (currentSnapshot.Height / 2d)
        });
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
        var width = DocumentWidth * CanvasZoom;
        var height = DocumentHeight * CanvasZoom;
        var x = Math.Max(SurfacePadding, (Bounds.Width - width) / 2);
        var y = Math.Max(SurfacePadding, (Bounds.Height - height) / 2);
        return new Rect(x, y, width, height);
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
            if (bounds.Contains(viewportPoint))
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
        var points = VectorPathService.GetAbsolutePoints(snapshot);
        for (var index = 0; index < points.Count; index++)
        {
            var screenPoint = ToScreenPoint(stageRect, points[index]);
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
        var handles = GetHandleRects(rect).ToArray();

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

    private IEnumerable<Rect> GetHandleRects(Rect rect)
    {
        var half = HandleSize / 2;
        yield return new Rect(rect.TopLeft.X - half, rect.TopLeft.Y - half, HandleSize, HandleSize);
        yield return new Rect(rect.TopRight.X - half, rect.TopRight.Y - half, HandleSize, HandleSize);
        yield return new Rect(rect.BottomLeft.X - half, rect.BottomLeft.Y - half, HandleSize, HandleSize);
        yield return new Rect(rect.BottomRight.X - half, rect.BottomRight.Y - half, HandleSize, HandleSize);
    }

    private Rect ToScreenRect(Rect stageRect, Rect documentRect)
    {
        return new Rect(
            stageRect.X + (documentRect.X * CanvasZoom),
            stageRect.Y + (documentRect.Y * CanvasZoom),
            documentRect.Width * CanvasZoom,
            documentRect.Height * CanvasZoom);
    }

    private Point ToScreenPoint(Rect stageRect, VectorPointModel point)
    {
        return new Point(
            stageRect.X + (point.X * CanvasZoom),
            stageRect.Y + (point.Y * CanvasZoom));
    }

    private Point ToViewportPoint(Rect stageRect, Point point)
    {
        return new Point(
            TimelineAnimations.Core.Services.TimelineMath.Clamp((point.X - stageRect.X) / CanvasZoom, 0, DocumentWidth),
            TimelineAnimations.Core.Services.TimelineMath.Clamp((point.Y - stageRect.Y) / CanvasZoom, 0, DocumentHeight));
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

    private IDisposable PushLayerRotation(DrawingContext context, Rect rect, double angle)
    {
        if (Math.Abs(angle) < 0.01d)
        {
            return Disposable.Empty;
        }

        var radians = angle * Math.PI / 180d;
        var center = rect.Center;
        var matrix =
            Matrix.CreateTranslation(-center.X, -center.Y) *
            Matrix.CreateRotation(radians) *
            Matrix.CreateTranslation(center.X, center.Y);

        return context.PushTransform(matrix);
    }

    private void DrawPathSnapshot(
        DrawingContext context,
        Rect stageRect,
        Rect rect,
        LayerSnapshot snapshot,
        IBrush fillBrush,
        SolidColorBrush strokeBrush,
        Color? tint,
        double opacityScale)
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
            Math.Max(8, (snapshot.StrokeThickness + 10) * CanvasZoom),
            lineCap: PenLineCap.Round,
            lineJoin: PenLineJoin.Round);
        var strokePen = new Pen(
            strokeBrush,
            Math.Max(1, snapshot.StrokeThickness * CanvasZoom),
            lineCap: PenLineCap.Round,
            lineJoin: PenLineJoin.Round);
        using (PushLayerRotation(context, rect, snapshot.Rotation))
        {
            context.DrawGeometry(null, glowPen, geometry);
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

    private void DrawPathPointHandles(DrawingContext context, Rect stageRect, LayerSnapshot snapshot, Color borderColor)
    {
        foreach (var point in VectorPathService.GetAbsolutePoints(snapshot))
        {
            var screenPoint = ToScreenPoint(stageRect, point);
            context.DrawEllipse(new SolidColorBrush(Color.Parse("#09101C")), new Pen(new SolidColorBrush(borderColor), 1.4), screenPoint, 5.5, 5.5);
        }
    }

    private IBrush CreateFillBrush(LayerSnapshot snapshot, Rect rect, Color solidFill, Color gradientFrom, Color gradientTo, double opacity)
    {
        if (!snapshot.UseGradient)
        {
            return new SolidColorBrush(solidFill);
        }

        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            [
                new GradientStop(ApplyAlpha(gradientFrom, (byte)(opacity * 255)), 0),
                new GradientStop(ApplyAlpha(gradientTo, (byte)(opacity * 255)), 1)
            ]
        };
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
            VectorPathService.ClonePoints(style.PathPoints),
            layer.Model.Compositing.Clone());
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

    private void HandleDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.TryGetText()?.StartsWith("palette:", StringComparison.Ordinal) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void HandleDrop(object? sender, DragEventArgs e)
    {
        var text = e.DataTransfer.TryGetText();
        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith("palette:", StringComparison.Ordinal))
        {
            return;
        }

        if (!Enum.TryParse<LayerKind>(text["palette:".Length..], out var kind))
        {
            return;
        }

        var documentPosition = ToWorldPoint(GetStageRect(), e.GetPosition(this));
        PaletteDropRequested?.Invoke(this, new CanvasPaletteDropRequestedEventArgs(kind, documentPosition));
        e.Handled = true;
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
                            VectorPathService.ClonePoints(layer.Model.Style.PathPoints),
                            layer.Model.Compositing.Clone())
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
                DrawLayerSnapshot(context, stageRect, sample.Kind, snapshot, Color.Parse("#61E6FF"), 0.28d);
                continue;
            }

            if (sample.Role == LayerCompositeRole.Mask)
            {
                var snapshot = CompositeFrameRenderer.TransformSnapshot(sample.Snapshot, sceneState.ActiveCamera, DocumentWidth, DocumentHeight, sample.ParallaxDepth);
                DrawLayerSnapshot(context, stageRect, sample.Kind, snapshot, Color.Parse("#FFD166"), 0.24d);
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

    private enum InteractionMode
    {
        None,
        Move,
        MovePathPoint,
        ResizeTopLeft,
        ResizeTopRight,
        ResizeBottomLeft,
        ResizeBottomRight,
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
