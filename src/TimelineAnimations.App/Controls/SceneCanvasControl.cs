using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using TimelineAnimations.App.Helpers;
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
    private Point _pointerOrigin;
    private Rect _originalBounds;
    private double? _verticalGuide;
    private double? _horizontalGuide;

    public static readonly StyledProperty<IReadOnlyList<LayerViewModel>?> LayersProperty =
        AvaloniaProperty.Register<SceneCanvasControl, IReadOnlyList<LayerViewModel>?>(nameof(Layers));

    public static readonly StyledProperty<LayerViewModel?> SelectedLayerProperty =
        AvaloniaProperty.Register<SceneCanvasControl, LayerViewModel?>(nameof(SelectedLayer));

    public static readonly StyledProperty<double> CanvasZoomProperty =
        AvaloniaProperty.Register<SceneCanvasControl, double>(nameof(CanvasZoom), 0.74d);

    public static readonly StyledProperty<double> DocumentWidthProperty =
        AvaloniaProperty.Register<SceneCanvasControl, double>(nameof(DocumentWidth), 1280d);

    public static readonly StyledProperty<double> DocumentHeightProperty =
        AvaloniaProperty.Register<SceneCanvasControl, double>(nameof(DocumentHeight), 720d);

    public static readonly StyledProperty<string> BackgroundFromProperty =
        AvaloniaProperty.Register<SceneCanvasControl, string>(nameof(BackgroundFrom), "#09111F");

    public static readonly StyledProperty<string> BackgroundToProperty =
        AvaloniaProperty.Register<SceneCanvasControl, string>(nameof(BackgroundTo), "#182748");

    public static readonly StyledProperty<bool> SnapToGridProperty =
        AvaloniaProperty.Register<SceneCanvasControl, bool>(nameof(SnapToGrid), true);

    static SceneCanvasControl()
    {
        AffectsRender<SceneCanvasControl>(
            LayersProperty,
            SelectedLayerProperty,
            CanvasZoomProperty,
            DocumentWidthProperty,
            DocumentHeightProperty,
            BackgroundFromProperty,
            BackgroundToProperty,
            SnapToGridProperty);
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

    public event EventHandler<CanvasInteractionStateChangedEventArgs>? TransformInteractionStateChanged;

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LayersProperty)
        {
            DetachLayerListeners(change.GetOldValue<IReadOnlyList<LayerViewModel>?>());
            AttachLayerListeners(change.GetNewValue<IReadOnlyList<LayerViewModel>?>());
            InvalidateVisual();
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
            DrawGrid(context, stageRect);

            if (Layers is not null)
            {
                foreach (var layer in Layers.OrderBy(item => item.ZIndex))
                {
                    if (!layer.IsVisible)
                    {
                        continue;
                    }

                    DrawLayer(context, stageRect, layer);
                }
            }

            DrawActiveGuides(context, stageRect);
        }

        DrawFrameLabel(context, stageRect);

        if (SelectedLayer is not null)
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
        _pointerOrigin = pointer;
        _interactionMode = InteractionMode.None;
        _activeLayerId = null;
        ClearGuides();

        if (!stageRect.Contains(pointer))
        {
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

        if (_interactionMode == InteractionMode.None || _activeLayerId is null)
        {
            return;
        }

        var stageRect = GetStageRect();
        var delta = e.GetPosition(this) - _pointerOrigin;
        var documentDelta = delta / CanvasZoom;
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
        var wasInteracting = _interactionMode != InteractionMode.None;
        _interactionMode = InteractionMode.None;
        _activeLayerId = null;
        ClearGuides();
        if (wasInteracting)
        {
            TransformInteractionStateChanged?.Invoke(this, new CanvasInteractionStateChangedEventArgs(false));
        }

        e.Pointer.Capture(null);
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
        var rect = ToScreenRect(stageRect, new Rect(layer.X, layer.Y, layer.Width, layer.Height));
        var fillColor = ApplyAlpha(layer.FillBrush.Color, (byte)(Math.Clamp(layer.Opacity, 0, 1) * 255));
        var glowBrush = new SolidColorBrush(ApplyAlpha(fillColor, 32));
        var fillBrush = new SolidColorBrush(fillColor);
        var strokeBrush = new SolidColorBrush(ApplyAlpha(layer.StrokeBrush.Color, (byte)(Math.Clamp(layer.Opacity, 0, 1) * 220)));

        context.DrawRectangle(glowBrush, null, rect.Inflate(10), layer.CornerRadius + 10, layer.CornerRadius + 10);

        if (layer.Kind == LayerKind.Rectangle)
        {
            using (PushLayerRotation(context, rect, layer.Rotation))
            {
                context.DrawRectangle(fillBrush, new Pen(strokeBrush, 1.5), rect, layer.CornerRadius, layer.CornerRadius);
            }

            return;
        }

        if (layer.Kind == LayerKind.Ellipse)
        {
            using (PushLayerRotation(context, rect, layer.Rotation))
            {
                context.DrawEllipse(fillBrush, new Pen(strokeBrush, 1.4), rect.Center, rect.Width / 2, rect.Height / 2);
            }

            return;
        }

        var textPlate = new SolidColorBrush(ApplyAlpha(Color.Parse("#102038"), (byte)(Math.Clamp(layer.Opacity, 0, 1) * 180)));
        context.DrawRectangle(textPlate, new Pen(new SolidColorBrush(Color.Parse("#385886")), 1), rect, 24, 24);
        DrawTextBlock(context, rect, layer.TextContent, layer.FillBrush, layer.FontSize * CanvasZoom);
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
        var rect = ToScreenRect(stageRect, new Rect(layer.X, layer.Y, layer.Width, layer.Height));
        var borderColor = layer.IsLocked ? Color.Parse("#FFB685") : Color.Parse("#9BFFF0");
        var borderPen = new Pen(new SolidColorBrush(borderColor), 2, dashStyle: new DashStyle([6, 4], 0));
        context.DrawRectangle(null, borderPen, rect, layer.CornerRadius, layer.CornerRadius);

        if (layer.IsLocked)
        {
            return;
        }

        foreach (var handle in GetHandleRects(rect))
        {
            context.DrawRectangle(new SolidColorBrush(Color.Parse("#09101C")), new Pen(new SolidColorBrush(Color.Parse("#9BFFF0")), 1.2), handle, 4, 4);
        }
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
            var x = stageRect.X + (verticalGuide * CanvasZoom);
            context.DrawLine(guidePen, new Point(x, stageRect.Y), new Point(x, stageRect.Bottom));
        }

        if (_horizontalGuide is double horizontalGuide)
        {
            var y = stageRect.Y + (horizontalGuide * CanvasZoom);
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

        var documentPoint = ToDocumentPoint(stageRect, point);

        foreach (var layer in Layers.OrderByDescending(item => item.ZIndex))
        {
            if (!layer.IsVisible)
            {
                continue;
            }

            var bounds = new Rect(layer.X, layer.Y, layer.Width, layer.Height);
            if (bounds.Contains(documentPoint))
            {
                return layer;
            }
        }

        return null;
    }

    private bool TryHitResizeHandle(Rect stageRect, LayerViewModel layer, Point point, out InteractionMode mode)
    {
        var rect = ToScreenRect(stageRect, new Rect(layer.X, layer.Y, layer.Width, layer.Height));
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

    private Point ToDocumentPoint(Rect stageRect, Point point)
    {
        return new Point(
            TimelineAnimations.Core.Services.TimelineMath.Clamp((point.X - stageRect.X) / CanvasZoom, 0, DocumentWidth),
            TimelineAnimations.Core.Services.TimelineMath.Clamp((point.Y - stageRect.Y) / CanvasZoom, 0, DocumentHeight));
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

        var documentPosition = ToDocumentPoint(GetStageRect(), e.GetPosition(this));
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

    private enum InteractionMode
    {
        None,
        Move,
        ResizeTopLeft,
        ResizeTopRight,
        ResizeBottomLeft,
        ResizeBottomRight
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
