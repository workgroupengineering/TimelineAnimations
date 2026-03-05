using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.Core.Models;

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

    public static readonly StyledProperty<IReadOnlyList<PropertyTrackViewModel>?> TracksProperty =
        AvaloniaProperty.Register<TimelineEditorControl, IReadOnlyList<PropertyTrackViewModel>?>(nameof(Tracks));

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
            TracksProperty,
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

    public IReadOnlyList<PropertyTrackViewModel>? Tracks
    {
        get => GetValue(TracksProperty);
        set => SetValue(TracksProperty, value);
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

        if (change.Property == TracksProperty)
        {
            DetachTrackListeners(change.GetOldValue<IReadOnlyList<PropertyTrackViewModel>?>());
            AttachTrackListeners(change.GetNewValue<IReadOnlyList<PropertyTrackViewModel>?>());
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
        if (!TryGetTrackAt(point, out var track))
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

        TrackSelectionRequested?.Invoke(this, new TimelineTrackSelectionRequestedEventArgs(track.Property));

        if (TryHitKeyframe(track, point, out var keyframe))
        {
            KeyframeSelectionRequested?.Invoke(this, new TimelineKeyframeSelectionRequestedEventArgs(track.Property, keyframe.Id));
            _isDraggingKeyframe = true;
            _dragKeyframeId = keyframe.Id;
            _dragProperty = track.Property;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (e.ClickCount >= 2)
        {
            KeyframeAddRequested?.Invoke(this, new TimelineKeyframeAddRequestedEventArgs(track.Property, TimeFromPoint(point.X)));
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
            KeyframeMoveRequested?.Invoke(this, new TimelineKeyframeMoveRequestedEventArgs(_dragProperty, _dragKeyframeId.Value, TimeFromPoint(point.X)));
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
        _isScrubbing = false;
        _isDraggingKeyframe = false;
        _dragKeyframeId = null;
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
        if (Tracks is null)
        {
            return;
        }

        var majorGridPen = new Pen(new SolidColorBrush(Color.Parse("#1B2942")), 1);
        var minorGridPen = new Pen(new SolidColorBrush(Color.Parse("#101A2B")), 1);
        var dividerPen = new Pen(new SolidColorBrush(Color.Parse("#223354")), 1);

        for (var index = 0; index < Tracks.Count; index++)
        {
            var track = Tracks[index];
            var rowY = HeaderHeight + (index * TrackHeight);
            var rowRect = new Rect(0, rowY, rect.Width, TrackHeight);
            var fill = track.Property == SelectedProperty
                ? new SolidColorBrush(Color.Parse("#101B2E"))
                : new SolidColorBrush(Color.Parse("#0A111B"));
            context.DrawRectangle(fill, dividerPen, rowRect);

            DrawLabel(context, track.Title, new Point(18, rowY + 8), 14, Color.Parse("#E8EFFD"));
            DrawLabel(context, track.CurrentValueLabel, new Point(18, rowY + 24), 12, Color.Parse("#86A0CF"));

            for (var second = 0d; second <= Duration + 0.0001d; second += 0.5d)
            {
                var x = TimelineStartX + (second * PixelsPerSecond);
                var pen = Math.Abs(second % 1) < 0.001 ? majorGridPen : minorGridPen;
                context.DrawLine(pen, new Point(x, rowY), new Point(x, rowY + TrackHeight));
            }

            foreach (var keyframe in track.Keyframes)
            {
                DrawKeyframe(context, track, keyframe, rowY + (TrackHeight / 2));
            }
        }
    }

    private void DrawPlayhead(DrawingContext context, Rect rect)
    {
        var x = TimelineStartX + (CurrentTime * PixelsPerSecond);
        var top = 0d;
        var bottom = HeaderHeight + ((Tracks?.Count ?? 0) * TrackHeight);
        var playheadPen = new Pen(new SolidColorBrush(Color.Parse("#9BFFF0")), 1.8);
        context.DrawLine(playheadPen, new Point(x, top), new Point(x, bottom));
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#9BFFF0")), null, new Rect(x - 5, 8, 10, 10), 5, 5);
    }

    private void DrawKeyframe(DrawingContext context, PropertyTrackViewModel track, KeyframeViewModel keyframe, double centerY)
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
            : track.Property == SelectedProperty
                ? new SolidColorBrush(Color.Parse("#FFB685"))
                : new SolidColorBrush(Color.Parse("#7B8AA7"));

        context.DrawGeometry(fill, new Pen(new SolidColorBrush(Color.Parse("#07101B")), 1.2), geometry);
    }

    private bool TryGetTrackAt(Point point, out PropertyTrackViewModel track)
    {
        track = null!;

        if (Tracks is null || point.Y < HeaderHeight)
        {
            return false;
        }

        var index = (int)((point.Y - HeaderHeight) / TrackHeight);
        if (index < 0 || index >= Tracks.Count)
        {
            return false;
        }

        track = Tracks[index];
        return true;
    }

    private bool TryHitKeyframe(PropertyTrackViewModel track, Point point, out KeyframeViewModel keyframe)
    {
        keyframe = null!;
        if (Tracks is null)
        {
            return false;
        }

        var trackIndex = 0;
        if (Tracks is not null)
        {
            for (var index = 0; index < Tracks.Count; index++)
            {
                if (ReferenceEquals(Tracks[index], track))
                {
                    trackIndex = index;
                    break;
                }
            }
        }

        var centerY = HeaderHeight + (trackIndex * TrackHeight) + (TrackHeight / 2);

        foreach (var item in track.Keyframes)
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

    private void AttachTrackListeners(IReadOnlyList<PropertyTrackViewModel>? tracks)
    {
        if (tracks is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += HandleTracksCollectionChanged;
        }

        if (tracks is null)
        {
            return;
        }

        foreach (var track in tracks)
        {
            AttachTrack(track);
        }
    }

    private void DetachTrackListeners(IReadOnlyList<PropertyTrackViewModel>? tracks)
    {
        if (tracks is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged -= HandleTracksCollectionChanged;
        }

        if (tracks is null)
        {
            return;
        }

        foreach (var track in tracks)
        {
            DetachTrack(track);
        }
    }

    private void AttachTrack(PropertyTrackViewModel track)
    {
        track.PropertyChanged += HandleTrackPropertyChanged;
        track.Keyframes.CollectionChanged += HandleKeyframesCollectionChanged;
        foreach (var keyframe in track.Keyframes)
        {
            keyframe.PropertyChanged += HandleTrackPropertyChanged;
        }
    }

    private void DetachTrack(PropertyTrackViewModel track)
    {
        track.PropertyChanged -= HandleTrackPropertyChanged;
        track.Keyframes.CollectionChanged -= HandleKeyframesCollectionChanged;
        foreach (var keyframe in track.Keyframes)
        {
            keyframe.PropertyChanged -= HandleTrackPropertyChanged;
        }
    }

    private void HandleTracksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (PropertyTrackViewModel track in e.OldItems)
            {
                DetachTrack(track);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (PropertyTrackViewModel track in e.NewItems)
            {
                AttachTrack(track);
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
