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
    private const double LabelWidth = 202;
    private const double HeaderHeight = 42;
    private const double RowHeight = 44;
    private bool _isSelecting;
    private int _selectionAnchorFrame;
    private Guid? _selectionLayerId;

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
            WorkAreaEndFrameProperty);
    }

    public event EventHandler<FrameTimelineFrameRequestedEventArgs>? FrameRequested;

    public event EventHandler<FrameTimelineLayerSelectionRequestedEventArgs>? LayerSelectionRequested;

    public event EventHandler<FrameTimelineRangeSelectionRequestedEventArgs>? RangeSelectionRequested;

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
        DrawHeader(context, rect);
        DrawRows(context, rect);
        DrawPlayhead(context);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var point = e.GetPosition(this);
        var frame = FrameFromPoint(point.X);

        if (point.Y <= HeaderHeight)
        {
            FrameRequested?.Invoke(this, new FrameTimelineFrameRequestedEventArgs(frame));
            _isSelecting = true;
            _selectionAnchorFrame = frame;
            _selectionLayerId = null;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!TryGetRowAt(point, out var row))
        {
            return;
        }

        LayerSelectionRequested?.Invoke(this, new FrameTimelineLayerSelectionRequestedEventArgs(row.LayerId));

        if (point.X <= TimelineStartX)
        {
            e.Handled = true;
            return;
        }

        _isSelecting = true;
        _selectionAnchorFrame = frame;
        _selectionLayerId = row.LayerId;
        FrameRequested?.Invoke(this, new FrameTimelineFrameRequestedEventArgs(frame));
        RangeSelectionRequested?.Invoke(this, new FrameTimelineRangeSelectionRequestedEventArgs(row.LayerId, frame, frame));
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
        FrameRequested?.Invoke(this, new FrameTimelineFrameRequestedEventArgs(frame));

        if (_selectionLayerId is Guid layerId)
        {
            RangeSelectionRequested?.Invoke(this, new FrameTimelineRangeSelectionRequestedEventArgs(layerId, _selectionAnchorFrame, frame));
        }

        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isSelecting = false;
        _selectionLayerId = null;
        e.Pointer.Capture(null);
    }

    private void DrawHeader(DrawingContext context, Rect rect)
    {
        var headerRect = new Rect(0, 0, rect.Width, HeaderHeight);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#0B1422")), new Pen(new SolidColorBrush(Color.Parse("#223352")), 1), headerRect, 20, 20);
        DrawWorkAreaBand(context, headerRect);
        DrawLabel(context, "Frames", new Point(18, 12), 13, Color.Parse("#B9C7E8"));

        var minorPen = new Pen(new SolidColorBrush(Color.Parse("#162338")), 1);
        var majorPen = new Pen(new SolidColorBrush(Color.Parse("#28415F")), 1);

        for (var frame = 0; frame < TotalFrames; frame++)
        {
            var x = TimelineStartX + (frame * PixelsPerFrame);
            var major = frame == 0 || ((frame + 1) % 5) == 0;
            context.DrawLine(major ? majorPen : minorPen, new Point(x, HeaderHeight - (major ? 28 : 18)), new Point(x, HeaderHeight));

            if (major)
            {
                DrawLabel(context, $"{frame + 1}", new Point(x + 4, 10), 11, Color.Parse("#8CA6D6"));
            }
        }

        if (Labels is null)
        {
            return;
        }

        foreach (var label in Labels)
        {
            var x = TimelineStartX + (label.Frame * PixelsPerFrame);
            var tagRect = new Rect(x + 2, 6, Math.Max(40, (label.Name.Length * 6.5) + 14), 18);
            context.DrawRectangle(new SolidColorBrush(Color.Parse("#2C4568")), null, tagRect, 8, 8);
            DrawLabel(context, label.Name, new Point(tagRect.X + 6, tagRect.Y + 3), 10, Color.Parse("#F1F6FF"));
        }

        if (Markers is not null)
        {
            foreach (var marker in Markers)
            {
                var x = TimelineStartX + (marker.Frame * PixelsPerFrame);
                var brush = marker.ColorBrush as ISolidColorBrush ?? new SolidColorBrush(Color.Parse("#57C9FF"));
                var badgeRect = new Rect(x + 4, 24, Math.Max(44, (marker.Name.Length * 6.4) + 18), 14);
                context.DrawRectangle(brush, null, new Rect(x - 1, 22, 3, 18));
                context.DrawRectangle(new SolidColorBrush(Color.Parse("#122236")), new Pen(brush, 1), badgeRect, 7, 7);
                DrawLabel(context, marker.Name, new Point(badgeRect.X + 6, badgeRect.Y + 2), 9, brush.Color);
            }
        }

        DrawRangeIndicator(context, InFrame, "In", Color.Parse("#57C9FF"), 4);
        DrawRangeIndicator(context, OutFrame, "Out", Color.Parse("#FFB54B"), 20);
    }

    private void DrawRows(DrawingContext context, Rect rect)
    {
        if (Rows is null)
        {
            return;
        }

        var dividerPen = new Pen(new SolidColorBrush(Color.Parse("#1F304C")), 1);
        var gridPen = new Pen(new SolidColorBrush(Color.Parse("#122033")), 1);

        for (var index = 0; index < Rows.Count; index++)
        {
            var row = Rows[index];
            var rowY = HeaderHeight + (index * RowHeight);
            var rowRect = new Rect(0, rowY, rect.Width, RowHeight);
            var rowFill = row.IsSelected
                ? new SolidColorBrush(Color.Parse("#102038"))
                : new SolidColorBrush(Color.Parse("#0A111B"));
            context.DrawRectangle(rowFill, dividerPen, rowRect);
            DrawWorkAreaBand(context, rowRect);

            var labelX = 18 + (row.Depth * 14);
            var layerLabel = row.IsFolder
                ? $"{(row.IsExpanded ? "v" : ">")} {row.LayerName}"
                : row.LayerName;
            DrawLabel(context, layerLabel, new Point(labelX, rowY + 10), 12, Color.Parse("#E7EEFD"));
            DrawLabel(context, row.IsFolder ? "Folder" : row.DurationLabel, new Point(labelX, rowY + 26), 10, Color.Parse("#8095BC"));

            if (row.IsLocked)
            {
                DrawLabel(context, "Locked", new Point(LabelWidth - 64, rowY + 12), 10, Color.Parse("#FFB685"));
            }
            else if (row.IsMuted)
            {
                DrawLabel(context, "Muted", new Point(LabelWidth - 72, rowY + 12), 10, Color.Parse("#FF8C8C"));
            }
            else if (row.IsSolo)
            {
                DrawLabel(context, "Solo", new Point(LabelWidth - 60, rowY + 12), 10, Color.Parse("#FFE08A"));
            }
            else if (row.HasExplicitFrames)
            {
                DrawLabel(context, "Cel", new Point(LabelWidth - 52, rowY + 12), 10, Color.Parse("#9BFFF0"));
            }

            for (var frame = 0; frame < TotalFrames; frame++)
            {
                var cellRect = new Rect(TimelineStartX + (frame * PixelsPerFrame), rowY, PixelsPerFrame, RowHeight);
                context.DrawRectangle(null, gridPen, cellRect);
            }

            DrawSelectionRange(context, row, rowY);
            DrawMediaClip(context, row, rowY);

            foreach (var span in row.Spans)
            {
                DrawSpan(context, row, span, rowY);
            }
        }

        DrawVerticalFrameGuide(context, InFrame, rect, Color.Parse("#57C9FF"));
        DrawVerticalFrameGuide(context, OutFrame, rect, Color.Parse("#FFB54B"));
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

    private void DrawVerticalFrameGuide(DrawingContext context, int frame, Rect rect, Color color)
    {
        if (frame < 0 || frame >= TotalFrames)
        {
            return;
        }

        var x = TimelineStartX + (frame * PixelsPerFrame);
        context.DrawLine(new Pen(new SolidColorBrush(color), 1.5), new Point(x, HeaderHeight), new Point(x, rect.Height));
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

    private void DrawSelectionRange(DrawingContext context, LayerFrameTimelineRowViewModel row, double rowY)
    {
        if (!row.IsSelected || SelectionStartFrame < 0 || SelectionEndFrame < 0)
        {
            return;
        }

        var start = Math.Min(SelectionStartFrame, SelectionEndFrame);
        var end = Math.Max(SelectionStartFrame, SelectionEndFrame);
        var x = TimelineStartX + (start * PixelsPerFrame);
        var width = ((end - start) + 1) * PixelsPerFrame;
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(36, 155, 255, 240)), new Pen(new SolidColorBrush(Color.Parse("#9BFFF0")), 1.2), new Rect(x, rowY + 2, width, RowHeight - 4), 8, 8);
    }

    private void DrawSpan(DrawingContext context, LayerFrameTimelineRowViewModel row, FrameSpanViewModel span, double rowY)
    {
        var x = TimelineStartX + (span.StartFrame * PixelsPerFrame) + 1;
        var width = Math.Max(1, (span.Length * PixelsPerFrame) - 2);
        var spanRect = new Rect(x, rowY + 6, width, RowHeight - 12);

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

    private void DrawMediaClip(DrawingContext context, LayerFrameTimelineRowViewModel row, double rowY)
    {
        if (!row.HasMediaClip)
        {
            return;
        }

        var x = TimelineStartX + (row.MediaStartFrame * PixelsPerFrame) + 1;
        var width = Math.Max(PixelsPerFrame, ((row.MediaEndFrame - row.MediaStartFrame) + 1) * PixelsPerFrame - 2);
        var clipRect = new Rect(x, rowY + 8, width, RowHeight - 16);
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
        var bottom = HeaderHeight + ((Rows?.Count ?? 0) * RowHeight);
        var pen = new Pen(new SolidColorBrush(Color.Parse("#FFB685")), 1.6);
        context.DrawLine(pen, new Point(x, 0), new Point(x, bottom));
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#FFB685")), null, new Rect(x - 5, 8, 10, 10), 5, 5);
    }

    private bool TryGetRowAt(Point point, out LayerFrameTimelineRowViewModel row)
    {
        row = null!;

        if (Rows is null || point.Y < HeaderHeight)
        {
            return false;
        }

        var index = (int)((point.Y - HeaderHeight) / RowHeight);
        if (index < 0 || index >= Rows.Count)
        {
            return false;
        }

        row = Rows[index];
        return true;
    }

    private int FrameFromPoint(double x)
    {
        var frame = (int)Math.Floor((x - TimelineStartX) / PixelsPerFrame);
        return Math.Clamp(frame, 0, Math.Max(0, TotalFrames - 1));
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
