using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Controls;

public sealed class TimelineEditorControl : Control
{
    private const double LabelWidth = 216;
    private const double HeaderHeight = 52;
    private const double TimeLaneHeight = 24;
    private const double LayerHeaderHeight = 42;
    private const double GroupHeaderHeight = 30;
    private const double TrackHeight = 46;
    private const double MarkerHalf = 7;
    private const double PlayheadHitWidth = 10;

    private bool _isScrubbing;
    private bool _isDraggingKeyframe;
    private Guid? _dragKeyframeId;
    private AnimatedProperty _dragProperty;
    private double _lastScrubTime = double.NaN;
    private double _lastMoveTime = double.NaN;
    private TimelineEditorInteractionKind _activeInteractionKind = TimelineEditorInteractionKind.Scrub;

    public static readonly StyledProperty<IReadOnlyList<TimelineTrackRowViewModel>?> RowsProperty =
        AvaloniaProperty.Register<TimelineEditorControl, IReadOnlyList<TimelineTrackRowViewModel>?>(nameof(Rows));

    public static readonly StyledProperty<double> CurrentTimeProperty =
        AvaloniaProperty.Register<TimelineEditorControl, double>(nameof(CurrentTime));

    public static readonly StyledProperty<double> DurationProperty =
        AvaloniaProperty.Register<TimelineEditorControl, double>(nameof(Duration), 6d);

    public static readonly StyledProperty<double> PixelsPerSecondProperty =
        AvaloniaProperty.Register<TimelineEditorControl, double>(nameof(PixelsPerSecond), 150d);

    public static readonly StyledProperty<double> FrameRateProperty =
        AvaloniaProperty.Register<TimelineEditorControl, double>(nameof(FrameRate), 24d);

    public static readonly StyledProperty<AnimatedProperty> SelectedPropertyProperty =
        AvaloniaProperty.Register<TimelineEditorControl, AnimatedProperty>(nameof(SelectedProperty), AnimatedProperty.X);

    public static readonly StyledProperty<Guid?> SelectedKeyframeIdProperty =
        AvaloniaProperty.Register<TimelineEditorControl, Guid?>(nameof(SelectedKeyframeId));

    public static readonly StyledProperty<bool> ShowHeaderProperty =
        AvaloniaProperty.Register<TimelineEditorControl, bool>(nameof(ShowHeader), true);

    public static readonly StyledProperty<bool> ShowBodyProperty =
        AvaloniaProperty.Register<TimelineEditorControl, bool>(nameof(ShowBody), true);

    static TimelineEditorControl()
    {
        AffectsRender<TimelineEditorControl>(
            RowsProperty,
            CurrentTimeProperty,
            DurationProperty,
            PixelsPerSecondProperty,
            FrameRateProperty,
            SelectedPropertyProperty,
            SelectedKeyframeIdProperty,
            ShowHeaderProperty,
            ShowBodyProperty);
    }

    public event EventHandler<TimelineScrubRequestedEventArgs>? ScrubRequested;

    public event EventHandler<TimelineLayerSelectionRequestedEventArgs>? LayerSelectionRequested;

    public event EventHandler<TimelineTrackSelectionRequestedEventArgs>? TrackSelectionRequested;

    public event EventHandler<TimelineHierarchyToggleRequestedEventArgs>? HierarchyToggleRequested;

    public event EventHandler<TimelineKeyframeSelectionRequestedEventArgs>? KeyframeSelectionRequested;

    public event EventHandler<TimelineKeyframeMoveRequestedEventArgs>? KeyframeMoveRequested;

    public event EventHandler<TimelineKeyframeAddRequestedEventArgs>? KeyframeAddRequested;

    public event EventHandler<TimelineInteractionStateChangedEventArgs>? KeyframeInteractionStateChanged;

    public IReadOnlyList<TimelineTrackRowViewModel>? Rows
    {
        get => GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    public double CurrentTime
    {
        get => GetValue(CurrentTimeProperty);
        set => SetValue(CurrentTimeProperty, value);
    }

    public double Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public double PixelsPerSecond
    {
        get => GetValue(PixelsPerSecondProperty);
        set => SetValue(PixelsPerSecondProperty, value);
    }

    public double FrameRate
    {
        get => GetValue(FrameRateProperty);
        set => SetValue(FrameRateProperty, value);
    }

    public AnimatedProperty SelectedProperty
    {
        get => GetValue(SelectedPropertyProperty);
        set => SetValue(SelectedPropertyProperty, value);
    }

    public Guid? SelectedKeyframeId
    {
        get => GetValue(SelectedKeyframeIdProperty);
        set => SetValue(SelectedKeyframeIdProperty, value);
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
            DetachTrackListeners(change.GetOldValue<IReadOnlyList<TimelineTrackRowViewModel>?>());
            AttachTrackListeners(change.GetNewValue<IReadOnlyList<TimelineTrackRowViewModel>?>());
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var rect = new Rect(Bounds.Size);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#09101A")), new Pen(new SolidColorBrush(Color.Parse("#1E2D48")), 1), rect, 24, 24);
        if (ShowHeader)
        {
            DrawHeader(context, rect);
        }

        if (ShowBody)
        {
            DrawRows(context, rect);
        }

        DrawPlayhead(context, rect);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var point = e.GetPosition(this);
        if (!TryGetRowAt(point, out var row))
        {
            if (ShowHeader && point.Y <= HeaderHeight)
            {
                StartScrubbing(e, point);
                e.Handled = true;
            }

            return;
        }

        if (TryHitPlayhead(point))
        {
            StartScrubbing(e, point);
            e.Handled = true;
            return;
        }

        if (TryHitHierarchyToggle(row, point))
        {
            HierarchyToggleRequested?.Invoke(this, new TimelineHierarchyToggleRequestedEventArgs(row.LayerId));
            e.Handled = true;
            return;
        }

        if (row.IsLayerHeader)
        {
            LayerSelectionRequested?.Invoke(this, new TimelineLayerSelectionRequestedEventArgs(row.LayerId));
            e.Handled = true;
            return;
        }

        if (!row.CanSelectTrack || row.Property is null)
        {
            e.Handled = true;
            return;
        }

        TrackSelectionRequested?.Invoke(this, new TimelineTrackSelectionRequestedEventArgs(row.LayerId, row.Property.Value));

        if (TryHitKeyframe(row, point, out var keyframe))
        {
            KeyframeSelectionRequested?.Invoke(this, new TimelineKeyframeSelectionRequestedEventArgs(row.LayerId, row.Property.Value, keyframe.Id));
            if (row.IsLocked)
            {
                e.Handled = true;
                return;
            }

            _isDraggingKeyframe = true;
            _dragKeyframeId = keyframe.Id;
            _dragProperty = row.Property.Value;
            _activeInteractionKind = TimelineEditorInteractionKind.KeyframeDrag;
            ResetDispatchState();
            KeyframeInteractionStateChanged?.Invoke(this, new TimelineInteractionStateChangedEventArgs(true, TimelineEditorInteractionKind.KeyframeDrag));
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (e.ClickCount >= 2)
        {
            if (row.IsLocked)
            {
                e.Handled = true;
                return;
            }

            KeyframeAddRequested?.Invoke(this, new TimelineKeyframeAddRequestedEventArgs(row.LayerId, row.Property.Value, TimeFromPoint(point.X)));
            e.Handled = true;
            return;
        }

        StartScrubbing(e, point);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var point = e.GetPosition(this);

        if (_isDraggingKeyframe && _dragKeyframeId is not null)
        {
            var layerId = Rows?.FirstOrDefault(item => item.Property == _dragProperty && item.Keyframes.Any(keyframe => keyframe.Id == _dragKeyframeId.Value))?.LayerId;
            if (layerId is Guid rowLayerId)
            {
                DispatchKeyframeMove(rowLayerId, _dragProperty, _dragKeyframeId.Value, TimeFromPoint(point.X));
            }

            e.Handled = true;
            return;
        }

        if (_isScrubbing)
        {
            DispatchScrub(TimeFromPoint(point.X));
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var activeInteractionKind = _activeInteractionKind;
        var wasDraggingKeyframe = _isDraggingKeyframe;
        var wasScrubbing = _isScrubbing;
        _isScrubbing = false;
        _isDraggingKeyframe = false;
        _dragKeyframeId = null;
        _activeInteractionKind = TimelineEditorInteractionKind.Scrub;
        ResetDispatchState();
        if (wasDraggingKeyframe || wasScrubbing)
        {
            KeyframeInteractionStateChanged?.Invoke(this, new TimelineInteractionStateChangedEventArgs(false, activeInteractionKind));
        }

        e.Pointer.Capture(null);
    }

    private void StartScrubbing(PointerPressedEventArgs e, Point point)
    {
        _isScrubbing = true;
        _activeInteractionKind = TimelineEditorInteractionKind.Scrub;
        ResetDispatchState();
        KeyframeInteractionStateChanged?.Invoke(this, new TimelineInteractionStateChangedEventArgs(true, TimelineEditorInteractionKind.Scrub));
        DispatchScrub(TimeFromPoint(point.X));
        e.Pointer.Capture(this);
    }

    private void DispatchScrub(double time)
    {
        if (!double.IsNaN(_lastScrubTime) && Math.Abs(_lastScrubTime - time) < 0.0001d)
        {
            return;
        }

        _lastScrubTime = time;
        ScrubRequested?.Invoke(this, new TimelineScrubRequestedEventArgs(time));
    }

    private void DispatchKeyframeMove(Guid layerId, AnimatedProperty property, Guid keyframeId, double time)
    {
        if (!double.IsNaN(_lastMoveTime) && Math.Abs(_lastMoveTime - time) < 0.0001d)
        {
            return;
        }

        _lastMoveTime = time;
        KeyframeMoveRequested?.Invoke(this, new TimelineKeyframeMoveRequestedEventArgs(layerId, property, keyframeId, time));
    }

    private void ResetDispatchState()
    {
        _lastScrubTime = double.NaN;
        _lastMoveTime = double.NaN;
    }

    private bool TryHitPlayhead(Point point)
    {
        return Math.Abs(point.X - (TimelineStartX + (CurrentTime * PixelsPerSecond))) <= PlayheadHitWidth;
    }

    private void DrawHeader(DrawingContext context, Rect rect)
    {
        var headerRect = new Rect(rect.X, rect.Y, rect.Width, HeaderHeight);
        var borderPen = new Pen(new SolidColorBrush(Color.Parse("#223354")), 1);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#0C1524")), borderPen, headerRect, 20, 20);

        var cornerRect = new Rect(0, 0, TimelineStartX - 8, HeaderHeight);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#0D1624")), null, cornerRect);
        context.DrawLine(borderPen, new Point(TimelineStartX - 0.5, 0), new Point(TimelineStartX - 0.5, HeaderHeight));
        var selectedTrack = Rows?.FirstOrDefault(static row => row.IsTrackRow && row.IsSelected);
        var summaryText = selectedTrack is null
            ? "Expand a layer to inspect grouped properties"
            : $"{selectedTrack.LayerName} • {selectedTrack.TrackTitle}";
        DrawLabel(context, "Motion editor", new Point(16, 7), 10.5, Color.Parse("#7F95BA"));
        DrawLabel(context, "Objects and curves", new Point(16, 22), 13, Color.Parse("#E8EFFD"));
        DrawLabel(context, summaryText, new Point(16, 38), 9.5, Color.Parse("#96A8C9"));

        var timeLaneRect = new Rect(TimelineStartX, 0, Math.Max(0, rect.Width - TimelineStartX), TimeLaneHeight);
        var frameLaneRect = new Rect(TimelineStartX, TimeLaneHeight, Math.Max(0, rect.Width - TimelineStartX), HeaderHeight - TimeLaneHeight);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#08111B")), null, timeLaneRect);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#0B1524")), null, frameLaneRect);

        DrawTimeScale(context, timeLaneRect);
        DrawFrameScale(context, frameLaneRect);
        DrawCurrentTimeBadge(context, timeLaneRect);
    }

    private void DrawRows(DrawingContext context, Rect rect)
    {
        if (Rows is null)
        {
            return;
        }

        var majorGridPen = new Pen(new SolidColorBrush(Color.Parse("#1B2942")), 1);
        var minorGridPen = new Pen(new SolidColorBrush(Color.Parse("#101A2B")), 1);
        var dividerPen = new Pen(new SolidColorBrush(Color.Parse("#223354")), 1);
        var rowOriginY = ShowHeader ? HeaderHeight : 0d;

        var rowY = rowOriginY;
        for (var index = 0; index < Rows.Count; index++)
        {
            var row = Rows[index];
            var rowHeight = GetRowHeight(row);
            var rowRect = new Rect(0, rowY, rect.Width, rowHeight);

            if (row.IsLayerHeader)
            {
                DrawLayerHeaderRow(context, row, rowRect, dividerPen);
            }
            else if (row.IsGroupHeader)
            {
                DrawGroupHeaderRow(context, row, rowRect, dividerPen);
            }
            else
            {
                DrawTrackRow(context, row, rowRect, dividerPen, majorGridPen, minorGridPen);
            }

            rowY += rowHeight;
        }
    }

    private void DrawLayerHeaderRow(DrawingContext context, TimelineTrackRowViewModel row, Rect rowRect, Pen dividerPen)
    {
        var fill = row.IsLayerSelected
            ? new SolidColorBrush(Color.Parse("#132238"))
            : new SolidColorBrush(Color.Parse("#101926"));
        context.DrawRectangle(fill, dividerPen, rowRect);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#1B2B42")), null, new Rect(0, rowRect.Y, rowRect.Width, 6));
        context.DrawRectangle(row.FillBrush, null, new Rect(18 + (row.IndentLevel * 14), rowRect.Y + 15, 12, 12), 6, 6);

        if (row.HasExpander)
        {
            DrawLabel(
                context,
                row.IsExpanded ? "v" : ">",
                new Point(18 + (row.IndentLevel * 14) - 12, rowRect.Y + 12),
                12,
                Color.Parse("#C8D5EE"));
        }

        var labelX = 38 + (row.IndentLevel * 14);
        DrawLabel(context, row.LayerName, new Point(labelX, rowRect.Y + 10), 13, Color.Parse("#F1F6FF"));
        DrawLabel(context, row.LayerKindLabel, new Point(labelX, rowRect.Y + 24), 10, Color.Parse("#90A4C9"));
        DrawLabel(context, row.SummaryLabel, new Point(labelX + 60, rowRect.Y + 24), 10, Color.Parse("#8095BC"));
        DrawStatusBadge(context, row, rowRect.Y + 9);
    }

    private void DrawGroupHeaderRow(DrawingContext context, TimelineTrackRowViewModel row, Rect rowRect, Pen dividerPen)
    {
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#0D1623")), dividerPen, rowRect);
        var labelX = 24 + (row.IndentLevel * 14);
        DrawLabel(context, row.GroupTitle, new Point(labelX, rowRect.Y + 8), 11, Color.Parse("#AAC1E8"));
        DrawLabel(context, row.SummaryLabel, new Point(labelX + 78, rowRect.Y + 8), 10, Color.Parse("#738AAD"));
    }

    private void DrawTrackRow(
        DrawingContext context,
        TimelineTrackRowViewModel row,
        Rect rowRect,
        Pen dividerPen,
        Pen majorGridPen,
        Pen minorGridPen)
    {
        var fill = row.IsSelected
            ? new SolidColorBrush(Color.Parse("#101B2E"))
            : row.IsLayerSelected
                ? new SolidColorBrush(Color.Parse("#0D1728"))
                : new SolidColorBrush(Color.Parse("#0A111B"));
        context.DrawRectangle(fill, dividerPen, rowRect);

        if (row.IsLocked)
        {
            context.DrawRectangle(new SolidColorBrush(Color.Parse("#14000000")), null, rowRect);
        }

        var guideX = 18 + (row.Depth * 14);
        if (row.Depth > 0)
        {
            context.DrawLine(
                new Pen(new SolidColorBrush(Color.Parse("#1F2E46")), 1),
                new Point(guideX - 7, rowRect.Y),
                new Point(guideX - 7, rowRect.Bottom));
        }

        context.DrawRectangle(row.FillBrush, null, new Rect(guideX + 16, rowRect.Y + 17, 10, 10), 5, 5);
        var labelX = 34 + (row.IndentLevel * 14);
        DrawLabel(context, row.TrackTitle, new Point(labelX, rowRect.Y + 10), 12.5, Color.Parse("#E8EFFD"));
        DrawLabel(context, row.LayerName, new Point(labelX, rowRect.Y + 26), 10, Color.Parse("#7E95BD"));
        DrawLabel(context, row.CurrentValueLabel, new Point(LabelWidth - 44, rowRect.Y + 18), 11.5, Color.Parse("#86A0CF"));
        DrawStatusBadge(context, row, rowRect.Y + 6);

        for (var second = 0d; second <= Duration + 0.0001d; second += 0.5d)
        {
            var x = TimelineStartX + (second * PixelsPerSecond);
            var pen = Math.Abs(second % 1) < 0.001 ? majorGridPen : minorGridPen;
            context.DrawLine(pen, new Point(x, rowRect.Y), new Point(x, rowRect.Bottom));
        }

        DrawInterpolationCurve(context, row, rowRect.Y, rowRect.Height);

        foreach (var keyframe in row.Keyframes)
        {
            DrawKeyframe(context, row, keyframe, rowRect.Y + (rowRect.Height / 2));
        }
    }

    private void DrawStatusBadge(DrawingContext context, TimelineTrackRowViewModel row, double y)
    {
        if (row.IsLocked)
        {
            DrawLabel(context, "Locked", new Point(LabelWidth - 98, y), 10, Color.Parse("#FFB685"));
        }
        else if (row.IsMuted)
        {
            DrawLabel(context, "Muted", new Point(LabelWidth - 98, y), 10, Color.Parse("#FF8C8C"));
        }
        else if (row.IsSolo)
        {
            DrawLabel(context, "Solo", new Point(LabelWidth - 88, y), 10, Color.Parse("#FFE08A"));
        }
    }

    private void DrawPlayhead(DrawingContext context, Rect rect)
    {
        var x = TimelineStartX + (CurrentTime * PixelsPerSecond);
        var top = 0d;
        var rowOriginY = ShowHeader ? HeaderHeight : 0d;
        var rowsHeight = Rows?.Sum(GetRowHeight) ?? 0d;
        var bottom = (ShowBody ? rowOriginY + rowsHeight : 0d) switch
        {
            <= 0d when ShowHeader => HeaderHeight,
            <= 0d => rect.Height,
            var value => value
        };
        var playheadPen = new Pen(new SolidColorBrush(Color.Parse("#9BFFF0")), 1.8);
        context.DrawLine(playheadPen, new Point(x, top), new Point(x, bottom));
        if (ShowHeader)
        {
            context.DrawRectangle(new SolidColorBrush(Color.Parse("#9BFFF0")), null, new Rect(x - 5, 8, 10, 10), 5, 5);
        }
    }

    private void DrawKeyframe(DrawingContext context, TimelineTrackRowViewModel row, KeyframeViewModel keyframe, double centerY)
    {
        var center = new Point(TimelineStartX + (keyframe.Time * PixelsPerSecond), centerY);
        var geometry = new StreamGeometry();

        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(center.X, center.Y - MarkerHalf), true);
            ctx.LineTo(new Point(center.X + MarkerHalf, center.Y));
            ctx.LineTo(new Point(center.X, center.Y + MarkerHalf));
            ctx.LineTo(new Point(center.X - MarkerHalf, center.Y));
            ctx.EndFigure(true);
        }

        var fill = keyframe.IsSelected
            ? new SolidColorBrush(Color.Parse("#9BFFF0"))
            : row.IsSelected
                ? new SolidColorBrush(Color.Parse("#FFB685"))
                : new SolidColorBrush(Color.Parse("#7B8AA7"));

        if (row.IsLocked)
        {
            fill = new SolidColorBrush(Color.Parse("#5F6980"));
        }

        context.DrawGeometry(fill, new Pen(new SolidColorBrush(Color.Parse("#07101B")), 1.2), geometry);
    }

    private void DrawInterpolationCurve(DrawingContext context, TimelineTrackRowViewModel row, double rowY, double rowHeight)
    {
        if (row.Keyframes.Count == 0)
        {
            return;
        }

        var sampleCount = Math.Max(24, (int)Math.Ceiling((Duration * PixelsPerSecond) / 18));
        var timeStep = sampleCount <= 1 ? Duration : Duration / sampleCount;
        var geometry = new StreamGeometry();

        using (var builder = geometry.Open())
        {
            for (var sampleIndex = 0; sampleIndex <= sampleCount; sampleIndex++)
            {
                var time = Math.Min(Duration, sampleIndex * timeStep);
                var point = new Point(
                    TimelineStartX + (time * PixelsPerSecond),
                    GetCurveY(row, rowY, rowHeight, SampleRowValue(row, time)));

                if (sampleIndex == 0)
                {
                    builder.BeginFigure(point, false);
                }
                else
                {
                    builder.LineTo(point);
                }
            }
        }

        var curveColor = row.IsLocked
            ? Color.Parse("#4B576F")
            : row.IsSelected
                ? Color.Parse("#9BFFF0")
                : row.FillBrush.Color;
        var glowColor = row.IsSelected ? Color.Parse("#24E5C1") : curveColor;
        context.DrawGeometry(
            null,
            new Pen(new SolidColorBrush(Color.FromArgb(64, glowColor.R, glowColor.G, glowColor.B)), row.IsSelected ? 4.6 : 3.2),
            geometry);
        context.DrawGeometry(
            null,
            new Pen(new SolidColorBrush(Color.FromArgb(row.IsLocked ? (byte)110 : (byte)210, curveColor.R, curveColor.G, curveColor.B)), row.IsSelected ? 2.1 : 1.4),
            geometry);

        DrawSelectedSegmentCallout(context, row, rowY, rowHeight);
    }

    private void DrawSelectedSegmentCallout(DrawingContext context, TimelineTrackRowViewModel row, double rowY, double rowHeight)
    {
        if (!row.IsSelected || row.IsLocked || row.Keyframes.Count < 2)
        {
            return;
        }

        var selectedIndex = -1;
        for (var index = 0; index < row.Keyframes.Count; index++)
        {
            if (row.Keyframes[index].IsSelected)
            {
                selectedIndex = index;
                break;
            }
        }

        if (selectedIndex <= 0)
        {
            return;
        }

        var previous = row.Keyframes[selectedIndex - 1];
        var selected = row.Keyframes[selectedIndex];
        var segmentGeometry = new StreamGeometry();
        using (var builder = segmentGeometry.Open())
        {
            const int segmentSamples = 18;
            for (var sampleIndex = 0; sampleIndex <= segmentSamples; sampleIndex++)
            {
                var progress = sampleIndex / (double)segmentSamples;
                var time = previous.Time + ((selected.Time - previous.Time) * progress);
                var point = new Point(
                    TimelineStartX + (time * PixelsPerSecond),
                    GetCurveY(row, rowY, rowHeight, SampleRowValue(row, time)));

                if (sampleIndex == 0)
                {
                    builder.BeginFigure(point, false);
                }
                else
                {
                    builder.LineTo(point);
                }
            }
        }

        context.DrawGeometry(null, new Pen(new SolidColorBrush(Color.Parse("#FFB685")), 2.8), segmentGeometry);

        var midpoint = previous.Time + ((selected.Time - previous.Time) / 2);
        DrawLabel(
            context,
            selected.EasingLabel,
            new Point(TimelineStartX + (midpoint * PixelsPerSecond) - 18, rowY + 4),
            10,
            Color.Parse("#FFD4B8"));
    }

    private double SampleRowValue(TimelineTrackRowViewModel row, double time)
    {
        if (row.Keyframes.Count == 1)
        {
            return row.Keyframes[0].Value;
        }

        KeyframeViewModel? previous = null;
        KeyframeViewModel? next = null;

        foreach (var keyframe in row.Keyframes)
        {
            if (keyframe.Time <= time)
            {
                previous = keyframe;
            }

            if (keyframe.Time >= time)
            {
                next = keyframe;
                break;
            }
        }

        if (previous is null)
        {
            return row.Keyframes[0].Value;
        }

        if (next is null)
        {
            return row.Keyframes[^1].Value;
        }

        if (ReferenceEquals(previous, next) || Math.Abs(next.Time - previous.Time) < 0.0001d)
        {
            return previous.Value;
        }

        var progress = (time - previous.Time) / (next.Time - previous.Time);
        var easedProgress = TimelineEasingService.Apply(next.Model, progress);
        return previous.Value + ((next.Value - previous.Value) * easedProgress);
    }

    private double GetCurveY(TimelineTrackRowViewModel row, double rowY, double value)
    {
        return GetCurveY(row, rowY, TrackHeight, value);
    }

    private double GetCurveY(TimelineTrackRowViewModel row, double rowY, double rowHeight, double value)
    {
        var top = rowY + 8;
        var bottom = rowY + rowHeight - 8;

        double minimum;
        double maximum;
        if (row.Property == AnimatedProperty.Opacity)
        {
            minimum = 0;
            maximum = 1;
        }
        else
        {
            minimum = row.Keyframes.Min(keyframe => keyframe.Value);
            maximum = row.Keyframes.Max(keyframe => keyframe.Value);
        }

        if (Math.Abs(maximum - minimum) < 0.0001d)
        {
            return rowY + (rowHeight / 2);
        }

        var normalized = (value - minimum) / (maximum - minimum);
        return bottom - (normalized * (bottom - top));
    }

    private bool TryGetRowAt(Point point, out TimelineTrackRowViewModel row)
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

    private bool TryHitKeyframe(TimelineTrackRowViewModel row, Point point, out KeyframeViewModel keyframe)
    {
        keyframe = null!;
        if (Rows is null || !row.IsTrackRow)
        {
            return false;
        }

        var rowOriginY = ShowHeader ? HeaderHeight : 0d;
        var rowY = rowOriginY;
        foreach (var item in Rows)
        {
            if (ReferenceEquals(item, row))
            {
                break;
            }

            rowY += GetRowHeight(item);
        }

        var centerY = rowY + (GetRowHeight(row) / 2);

        foreach (var item in row.Keyframes)
        {
            var center = new Point(TimelineStartX + (item.Time * PixelsPerSecond), centerY);
            var markerRect = new Rect(center.X - 10, center.Y - 10, 20, 20);
            if (markerRect.Contains(point))
            {
                keyframe = item;
                return true;
            }
        }

        return false;
    }

    private double TimeFromPoint(double x)
    {
        var time = (x - TimelineStartX) / PixelsPerSecond;
        return Math.Clamp(time, 0, Duration);
    }

    private void DrawTimeScale(DrawingContext context, Rect laneRect)
    {
        var (minorSeconds, majorSeconds, labelSeconds) = GetTimeTickIntervals();
        var minorPen = new Pen(new SolidColorBrush(Color.Parse("#152338")), 1);
        var majorPen = new Pen(new SolidColorBrush(Color.Parse("#2A4468")), 1);

        for (var seconds = 0d; seconds <= Duration + 0.0001d; seconds += minorSeconds)
        {
            var x = TimelineStartX + (seconds * PixelsPerSecond);
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

    private void DrawFrameScale(DrawingContext context, Rect laneRect)
    {
        var totalFrames = Math.Max(1, (int)Math.Ceiling(Duration * Math.Max(1d, FrameRate)));
        var pixelsPerFrame = PixelsPerSecond / Math.Max(1d, FrameRate);
        var (minorStep, majorStep, labelStep) = GetFrameTickIntervals(pixelsPerFrame);
        var minorPen = new Pen(new SolidColorBrush(Color.Parse("#122033")), 1);
        var majorPen = new Pen(new SolidColorBrush(Color.Parse("#28415F")), 1);

        for (var frame = 0; frame <= totalFrames; frame += minorStep)
        {
            var x = TimelineStartX + (frame * pixelsPerFrame);
            if (x < TimelineStartX || x > Bounds.Width)
            {
                continue;
            }

            var isMajor = frame == 0 || frame % majorStep == 0;
            context.DrawLine(
                isMajor ? majorPen : minorPen,
                new Point(x, laneRect.Y + 2),
                new Point(x, laneRect.Bottom));

            if (frame == 0 || frame % labelStep == 0)
            {
                DrawLabel(context, $"F{frame + 1}", new Point(x + 4, laneRect.Y + 3), 10, Color.Parse("#C1D1ED"));
            }
        }
    }

    private void DrawCurrentTimeBadge(DrawingContext context, Rect laneRect)
    {
        var x = TimelineStartX + (CurrentTime * PixelsPerSecond);
        var currentFrame = Math.Max(0, (int)Math.Round(CurrentTime * Math.Max(1d, FrameRate)));
        var badgeRect = new Rect(x + 6, laneRect.Y + 3, 64, 16);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#13243A")), new Pen(new SolidColorBrush(Color.Parse("#9BFFF0")), 1), badgeRect, 8, 8);
        DrawLabel(context, $"F{currentFrame + 1}", new Point(badgeRect.X + 6, badgeRect.Y + 2), 9.5, Color.Parse("#D9FFF7"));
    }

    private bool TryHitHierarchyToggle(TimelineTrackRowViewModel row, Point point)
    {
        if (!row.IsLayerHeader || !row.HasExpander)
        {
            return false;
        }

        var rowY = GetRowTop(row);
        var toggleRect = new Rect(8 + (row.IndentLevel * 14), rowY + 8, 16, 16);
        return toggleRect.Contains(point);
    }

    private double GetRowTop(TimelineTrackRowViewModel targetRow)
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

    private double TimelineStartX => LabelWidth + 12;

    private static double GetRowHeight(TimelineTrackRowViewModel row)
    {
        return row.RowHeight;
    }

    private (double MinorSeconds, double MajorSeconds, double LabelSeconds) GetTimeTickIntervals()
    {
        if (PixelsPerSecond >= 320d)
        {
            return (1d / Math.Max(1d, FrameRate), 0.5d, 1d);
        }

        if (PixelsPerSecond >= 180d)
        {
            return (0.25d, 0.5d, 1d);
        }

        if (PixelsPerSecond >= 96d)
        {
            return (0.5d, 1d, 1d);
        }

        if (PixelsPerSecond >= 48d)
        {
            return (1d, 2d, 2d);
        }

        return (2d, 5d, 5d);
    }

    private static (int MinorStep, int MajorStep, int LabelStep) GetFrameTickIntervals(double pixelsPerFrame)
    {
        if (pixelsPerFrame >= 18d)
        {
            return (1, 5, 5);
        }

        if (pixelsPerFrame >= 10d)
        {
            return (1, 5, 10);
        }

        if (pixelsPerFrame >= 6d)
        {
            return (2, 10, 10);
        }

        return (5, 10, 20);
    }

    private void AttachTrackListeners(IReadOnlyList<TimelineTrackRowViewModel>? rows)
    {
        if (rows is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += HandleTracksCollectionChanged;
        }

        if (rows is null)
        {
            return;
        }

        foreach (var row in rows)
        {
            AttachTrack(row);
        }
    }

    private void DetachTrackListeners(IReadOnlyList<TimelineTrackRowViewModel>? rows)
    {
        if (rows is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged -= HandleTracksCollectionChanged;
        }

        if (rows is null)
        {
            return;
        }

        foreach (var row in rows)
        {
            DetachTrack(row);
        }
    }

    private void AttachTrack(TimelineTrackRowViewModel row)
    {
        row.PropertyChanged += HandleTrackPropertyChanged;
        row.Keyframes.CollectionChanged += HandleKeyframesCollectionChanged;
        foreach (var keyframe in row.Keyframes)
        {
            keyframe.PropertyChanged += HandleTrackPropertyChanged;
        }
    }

    private void DetachTrack(TimelineTrackRowViewModel row)
    {
        row.PropertyChanged -= HandleTrackPropertyChanged;
        row.Keyframes.CollectionChanged -= HandleKeyframesCollectionChanged;
        foreach (var keyframe in row.Keyframes)
        {
            keyframe.PropertyChanged -= HandleTrackPropertyChanged;
        }
    }

    private void HandleTracksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (TimelineTrackRowViewModel row in e.OldItems)
            {
                DetachTrack(row);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (TimelineTrackRowViewModel row in e.NewItems)
            {
                AttachTrack(row);
            }
        }

        InvalidateVisual();
    }

    private void HandleKeyframesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (KeyframeViewModel keyframe in e.OldItems)
            {
                keyframe.PropertyChanged -= HandleTrackPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (KeyframeViewModel keyframe in e.NewItems)
            {
                keyframe.PropertyChanged += HandleTrackPropertyChanged;
            }
        }

        InvalidateVisual();
    }

    private void HandleTrackPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }
}
