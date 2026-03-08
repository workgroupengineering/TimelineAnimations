using System.Numerics;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public readonly record struct WarpPinVisual(WarpPinModel Pin, Vector2 BindPoint, Vector2 CurrentPoint);

public readonly record struct RigBoneVisual(RigBoneModel Bone, Vector2 StartPoint, Vector2 EndPoint);

public static class LayerDeformationService
{
    private const double MinimumRadius = 0.05d;
    private const double MinimumEnvelope = 0.08d;

    public static void Normalize(TimelineLayer layer)
    {
        layer.Warp.MeshColumns = Math.Clamp(layer.Warp.MeshColumns, 2, 12);
        layer.Warp.MeshRows = Math.Clamp(layer.Warp.MeshRows, 2, 12);
        foreach (var pin in layer.Warp.Pins)
        {
            pin.BindX = TimelineMath.Clamp(pin.BindX, 0d, 1d);
            pin.BindY = TimelineMath.Clamp(pin.BindY, 0d, 1d);
            pin.X = TimelineMath.Clamp(pin.X, 0d, 1d);
            pin.Y = TimelineMath.Clamp(pin.Y, 0d, 1d);
            pin.Radius = TimelineMath.Clamp(pin.Radius, MinimumRadius, 1.5d);
            pin.Strength = TimelineMath.Clamp(pin.Strength, 0d, 2d);
        }

        foreach (var bone in layer.Rig.Bones)
        {
            bone.BindStartX = TimelineMath.Clamp(bone.BindStartX, 0d, 1d);
            bone.BindStartY = TimelineMath.Clamp(bone.BindStartY, 0d, 1d);
            bone.StartX = TimelineMath.Clamp(bone.StartX, 0d, 1d);
            bone.StartY = TimelineMath.Clamp(bone.StartY, 0d, 1d);
            bone.Length = TimelineMath.Clamp(bone.Length, 0.05d, 1.5d);
            bone.Envelope = TimelineMath.Clamp(bone.Envelope, MinimumEnvelope, 1.5d);
        }

        foreach (var pose in layer.Rig.Poses)
        {
            pose.Bones = pose.Bones
                .GroupBy(item => item.BoneId)
                .Select(group => group.First())
                .ToList();
        }
    }

    public static void EnsureDefaultWarpPins(TimelineLayer layer)
    {
        layer.Warp.IsEnabled = true;
        if (layer.Warp.Pins.Count > 0)
        {
            Normalize(layer);
            return;
        }

        layer.Warp.Pins =
        [
            CreatePin(0.08d, 0.08d),
            CreatePin(0.92d, 0.08d),
            CreatePin(0.92d, 0.92d),
            CreatePin(0.08d, 0.92d),
            CreatePin(0.50d, 0.50d, 0.48d)
        ];
        Normalize(layer);
    }

    public static void EnsureDefaultRig(TimelineLayer layer)
    {
        layer.Rig.IsEnabled = true;
        if (layer.Rig.Bones.Count > 0)
        {
            Normalize(layer);
            return;
        }

        var root = new RigBoneModel
        {
            Name = "Root",
            BindStartX = 0.18d,
            BindStartY = 0.52d,
            StartX = 0.18d,
            StartY = 0.52d,
            Length = 0.32d,
            Rotation = 0d,
            Envelope = 0.34d
        };
        var tip = new RigBoneModel
        {
            Name = "Tip",
            ParentBoneId = root.Id,
            BindStartX = 0.50d,
            BindStartY = 0.52d,
            StartX = 0.50d,
            StartY = 0.52d,
            Length = 0.24d,
            Rotation = -8d,
            Envelope = 0.28d
        };
        layer.Rig.Bones = [root, tip];
        Normalize(layer);
    }

    public static bool CapturePose(TimelineLayer layer, string? name, double time)
    {
        if (layer.Rig.Bones.Count == 0)
        {
            return false;
        }

        layer.Rig.IsEnabled = true;
        var poseName = string.IsNullOrWhiteSpace(name)
            ? $"Pose {layer.Rig.Poses.Count + 1}"
            : name.Trim();
        var existing = layer.Rig.Poses.FirstOrDefault(item => Math.Abs(item.Time - time) < 0.0001d);
        if (existing is null)
        {
            existing = new RigPoseModel { Name = poseName, Time = Math.Max(0d, time) };
            layer.Rig.Poses.Add(existing);
        }
        else
        {
            existing.Name = poseName;
        }

        existing.Bones = layer.Rig.Bones
            .Select(bone => new RigBonePoseModel
            {
                BoneId = bone.Id,
                StartX = bone.StartX,
                StartY = bone.StartY,
                Length = bone.Length,
                Rotation = bone.Rotation
            })
            .ToList();
        layer.Rig.Poses = [.. layer.Rig.Poses.OrderBy(item => item.Time)];
        Normalize(layer);
        return true;
    }

    public static bool ApplyPose(TimelineLayer layer, Guid poseId)
    {
        var pose = layer.Rig.Poses.FirstOrDefault(item => item.Id == poseId);
        if (pose is null)
        {
            return false;
        }

        foreach (var bone in layer.Rig.Bones)
        {
            var poseBone = pose.Bones.FirstOrDefault(item => item.BoneId == bone.Id);
            if (poseBone is null)
            {
                continue;
            }

            bone.StartX = poseBone.StartX;
            bone.StartY = poseBone.StartY;
            bone.Length = poseBone.Length;
            bone.Rotation = poseBone.Rotation;
        }

        Normalize(layer);
        return true;
    }

    public static bool DeletePose(TimelineLayer layer, Guid poseId)
    {
        var removed = layer.Rig.Poses.RemoveAll(item => item.Id == poseId);
        return removed > 0;
    }

    public static bool MoveWarpPin(TimelineLayer layer, Guid pinId, double normalizedX, double normalizedY)
    {
        var pin = layer.Warp.Pins.FirstOrDefault(item => item.Id == pinId);
        if (pin is null)
        {
            return false;
        }

        layer.Warp.IsEnabled = true;
        pin.X = TimelineMath.Clamp(normalizedX, 0d, 1d);
        pin.Y = TimelineMath.Clamp(normalizedY, 0d, 1d);
        Normalize(layer);
        return true;
    }

    public static bool MoveRigBoneHandle(TimelineLayer layer, Guid boneId, RigBoneHandleKind handleKind, double normalizedX, double normalizedY)
    {
        var bone = layer.Rig.Bones.FirstOrDefault(item => item.Id == boneId);
        if (bone is null)
        {
            return false;
        }

        layer.Rig.IsEnabled = true;
        var target = new Vector2((float)TimelineMath.Clamp(normalizedX, 0d, 1d), (float)TimelineMath.Clamp(normalizedY, 0d, 1d));
        if (handleKind == RigBoneHandleKind.Start)
        {
            bone.StartX = target.X;
            bone.StartY = target.Y;
        }
        else
        {
            var start = new Vector2((float)bone.StartX, (float)bone.StartY);
            var delta = target - start;
            bone.Length = TimelineMath.Clamp(delta.Length(), 0.05d, 1.5d);
            bone.Rotation = Math.Atan2(delta.Y, delta.X) * 180d / Math.PI;
        }

        Normalize(layer);
        return true;
    }

    public static IReadOnlyList<VectorPointModel> GetDeformedPathPoints(LayerSnapshot snapshot, TimelineLayer layer, double time)
    {
        var absolutePoints = VectorPathService.GetAbsolutePoints(snapshot)
            .Select(point => new Vector2((float)point.X, (float)point.Y))
            .ToArray();
        if (absolutePoints.Length == 0)
        {
            return [];
        }

        if (layer.Warp.IsEnabled && layer.Warp.Pins.Count > 0)
        {
            ApplyWarp(snapshot, layer.Warp, absolutePoints);
        }

        if (layer.Rig.IsEnabled && layer.Rig.Bones.Count > 0)
        {
            ApplyRig(snapshot, layer.Rig, absolutePoints, time);
        }

        return absolutePoints
            .Select(point => new VectorPointModel { X = point.X, Y = point.Y })
            .ToList();
    }

    public static IReadOnlyList<WarpPinVisual> ResolveWarpPins(LayerSnapshot snapshot, LayerWarpSettings warp)
    {
        var x = snapshot.X;
        var y = snapshot.Y;
        var width = snapshot.Width;
        var height = snapshot.Height;
        return
        [
            .. warp.Pins.Select(pin => new WarpPinVisual(
                pin,
                ResolvePoint(x, y, width, height, pin.BindX, pin.BindY),
                ResolvePoint(x, y, width, height, pin.X, pin.Y)))
        ];
    }

    public static IReadOnlyList<RigBoneVisual> ResolveRigBones(LayerSnapshot snapshot, LayerRigSettings rig, double time)
    {
        var resolvedBones = ResolveRigState(rig, time);
        var x = snapshot.X;
        var y = snapshot.Y;
        var width = snapshot.Width;
        var height = snapshot.Height;
        var scale = Math.Max(width, height);

        return
        [
            .. resolvedBones.Select(bone =>
            {
                var start = ResolvePoint(x, y, width, height, bone.StartX, bone.StartY);
                var radians = bone.Rotation * Math.PI / 180d;
                var end = new Vector2(
                    start.X + (float)(Math.Cos(radians) * bone.Length * scale),
                    start.Y + (float)(Math.Sin(radians) * bone.Length * scale));
                return new RigBoneVisual(bone, start, end);
            })
        ];
    }

    private static void ApplyWarp(LayerSnapshot snapshot, LayerWarpSettings warp, Vector2[] points)
    {
        var scale = (float)Math.Max(snapshot.Width, snapshot.Height);
        foreach (var pin in warp.Pins)
        {
            var bindPoint = ResolvePoint(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height, pin.BindX, pin.BindY);
            var currentPoint = ResolvePoint(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height, pin.X, pin.Y);
            var delta = currentPoint - bindPoint;
            if (delta.LengthSquared() < 0.0001f)
            {
                continue;
            }

            var radius = (float)Math.Max(MinimumRadius, pin.Radius) * scale;
            for (var index = 0; index < points.Length; index++)
            {
                var distance = Vector2.Distance(points[index], bindPoint);
                if (distance > radius)
                {
                    continue;
                }

                var weight = 1f - (distance / radius);
                points[index] += delta * weight * (float)pin.Strength;
            }
        }
    }

    private static void ApplyRig(LayerSnapshot snapshot, LayerRigSettings rig, Vector2[] points, double time)
    {
        var resolvedBones = ResolveRigState(rig, time);
        var baseBones = rig.Bones.ToDictionary(item => item.Id);
        var scale = (float)Math.Max(snapshot.Width, snapshot.Height);

        foreach (var currentBone in resolvedBones)
        {
            if (!baseBones.TryGetValue(currentBone.Id, out var bindBone))
            {
                continue;
            }

            var bindStart = ResolvePoint(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height, bindBone.BindStartX, bindBone.BindStartY);
            var currentStart = ResolvePoint(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height, currentBone.StartX, currentBone.StartY);
            var bindRadians = bindBone.Rotation * Math.PI / 180d;
            var bindEnd = bindStart + new Vector2(
                (float)(Math.Cos(bindRadians) * bindBone.Length * scale),
                (float)(Math.Sin(bindRadians) * bindBone.Length * scale));
            var angleDelta = (float)((currentBone.Rotation - bindBone.Rotation) * Math.PI / 180d);
            var lengthRatio = (float)(currentBone.Length / Math.Max(0.05d, bindBone.Length));
            var envelope = (float)Math.Max(MinimumEnvelope, currentBone.Envelope) * scale;

            for (var index = 0; index < points.Length; index++)
            {
                var distance = DistanceToSegment(points[index], bindStart, bindEnd);
                if (distance > envelope)
                {
                    continue;
                }

                var weight = 1f - (distance / envelope);
                var local = points[index] - bindStart;
                var transformed = Vector2.Transform(local * new Vector2(lengthRatio, lengthRatio), Matrix3x2.CreateRotation(angleDelta));
                var target = currentStart + transformed;
                points[index] = Vector2.Lerp(points[index], target, weight);
            }
        }
    }

    private static List<RigBoneModel> ResolveRigState(LayerRigSettings rig, double time)
    {
        if (rig.Poses.Count == 0)
        {
            return rig.Bones
                .Select(CloneBone)
                .ToList();
        }

        var ordered = rig.Poses.OrderBy(item => item.Time).ToList();
        RigPoseModel? previous = null;
        RigPoseModel? next = null;
        foreach (var pose in ordered)
        {
            if (pose.Time <= time)
            {
                previous = pose;
            }

            if (pose.Time >= time)
            {
                next = pose;
                break;
            }
        }

        if (previous is null)
        {
            previous = ordered.First();
        }

        if (next is null)
        {
            next = ordered.Last();
        }

        if (previous.Id == next.Id || Math.Abs(next.Time - previous.Time) < 0.0001d)
        {
            return BuildPoseBones(rig.Bones, previous);
        }

        var progress = TimelineMath.Clamp((time - previous.Time) / (next.Time - previous.Time), 0d, 1d);
        var previousBones = BuildPoseBones(rig.Bones, previous).ToDictionary(item => item.Id);
        var nextBones = BuildPoseBones(rig.Bones, next).ToDictionary(item => item.Id);
        var interpolated = new List<RigBoneModel>(rig.Bones.Count);

        foreach (var bindBone in rig.Bones)
        {
            var left = previousBones.GetValueOrDefault(bindBone.Id, CloneBone(bindBone));
            var right = nextBones.GetValueOrDefault(bindBone.Id, CloneBone(bindBone));
            interpolated.Add(new RigBoneModel
            {
                Id = bindBone.Id,
                Name = bindBone.Name,
                ParentBoneId = bindBone.ParentBoneId,
                BindStartX = bindBone.BindStartX,
                BindStartY = bindBone.BindStartY,
                StartX = left.StartX + ((right.StartX - left.StartX) * progress),
                StartY = left.StartY + ((right.StartY - left.StartY) * progress),
                Length = left.Length + ((right.Length - left.Length) * progress),
                Rotation = left.Rotation + ((right.Rotation - left.Rotation) * progress),
                Envelope = bindBone.Envelope
            });
        }

        return interpolated;
    }

    private static List<RigBoneModel> BuildPoseBones(IReadOnlyList<RigBoneModel> sourceBones, RigPoseModel pose)
    {
        return
        [
            .. sourceBones.Select(bindBone =>
            {
                var poseBone = pose.Bones.FirstOrDefault(item => item.BoneId == bindBone.Id);
                return poseBone is null
                    ? CloneBone(bindBone)
                    : new RigBoneModel
                    {
                        Id = bindBone.Id,
                        Name = bindBone.Name,
                        ParentBoneId = bindBone.ParentBoneId,
                        BindStartX = bindBone.BindStartX,
                        BindStartY = bindBone.BindStartY,
                        StartX = poseBone.StartX,
                        StartY = poseBone.StartY,
                        Length = poseBone.Length,
                        Rotation = poseBone.Rotation,
                        Envelope = bindBone.Envelope
                    };
            })
        ];
    }

    private static RigBoneModel CloneBone(RigBoneModel bone)
    {
        return new RigBoneModel
        {
            Id = bone.Id,
            Name = bone.Name,
            ParentBoneId = bone.ParentBoneId,
            BindStartX = bone.BindStartX,
            BindStartY = bone.BindStartY,
            StartX = bone.StartX,
            StartY = bone.StartY,
            Length = bone.Length,
            Rotation = bone.Rotation,
            Envelope = bone.Envelope
        };
    }

    private static WarpPinModel CreatePin(double x, double y, double radius = 0.34d)
    {
        return new WarpPinModel
        {
            BindX = x,
            BindY = y,
            X = x,
            Y = y,
            Radius = radius,
            Strength = 1d
        };
    }

    private static Vector2 ResolvePoint(double x, double y, double width, double height, double normalizedX, double normalizedY)
    {
        return new Vector2(
            (float)(x + (width * normalizedX)),
            (float)(y + (height * normalizedY)));
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        var segment = end - start;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared < 0.0001f)
        {
            return Vector2.Distance(point, start);
        }

        var projection = Vector2.Dot(point - start, segment) / lengthSquared;
        projection = Math.Clamp(projection, 0f, 1f);
        var closest = start + (segment * projection);
        return Vector2.Distance(point, closest);
    }
}
