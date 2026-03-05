using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class CanvasSnapService
{
    public const double GridSize = 20d;
    public const double GuideThreshold = 12d;

    public static CanvasSnapResult SnapMove(
        double x,
        double y,
        double width,
        double height,
        double canvasWidth,
        double canvasHeight,
        bool snapEnabled)
    {
        return SnapBounds(
            x,
            y,
            width,
            height,
            canvasWidth,
            canvasHeight,
            snapEnabled,
            snapEnabled,
            snapEnabled,
            false);
    }

    public static CanvasSnapResult SnapResize(
        double x,
        double y,
        double width,
        double height,
        double canvasWidth,
        double canvasHeight,
        bool snapEnabled)
    {
        return SnapBounds(
            x,
            y,
            width,
            height,
            canvasWidth,
            canvasHeight,
            snapEnabled,
            snapEnabled,
            false,
            snapEnabled);
    }

    private static CanvasSnapResult SnapBounds(
        double x,
        double y,
        double width,
        double height,
        double canvasWidth,
        double canvasHeight,
        bool enabled,
        bool snapPosition,
        bool snapToCenter,
        bool snapSize)
    {
        if (!enabled)
        {
            return new CanvasSnapResult(x, y, width, height, null, null);
        }

        var snappedX = snapPosition ? TimelineMath.Snap(x, GridSize) : x;
        var snappedY = snapPosition ? TimelineMath.Snap(y, GridSize) : y;
        var snappedWidth = snapSize ? Math.Max(24, TimelineMath.Snap(width, GridSize)) : width;
        var snappedHeight = snapSize ? Math.Max(24, TimelineMath.Snap(height, GridSize)) : height;

        double? verticalGuide = null;
        double? horizontalGuide = null;

        if (snapToCenter)
        {
            var centerX = snappedX + (snappedWidth / 2);
            var centerY = snappedY + (snappedHeight / 2);
            var canvasCenterX = canvasWidth / 2;
            var canvasCenterY = canvasHeight / 2;

            if (Math.Abs(centerX - canvasCenterX) <= GuideThreshold)
            {
                snappedX = (canvasWidth - snappedWidth) / 2;
                verticalGuide = canvasCenterX;
            }

            if (Math.Abs(centerY - canvasCenterY) <= GuideThreshold)
            {
                snappedY = (canvasHeight - snappedHeight) / 2;
                horizontalGuide = canvasCenterY;
            }
        }

        return new CanvasSnapResult(snappedX, snappedY, snappedWidth, snappedHeight, verticalGuide, horizontalGuide);
    }
}
