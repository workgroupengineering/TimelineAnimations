using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class FrameTimelineService
{
    public static int GetTotalFrames(double duration, double frameRate)
    {
        var safeDuration = Math.Max(0, duration);
        var safeFrameRate = Math.Max(1, frameRate);
        return Math.Max(1, (int)Math.Ceiling(safeDuration * safeFrameRate) + 1);
    }

    public static int TimeToFrame(double time, double frameRate, int totalFrames)
    {
        if (totalFrames <= 1)
        {
            return 0;
        }

        var frame = (int)Math.Round(Math.Max(0, time) * Math.Max(1, frameRate), MidpointRounding.AwayFromZero);
        return Math.Clamp(frame, 0, totalFrames - 1);
    }

    public static double FrameToTime(int frame, double frameRate)
    {
        return Math.Max(0, frame) / Math.Max(1, frameRate);
    }

    public static IReadOnlyList<FrameSpanModel> GetEffectiveSpans(TimelineLayer layer, int totalFrames)
    {
        if (totalFrames <= 0)
        {
            return [];
        }

        if (layer.FrameSpans.Count == 0)
        {
            return
            [
                new FrameSpanModel
                {
                    StartFrame = 0,
                    EndFrame = totalFrames - 1,
                    IsBlank = false
                }
            ];
        }

        return NormalizeSpans(layer.FrameSpans, totalFrames);
    }

    public static FrameSpanModel InsertContentKeyframe(TimelineLayer layer, int frame, int totalFrames)
    {
        return UpdateSpanState(layer, frame, totalFrames, isBlank: false);
    }

    public static FrameSpanModel InsertBlankKeyframe(TimelineLayer layer, int frame, int totalFrames)
    {
        return UpdateSpanState(layer, frame, totalFrames, isBlank: true);
    }

    public static FrameSpanModel ExtendSpanToFrame(TimelineLayer layer, int anchorFrame, int targetFrame, int totalFrames)
    {
        var spans = GetMutableSpans(layer, totalFrames);
        var span = GetSpanAtFrame(spans, anchorFrame)
            ?? throw new InvalidOperationException("No frame span found.");
        var normalizedTarget = Math.Clamp(targetFrame, span.StartFrame, totalFrames - 1);

        if (normalizedTarget <= span.EndFrame)
        {
            return span;
        }

        var rebuilt = new List<FrameSpanModel>();
        foreach (var candidate in spans)
        {
            if (candidate.EndFrame < span.StartFrame || candidate.StartFrame > normalizedTarget)
            {
                rebuilt.Add(candidate);
                continue;
            }

            if (candidate.StartFrame < span.StartFrame)
            {
                rebuilt.Add(CloneSpan(candidate.StartFrame, span.StartFrame - 1, candidate.IsBlank));
            }

            if (candidate.EndFrame > normalizedTarget)
            {
                rebuilt.Add(CloneSpan(normalizedTarget + 1, candidate.EndFrame, candidate.IsBlank));
            }
        }

        rebuilt.Add(CloneSpan(span.StartFrame, normalizedTarget, span.IsBlank));
        rebuilt.Sort(static (left, right) => left.StartFrame.CompareTo(right.StartFrame));
        PersistSpans(layer, rebuilt, totalFrames);

        return GetSpanAtFrame(layer, span.StartFrame, totalFrames)
            ?? throw new InvalidOperationException("Unable to resolve extended frame span.");
    }

    public static FrameRangeClipboard CopyRange(TimelineLayer layer, int startFrame, int endFrame, int totalFrames, double frameRate)
    {
        var normalizedStart = Math.Clamp(Math.Min(startFrame, endFrame), 0, totalFrames - 1);
        var normalizedEnd = Math.Clamp(Math.Max(startFrame, endFrame), normalizedStart, totalFrames - 1);

        return new FrameRangeClipboard
        {
            FrameCount = (normalizedEnd - normalizedStart) + 1,
            Spans =
            [
                .. GetEffectiveSpans(layer, totalFrames)
                    .Where(span => span.EndFrame >= normalizedStart && span.StartFrame <= normalizedEnd)
                    .Select(span => CloneSpan(
                        Math.Max(0, span.StartFrame - normalizedStart),
                        Math.Min(normalizedEnd, span.EndFrame) - normalizedStart,
                        span.IsBlank))
            ],
            ShapeKeyframes = BuildClipboardShapeKeyframes(layer, normalizedStart, normalizedEnd, frameRate),
            Tracks = BuildClipboardTracks(layer, normalizedStart, normalizedEnd, totalFrames, frameRate)
        };
    }

    public static void PasteRange(TimelineLayer layer, int targetFrame, FrameRangeClipboard clipboard, int totalFrames, double frameRate)
    {
        if (clipboard.FrameCount <= 0 || totalFrames <= 0)
        {
            return;
        }

        var start = Math.Clamp(targetFrame, 0, totalFrames - 1);
        var end = Math.Min(totalFrames - 1, start + clipboard.FrameCount - 1);
        var spans = GetMutableSpans(layer, totalFrames);
        var rebuilt = new List<FrameSpanModel>();

        foreach (var span in spans)
        {
            if (span.EndFrame < start || span.StartFrame > end)
            {
                rebuilt.Add(span);
                continue;
            }

            if (span.StartFrame < start)
            {
                rebuilt.Add(CloneSpan(span.StartFrame, start - 1, span.IsBlank));
            }

            if (span.EndFrame > end)
            {
                rebuilt.Add(CloneSpan(end + 1, span.EndFrame, span.IsBlank));
            }
        }

        foreach (var span in clipboard.Spans.OrderBy(item => item.StartFrame))
        {
            var pastedStart = start + span.StartFrame;
            var pastedEnd = Math.Min(end, start + span.EndFrame);
            if (pastedStart > end || pastedStart >= totalFrames || pastedEnd < pastedStart)
            {
                continue;
            }

            rebuilt.Add(CloneSpan(pastedStart, pastedEnd, span.IsBlank));
        }

        rebuilt.Sort(static (left, right) => left.StartFrame.CompareTo(right.StartFrame));
        PersistSpans(layer, rebuilt, totalFrames);
        PasteTrackKeys(layer, clipboard, start, end, frameRate);
        PasteShapeKeyframes(layer, clipboard, start, end, frameRate);
    }

    public static bool SetFrameLabel(SceneModel scene, int frame, string name, int totalFrames)
    {
        var normalizedFrame = Math.Clamp(frame, 0, Math.Max(0, totalFrames - 1));
        var existing = scene.FrameLabels.FirstOrDefault(item => item.Frame == normalizedFrame);
        var normalizedName = name.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            if (existing is null)
            {
                return false;
            }

            scene.FrameLabels.Remove(existing);
            return true;
        }

        if (existing is null)
        {
            scene.FrameLabels.Add(new FrameLabelModel
            {
                Frame = normalizedFrame,
                Name = normalizedName
            });
        }
        else
        {
            existing.Name = normalizedName;
        }

        scene.FrameLabels.Sort(static (left, right) => left.Frame.CompareTo(right.Frame));
        return true;
    }

    public static bool RemoveFrameLabel(SceneModel scene, int frame)
    {
        return scene.FrameLabels.RemoveAll(item => item.Frame == frame) > 0;
    }

    public static FrameLabelModel? GetFrameLabel(SceneModel scene, int frame)
    {
        return scene.FrameLabels.FirstOrDefault(item => item.Frame == frame);
    }

    public static bool HasExplicitFrameTimeline(TimelineLayer layer, int totalFrames)
    {
        return totalFrames > 0 && layer.FrameSpans.Count > 0;
    }

    public static bool IsLayerVisibleAtTime(TimelineLayer layer, double time, double frameRate, int totalFrames)
    {
        if (!layer.IsVisible)
        {
            return false;
        }

        var sampleTime = GetSampleTime(layer, time, frameRate, totalFrames);
        return sampleTime is not null;
    }

    public static double? GetSampleTime(TimelineLayer layer, double time, double frameRate, int totalFrames)
    {
        if (totalFrames <= 0 || frameRate <= 0)
        {
            return time;
        }

        if (!HasExplicitFrameTimeline(layer, totalFrames))
        {
            return time;
        }

        var frame = TimeToFrame(time, frameRate, totalFrames);
        var span = GetSpanAtFrame(layer, frame, totalFrames);
        if (span is null || span.IsBlank)
        {
            return null;
        }

        return FrameToTime(span.StartFrame, frameRate);
    }

    public static LayerSnapshot? SampleLayer(TimelineLayer layer, double time, double frameRate, int totalFrames)
    {
        var sampleTime = GetSampleTime(layer, time, frameRate, totalFrames);
        return sampleTime is null
            ? null
            : TimelineInterpolationService.SampleLayer(layer, sampleTime.Value);
    }

    public static double SampleProperty(TimelineLayer layer, AnimatedProperty property, double time, double frameRate, int totalFrames)
    {
        var sampleTime = GetSampleTime(layer, time, frameRate, totalFrames);
        return sampleTime is null
            ? TimelineInterpolationService.GetDefaultValue(layer, property)
            : TimelineInterpolationService.SampleProperty(layer, property, sampleTime.Value);
    }

    public static int GetLayerDurationFrames(TimelineLayer layer, int totalFrames)
    {
        var span = GetEffectiveSpans(layer, totalFrames).LastOrDefault();
        return span is null ? 0 : span.EndFrame + 1;
    }

    public static void NormalizeLayerFrameSpans(TimelineLayer layer, int totalFrames)
    {
        PersistSpans(layer, GetEffectiveSpans(layer, totalFrames), totalFrames);
    }

    private static FrameSpanModel UpdateSpanState(TimelineLayer layer, int frame, int totalFrames, bool isBlank)
    {
        var spans = GetMutableSpans(layer, totalFrames);
        var span = GetSpanAtFrame(spans, frame)
            ?? throw new InvalidOperationException("No frame span found.");
        var normalizedFrame = Math.Clamp(frame, 0, totalFrames - 1);
        var rebuilt = new List<FrameSpanModel>();

        foreach (var candidate in spans)
        {
            if (candidate.Id != span.Id)
            {
                rebuilt.Add(candidate);
                continue;
            }

            if (normalizedFrame > candidate.StartFrame)
            {
                rebuilt.Add(CloneSpan(candidate.StartFrame, normalizedFrame - 1, candidate.IsBlank));
            }

            rebuilt.Add(CloneSpan(normalizedFrame, candidate.EndFrame, isBlank));
        }

        rebuilt.Sort(static (left, right) => left.StartFrame.CompareTo(right.StartFrame));
        PersistSpans(layer, rebuilt, totalFrames);
        return GetSpanAtFrame(layer, normalizedFrame, totalFrames)
            ?? throw new InvalidOperationException("Unable to resolve updated frame span.");
    }

    private static FrameSpanModel? GetSpanAtFrame(TimelineLayer layer, int frame, int totalFrames)
    {
        return GetEffectiveSpans(layer, totalFrames)
            .FirstOrDefault(item => frame >= item.StartFrame && frame <= item.EndFrame);
    }

    private static FrameSpanModel? GetSpanAtFrame(IReadOnlyList<FrameSpanModel> spans, int frame)
    {
        return spans
            .FirstOrDefault(item => frame >= item.StartFrame && frame <= item.EndFrame);
    }

    private static List<FrameSpanModel> GetMutableSpans(TimelineLayer layer, int totalFrames)
    {
        return
        [
            .. GetEffectiveSpans(layer, totalFrames)
                .Select(span => CloneSpan(span.StartFrame, span.EndFrame, span.IsBlank))
        ];
    }

    private static List<LayerTrack> BuildClipboardTracks(TimelineLayer layer, int startFrame, int endFrame, int totalFrames, double frameRate)
    {
        var safeFrameRate = Math.Max(1, frameRate);
        var startTime = FrameToTime(startFrame, safeFrameRate);
        var endTime = FrameToTime(endFrame, safeFrameRate);
        var clipboardTracks = new List<LayerTrack>();

        foreach (var property in Enum.GetValues<AnimatedProperty>())
        {
            var sampledValue = SampleProperty(layer, property, startTime, safeFrameRate, totalFrames);
            var keyframes = new List<KeyframeModel>
            {
                new()
                {
                    Time = 0,
                    Value = sampledValue,
                    Easing = EasingKind.Linear,
                    ControlPoint1X = 0.25d,
                    ControlPoint1Y = 0.1d,
                    ControlPoint2X = 0.25d,
                    ControlPoint2Y = 1d
                }
            };

            var sourceTrack = layer.Tracks.FirstOrDefault(track => track.Property == property);
            if (sourceTrack is not null)
            {
                foreach (var keyframe in sourceTrack.Keyframes
                    .Where(keyframe => keyframe.Time >= startTime && keyframe.Time <= endTime))
                {
                    var relativeTime = Math.Max(0, keyframe.Time - startTime);
                    if (keyframes.Any(existing => Math.Abs(existing.Time - relativeTime) < 0.0001d))
                    {
                        continue;
                    }

                    keyframes.Add(new KeyframeModel
                    {
                        Time = relativeTime,
                        Value = keyframe.Value,
                        Easing = keyframe.Easing,
                        ControlPoint1X = keyframe.ControlPoint1X,
                        ControlPoint1Y = keyframe.ControlPoint1Y,
                        ControlPoint2X = keyframe.ControlPoint2X,
                        ControlPoint2Y = keyframe.ControlPoint2Y
                    });
                }
            }

            clipboardTracks.Add(new LayerTrack
            {
                Property = property,
                Keyframes = keyframes.OrderBy(item => item.Time).ToList()
            });
        }

        return clipboardTracks;
    }

    private static List<FrameSpanModel> NormalizeSpans(IReadOnlyList<FrameSpanModel> spans, int totalFrames)
    {
        if (totalFrames <= 0)
        {
            return [];
        }

        var normalized = new List<FrameSpanModel>();
        var cursor = 0;
        foreach (var span in spans.OrderBy(item => item.StartFrame).ThenBy(item => item.EndFrame))
        {
            if (cursor >= totalFrames)
            {
                break;
            }

            var start = Math.Clamp(span.StartFrame, 0, totalFrames - 1);
            var end = Math.Clamp(span.EndFrame, start, totalFrames - 1);

            if (end < cursor)
            {
                continue;
            }

            if (start > cursor)
            {
                normalized.Add(CloneSpan(cursor, start - 1, false));
            }

            start = Math.Max(cursor, start);
            normalized.Add(CloneSpan(start, end, span.IsBlank));
            cursor = end + 1;
        }

        if (cursor <= totalFrames - 1)
        {
            normalized.Add(CloneSpan(cursor, totalFrames - 1, false));
        }

        return normalized;
    }

    private static void PersistSpans(TimelineLayer layer, IReadOnlyList<FrameSpanModel> spans, int totalFrames)
    {
        var normalized = NormalizeSpans(spans, totalFrames);
        if (normalized.Count == 1 &&
            !normalized[0].IsBlank &&
            normalized[0].StartFrame == 0 &&
            normalized[0].EndFrame == totalFrames - 1)
        {
            layer.FrameSpans = [];
            return;
        }

        layer.FrameSpans =
        [
            .. normalized.Select(span => CloneSpan(span.StartFrame, span.EndFrame, span.IsBlank))
        ];
    }

    private static FrameSpanModel CloneSpan(int startFrame, int endFrame, bool isBlank)
    {
        return new FrameSpanModel
        {
            StartFrame = startFrame,
            EndFrame = endFrame,
            IsBlank = isBlank
        };
    }

    private static void PasteTrackKeys(TimelineLayer layer, FrameRangeClipboard clipboard, int startFrame, int endFrame, double frameRate)
    {
        var safeFrameRate = Math.Max(1, frameRate);
        var startTime = FrameToTime(startFrame, safeFrameRate);
        var endTime = FrameToTime(endFrame, safeFrameRate);

        foreach (var clipboardTrack in clipboard.Tracks)
        {
            var targetTrack = layer.Tracks.FirstOrDefault(track => track.Property == clipboardTrack.Property);
            if (targetTrack is null)
            {
                targetTrack = new LayerTrack
                {
                    Property = clipboardTrack.Property
                };
                layer.Tracks.Add(targetTrack);
            }

            targetTrack.Keyframes.RemoveAll(keyframe => keyframe.Time >= startTime && keyframe.Time <= endTime);

            foreach (var clipboardKeyframe in clipboardTrack.Keyframes)
            {
                var keyTime = Math.Min(endTime, startTime + clipboardKeyframe.Time);
                targetTrack.Keyframes.Add(new KeyframeModel
                {
                    Time = keyTime,
                    Value = clipboardKeyframe.Value,
                    Easing = clipboardKeyframe.Easing,
                    ControlPoint1X = clipboardKeyframe.ControlPoint1X,
                    ControlPoint1Y = clipboardKeyframe.ControlPoint1Y,
                    ControlPoint2X = clipboardKeyframe.ControlPoint2X,
                    ControlPoint2Y = clipboardKeyframe.ControlPoint2Y
                });
            }

            targetTrack.Keyframes = targetTrack.Keyframes
                .OrderBy(keyframe => keyframe.Time)
                .ThenBy(keyframe => keyframe.Id)
                .GroupBy(keyframe => Math.Round(keyframe.Time, 4))
                .Select(group => group.Last())
                .OrderBy(keyframe => keyframe.Time)
                .ToList();
        }
    }

    private static List<ShapeKeyframeModel> BuildClipboardShapeKeyframes(TimelineLayer layer, int startFrame, int endFrame, double frameRate)
    {
        var safeFrameRate = Math.Max(1, frameRate);
        var startTime = FrameToTime(startFrame, safeFrameRate);
        var endTime = FrameToTime(endFrame, safeFrameRate);
        var clipboardKeys = new List<ShapeKeyframeModel>();

        var sampledShape = TimelineInterpolationService.SamplePathGeometry(layer, startTime);
        clipboardKeys.Add(new ShapeKeyframeModel
        {
            Time = 0,
            Easing = EasingKind.Linear,
            IsClosed = sampledShape.IsClosed,
            PathPoints = VectorPathService.ClonePoints(sampledShape.PathPoints)
        });

        foreach (var keyframe in layer.ShapeKeyframes
            .Where(keyframe => keyframe.Time >= startTime && keyframe.Time <= endTime))
        {
            var relativeTime = Math.Max(0, keyframe.Time - startTime);
            if (clipboardKeys.Any(existing => Math.Abs(existing.Time - relativeTime) < 0.0001d))
            {
                continue;
            }

            clipboardKeys.Add(new ShapeKeyframeModel
            {
                Time = relativeTime,
                Easing = keyframe.Easing,
                ControlPoint1X = keyframe.ControlPoint1X,
                ControlPoint1Y = keyframe.ControlPoint1Y,
                ControlPoint2X = keyframe.ControlPoint2X,
                ControlPoint2Y = keyframe.ControlPoint2Y,
                IsClosed = keyframe.IsClosed,
                PathPoints = VectorPathService.ClonePoints(keyframe.PathPoints)
            });
        }

        return clipboardKeys
            .OrderBy(item => item.Time)
            .ToList();
    }

    private static void PasteShapeKeyframes(TimelineLayer layer, FrameRangeClipboard clipboard, int startFrame, int endFrame, double frameRate)
    {
        if (clipboard.ShapeKeyframes.Count == 0)
        {
            return;
        }

        var safeFrameRate = Math.Max(1, frameRate);
        var startTime = FrameToTime(startFrame, safeFrameRate);
        var endTime = FrameToTime(endFrame, safeFrameRate);

        layer.ShapeKeyframes.RemoveAll(keyframe => keyframe.Time >= startTime && keyframe.Time <= endTime);
        foreach (var clipboardKeyframe in clipboard.ShapeKeyframes)
        {
            var keyTime = Math.Min(endTime, startTime + clipboardKeyframe.Time);
            layer.ShapeKeyframes.Add(new ShapeKeyframeModel
            {
                Time = keyTime,
                Easing = clipboardKeyframe.Easing,
                ControlPoint1X = clipboardKeyframe.ControlPoint1X,
                ControlPoint1Y = clipboardKeyframe.ControlPoint1Y,
                ControlPoint2X = clipboardKeyframe.ControlPoint2X,
                ControlPoint2Y = clipboardKeyframe.ControlPoint2Y,
                IsClosed = clipboardKeyframe.IsClosed,
                PathPoints = VectorPathService.ClonePoints(clipboardKeyframe.PathPoints)
            });
        }

        layer.ShapeKeyframes = layer.ShapeKeyframes
            .OrderBy(keyframe => keyframe.Time)
            .ThenBy(keyframe => keyframe.Id)
            .GroupBy(keyframe => Math.Round(keyframe.Time, 4))
            .Select(group => group.Last())
            .OrderBy(keyframe => keyframe.Time)
            .ToList();
    }

}
