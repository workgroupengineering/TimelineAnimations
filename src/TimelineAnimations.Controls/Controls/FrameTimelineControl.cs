using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using TimelineAnimations.App.ViewModels;

namespace TimelineAnimations.App.Controls;

public sealed class FrameTimelineControl : Control
{
    private const double LabelWidth = 224;
    private const double HeaderHeight = 64;
    private const double TimeLaneHeight = 20;
    private const double MarkerLaneHeight = 20;
    private const double RangeLaneHeight = 24;
    private const double FolderRowHeight = 40;
    private const double RowHeight = 44;
    private bool _isSelecting;
    private FrameTimelineRulerInteractionKind _activeRulerInteraction = FrameTimelineRulerInteractionKind.Playhead;
    private int _selectionAnchorFrame;
    private Guid? _selectionLayerId;
    private int _lastRequestedFrame = -1;
    private int _lastRequestedRangeStartFrame = -1;
    private int _lastRequestedRangeEndFrame = -1;
    private int _lastRequestedRulerFrame = -1;
    private FrameTimelineRulerInteractionKind _lastRequestedRulerInteraction = FrameTimelineRulerInteractionKind.Playhead;

    public static readonly StyledProperty<IReadOnlyList<LayerFrameTimelineRowViewModel>?> RowsProperty =
        AvaloniaProperty.Register<FrameTimelineControl, IReadOnlyList<LayerFrameTimelineRowViewModel>?>(nameof(Rows));

    public static readonly StyledProperty<IReadOnlyList<FrameLabelViewModel>?> LabelsProperty =
        AvaloniaProperty.Register<FrameTimelineControl, IReadOnlyList<FrameLabelViewModel>?>(nameof(Labels));

    public static readonly StyledProperty<IReadOnlyList<SceneMarkerViewModel>?> MarkersProperty =
        AvaloniaProperty.Register<FrameTimelineControl, IReadOnlyList<SceneMarkerViewModel>?>(nameof(Markers));

    public static readonly StyledProperty<int> CurrentFrameProperty =
        AvaloniaProperty.Register<FrameTimelineControl, int>(nameof(CurrentFrame));

    public static readonly StyledProperty<int> TotalFramesProperty =
        AvaloniaProperty.Register<FrameTimelineControl, int>(nameof(TotalFrames), 1);

    public static readonly StyledProperty<double> PixelsPerFrameProperty =
        AvaloniaProperty.Register<FrameTimelineControl, double>(nameof(PixelsPerFrame), 18d);

    public static readonly StyledProperty<Guid?> SelectedLayerIdProperty =
        AvaloniaProperty.Register<FrameTimelineControl, Guid?>(nameof(SelectedLayerId));

    public static readonly StyledProperty<int> SelectionStartFrameProperty =
        AvaloniaProperty.Register<FrameTimelineControl, int>(nameof(SelectionStartFrame), -1);

    public static readonly StyledProperty<int> SelectionEndFrameProperty =
        AvaloniaProperty.Register<FrameTimelineControl, int>(nameof(SelectionEndFrame), -1);

    public static readonly StyledProperty<int> InFrameProperty =
        AvaloniaProperty.Register<FrameTimelineControl, int>(nameof(InFrame), 0);

    public static readonly StyledProperty<int> OutFrameProperty =
        AvaloniaProperty.Register<FrameTimelineControl, int>(nameof(OutFrame), 0);

    public static readonly StyledProperty<int> WorkAreaStartFrameProperty =
        AvaloniaProperty.Register<FrameTimelineControl, int>(nameof(WorkAreaStartFrame), 0);

    public static readonly StyledProperty<int> WorkAreaEndFrameProperty =
        AvaloniaProperty.Register<FrameTimelineControl, int>(nameof(WorkAreaEndFrame), 0);

    public static readonly StyledProperty<double> FrameRateProperty =
        AvaloniaProperty.Register<FrameTimelineControl, double>(nameof(FrameRate), 24d);

    public static readonly StyledProperty<bool> OnionSkinEnabledProperty =
        AvaloniaProperty.Register<FrameTimelineControl, bool>(nameof(OnionSkinEnabled), true);

    public static readonly StyledProperty<int> OnionSkinBeforeProperty =
        AvaloniaProperty.Register<FrameTimelineControl, int>(nameof(OnionSkinBefore), 2);

    public static readonly StyledProperty<int> OnionSkinAfterProperty =
        AvaloniaProperty.Register<FrameTimelineControl, int>(nameof(OnionSkinAfter), 2);

    public static readonly StyledProperty<bool> EditMultipleFramesEnabledProperty =
        AvaloniaProperty.Register<FrameTimelineControl, bool>(nameof(EditMultipleFramesEnabled));

    public static readonly StyledProperty<bool> ShowHeaderProperty =
        AvaloniaProperty.Register<FrameTimelineControl, bool>(nameof(ShowHeader), true);

    public static readonly StyledProperty<bool> ShowBodyProperty =
        AvaloniaProperty.Register<FrameTimelineControl, bool>(nameof(ShowBody), true);

    static FrameTimelineControl()
    {
        AffectsRender<FrameTimelineControl>(
            RowsProperty,
            LabelsProperty,
            MarkersProperty,
            CurrentFrameProperty,
            TotalFramesProperty,
            PixelsPerFrameProperty,
            SelectedLayerIdProperty,
            SelectionStartFrameProperty,
            SelectionEndFrameProperty,
            InFrameProperty,
            OutFrameProperty,
            WorkAreaStartFrameProperty,
            WorkAreaEndFrameProperty,
            FrameRateProperty,
            OnionSkinEnabledProperty,
            OnionSkinBeforeProperty,
            OnionSkinAfterProperty,
            EditMultipleFramesEnabledProperty,
            ShowHeaderProperty,
            ShowBodyProperty);
    }

    public event EventHandler<FrameTimelineFrameRequestedEventArgs>? FrameRequested;

    public event EventHandler<FrameTimelineLayerSelectionRequestedEventArgs>? LayerSelectionRequested;

    public event EventHandler<FrameTimelineRangeSelectionRequestedEventArgs>? RangeSelectionRequested;

    public event EventHandler<FrameTimelineHierarchyToggleRequestedEventArgs>? HierarchyToggleRequested;

    public event EventHandler<FrameTimelineRulerInteractionRequestedEventArgs>? RulerInteractionRequested;

    public event EventHandler<FrameTimelineInteractionStateChangedEventArgs>? InteractionStateChanged;

    public IReadOnlyList<LayerFrameTimelineRowViewModel>? Rows
    {
        get => GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    public IReadOnlyList<FrameLabelViewModel>? Labels
    {
        get => GetValue(LabelsProperty);
        set => SetValue(LabelsProperty, value);
    }

    public IReadOnlyList<SceneMarkerViewModel>? Markers
    {
        get => GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    public int CurrentFrame
    {
        get => GetValue(CurrentFrameProperty);
        set => SetValue(CurrentFrameProperty, value);
    }

    public int TotalFrames
    {
        get => GetValue(TotalFramesProperty);
        set => SetValue(TotalFramesProperty, value);
    }

    public double PixelsPerFrame
    {
        get => GetValue(PixelsPerFrameProperty);
        set => SetValue(PixelsPerFrameProperty, value);
    }

    public Guid? SelectedLayerId
    {
        get => GetValue(SelectedLayerIdProperty);
        set => SetValue(SelectedLayerIdProperty, value);
    }

    public int SelectionStartFrame
    {
        get => GetValue(SelectionStartFrameProperty);
        set => SetValue(SelectionStartFrameProperty, value);
    }

    public int SelectionEndFrame
    {
        get => GetValue(SelectionEndFrameProperty);
        set => SetValue(SelectionEndFrameProperty, value);
    }

    public int InFrame
    {
        get => GetValue(InFrameProperty);
        set => SetValue(InFrameProperty, value);
    }

    public int OutFrame
    {
        get => GetValue(OutFrameProperty);
        set => SetValue(OutFrameProperty, value);
    }

    public int WorkAreaStartFrame
    {
        get => GetValue(WorkAreaStartFrameProperty);
        set => SetValue(WorkAreaStartFrameProperty, value);
    }

    public int WorkAreaEndFrame
    {
        get => GetValue(WorkAreaEndFrameProperty);
        set => SetValue(WorkAreaEndFrameProperty, value);
    }

    public double FrameRate
    {
        get => GetValue(FrameRateProperty);
        set => SetValue(FrameRateProperty, value);
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

    public bool EditMultipleFramesEnabled
    {
        get => GetValue(EditMultipleFramesEnabledProperty);
        set => SetValue(EditMultipleFramesEnabledProperty, value);
    }

    public bool ShowHeader
    {
        get => GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    public bool ShowBody
    {
        get => GetValue(ShowBodyProperty);
        set => SetValue(ShowBodyProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == RowsProperty)
        {
            DetachRowListeners(change.GetOldValue<IReadOnlyList<LayerFrameTimelineRowViewModel>?>());
            AttachRowListeners(change.GetNewValue<IReadOnlyList<LayerFrameTimelineRowViewModel>?>());
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var rect = new Rect(Bounds.Size);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#081018")), new Pen(new SolidColorBrush(Color.Parse("#1E2C45")), 1), rect, 22, 22);
        if (ShowHeader)
        {
            DrawHeader(context, rect);
        }

        if (ShowBody)
        {
            DrawRows(context, rect);
        }

        DrawPlayhead(context);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var point = e.GetPosition(this);
        var frame = FrameFromPoint(point.X);

        if (ShowHeader && point.Y <= HeaderHeight)
        {
            _activeRulerInteraction = ResolveHeaderInteraction(point);
            _isSelecting = true;
            _selectionAnchorFrame = frame;
            _selectionLayerId = null;
            ResetInteractionDispatchState();
            InteractionStateChanged?.Invoke(this, new FrameTimelineInteractionStateChangedEventArgs(true));
            DispatchHeaderInteraction(frame);
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!TryGetRowAt(point, out var row))
        {
            return;
        }

        LayerSelectionRequested?.Invoke(this, new FrameTimelineLayerSelectionRequestedEventArgs(row.LayerId));

        if (TryHitHierarchyToggle(row, point))
        {
            HierarchyToggleRequested?.Invoke(this, new FrameTimelineHierarchyToggleRequestedEventArgs(row.LayerId));
            e.Handled = true;
            return;
        }

        if (row.IsFolder)
        {
            e.Handled = true;
            return;
        }

        if (point.X <= TimelineStartX)
        {
            e.Handled = true;
            return;
        }

        _isSelecting = true;
        _selectionAnchorFrame = frame;
        _selectionLayerId = row.LayerId;
        ResetInteractionDispatchState();
        InteractionStateChanged?.Invoke(this, new FrameTimelineInteractionStateChangedEventArgs(true));
        DispatchFrameRequest(frame);
        DispatchRangeSelection(row.LayerId, frame, frame);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isSelecting)
        {
            return;
        }

        var point = e.GetPosition(this);
        var frame = FrameFromPoint(point.X);

        if (_selectionLayerId is Guid layerId)
        {
            DispatchFrameRequest(frame);
            DispatchRangeSelection(layerId, _selectionAnchorFrame, frame);
        }
        else if (_activeRulerInteraction == FrameTimelineRulerInteractionKind.Playhead)
        {
            DispatchFrameRequest(frame);
        }
        else
        {
            DispatchRulerInteraction(_activeRulerInteraction, frame);
        }

        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var wasSelecting = _isSelecting;
        _isSelecting = false;
        _selectionLayerId = null;
        _activeRulerInteraction = FrameTimelineRulerInteractionKind.Playhead;
        ResetInteractionDispatchState();
        if (wasSelecting)
        {
            InteractionStateChanged?.Invoke(this, new FrameTimelineInteractionStateChangedEventArgs(false));
        }

        e.Pointer.Capture(null);
    }

    private void DispatchHeaderInteraction(int frame)
    {
        if (_activeRulerInteraction == FrameTimelineRulerInteractionKind.Playhead)
        {
            DispatchFrameRequest(frame);
            return;
        }

        DispatchRulerInteraction(_activeRulerInteraction, frame);
    }

    private void DispatchFrameRequest(int frame)
    {
        if (_lastRequestedFrame == frame)
        {
            return;
        }

        _lastRequestedFrame = frame;
        FrameRequested?.Invoke(this, new FrameTimelineFrameRequestedEventArgs(frame));
    }

    private void DispatchRangeSelection(Guid layerId, int startFrame, int endFrame)
    {
        if (_selectionLayerId == layerId &&
            _lastRequestedRangeStartFrame == startFrame &&
            _lastRequestedRangeEndFrame == endFrame)
        {
            return;
        }

        _selectionLayerId = layerId;
        _lastRequestedRangeStartFrame = startFrame;
        _lastRequestedRangeEndFrame = endFrame;
        RangeSelectionRequested?.Invoke(this, new FrameTimelineRangeSelectionRequestedEventArgs(layerId, startFrame, endFrame));
    }

    private void DispatchRulerInteraction(FrameTimelineRulerInteractionKind interactionKind, int frame)
    {
        if (_lastRequestedRulerInteraction == interactionKind && _lastRequestedRulerFrame == frame)
        {
            return;
        }

        _lastRequestedRulerInteraction = interactionKind;
        _lastRequestedRulerFrame = frame;
        RulerInteractionRequested?.Invoke(this, new FrameTimelineRulerInteractionRequestedEventArgs(interactionKind, frame));
    }

    private void ResetInteractionDispatchState()
    {
        _lastRequestedFrame = -1;
        _lastRequestedRangeStartFrame = -1;
        _lastRequestedRangeEndFrame = -1;
        _lastRequestedRulerFrame = -1;
        _lastRequestedRulerInteraction = FrameTimelineRulerInteractionKind.Playhead;
    }

    private void DrawHeader(DrawingContext context, Rect rect)
    {
        var headerRect = new Rect(0, 0, rect.Width, HeaderHeight);
        var borderPen = new Pen(new SolidColorBrush(Color.Parse("#223352")), 1);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#09111C")), borderPen, headerRect, 20, 20);

        var cornerRect = new Rect(0, 0, TimelineStartX - 8, HeaderHeight);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#0D1624")), null, cornerRect);
        context.DrawLine(borderPen, new Point(TimelineStartX - 0.5, 0), new Point(TimelineStartX - 0.5, HeaderHeight));
        DrawLabel(context, "Timeline", new Point(16, 6), 10.5, Color.Parse("#7F95BA"));
        DrawLabel(context, "Frames", new Point(16, 21), 13, Color.Parse("#E8EFFD"));
        DrawLabel(context, "Time, labels, work area, onion range", new Point(16, 40), 10, Color.Parse("#95A8CB"));

        var timeLaneRect = new Rect(TimelineStartX, 0, Math.Max(0, rect.Width - TimelineStartX), TimeLaneHeight);
        var markerLaneRect = new Rect(TimelineStartX, TimeLaneHeight, Math.Max(0, rect.Width - TimelineStartX), MarkerLaneHeight);
        var rangeLaneRect = new Rect(TimelineStartX, TimeLaneHeight + MarkerLaneHeight, Math.Max(0, rect.Width - TimelineStartX), RangeLaneHeight);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#08111B")), null, timeLaneRect);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#0B1524")), null, markerLaneRect);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#0C1828")), null, rangeLaneRect);

        DrawFrameTimeScale(context, timeLaneRect);
        DrawFrameNumberScale(context, markerLaneRect);
        DrawHeaderLabelsAndMarkers(context, markerLaneRect);
        DrawHeaderRanges(context, rangeLaneRect);
        DrawCurrentFrameBadge(context, timeLaneRect);
    }

    private void DrawRows(DrawingContext context, Rect rect)
    {
        if (Rows is null)
        {
            return;
        }

        var dividerPen = new Pen(new SolidColorBrush(Color.Parse("#1F304C")), 1);
        var gridPen = new Pen(new SolidColorBrush(Color.Parse("#122033")), 1);
        var rowOriginY = ShowHeader ? HeaderHeight : 0d;

        var rowY = rowOriginY;
        for (var index = 0; index < Rows.Count; index++)
        {
            var row = Rows[index];
            var rowHeight = GetRowHeight(row);
            var rowRect = new Rect(0, rowY, rect.Width, rowHeight);

            if (row.IsFolder)
            {
                DrawFolderRow(context, row, rowRect, dividerPen);
            }
            else
            {
                DrawLayerRow(context, row, rowRect, dividerPen, gridPen);
            }

            rowY += rowHeight;
        }

        DrawVerticalFrameGuide(context, InFrame, rect, Color.Parse("#57C9FF"), rowOriginY);
        DrawVerticalFrameGuide(context, OutFrame, rect, Color.Parse("#FFB54B"), rowOriginY);
    }

    private void DrawFolderRow(DrawingContext context, LayerFrameTimelineRowViewModel row, Rect rowRect, Pen dividerPen)
    {
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#111B28")), dividerPen, rowRect);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#1B2B42")), null, new Rect(0, rowRect.Y, rowRect.Width, 5));
        DrawWorkAreaBand(context, rowRect);

        var labelX = 18 + (row.IndentLevel * 14);
        DrawLabel(context, row.IsExpanded ? "v" : ">", new Point(labelX - 12, rowRect.Y + 10), 12, Color.Parse("#E7EEFD"));
        DrawLabel(context, row.LayerName, new Point(labelX + 6, rowRect.Y + 10), 12.5, Color.Parse("#F1F6FF"));
        DrawLabel(context, row.KindLabel, new Point(labelX + 6, rowRect.Y + 24), 10, Color.Parse("#8CA1C5"));
        DrawLabel(context, row.RowSummary, new Point(labelX + 54, rowRect.Y + 24), 10, Color.Parse("#7A90B5"));
        DrawFrameStatusBadge(context, row, rowRect.Y + 10);
    }

    private void DrawLayerRow(DrawingContext context, LayerFrameTimelineRowViewModel row, Rect rowRect, Pen dividerPen, Pen gridPen)
    {
        var rowFill = row.IsSelected
            ? new SolidColorBrush(Color.Parse("#102038"))
            : new SolidColorBrush(Color.Parse("#0A111B"));
        context.DrawRectangle(rowFill, dividerPen, rowRect);
        DrawWorkAreaBand(context, rowRect);

        var guideX = 18 + (row.Depth * 14);
        if (row.Depth > 0)
        {
            context.DrawLine(
                new Pen(new SolidColorBrush(Color.Parse("#1F2E46")), 1),
                new Point(guideX - 7, rowRect.Y),
                new Point(guideX - 7, rowRect.Bottom));
        }

        context.DrawRectangle(row.FillBrush, null, new Rect(guideX + 2, rowRect.Y + 12, 10, 10), 5, 5);
        var labelX = guideX + 18;
        DrawLabel(context, row.LayerName, new Point(labelX, rowRect.Y + 8), 12, Color.Parse("#E7EEFD"));
        DrawLabel(context, row.KindLabel, new Point(labelX, rowRect.Y + 24), 10, Color.Parse("#8CA1C5"));
        DrawLabel(context, row.RowSummary, new Point(labelX + 42, rowRect.Y + 24), 10, Color.Parse("#7A90B5"));
        DrawFrameStatusBadge(context, row, rowRect.Y + 8);

        for (var frame = 0; frame < TotalFrames; frame++)
        {
            var cellRect = new Rect(TimelineStartX + (frame * PixelsPerFrame), rowRect.Y, PixelsPerFrame, rowRect.Height);
            context.DrawRectangle(null, gridPen, cellRect);
        }

        DrawSelectionRange(context, row, rowRect.Y, rowRect.Height);
        DrawMediaClip(context, row, rowRect.Y, rowRect.Height);

        foreach (var span in row.Spans)
        {
            DrawSpan(context, row, span, rowRect.Y, rowRect.Height);
        }
    }

    private void DrawFrameStatusBadge(DrawingContext context, LayerFrameTimelineRowViewModel row, double y)
    {
        if (row.IsLocked)
        {
            DrawLabel(context, "Locked", new Point(LabelWidth - 72, y), 10, Color.Parse("#FFB685"));
        }
        else if (row.IsMuted)
        {
            DrawLabel(context, "Muted", new Point(LabelWidth - 76, y), 10, Color.Parse("#FF8C8C"));
        }
        else if (row.IsSolo)
        {
            DrawLabel(context, "Solo", new Point(LabelWidth - 62, y), 10, Color.Parse("#FFE08A"));
        }
        else if (row.HasExplicitFrames)
        {
            DrawLabel(context, "Cel", new Point(LabelWidth - 52, y), 10, Color.Parse("#9BFFF0"));
        }
    }

    private void DrawWorkAreaBand(DrawingContext context, Rect rowRect)
    {
        if (WorkAreaEndFrame < WorkAreaStartFrame)
        {
            return;
        }

        var startX = TimelineStartX + (WorkAreaStartFrame * PixelsPerFrame);
        var width = Math.Max(PixelsPerFrame, ((WorkAreaEndFrame - WorkAreaStartFrame) + 1) * PixelsPerFrame);
        var highlightRect = new Rect(startX, rowRect.Y, width, rowRect.Height);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#18273D")), null, highlightRect);
    }

    private void DrawVerticalFrameGuide(DrawingContext context, int frame, Rect rect, Color color, double top)
    {
        if (frame < 0 || frame >= TotalFrames)
        {
            return;
        }

        var x = TimelineStartX + (frame * PixelsPerFrame);
        context.DrawLine(new Pen(new SolidColorBrush(color), 1.5), new Point(x, top), new Point(x, rect.Height));
    }

    private void DrawRangeIndicator(DrawingContext context, int frame, string label, Color color, double y)
    {
        if (frame < 0 || frame >= TotalFrames)
        {
            return;
        }

        var x = TimelineStartX + (frame * PixelsPerFrame);
        var tagRect = new Rect(x + 4, y, label.Length > 2 ? 32 : 24, 14);
        var brush = new SolidColorBrush(color);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#081018")), new Pen(brush, 1), tagRect, 7, 7);
        DrawLabel(context, label, new Point(tagRect.X + 5, tagRect.Y + 2), 9, color);
    }

    private void DrawFrameTimeScale(DrawingContext context, Rect laneRect)
    {
        var (minorSeconds, majorSeconds, labelSeconds) = GetTimeTickIntervals();
        var pixelsPerSecond = Math.Max(1d, PixelsPerFrame * Math.Max(1d, FrameRate));
        var minorPen = new Pen(new SolidColorBrush(Color.Parse("#152338")), 1);
        var majorPen = new Pen(new SolidColorBrush(Color.Parse("#2A4468")), 1);

        for (var seconds = 0d; seconds <= TotalFrames / Math.Max(1d, FrameRate) + 0.0001d; seconds += minorSeconds)
        {
            var x = TimelineStartX + (seconds * pixelsPerSecond);
            if (x < TimelineStartX || x > Bounds.Width)
            {
                continue;
            }

            var isMajor = Math.Abs((seconds / majorSeconds) - Math.Round(seconds / majorSeconds)) < 0.001d;
            context.DrawLine(
                isMajor ? majorPen : minorPen,
                new Point(x, laneRect.Bottom - (isMajor ? 14 : 8)),
                new Point(x, laneRect.Bottom));

            var isLabel = Math.Abs((seconds / labelSeconds) - Math.Round(seconds / labelSeconds)) < 0.001d;
            if (isLabel)
            {
                DrawLabel(context, $"{seconds:0.0}s", new Point(x + 4, laneRect.Y + 3), 9.5, Color.Parse("#89A4D2"));
            }
        }
    }

    private void DrawFrameNumberScale(DrawingContext context, Rect laneRect)
    {
        var (minorStep, majorStep, labelStep) = GetFrameTickIntervals();
        var minorPen = new Pen(new SolidColorBrush(Color.Parse("#122033")), 1);
        var majorPen = new Pen(new SolidColorBrush(Color.Parse("#28415F")), 1);

        for (var frame = 0; frame < TotalFrames; frame += minorStep)
        {
            var x = TimelineStartX + (frame * PixelsPerFrame);
            var isMajor = frame == 0 || frame % majorStep == 0;
            context.DrawLine(
                isMajor ? majorPen : minorPen,
                new Point(x, laneRect.Y),
                new Point(x, laneRect.Bottom - 1));

            if (frame == 0 || frame % labelStep == 0)
            {
                DrawLabel(context, $"{frame + 1}", new Point(x + 4, laneRect.Y + 2), 10.5, Color.Parse("#C1D1ED"));
            }
        }
    }

    private void DrawHeaderLabelsAndMarkers(DrawingContext context, Rect laneRect)
    {
        if (Labels is not null)
        {
            foreach (var label in Labels)
            {
                var x = TimelineStartX + (label.Frame * PixelsPerFrame);
                var tagRect = new Rect(x + 4, laneRect.Y + 2, Math.Max(42, (label.Name.Length * 6.3) + 16), 16);
                context.DrawRectangle(new SolidColorBrush(Color.Parse("#17304D")), null, tagRect, 8, 8);
                DrawLabel(context, label.Name, new Point(tagRect.X + 6, tagRect.Y + 2), 9.5, Color.Parse("#F1F6FF"));
            }
        }

        if (Markers is null)
        {
            return;
        }

        foreach (var marker in Markers)
        {
            var x = TimelineStartX + (marker.Frame * PixelsPerFrame);
            var brush = marker.ColorBrush as ISolidColorBrush ?? new SolidColorBrush(Color.Parse("#57C9FF"));
            context.DrawLine(new Pen(brush, 1.5), new Point(x, laneRect.Y), new Point(x, laneRect.Bottom));

            var badgeRect = new Rect(x + 6, laneRect.Y + 2, Math.Max(44, (marker.Name.Length * 6.2) + 18), 16);
            context.DrawRectangle(new SolidColorBrush(Color.Parse("#0E1E31")), new Pen(brush, 1), badgeRect, 8, 8);
            DrawLabel(context, marker.Name, new Point(badgeRect.X + 6, badgeRect.Y + 2), 9, brush.Color);
        }
    }

    private void DrawHeaderRanges(DrawingContext context, Rect laneRect)
    {
        var workStartX = TimelineStartX + (WorkAreaStartFrame * PixelsPerFrame);
        var workEndX = TimelineStartX + ((WorkAreaEndFrame + 1) * PixelsPerFrame);
        var workRect = new Rect(workStartX, laneRect.Y + 4, Math.Max(PixelsPerFrame, workEndX - workStartX), laneRect.Height - 8);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(58, 87, 201, 255)), new Pen(new SolidColorBrush(Color.Parse("#57C9FF")), 1.2), workRect, 7, 7);

        if (EditMultipleFramesEnabled && SelectionStartFrame >= 0 && SelectionEndFrame >= 0)
        {
            var multiStart = Math.Min(SelectionStartFrame, SelectionEndFrame);
            var multiEnd = Math.Max(SelectionStartFrame, SelectionEndFrame);
            var multiRect = new Rect(
                TimelineStartX + (multiStart * PixelsPerFrame),
                laneRect.Y + 7,
                Math.Max(PixelsPerFrame, ((multiEnd - multiStart) + 1) * PixelsPerFrame),
                laneRect.Height - 14);
            context.DrawRectangle(new SolidColorBrush(Color.FromArgb(34, 255, 166, 0)), new Pen(new SolidColorBrush(Color.Parse("#FFB54B")), 1), multiRect, 6, 6);
        }

        if (OnionSkinEnabled)
        {
            var beforeFrame = Math.Max(0, CurrentFrame - OnionSkinBefore);
            var afterFrame = Math.Min(Math.Max(0, TotalFrames - 1), CurrentFrame + OnionSkinAfter);
            var onionRect = new Rect(
                TimelineStartX + (beforeFrame * PixelsPerFrame),
                laneRect.Y + 10,
                Math.Max(PixelsPerFrame, ((afterFrame - beforeFrame) + 1) * PixelsPerFrame),
                laneRect.Height - 18);
            context.DrawRectangle(new SolidColorBrush(Color.FromArgb(28, 155, 255, 240)), new Pen(new SolidColorBrush(Color.Parse("#9BFFF0")), 1), onionRect, 6, 6);
        }

        DrawHeaderHandle(context, GetHeaderHandleRect(FrameTimelineRulerInteractionKind.InPoint, InFrame), "In", Color.Parse("#57C9FF"));
        DrawHeaderHandle(context, GetHeaderHandleRect(FrameTimelineRulerInteractionKind.OutPoint, OutFrame), "Out", Color.Parse("#FFB54B"));
        DrawHeaderHandle(context, GetHeaderHandleRect(FrameTimelineRulerInteractionKind.WorkAreaStart, WorkAreaStartFrame), "W", Color.Parse("#9BFFF0"));
        DrawHeaderHandle(context, GetHeaderHandleRect(FrameTimelineRulerInteractionKind.WorkAreaEnd, WorkAreaEndFrame), "W", Color.Parse("#9BFFF0"));

        if (OnionSkinEnabled)
        {
            DrawHeaderHandle(context, GetHeaderHandleRect(FrameTimelineRulerInteractionKind.OnionBefore, Math.Max(0, CurrentFrame - OnionSkinBefore)), "<", Color.Parse("#FF8A4C"));
            DrawHeaderHandle(context, GetHeaderHandleRect(FrameTimelineRulerInteractionKind.OnionAfter, Math.Min(Math.Max(0, TotalFrames - 1), CurrentFrame + OnionSkinAfter)), ">", Color.Parse("#61E6FF"));
        }
    }

    private void DrawHeaderHandle(DrawingContext context, Rect rect, string label, Color color)
    {
        var brush = new SolidColorBrush(color);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#0B1422")), new Pen(brush, 1), rect, 6, 6);
        DrawLabel(context, label, new Point(rect.X + 4, rect.Y + 2), 8.5, color);
    }

    private void DrawCurrentFrameBadge(DrawingContext context, Rect laneRect)
    {
        var x = TimelineStartX + (CurrentFrame * PixelsPerFrame);
        var badgeText = $"F{CurrentFrame + 1}/{TotalFrames}";
        var badgeRect = new Rect(x + 6, laneRect.Y + 2, Math.Max(58, badgeText.Length * 6.4), 16);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#13243A")), new Pen(new SolidColorBrush(Color.Parse("#FFB685")), 1), badgeRect, 8, 8);
        DrawLabel(context, badgeText, new Point(badgeRect.X + 6, badgeRect.Y + 2), 9.5, Color.Parse("#FFE2C9"));
    }

    private void DrawSelectionRange(DrawingContext context, LayerFrameTimelineRowViewModel row, double rowY, double rowHeight)
    {
        if (!row.IsSelected || SelectionStartFrame < 0 || SelectionEndFrame < 0)
        {
            return;
        }

        var start = Math.Min(SelectionStartFrame, SelectionEndFrame);
        var end = Math.Max(SelectionStartFrame, SelectionEndFrame);
        var x = TimelineStartX + (start * PixelsPerFrame);
        var width = ((end - start) + 1) * PixelsPerFrame;
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(36, 155, 255, 240)), new Pen(new SolidColorBrush(Color.Parse("#9BFFF0")), 1.2), new Rect(x, rowY + 2, width, rowHeight - 4), 8, 8);
    }

    private void DrawSpan(DrawingContext context, LayerFrameTimelineRowViewModel row, FrameSpanViewModel span, double rowY, double rowHeight)
    {
        var x = TimelineStartX + (span.StartFrame * PixelsPerFrame) + 1;
        var width = Math.Max(1, (span.Length * PixelsPerFrame) - 2);
        var spanRect = new Rect(x, rowY + 6, width, rowHeight - 12);

        var fill = span.IsBlank
            ? new SolidColorBrush(Color.Parse("#1A2232"))
            : new SolidColorBrush(Color.FromArgb((byte)(row.IsLocked ? 86 : 160), row.FillBrush.Color.R, row.FillBrush.Color.G, row.FillBrush.Color.B));
        var border = span.IsBlank
            ? new Pen(new SolidColorBrush(Color.Parse("#31405A")), 1)
            : new Pen(new SolidColorBrush(row.IsLocked ? Color.Parse("#657489") : row.FillBrush.Color), span.IsSelected ? 1.8 : 1);
        context.DrawRectangle(fill, border, spanRect, 8, 8);

        if (!span.IsBlank)
        {
            var keyX = TimelineStartX + (span.StartFrame * PixelsPerFrame) + (PixelsPerFrame / 2);
            var geometry = new StreamGeometry();
            using (var builder = geometry.Open())
            {
                builder.BeginFigure(new Point(keyX, rowY + 7), true);
                builder.LineTo(new Point(keyX + 5, rowY + 12));
                builder.LineTo(new Point(keyX, rowY + 17));
                builder.LineTo(new Point(keyX - 5, rowY + 12));
                builder.EndFigure(true);
            }

            context.DrawGeometry(new SolidColorBrush(Color.Parse("#F4FBFF")), null, geometry);
        }
    }

    private void DrawMediaClip(DrawingContext context, LayerFrameTimelineRowViewModel row, double rowY, double rowHeight)
    {
        if (!row.HasMediaClip)
        {
            return;
        }

        var x = TimelineStartX + (row.MediaStartFrame * PixelsPerFrame) + 1;
        var width = Math.Max(PixelsPerFrame, ((row.MediaEndFrame - row.MediaStartFrame) + 1) * PixelsPerFrame - 2);
        var clipRect = new Rect(x, rowY + 8, width, rowHeight - 16);
        var baseColor = row.FillBrush.Color;
        var accent = row.IsAudioClip ? Color.Parse("#61E6FF") : Color.Parse("#FFD166");
        var fill = row.IsAudioClip
            ? new SolidColorBrush(Color.FromArgb(58, accent.R, accent.G, accent.B))
            : new SolidColorBrush(Color.FromArgb(42, baseColor.R, baseColor.G, baseColor.B));
        var border = new Pen(new SolidColorBrush(row.IsLocked ? Color.Parse("#657489") : accent), 1.2);
        context.DrawRectangle(fill, border, clipRect, 8, 8);

        if (row.IsAudioClip && row.MediaWaveformSamples.Count > 0)
        {
            var sampleWidth = clipRect.Width / row.MediaWaveformSamples.Count;
            for (var sampleIndex = 0; sampleIndex < row.MediaWaveformSamples.Count; sampleIndex++)
            {
                var amplitude = Math.Clamp(row.MediaWaveformSamples[sampleIndex], 0d, 1d);
                var barHeight = Math.Max(2d, amplitude * (clipRect.Height - 6));
                var barX = clipRect.X + (sampleIndex * sampleWidth);
                var barRect = new Rect(barX, clipRect.Center.Y - (barHeight / 2d), Math.Max(1d, sampleWidth - 1d), barHeight);
                context.DrawRectangle(new SolidColorBrush(Color.FromArgb(196, accent.R, accent.G, accent.B)), null, barRect, 1, 1);
            }
        }
        else if (row.IsVideoClip)
        {
            var divisions = Math.Clamp(row.MediaFrameCount, 1, 12);
            var segmentWidth = clipRect.Width / divisions;
            for (var segment = 0; segment < divisions; segment++)
            {
                var segmentRect = new Rect(clipRect.X + (segment * segmentWidth), clipRect.Y, Math.Max(1d, segmentWidth - 1d), clipRect.Height);
                var segmentColor = segment % 2 == 0
                    ? Color.FromArgb(52, 255, 209, 102)
                    : Color.FromArgb(44, 97, 230, 255);
                context.DrawRectangle(new SolidColorBrush(segmentColor), null, segmentRect, 4, 4);
            }
        }

        if (!string.IsNullOrWhiteSpace(row.MediaLabel))
        {
            DrawLabel(context, row.MediaLabel, new Point(clipRect.X + 8, clipRect.Y + 2), 10, row.IsLocked ? Color.Parse("#99A8BF") : Color.Parse("#F4FBFF"));
        }
    }

    private void DrawPlayhead(DrawingContext context)
    {
        var x = TimelineStartX + (CurrentFrame * PixelsPerFrame);
        var rowOriginY = ShowHeader ? HeaderHeight : 0d;
        var rowsHeight = Rows?.Sum(GetRowHeight) ?? 0d;
        var bottom = (ShowBody ? rowOriginY + rowsHeight : 0d) switch
        {
            <= 0d when ShowHeader => HeaderHeight,
            <= 0d => Bounds.Height,
            var value => value
        };
        var pen = new Pen(new SolidColorBrush(Color.Parse("#FFB685")), 1.6);
        context.DrawLine(pen, new Point(x, 0), new Point(x, bottom));
        if (ShowHeader)
        {
            context.DrawRectangle(new SolidColorBrush(Color.Parse("#FFB685")), null, new Rect(x - 5, 8, 10, 10), 5, 5);
        }
    }

    private bool TryGetRowAt(Point point, out LayerFrameTimelineRowViewModel row)
    {
        row = null!;

        var rowOriginY = ShowHeader ? HeaderHeight : 0d;
        if (!ShowBody || Rows is null || point.Y < rowOriginY)
        {
            return false;
        }

        var rowY = rowOriginY;
        foreach (var item in Rows)
        {
            var rowHeight = GetRowHeight(item);
            if (point.Y >= rowY && point.Y <= rowY + rowHeight)
            {
                row = item;
                return true;
            }

            rowY += rowHeight;
        }

        return false;
    }

    private bool TryHitHierarchyToggle(LayerFrameTimelineRowViewModel row, Point point)
    {
        if (!row.HasExpander)
        {
            return false;
        }

        var rowY = GetRowTop(row);
        var toggleRect = new Rect(8 + (row.IndentLevel * 14), rowY + 8, 16, 16);
        return toggleRect.Contains(point);
    }

    private double GetRowTop(LayerFrameTimelineRowViewModel targetRow)
    {
        var rowOriginY = ShowHeader ? HeaderHeight : 0d;
        if (Rows is null)
        {
            return rowOriginY;
        }

        var rowY = rowOriginY;
        foreach (var row in Rows)
        {
            if (ReferenceEquals(row, targetRow))
            {
                return rowY;
            }

            rowY += GetRowHeight(row);
        }

        return rowY;
    }

    private int FrameFromPoint(double x)
    {
        var frame = (int)Math.Floor((x - TimelineStartX) / PixelsPerFrame);
        return Math.Clamp(frame, 0, Math.Max(0, TotalFrames - 1));
    }

    private FrameTimelineRulerInteractionKind ResolveHeaderInteraction(Point point)
    {
        foreach (var interaction in GetVisibleHeaderInteractions())
        {
            if (GetHeaderHandleRect(interaction.Kind, interaction.Frame).Contains(point))
            {
                return interaction.Kind;
            }
        }

        return FrameTimelineRulerInteractionKind.Playhead;
    }

    private IEnumerable<(FrameTimelineRulerInteractionKind Kind, int Frame)> GetVisibleHeaderInteractions()
    {
        yield return (FrameTimelineRulerInteractionKind.InPoint, InFrame);
        yield return (FrameTimelineRulerInteractionKind.OutPoint, OutFrame);
        yield return (FrameTimelineRulerInteractionKind.WorkAreaStart, WorkAreaStartFrame);
        yield return (FrameTimelineRulerInteractionKind.WorkAreaEnd, WorkAreaEndFrame);

        if (!OnionSkinEnabled)
        {
            yield break;
        }

        yield return (FrameTimelineRulerInteractionKind.OnionBefore, Math.Max(0, CurrentFrame - OnionSkinBefore));
        yield return (FrameTimelineRulerInteractionKind.OnionAfter, Math.Min(Math.Max(0, TotalFrames - 1), CurrentFrame + OnionSkinAfter));
    }

    private Rect GetHeaderHandleRect(FrameTimelineRulerInteractionKind interactionKind, int frame)
    {
        var clampedFrame = Math.Clamp(frame, 0, Math.Max(0, TotalFrames - 1));
        var x = TimelineStartX + (clampedFrame * PixelsPerFrame);
        var y = interactionKind switch
        {
            FrameTimelineRulerInteractionKind.InPoint or FrameTimelineRulerInteractionKind.OutPoint => TimeLaneHeight + MarkerLaneHeight + 2,
            _ => TimeLaneHeight + MarkerLaneHeight + 10
        };

        return new Rect(x - 8, y, 18, 16);
    }

    private (int MinorStep, int MajorStep, int LabelStep) GetFrameTickIntervals()
    {
        if (PixelsPerFrame >= 18d)
        {
            return (1, 5, 5);
        }

        if (PixelsPerFrame >= 10d)
        {
            return (1, 5, 10);
        }

        if (PixelsPerFrame >= 6d)
        {
            return (2, 10, 10);
        }

        return (5, 10, 20);
    }

    private (double MinorSeconds, double MajorSeconds, double LabelSeconds) GetTimeTickIntervals()
    {
        var pixelsPerSecond = PixelsPerFrame * Math.Max(1d, FrameRate);
        if (pixelsPerSecond >= 260d)
        {
            return (1d / Math.Max(1d, FrameRate), 0.5d, 1d);
        }

        if (pixelsPerSecond >= 140d)
        {
            return (0.25d, 0.5d, 1d);
        }

        if (pixelsPerSecond >= 72d)
        {
            return (0.5d, 1d, 1d);
        }

        if (pixelsPerSecond >= 36d)
        {
            return (1d, 2d, 2d);
        }

        return (2d, 5d, 5d);
    }

    private void DrawLabel(DrawingContext context, string text, Point origin, double fontSize, Color color)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default),
            fontSize,
            new SolidColorBrush(color));
        context.DrawText(formattedText, origin);
    }

    private double TimelineStartX => LabelWidth + 10;

    private static double GetRowHeight(LayerFrameTimelineRowViewModel row)
    {
        return row.RowHeight;
    }

    private void AttachRowListeners(IReadOnlyList<LayerFrameTimelineRowViewModel>? rows)
    {
        if (rows is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += HandleRowsCollectionChanged;
        }

        if (rows is null)
        {
            return;
        }

        foreach (var row in rows)
        {
            AttachRow(row);
        }
    }

    private void DetachRowListeners(IReadOnlyList<LayerFrameTimelineRowViewModel>? rows)
    {
        if (rows is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged -= HandleRowsCollectionChanged;
        }

        if (rows is null)
        {
            return;
        }

        foreach (var row in rows)
        {
            DetachRow(row);
        }
    }

    private void AttachRow(LayerFrameTimelineRowViewModel row)
    {
        row.PropertyChanged += HandleRowPropertyChanged;
        row.Spans.CollectionChanged += HandleSpansCollectionChanged;
        foreach (var span in row.Spans)
        {
            span.PropertyChanged += HandleRowPropertyChanged;
        }
    }

    private void DetachRow(LayerFrameTimelineRowViewModel row)
    {
        row.PropertyChanged -= HandleRowPropertyChanged;
        row.Spans.CollectionChanged -= HandleSpansCollectionChanged;
        foreach (var span in row.Spans)
        {
            span.PropertyChanged -= HandleRowPropertyChanged;
        }
    }

    private void HandleRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (LayerFrameTimelineRowViewModel row in e.OldItems)
            {
                DetachRow(row);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (LayerFrameTimelineRowViewModel row in e.NewItems)
            {
                AttachRow(row);
            }
        }

        InvalidateVisual();
    }

    private void HandleSpansCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (FrameSpanViewModel span in e.OldItems)
            {
                span.PropertyChanged -= HandleRowPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (FrameSpanViewModel span in e.NewItems)
            {
                span.PropertyChanged += HandleRowPropertyChanged;
            }
        }

        InvalidateVisual();
    }

    private void HandleRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }
}
