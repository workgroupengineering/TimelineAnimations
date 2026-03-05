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
    private const double LabelWidth = 168;
    private const double HeaderHeight = 34;
    private const double TrackHeight = 48;
    private const double MarkerHalf = 7;

    private bool _isScrubbing;
    private bool _isDraggingKeyframe;
    private Guid? _dragKeyframeId;
    private AnimatedProperty _dragProperty;

    public static readonly StyledProperty<IReadOnlyList<TimelineTrackRowViewModel>?> RowsProperty =
        AvaloniaProperty.Register<TimelineEditorControl, IReadOnlyList<TimelineTrackRowViewModel>?>(nameof(Rows));

    public static readonly StyledProperty<double> CurrentTimeProperty =
        AvaloniaProperty.Register<TimelineEditorControl, double>(nameof(CurrentTime));

    public static readonly StyledProperty<double> DurationProperty =
        AvaloniaProperty.Register<TimelineEditorControl, double>(nameof(Duration), 6d);

    public static readonly StyledProperty<double> PixelsPerSecondProperty =
        AvaloniaProperty.Register<TimelineEditorControl, double>(nameof(PixelsPerSecond), 150d);

    public static readonly StyledProperty<AnimatedProperty> SelectedPropertyProperty =
        AvaloniaProperty.Register<TimelineEditorControl, AnimatedProperty>(nameof(SelectedProperty), AnimatedProperty.X);

    public static readonly StyledProperty<Guid?> SelectedKeyframeIdProperty =
        AvaloniaProperty.Register<TimelineEditorControl, Guid?>(nameof(SelectedKeyframeId));

    static TimelineEditorControl()
    {
        AffectsRender<TimelineEditorControl>(
            RowsProperty,
            CurrentTimeProperty,
            DurationProperty,
            PixelsPerSecondProperty,
            SelectedPropertyProperty,
            SelectedKeyframeIdProperty);
    }

    public event EventHandler<TimelineScrubRequestedEventArgs>? ScrubRequested;

    public event EventHandler<TimelineTrackSelectionRequestedEventArgs>? TrackSelectionRequested;

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
        DrawHeader(context, rect);
        DrawRows(context, rect);
        DrawPlayhead(context, rect);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var point = e.GetPosition(this);
        if (!TryGetRowAt(point, out var row))
        {
            if (point.Y <= HeaderHeight)
            {
                _isScrubbing = true;
                ScrubRequested?.Invoke(this, new TimelineScrubRequestedEventArgs(TimeFromPoint(point.X)));
                e.Pointer.Capture(this);
                e.Handled = true;
            }

            return;
        }

        TrackSelectionRequested?.Invoke(this, new TimelineTrackSelectionRequestedEventArgs(row.LayerId, row.Property));

        if (TryHitKeyframe(row, point, out var keyframe))
        {
            KeyframeSelectionRequested?.Invoke(this, new TimelineKeyframeSelectionRequestedEventArgs(row.LayerId, row.Property, keyframe.Id));
            if (row.IsLocked)
            {
                e.Handled = true;
                return;
            }

            _isDraggingKeyframe = true;
            _dragKeyframeId = keyframe.Id;
            _dragProperty = row.Property;
            KeyframeInteractionStateChanged?.Invoke(this, new TimelineInteractionStateChangedEventArgs(true));
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

            KeyframeAddRequested?.Invoke(this, new TimelineKeyframeAddRequestedEventArgs(row.LayerId, row.Property, TimeFromPoint(point.X)));
            e.Handled = true;
            return;
        }

        _isScrubbing = true;
        ScrubRequested?.Invoke(this, new TimelineScrubRequestedEventArgs(TimeFromPoint(point.X)));
        e.Pointer.Capture(this);
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
                KeyframeMoveRequested?.Invoke(this, new TimelineKeyframeMoveRequestedEventArgs(rowLayerId, _dragProperty, _dragKeyframeId.Value, TimeFromPoint(point.X)));
            }

            e.Handled = true;
            return;
        }

        if (_isScrubbing)
        {
            ScrubRequested?.Invoke(this, new TimelineScrubRequestedEventArgs(TimeFromPoint(point.X)));
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var wasDraggingKeyframe = _isDraggingKeyframe;
        _isScrubbing = false;
        _isDraggingKeyframe = false;
        _dragKeyframeId = null;
        if (wasDraggingKeyframe)
        {
            KeyframeInteractionStateChanged?.Invoke(this, new TimelineInteractionStateChangedEventArgs(false));
        }

        e.Pointer.Capture(null);
    }

    private void DrawHeader(DrawingContext context, Rect rect)
    {
        var headerRect = new Rect(rect.X, rect.Y, rect.Width, HeaderHeight);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#0C1524")), new Pen(new SolidColorBrush(Color.Parse("#223354")), 1), headerRect, 20, 20);

        DrawLabel(context, "Property", new Point(18, 9), 13, Color.Parse("#B9C7E8"));

        var majorTickPen = new Pen(new SolidColorBrush(Color.Parse("#243757")), 1);
        for (var second = 0d; second <= Duration + 0.0001d; second += 0.5d)
        {
            var x = TimelineStartX + (second * PixelsPerSecond);
            if (x < TimelineStartX || x > rect.Right)
            {
                continue;
            }

            var top = Math.Abs(second % 1) < 0.001 ? headerRect.Y + 8 : headerRect.Y + 16;
            context.DrawLine(majorTickPen, new Point(x, top), new Point(x, headerRect.Bottom));

            if (Math.Abs(second % 1) < 0.001)
            {
                DrawLabel(context, $"{second:0.0}s", new Point(x + 6, 8), 12, Color.Parse("#8CA6D6"));
            }
        }
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

        for (var index = 0; index < Rows.Count; index++)
        {
            var row = Rows[index];
            var rowY = HeaderHeight + (index * TrackHeight);
            var rowRect = new Rect(0, rowY, rect.Width, TrackHeight);
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

            if (row.IsFirstForLayer)
            {
                context.DrawRectangle(new SolidColorBrush(Color.Parse("#18263C")), null, new Rect(0, rowY, rect.Width, 6));
                context.DrawRectangle(row.FillBrush, null, new Rect(18, rowY + 11, 12, 12), 6, 6);
                DrawLabel(context, row.LayerName, new Point(38, rowY + 8), 13, Color.Parse("#B7C8EA"));
                DrawLabel(context, row.TrackTitle, new Point(18, rowY + 24), 12, Color.Parse("#E8EFFD"));
            }
            else
            {
                context.DrawRectangle(row.FillBrush, null, new Rect(18, rowY + 14, 10, 10), 5, 5);
                DrawLabel(context, row.TrackTitle, new Point(36, rowY + 10), 13, Color.Parse("#E8EFFD"));
                DrawLabel(context, row.LayerName, new Point(36, rowY + 26), 11, Color.Parse("#7E95BD"));
            }

            if (row.IsLocked)
            {
                DrawLabel(context, "Locked", new Point(LabelWidth - 92, rowY + 4), 10, Color.Parse("#FFB685"));
            }

            DrawLabel(context, row.CurrentValueLabel, new Point(LabelWidth - 28, rowY + 18), 12, Color.Parse("#86A0CF"));

            for (var second = 0d; second <= Duration + 0.0001d; second += 0.5d)
            {
                var x = TimelineStartX + (second * PixelsPerSecond);
                var pen = Math.Abs(second % 1) < 0.001 ? majorGridPen : minorGridPen;
                context.DrawLine(pen, new Point(x, rowY), new Point(x, rowY + TrackHeight));
            }

            DrawInterpolationCurve(context, row, rowY);

            foreach (var keyframe in row.Keyframes)
            {
                DrawKeyframe(context, row, keyframe, rowY + (TrackHeight / 2));
            }
        }
    }

    private void DrawPlayhead(DrawingContext context, Rect rect)
    {
        var x = TimelineStartX + (CurrentTime * PixelsPerSecond);
        var top = 0d;
        var bottom = HeaderHeight + ((Rows?.Count ?? 0) * TrackHeight);
        var playheadPen = new Pen(new SolidColorBrush(Color.Parse("#9BFFF0")), 1.8);
        context.DrawLine(playheadPen, new Point(x, top), new Point(x, bottom));
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#9BFFF0")), null, new Rect(x - 5, 8, 10, 10), 5, 5);
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

    private void DrawInterpolationCurve(DrawingContext context, TimelineTrackRowViewModel row, double rowY)
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
                    GetCurveY(row, rowY, SampleRowValue(row, time)));

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

        DrawSelectedSegmentCallout(context, row, rowY);
    }

    private void DrawSelectedSegmentCallout(DrawingContext context, TimelineTrackRowViewModel row, double rowY)
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
                    GetCurveY(row, rowY, SampleRowValue(row, time)));

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
        var easedProgress = TimelineEasingService.Apply(next.Easing, progress);
        return previous.Value + ((next.Value - previous.Value) * easedProgress);
    }

    private double GetCurveY(TimelineTrackRowViewModel row, double rowY, double value)
    {
        var top = rowY + 8;
        var bottom = rowY + TrackHeight - 8;

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
            return rowY + (TrackHeight / 2);
        }

        var normalized = (value - minimum) / (maximum - minimum);
        return bottom - (normalized * (bottom - top));
    }

    private bool TryGetRowAt(Point point, out TimelineTrackRowViewModel row)
    {
        row = null!;

        if (Rows is null || point.Y < HeaderHeight)
        {
            return false;
        }

        var index = (int)((point.Y - HeaderHeight) / TrackHeight);
        if (index < 0 || index >= Rows.Count)
        {
            return false;
        }

        row = Rows[index];
        return true;
    }

    private bool TryHitKeyframe(TimelineTrackRowViewModel row, Point point, out KeyframeViewModel keyframe)
    {
        keyframe = null!;
        if (Rows is null)
        {
            return false;
        }

        var trackIndex = 0;
        if (Rows is not null)
        {
            for (var index = 0; index < Rows.Count; index++)
            {
                if (ReferenceEquals(Rows[index], row))
                {
                    trackIndex = index;
                    break;
                }
            }
        }

        var centerY = HeaderHeight + (trackIndex * TrackHeight) + (TrackHeight / 2);

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
