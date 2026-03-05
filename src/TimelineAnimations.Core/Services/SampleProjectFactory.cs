using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class SampleProjectFactory
{
    public static TimelineDocument Create()
    {
        return new TimelineDocument
        {
            Name = "Aurora Product Reveal",
            Duration = 6,
            CanvasWidth = 1280,
            CanvasHeight = 720,
            BackgroundFrom = "#09111F",
            BackgroundTo = "#182748",
            Layers =
            [
                CreateHeroCard(),
                CreateOrb(),
                CreateCaption()
            ]
        };
    }

    private static TimelineLayer CreateHeroCard()
    {
        return new TimelineLayer
        {
            Name = "Hero Card",
            Kind = LayerKind.Rectangle,
            ZIndex = 0,
            Defaults = new LayerDefaults
            {
                X = 140,
                Y = 118,
                Width = 320,
                Height = 188,
                Rotation = -8,
                Opacity = 0.92
            },
            Style = new LayerStyle
            {
                Fill = "#24E5C1",
                Stroke = "#E5FFFB",
                CornerRadius = 36
            },
            Tracks =
            [
                Track(AnimatedProperty.X, (0, 140), (1.8, 520), (4.8, 760)),
                Track(AnimatedProperty.Y, (0, 118), (1.8, 146), (4.8, 194)),
                Track(AnimatedProperty.Width, (0, 320), (1.8, 364), (4.8, 340)),
                Track(AnimatedProperty.Height, (0, 188), (1.8, 212), (4.8, 196)),
                Track(AnimatedProperty.Rotation, (0, -8), (1.8, 3), (4.8, -1)),
                Track(AnimatedProperty.Opacity, (0, 0.5), (0.6, 0.92), (4.8, 1))
            ]
        };
    }

    private static TimelineLayer CreateOrb()
    {
        return new TimelineLayer
        {
            Name = "Solar Orb",
            Kind = LayerKind.Ellipse,
            ZIndex = 1,
            Defaults = new LayerDefaults
            {
                X = 860,
                Y = 120,
                Width = 184,
                Height = 184,
                Rotation = 0,
                Opacity = 0.84
            },
            Style = new LayerStyle
            {
                Fill = "#FF8A4C",
                Stroke = "#FFE6D1",
                CornerRadius = 999
            },
            Tracks =
            [
                Track(AnimatedProperty.X, (0, 860), (2.4, 690), (5.2, 830)),
                Track(AnimatedProperty.Y, (0, 120), (2.4, 262), (5.2, 142)),
                Track(AnimatedProperty.Width, (0, 184), (2.4, 260), (5.2, 194)),
                Track(AnimatedProperty.Height, (0, 184), (2.4, 260), (5.2, 194)),
                Track(AnimatedProperty.Rotation, (0, 0), (2.4, 28), (5.2, 6)),
                Track(AnimatedProperty.Opacity, (0, 0.12), (0.9, 0.84), (5.2, 0.94))
            ]
        };
    }

    private static TimelineLayer CreateCaption()
    {
        return new TimelineLayer
        {
            Name = "Launch Caption",
            Kind = LayerKind.Text,
            ZIndex = 2,
            Defaults = new LayerDefaults
            {
                X = 160,
                Y = 456,
                Width = 540,
                Height = 108,
                Rotation = 0,
                Opacity = 1
            },
            Style = new LayerStyle
            {
                Fill = "#F7F5ED",
                Stroke = "#93A6D8",
                Text = "TIMELINE ATELIER",
                FontSize = 52,
                CornerRadius = 24
            },
            Tracks =
            [
                Track(AnimatedProperty.X, (0, 260), (1.2, 160), (4.2, 224)),
                Track(AnimatedProperty.Y, (0, 484), (1.2, 456), (4.2, 448)),
                Track(AnimatedProperty.Width, (0, 420), (1.2, 540), (4.2, 488)),
                Track(AnimatedProperty.Height, (0, 90), (1.2, 108), (4.2, 102)),
                Track(AnimatedProperty.Rotation, (0, 0), (4.2, -2)),
                Track(AnimatedProperty.Opacity, (0, 0), (0.7, 1), (5.6, 1))
            ]
        };
    }

    private static LayerTrack Track(AnimatedProperty property, params (double Time, double Value)[] points)
    {
        return new LayerTrack
        {
            Property = property,
            Keyframes =
            [
                .. points.Select(static point => new KeyframeModel
                {
                    Time = point.Time,
                    Value = point.Value
                })
            ]
        };
    }
}
