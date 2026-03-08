using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace TimelineAnimations.App.Controls;

public sealed class EasingCurveEditorControl : Control
{
    private const double Padding = 22d;
    private const double HandleRadius = 6d;
    private ActiveHandle _activeHandle;

    public static readonly StyledProperty<double> ControlPoint1XProperty =
        AvaloniaProperty.Register<EasingCurveEditorControl, double>(nameof(ControlPoint1X), 0.25d);

    public static readonly StyledProperty<double> ControlPoint1YProperty =
        AvaloniaProperty.Register<EasingCurveEditorControl, double>(nameof(ControlPoint1Y), 0.1d);

    public static readonly StyledProperty<double> ControlPoint2XProperty =
        AvaloniaProperty.Register<EasingCurveEditorControl, double>(nameof(ControlPoint2X), 0.25d);

    public static readonly StyledProperty<double> ControlPoint2YProperty =
        AvaloniaProperty.Register<EasingCurveEditorControl, double>(nameof(ControlPoint2Y), 1d);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<EasingCurveEditorControl, bool>(nameof(IsReadOnly));

    static EasingCurveEditorControl()
    {
        AffectsRender<EasingCurveEditorControl>(
            ControlPoint1XProperty,
            ControlPoint1YProperty,
            ControlPoint2XProperty,
            ControlPoint2YProperty,
            IsReadOnlyProperty);
    }

    public double ControlPoint1X
    {
        get => GetValue(ControlPoint1XProperty);
        set => SetValue(ControlPoint1XProperty, value);
    }

    public double ControlPoint1Y
    {
        get => GetValue(ControlPoint1YProperty);
        set => SetValue(ControlPoint1YProperty, value);
    }

    public double ControlPoint2X
    {
        get => GetValue(ControlPoint2XProperty);
        set => SetValue(ControlPoint2XProperty, value);
    }

    public double ControlPoint2Y
    {
        get => GetValue(ControlPoint2YProperty);
        set => SetValue(ControlPoint2YProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = new Rect(Bounds.Size);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#09101A")), new Pen(new SolidColorBrush(Color.Parse("#223354")), 1), bounds, 18, 18);

        var graphRect = GetGraphRect(bounds);
        var gridPen = new Pen(new SolidColorBrush(Color.Parse("#162438")), 1);
        var axisPen = new Pen(new SolidColorBrush(Color.Parse("#35537D")), 1.2);

        for (var index = 1; index <= 3; index++)
        {
            var normalized = index / 4d;
            var x = graphRect.X + (normalized * graphRect.Width);
            var y = graphRect.Bottom - (normalized * graphRect.Height);
            context.DrawLine(gridPen, new Point(x, graphRect.Y), new Point(x, graphRect.Bottom));
            context.DrawLine(gridPen, new Point(graphRect.X, y), new Point(graphRect.Right, y));
        }

        context.DrawRectangle(null, axisPen, graphRect, 14, 14);

        var start = ToGraphPoint(graphRect, 0, 0);
        var end = ToGraphPoint(graphRect, 1, 1);
        var handle1 = ToGraphPoint(graphRect, ControlPoint1X, ControlPoint1Y);
        var handle2 = ToGraphPoint(graphRect, ControlPoint2X, ControlPoint2Y);

        var guidePen = new Pen(new SolidColorBrush(Color.Parse("#375471")), 1, dashStyle: new DashStyle([4, 4], 0));
        context.DrawLine(guidePen, start, handle1);
        context.DrawLine(guidePen, end, handle2);

        var geometry = new StreamGeometry();
        using (var builder = geometry.Open())
        {
            const int sampleCount = 56;
            for (var sampleIndex = 0; sampleIndex <= sampleCount; sampleIndex++)
            {
                var t = sampleIndex / (double)sampleCount;
                var point = EvaluateCurve(graphRect, t);
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

        context.DrawGeometry(null, new Pen(new SolidColorBrush(Color.Parse("#244C63")), 5.4), geometry);
        context.DrawGeometry(null, new Pen(new SolidColorBrush(Color.Parse("#9BFFF0")), 2.2), geometry);

        DrawHandle(context, handle1, "#FFB685");
        DrawHandle(context, handle2, "#61E6FF");
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (IsReadOnly)
        {
            return;
        }

        var point = e.GetPosition(this);
        var graphRect = GetGraphRect(new Rect(Bounds.Size));
        var handle1 = ToGraphPoint(graphRect, ControlPoint1X, ControlPoint1Y);
        var handle2 = ToGraphPoint(graphRect, ControlPoint2X, ControlPoint2Y);

        if (Distance(handle1, point) <= 14)
        {
            _activeHandle = ActiveHandle.First;
        }
        else if (Distance(handle2, point) <= 14)
        {
            _activeHandle = ActiveHandle.Second;
        }
        else
        {
            _activeHandle = ActiveHandle.None;
        }

        if (_activeHandle != ActiveHandle.None)
        {
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_activeHandle == ActiveHandle.None || IsReadOnly)
        {
            return;
        }

        var graphRect = GetGraphRect(new Rect(Bounds.Size));
        var point = e.GetPosition(this);
        var normalizedX = Math.Clamp((point.X - graphRect.X) / Math.Max(1, graphRect.Width), 0, 1);
        var normalizedY = Math.Clamp((graphRect.Bottom - point.Y) / Math.Max(1, graphRect.Height), 0, 1);

        if (_activeHandle == ActiveHandle.First)
        {
            ControlPoint1X = normalizedX;
            ControlPoint1Y = normalizedY;
        }
        else
        {
            ControlPoint2X = normalizedX;
            ControlPoint2Y = normalizedY;
        }

        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_activeHandle != ActiveHandle.None)
        {
            _activeHandle = ActiveHandle.None;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private static void DrawHandle(DrawingContext context, Point center, string color)
    {
        context.DrawEllipse(new SolidColorBrush(Color.Parse("#09101A")), new Pen(new SolidColorBrush(Color.Parse(color)), 1.6), center, HandleRadius, HandleRadius);
    }

    private static Rect GetGraphRect(Rect bounds)
    {
        return new Rect(
            Padding,
            Padding,
            Math.Max(60, bounds.Width - (Padding * 2)),
            Math.Max(60, bounds.Height - (Padding * 2)));
    }

    private Point EvaluateCurve(Rect graphRect, double t)
    {
        var inverse = 1 - t;
        var x = (3 * inverse * inverse * t * ControlPoint1X) +
            (3 * inverse * t * t * ControlPoint2X) +
            (t * t * t);
        var y = (3 * inverse * inverse * t * ControlPoint1Y) +
            (3 * inverse * t * t * ControlPoint2Y) +
            (t * t * t);
        return ToGraphPoint(graphRect, x, y);
    }

    private static Point ToGraphPoint(Rect graphRect, double normalizedX, double normalizedY)
    {
        return new Point(
            graphRect.X + (Math.Clamp(normalizedX, 0, 1) * graphRect.Width),
            graphRect.Bottom - (Math.Clamp(normalizedY, 0, 1) * graphRect.Height));
    }

    private static double Distance(Point left, Point right)
    {
        var delta = right - left;
        return Math.Sqrt((delta.X * delta.X) + (delta.Y * delta.Y));
    }

    private enum ActiveHandle
    {
        None,
        First,
        Second
    }
}
