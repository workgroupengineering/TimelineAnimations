using TimelineAnimations.Core.Models;
using TimelineAnimations.Rendering.Models;

namespace TimelineAnimations.Rendering.Services;

public static class SceneRenderTransformService
{
    public static LayerSnapshot? ResolveActiveCamera(IReadOnlyList<RenderableLayerSample> samples)
    {
        LayerSnapshot? activeCamera = null;
        foreach (var sample in samples)
        {
            if (sample.Role == LayerCompositeRole.Camera)
            {
                activeCamera = sample.Snapshot;
            }
        }

        return activeCamera;
    }

    public static LayerSnapshot TransformSnapshot(
        LayerSnapshot snapshot,
        LayerSnapshot? activeCamera,
        AnimateDocumentSettings? animate,
        double canvasWidth,
        double canvasHeight,
        double? parallaxDepth = null)
    {
        var transformed = ApplyDocument3DTransform(snapshot, animate, canvasWidth, canvasHeight);
        if (activeCamera is null || transformed.Compositing.Role == LayerCompositeRole.Camera)
        {
            return transformed;
        }

        var camera = activeCamera.Value;
        var depth = Math.Max(0.2d, parallaxDepth ?? transformed.Compositing.ParallaxDepth);
        var cameraWidth = Math.Max(1d, camera.Width);
        var cameraHeight = Math.Max(1d, camera.Height);
        var scaleX = canvasWidth / cameraWidth;
        var scaleY = canvasHeight / cameraHeight;
        var styleScale = (scaleX + scaleY) / 2d;
        return transformed with
        {
            X = (transformed.X - (camera.X / depth)) * scaleX,
            Y = (transformed.Y - (camera.Y / depth)) * scaleY,
            Width = transformed.Width * scaleX,
            Height = transformed.Height * scaleY,
            CornerRadius = transformed.CornerRadius * styleScale,
            FontSize = transformed.FontSize * styleScale,
            StrokeThickness = Math.Max(1d, transformed.StrokeThickness * styleScale),
            Compositing = transformed.Compositing.Clone()
        };
    }

    public static LayerSnapshot TransformSnapshot(
        LayerSnapshot snapshot,
        LayerSnapshot? activeCamera,
        double canvasWidth,
        double canvasHeight,
        double? parallaxDepth = null)
    {
        return TransformSnapshot(snapshot, activeCamera, animate: null, canvasWidth, canvasHeight, parallaxDepth);
    }

    public static RenderPoint TransformPoint(
        RenderPoint worldPoint,
        LayerSnapshot? activeCamera,
        double canvasWidth,
        double canvasHeight,
        double parallaxDepth = 1d)
    {
        if (activeCamera is null)
        {
            return worldPoint;
        }

        var camera = activeCamera.Value;
        var depth = Math.Max(0.2d, parallaxDepth);
        var scaleX = canvasWidth / Math.Max(1d, camera.Width);
        var scaleY = canvasHeight / Math.Max(1d, camera.Height);
        return new RenderPoint(
            (worldPoint.X - (camera.X / depth)) * scaleX,
            (worldPoint.Y - (camera.Y / depth)) * scaleY);
    }

    public static RenderPoint InverseTransformPoint(
        RenderPoint viewportPoint,
        LayerSnapshot? activeCamera,
        double canvasWidth,
        double canvasHeight,
        double parallaxDepth = 1d)
    {
        if (activeCamera is null)
        {
            return viewportPoint;
        }

        var camera = activeCamera.Value;
        var depth = Math.Max(0.2d, parallaxDepth);
        var scaleX = canvasWidth / Math.Max(1d, camera.Width);
        var scaleY = canvasHeight / Math.Max(1d, camera.Height);
        return new RenderPoint(
            (viewportPoint.X / scaleX) + (camera.X / depth),
            (viewportPoint.Y / scaleY) + (camera.Y / depth));
    }

    private static LayerSnapshot ApplyDocument3DTransform(
        LayerSnapshot snapshot,
        AnimateDocumentSettings? animate,
        double canvasWidth,
        double canvasHeight)
    {
        if (animate is null ||
            !animate.Enable3DTransforms ||
            (Math.Abs(snapshot.RotationX) < 0.01d &&
             Math.Abs(snapshot.RotationY) < 0.01d &&
             Math.Abs(snapshot.ZDepth) < 0.01d))
        {
            return snapshot;
        }

        var perspectiveAngle = Math.Clamp(animate.PerspectiveAngle, 1d, 89d);
        var stageSize = Math.Max(1d, Math.Max(canvasWidth, canvasHeight));
        var viewDistance = stageSize / Math.Tan((perspectiveAngle * Math.PI / 180d) / 2d);
        var vanishingPoint = new RenderPoint(
            canvasWidth * Math.Clamp(animate.VanishingPointX, 0d, 1d),
            canvasHeight * Math.Clamp(animate.VanishingPointY, 0d, 1d));
        var perspectiveScale = viewDistance / Math.Max(1d, viewDistance + snapshot.ZDepth);
        perspectiveScale = Math.Clamp(perspectiveScale, 0.1d, 6d);

        var rotationXRadians = snapshot.RotationX * Math.PI / 180d;
        var rotationYRadians = snapshot.RotationY * Math.PI / 180d;
        var projectedWidth = Math.Max(1d, snapshot.Width * perspectiveScale * Math.Max(0.08d, Math.Abs(Math.Cos(rotationYRadians))));
        var projectedHeight = Math.Max(1d, snapshot.Height * perspectiveScale * Math.Max(0.08d, Math.Abs(Math.Cos(rotationXRadians))));
        var center = new RenderPoint(snapshot.X + (snapshot.Width / 2d), snapshot.Y + (snapshot.Height / 2d));
        var projectedCenter = new RenderPoint(
            vanishingPoint.X + ((center.X - vanishingPoint.X) * perspectiveScale),
            vanishingPoint.Y + ((center.Y - vanishingPoint.Y) * perspectiveScale));
        var styleScale = perspectiveScale * Math.Sqrt(
            Math.Max(0.08d, Math.Abs(Math.Cos(rotationXRadians))) *
            Math.Max(0.08d, Math.Abs(Math.Cos(rotationYRadians))));

        return snapshot with
        {
            X = projectedCenter.X - (projectedWidth / 2d),
            Y = projectedCenter.Y - (projectedHeight / 2d),
            Width = projectedWidth,
            Height = projectedHeight,
            SkewX = snapshot.SkewX + (Math.Sin(rotationYRadians) * 18d),
            SkewY = snapshot.SkewY - (Math.Sin(rotationXRadians) * 18d),
            Rotation = snapshot.Rotation + (snapshot.RotationY * 0.12d) - (snapshot.RotationX * 0.04d),
            CornerRadius = snapshot.CornerRadius * styleScale,
            FontSize = snapshot.FontSize * styleScale,
            StrokeThickness = Math.Max(1d, snapshot.StrokeThickness * styleScale)
        };
    }
}
