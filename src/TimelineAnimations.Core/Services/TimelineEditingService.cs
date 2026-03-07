using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class TimelineEditingService
{
    private const double TimeTolerance = 0.0001d;

    public static TimelineLayer CreateLayer(
        LayerKind kind,
        string name,
        string fill,
        string text,
        double x,
        double y,
        int zIndex)
    {
        var defaults = kind switch
        {
            LayerKind.Folder => new LayerDefaults { X = 0, Y = 0, Width = 0, Height = 0, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            LayerKind.Rectangle => new LayerDefaults { X = x, Y = y, Width = 240, Height = 152, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            LayerKind.Ellipse => new LayerDefaults { X = x, Y = y, Width = 184, Height = 184, ScaleX = 1, ScaleY = 1, Opacity = 0.9 },
            LayerKind.Text => new LayerDefaults { X = x, Y = y, Width = 360, Height = 92, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            LayerKind.Path => new LayerDefaults { X = x, Y = y, Width = 240, Height = 120, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            LayerKind.AvaloniaControl => new LayerDefaults { X = x, Y = y, Width = 260, Height = 160, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            LayerKind.Video => new LayerDefaults { X = x, Y = y, Width = 420, Height = 236, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            LayerKind.Audio => new LayerDefaults { X = 0, Y = 0, Width = 0, Height = 0, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            _ => new LayerDefaults { X = x, Y = y }
        };

        var style = new LayerStyle
        {
            HasFill = true,
            Fill = fill,
            HasStroke = true,
            Stroke = "#FFFFFF",
            StrokeThickness = kind == LayerKind.Path ? 3.2d : 1.6d,
            StrokeCapStyle = LayerStrokeCapStyle.Round,
            StrokeJoinStyle = LayerStrokeJoinStyle.Round,
            StrokeMiterLimit = 3d,
            Text = kind == LayerKind.Text ? text : string.Empty,
            FontSize = kind == LayerKind.Text ? 48 : 42,
            CornerRadius = kind is LayerKind.Rectangle or LayerKind.Video or LayerKind.AvaloniaControl ? 28 : 999,
            GradientKind = LayerGradientKind.Linear,
            GradientAngle = 45d,
            GradientFrom = fill,
            GradientTo = "#FFFFFF",
            PathPoints = kind == LayerKind.Path
                ? [new VectorPointModel { X = 0, Y = 1 }, new VectorPointModel { X = 1, Y = 0 }]
                : []
        };

        if (kind == LayerKind.Folder)
        {
            style.Fill = "#22324C";
            style.Stroke = "#93A6D8";
            style.CornerRadius = 16d;
            style.FontSize = 22d;
        }

        return new TimelineLayer
        {
            Name = name,
            Kind = kind,
            ZIndex = zIndex,
            Defaults = defaults,
            Style = style,
            Tracks = kind == LayerKind.Folder ? [] : CreateDefaultTracks(defaults)
        };
    }

    public static TimelineLayer CreateFolderLayer(string name, int zIndex)
    {
        return CreateLayer(LayerKind.Folder, name, "#22324C", string.Empty, 0d, 0d, zIndex);
    }

    public static TimelineLayer CreateAvaloniaControlLayer(
        AvaloniaControlKind controlKind,
        string name,
        string fill,
        string stroke,
        string text,
        double x,
        double y,
        int zIndex)
    {
        var layer = CreateLayer(LayerKind.AvaloniaControl, name, fill, text, x, y, zIndex);
        var defaults = controlKind switch
        {
            AvaloniaControlKind.Button => new LayerDefaults { X = x, Y = y, Width = 220, Height = 58, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            AvaloniaControlKind.TextBlock => new LayerDefaults { X = x, Y = y, Width = 320, Height = 72, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            AvaloniaControlKind.TextBox => new LayerDefaults { X = x, Y = y, Width = 260, Height = 56, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            AvaloniaControlKind.CheckBox => new LayerDefaults { X = x, Y = y, Width = 220, Height = 42, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            AvaloniaControlKind.ToggleButton => new LayerDefaults { X = x, Y = y, Width = 210, Height = 48, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            AvaloniaControlKind.Slider => new LayerDefaults { X = x, Y = y, Width = 280, Height = 44, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            AvaloniaControlKind.ProgressBar => new LayerDefaults { X = x, Y = y, Width = 280, Height = 30, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            AvaloniaControlKind.Image => new LayerDefaults { X = x, Y = y, Width = 280, Height = 180, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            AvaloniaControlKind.ComboBox => new LayerDefaults { X = x, Y = y, Width = 260, Height = 54, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            AvaloniaControlKind.ListBox => new LayerDefaults { X = x, Y = y, Width = 260, Height = 180, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            AvaloniaControlKind.TabControl => new LayerDefaults { X = x, Y = y, Width = 320, Height = 200, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            AvaloniaControlKind.Grid => new LayerDefaults { X = x, Y = y, Width = 300, Height = 200, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            AvaloniaControlKind.StackPanel => new LayerDefaults { X = x, Y = y, Width = 240, Height = 220, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            AvaloniaControlKind.PathIcon => new LayerDefaults { X = x, Y = y, Width = 84, Height = 84, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            AvaloniaControlKind.Panel => new LayerDefaults { X = x, Y = y, Width = 300, Height = 200, ScaleX = 1, ScaleY = 1, Opacity = 1 },
            _ => new LayerDefaults { X = x, Y = y, Width = 260, Height = 160, ScaleX = 1, ScaleY = 1, Opacity = 1 }
        };

        layer.Defaults = defaults;
        layer.Tracks = CreateDefaultTracks(defaults);
        layer.Style.Fill = fill;
        layer.Style.Stroke = stroke;
        layer.Style.Text = text;
        layer.Style.FontSize = controlKind == AvaloniaControlKind.TextBlock ? 34d : 18d;
        layer.Style.CornerRadius = controlKind switch
        {
            AvaloniaControlKind.Border or AvaloniaControlKind.Button or AvaloniaControlKind.TextBox or AvaloniaControlKind.ProgressBar or AvaloniaControlKind.Panel or AvaloniaControlKind.Image or AvaloniaControlKind.ComboBox or AvaloniaControlKind.ListBox or AvaloniaControlKind.TabControl or AvaloniaControlKind.Grid or AvaloniaControlKind.StackPanel => 18d,
            AvaloniaControlKind.ToggleButton => 22d,
            AvaloniaControlKind.PathIcon => 14d,
            _ => 10d
        };
        layer.Style.AvaloniaControl = CreateDefaultAvaloniaControlSettings(controlKind, text);
        return layer;
    }

    public static TimelineLayer CreatePathLayer(
        string name,
        string fill,
        string stroke,
        IEnumerable<VectorPointModel> points,
        bool isClosed,
        int zIndex,
        double strokeThickness,
        bool useGradient = false,
        string? gradientFrom = null,
        string? gradientTo = null,
        LayerGradientKind gradientKind = LayerGradientKind.Linear,
        double gradientAngle = 45d,
        bool hasFill = true,
        bool hasStroke = true,
        LayerStrokeCapStyle strokeCapStyle = LayerStrokeCapStyle.Round,
        LayerStrokeJoinStyle strokeJoinStyle = LayerStrokeJoinStyle.Round,
        double strokeMiterLimit = 3d)
    {
        var (defaults, pathPoints) = VectorPathService.CreateLayerGeometry(points);
        var style = new LayerStyle
        {
            HasFill = hasFill,
            Fill = fill,
            HasStroke = hasStroke,
            Stroke = stroke,
            StrokeThickness = Math.Max(1d, strokeThickness),
            StrokeCapStyle = strokeCapStyle,
            StrokeJoinStyle = strokeJoinStyle,
            StrokeMiterLimit = Math.Max(1d, strokeMiterLimit),
            Text = string.Empty,
            FontSize = 42,
            CornerRadius = 0,
            UseGradient = useGradient,
            GradientKind = gradientKind,
            GradientAngle = gradientAngle,
            GradientFrom = string.IsNullOrWhiteSpace(gradientFrom) ? fill : gradientFrom!,
            GradientTo = string.IsNullOrWhiteSpace(gradientTo) ? stroke : gradientTo!,
            IsClosed = isClosed,
            PathPoints = pathPoints
        };

        return new TimelineLayer
        {
            Name = name,
            Kind = LayerKind.Path,
            ZIndex = zIndex,
            Defaults = defaults,
            Style = style,
            Tracks = CreateDefaultTracks(defaults)
        };
    }

    public static TimelineLayer CreateCameraLayer(string name, double canvasWidth, double canvasHeight, int zIndex)
    {
        var defaults = new LayerDefaults
        {
            X = 0,
            Y = 0,
            Width = Math.Max(320d, canvasWidth),
            Height = Math.Max(180d, canvasHeight),
            ScaleX = 1d,
            ScaleY = 1d,
            Rotation = 0,
            Opacity = 1
        };

        return new TimelineLayer
        {
            Name = name,
            Kind = LayerKind.Rectangle,
            ZIndex = zIndex,
            Defaults = defaults,
            Style = new LayerStyle
            {
                Fill = "#15223A",
                Stroke = "#FFD166",
                StrokeThickness = 2.4d,
                CornerRadius = 28d
            },
            Compositing = new LayerCompositeSettings
            {
                Role = LayerCompositeRole.Camera
            },
            Tracks = CreateDefaultTracks(defaults)
        };
    }

    public static TimelineLayer CreateVideoLayer(MediaAsset asset, double x, double y, int zIndex, double startTime)
    {
        var aspectWidth = asset.Width > 0 ? asset.Width : 640;
        var aspectHeight = asset.Height > 0 ? asset.Height : 360;
        var targetWidth = Math.Min(520d, aspectWidth);
        var targetHeight = Math.Max(120d, targetWidth * (aspectHeight / (double)Math.Max(1, aspectWidth)));
        var layer = CreateLayer(LayerKind.Video, asset.Name, asset.PreviewFill, string.Empty, x, y, zIndex);
        layer.Defaults.Width = targetWidth;
        layer.Defaults.Height = targetHeight;
        layer.Style.Fill = asset.PreviewFill;
        layer.Style.Stroke = asset.PreviewAccent;
        layer.Style.CornerRadius = 24d;
        layer.Media = new LayerMediaSettings
        {
            SourceMediaAssetId = asset.Id,
            PlaybackMode = MediaPlaybackMode.Stream,
            StartTime = Math.Max(0d, startTime),
            ClipDuration = Math.Max(0.1d, asset.Duration),
            ClipOffset = 0d,
            Loop = false,
            Volume = 1d
        };
        layer.Tracks = CreateDefaultTracks(layer.Defaults);
        return layer;
    }

    public static TimelineLayer CreateAudioLayer(MediaAsset asset, int zIndex, double startTime)
    {
        var layer = CreateLayer(LayerKind.Audio, asset.Name, asset.PreviewFill, string.Empty, 0, 0, zIndex);
        layer.Style.Fill = asset.PreviewFill;
        layer.Style.Stroke = asset.PreviewAccent;
        layer.Media = new LayerMediaSettings
        {
            SourceMediaAssetId = asset.Id,
            PlaybackMode = MediaPlaybackMode.Stream,
            StartTime = Math.Max(0d, startTime),
            ClipDuration = Math.Max(0.1d, asset.Duration),
            ClipOffset = 0d,
            Loop = false,
            Volume = 1d
        };
        layer.Tracks = [];
        return layer;
    }

    public static TimelineLayer DuplicateLayer(TimelineLayer source, int newZIndex)
    {
        var clone = DocumentSerializer.Clone(source);
        clone.Id = Guid.NewGuid();
        clone.Name = $"{source.Name} Copy";
        clone.ZIndex = newZIndex;

        foreach (var track in clone.Tracks)
        {
            foreach (var keyframe in track.Keyframes)
            {
                keyframe.Id = Guid.NewGuid();
            }
        }

        return clone;
    }

    public static LibraryItem CreateLibraryItemFromLayer(TimelineLayer source, string name, SymbolKind symbolKind)
    {
        var symbolLayers = CreateSymbolTimelineFromLayer(source);
        var template = BuildSymbolTemplate(symbolLayers, source.Kind, source.Style.Fill, name);

        return new LibraryItem
        {
            Name = name,
            SymbolKind = symbolKind,
            Duration = GetTimelineDuration(symbolLayers),
            FrameRate = 24,
            Template = template,
            Layers = symbolLayers,
            ButtonStates = symbolKind == SymbolKind.Button
                ? CreateDefaultButtonStates(symbolLayers)
                : []
        };
    }

    public static void AddLibraryItem(TimelineDocument document, LibraryItem item)
    {
        item.Name = LibraryManagementService.EnsureUniqueLibraryName(document, item.Name, item.Id);
        item.FolderPath = LibraryManagementService.NormalizeFolderPath(string.IsNullOrWhiteSpace(item.FolderPath)
            ? LibraryManagementService.GetDefaultFolderPath(item)
            : item.FolderPath);
        item.LinkageId = LibraryManagementService.EnsureUniqueLinkageId(document, item.LinkageId, item.Id, item.Name);
        item.BaseClassName = LibraryManagementService.NormalizeClassName(item.BaseClassName);
        item.SharedLibraryPath = LibraryManagementService.NormalizeSharedLibraryPath(item.SharedLibraryPath);
        item.SourceAssetPath = LibraryManagementService.NormalizeSourceAssetPath(item.SourceAssetPath);
        LibraryManagementService.NormalizeScale9Grid(item);
        LibraryManagementService.NormalizeRegistrationPoint(item);
        document.LibraryItems.Add(item);
    }

    public static void UpdateLibraryItemFromLayer(LibraryItem item, TimelineLayer source)
    {
        var symbolLayers = CreateSymbolTimelineFromLayer(source);
        item.Duration = GetTimelineDuration(symbolLayers);
        item.Template = BuildSymbolTemplate(symbolLayers, source.Kind, source.Style.Fill, item.Name);

        if (item.SymbolKind == SymbolKind.Button)
        {
            EnsureButtonStates(item);
            SetButtonStateLayers(item, ButtonVisualState.Up, symbolLayers);
        }
        else
        {
            item.Layers = symbolLayers;
        }
    }

    public static TimelineLayer CreateLayerFromLibraryItem(LibraryItem item, double x, double y, int zIndex)
    {
        var layer = CloneLibraryTemplate(item.Template);
        var targetOriginX = x - (layer.Defaults.Width * item.RegistrationPointX);
        var targetOriginY = y - (layer.Defaults.Height * item.RegistrationPointY);
        var offsetX = targetOriginX - layer.Defaults.X;
        var offsetY = targetOriginY - layer.Defaults.Y;

        OffsetAnimatedProperty(layer, AnimatedProperty.X, offsetX);
        OffsetAnimatedProperty(layer, AnimatedProperty.Y, offsetY);

        layer.Name = item.Name;
        layer.ZIndex = zIndex;
        layer.IsVisible = true;
        layer.IsLocked = false;
        layer.SourceLibraryItemId = item.Id;
        layer.InstanceName = CreateInstanceName(item.Name);
        layer.SymbolPlaybackMode = item.SymbolKind switch
        {
            SymbolKind.MovieClip => SymbolPlaybackMode.IndependentLoop,
            _ => SymbolPlaybackMode.SceneTime
        };
        layer.SymbolPlaybackOffset = 0;
        layer.SymbolLockedFrame = 0;
        layer.SymbolButtonState = ButtonVisualState.Up;
        layer.Behaviors = InteractionBehaviorService.CloneBehaviors(item.DefaultBehaviors);
        return layer;
    }

    private static string CreateInstanceName(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        var characters = source.Where(char.IsLetterOrDigit).ToArray();
        if (characters.Length == 0)
        {
            return string.Empty;
        }

        var normalized = new string(characters);
        return char.ToLowerInvariant(normalized[0]) + normalized[1..];
    }

    public static int SynchronizeLibraryItemInstances(TimelineDocument document, Guid libraryItemId)
    {
        var libraryItem = document.LibraryItems.FirstOrDefault(item => item.Id == libraryItemId);
        if (libraryItem is null)
        {
            return 0;
        }

        var updated = 0;
        foreach (var scene in document.Scenes)
        {
            updated += SynchronizeLibraryItemInstances(scene.Layers, libraryItem);
        }

        if (document.Scenes.Count == 0)
        {
            updated += SynchronizeLibraryItemInstances(document.Layers, libraryItem);
        }

        NormalizeZOrder(document);
        return updated;
    }

    public static List<TimelineLayer> GetEditableSymbolLayers(LibraryItem item, ButtonVisualState state)
    {
        return CloneLayerGraph(GetSymbolLayers(item, state));
    }

    public static IReadOnlyList<TimelineLayer> GetSymbolLayers(LibraryItem item, ButtonVisualState state)
    {
        if (item.SymbolKind == SymbolKind.Button)
        {
            EnsureButtonStates(item);
            return item.ButtonStates.First(buttonState => buttonState.State == state).Layers;
        }

        if (item.Layers.Count == 0)
        {
            item.Layers = [CloneLibraryTemplate(item.Template)];
        }

        return item.Layers;
    }

    public static void SetEditableSymbolLayers(
        LibraryItem item,
        IEnumerable<TimelineLayer> layers,
        double duration,
        double frameRate,
        ButtonVisualState state)
    {
        var normalizedLayers = NormalizeSymbolLayers(layers);
        item.Duration = Math.Max(0.1d, duration);
        item.FrameRate = Math.Max(1, frameRate);
        item.Template = BuildSymbolTemplate(
            normalizedLayers,
            normalizedLayers.FirstOrDefault()?.Kind ?? item.Template.Kind,
            normalizedLayers.FirstOrDefault()?.Style.Fill ?? item.Template.Style.Fill,
            item.Name);

        if (item.SymbolKind == SymbolKind.Button)
        {
            EnsureButtonStates(item);
            SetButtonStateLayers(item, state, normalizedLayers);
        }
        else
        {
            item.Layers = normalizedLayers;
        }
    }

    public static void AddLayer(TimelineDocument document, TimelineLayer layer)
    {
        document.Layers.Add(layer);
        NormalizeZOrder(document);
    }

    public static bool RemoveLayer(TimelineDocument document, Guid layerId)
    {
        var removed = document.Layers.RemoveAll(layer => layer.Id == layerId) > 0;
        if (removed)
        {
            NormalizeZOrder(document);
        }

        return removed;
    }

    public static LayerTrack GetOrCreateTrack(TimelineLayer layer, AnimatedProperty property)
    {
        var track = layer.Tracks.FirstOrDefault(item => item.Property == property);
        if (track is not null)
        {
            return track;
        }

        track = new LayerTrack { Property = property };
        layer.Tracks.Add(track);
        layer.Tracks.Sort(static (left, right) => left.Property.CompareTo(right.Property));
        return track;
    }

    public static KeyframeModel SetKeyframe(TimelineLayer layer, AnimatedProperty property, double time, double value, double duration)
    {
        var clampedTime = TimelineMath.Clamp(time, 0, duration);
        var track = GetOrCreateTrack(layer, property);
        var existing = track.Keyframes.FirstOrDefault(item => Math.Abs(item.Time - clampedTime) < TimeTolerance);

        if (existing is not null)
        {
            existing.Value = value;
            SortTrack(track);
            return existing;
        }

        var keyframe = new KeyframeModel
        {
            Time = clampedTime,
            Value = value
        };

        track.Keyframes.Add(keyframe);
        SortTrack(track);
        return keyframe;
    }

    public static bool RemoveKeyframe(TimelineLayer layer, AnimatedProperty property, Guid keyframeId)
    {
        var track = layer.Tracks.FirstOrDefault(item => item.Property == property);
        return track is not null && track.Keyframes.RemoveAll(item => item.Id == keyframeId) > 0;
    }

    public static bool MoveKeyframe(TimelineLayer layer, AnimatedProperty property, Guid keyframeId, double time, double duration)
    {
        var track = layer.Tracks.FirstOrDefault(item => item.Property == property);
        var keyframe = track?.Keyframes.FirstOrDefault(item => item.Id == keyframeId);

        if (track is null || keyframe is null)
        {
            return false;
        }

        keyframe.Time = TimelineMath.Clamp(time, 0, duration);

        var duplicates = track.Keyframes
            .Where(item => item.Id != keyframeId && Math.Abs(item.Time - keyframe.Time) < TimeTolerance)
            .ToList();

        foreach (var duplicate in duplicates)
        {
            track.Keyframes.Remove(duplicate);
        }

        SortTrack(track);
        return true;
    }

    public static void ApplyValue(TimelineLayer layer, AnimatedProperty property, double value, double time, bool createKeyframe, double duration)
    {
        if (createKeyframe)
        {
            SetKeyframe(layer, property, time, value, duration);
            return;
        }

        UpdateDefaultValue(layer, property, value);
    }

    public static ShapeKeyframeModel SetShapeKeyframe(
        TimelineLayer layer,
        double time,
        IReadOnlyList<VectorPointModel> pathPoints,
        bool isClosed,
        double duration)
    {
        var clampedTime = TimelineMath.Clamp(time, 0, duration);
        var existing = layer.ShapeKeyframes.FirstOrDefault(item => Math.Abs(item.Time - clampedTime) < TimeTolerance);
        if (existing is not null)
        {
            existing.PathPoints = VectorPathService.ClonePoints(pathPoints);
            existing.IsClosed = isClosed;
            SortShapeKeyframes(layer);
            return existing;
        }

        var shapeKeyframe = new ShapeKeyframeModel
        {
            Time = clampedTime,
            IsClosed = isClosed,
            PathPoints = VectorPathService.ClonePoints(pathPoints)
        };
        layer.ShapeKeyframes.Add(shapeKeyframe);
        SortShapeKeyframes(layer);
        return shapeKeyframe;
    }

    public static ShapeKeyframeModel CaptureShapeKeyframe(TimelineLayer layer, double time, double duration)
    {
        var sampled = TimelineInterpolationService.SamplePathGeometry(layer, time);
        return SetShapeKeyframe(layer, time, sampled.PathPoints, sampled.IsClosed, duration);
    }

    public static bool ApplyPathPoint(
        TimelineLayer layer,
        int pointIndex,
        VectorPointModel documentPoint,
        LayerSnapshot snapshot,
        double time,
        bool createKeyframe,
        double duration)
    {
        if (layer.Kind != LayerKind.Path)
        {
            return false;
        }

        if (createKeyframe)
        {
            var target = CaptureShapeKeyframe(layer, time, duration);
            return VectorPathService.TryMovePoint(target.PathPoints, snapshot with { PathPoints = target.PathPoints }, pointIndex, documentPoint);
        }

        return VectorPathService.TryMovePoint(layer, snapshot, pointIndex, documentPoint);
    }

    public static bool ApplyPathClosed(TimelineLayer layer, bool isClosed, double time, bool createKeyframe, double duration)
    {
        if (layer.Kind != LayerKind.Path)
        {
            return false;
        }

        if (createKeyframe)
        {
            var target = CaptureShapeKeyframe(layer, time, duration);
            target.IsClosed = isClosed;
            return true;
        }

        if (layer.Style.IsClosed == isClosed)
        {
            return false;
        }

        layer.Style.IsClosed = isClosed;
        return true;
    }

    public static bool RemoveShapeKeyframe(TimelineLayer layer, Guid keyframeId)
    {
        return layer.ShapeKeyframes.RemoveAll(item => item.Id == keyframeId) > 0;
    }

    public static void UpdateDefaultValue(TimelineLayer layer, AnimatedProperty property, double value)
    {
        var normalizedValue = property switch
        {
            AnimatedProperty.Width or AnimatedProperty.Height => Math.Max(24d, value),
            AnimatedProperty.Opacity => TimelineMath.Clamp(value, 0d, 1d),
            _ => value
        };

        switch (property)
        {
            case AnimatedProperty.X:
                layer.Defaults.X = normalizedValue;
                break;
            case AnimatedProperty.Y:
                layer.Defaults.Y = normalizedValue;
                break;
            case AnimatedProperty.Width:
                layer.Defaults.Width = normalizedValue;
                break;
            case AnimatedProperty.Height:
                layer.Defaults.Height = normalizedValue;
                break;
            case AnimatedProperty.ScaleX:
                layer.Defaults.ScaleX = normalizedValue;
                break;
            case AnimatedProperty.ScaleY:
                layer.Defaults.ScaleY = normalizedValue;
                break;
            case AnimatedProperty.SkewX:
                layer.Defaults.SkewX = normalizedValue;
                break;
            case AnimatedProperty.SkewY:
                layer.Defaults.SkewY = normalizedValue;
                break;
            case AnimatedProperty.Rotation:
                layer.Defaults.Rotation = normalizedValue;
                break;
            case AnimatedProperty.Opacity:
                layer.Defaults.Opacity = normalizedValue;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(property), property, null);
        }

        var track = layer.Tracks.FirstOrDefault(item => item.Property == property);
        if (track is null)
        {
            return;
        }

        var defaultKeyframe = track.Keyframes.FirstOrDefault(item => Math.Abs(item.Time) < TimeTolerance);
        if (defaultKeyframe is null)
        {
            track.Keyframes.Add(new KeyframeModel
            {
                Time = 0d,
                Value = normalizedValue
            });
        }
        else
        {
            defaultKeyframe.Value = normalizedValue;
        }

        SortTrack(track);
    }

    public static void NormalizeZOrder(TimelineDocument document)
    {
        for (var index = 0; index < document.Layers.Count; index++)
        {
            document.Layers[index].ZIndex = index;
        }

        LayerHierarchyService.Normalize(document.Layers);
    }

    public static void BringForward(TimelineDocument document, Guid layerId)
    {
        var ordered = document.Layers.OrderBy(layer => layer.ZIndex).ToList();
        var index = ordered.FindIndex(layer => layer.Id == layerId);
        if (index < 0 || index == ordered.Count - 1)
        {
            return;
        }

        (ordered[index], ordered[index + 1]) = (ordered[index + 1], ordered[index]);
        document.Layers = ordered;
        NormalizeZOrder(document);
    }

    public static void SendBackward(TimelineDocument document, Guid layerId)
    {
        var ordered = document.Layers.OrderBy(layer => layer.ZIndex).ToList();
        var index = ordered.FindIndex(layer => layer.Id == layerId);
        if (index <= 0)
        {
            return;
        }

        (ordered[index], ordered[index - 1]) = (ordered[index - 1], ordered[index]);
        document.Layers = ordered;
        NormalizeZOrder(document);
    }

    private static List<LayerTrack> CreateDefaultTracks(LayerDefaults defaults)
    {
        return
        [
            new LayerTrack
            {
                Property = AnimatedProperty.X,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.X }
                ]
            },
            new LayerTrack
            {
                Property = AnimatedProperty.Y,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.Y }
                ]
            },
            new LayerTrack
            {
                Property = AnimatedProperty.Width,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.Width }
                ]
            },
            new LayerTrack
            {
                Property = AnimatedProperty.Height,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.Height }
                ]
            },
            new LayerTrack
            {
                Property = AnimatedProperty.ScaleX,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.ScaleX }
                ]
            },
            new LayerTrack
            {
                Property = AnimatedProperty.ScaleY,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.ScaleY }
                ]
            },
            new LayerTrack
            {
                Property = AnimatedProperty.SkewX,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.SkewX }
                ]
            },
            new LayerTrack
            {
                Property = AnimatedProperty.SkewY,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.SkewY }
                ]
            },
            new LayerTrack
            {
                Property = AnimatedProperty.Rotation,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.Rotation }
                ]
            },
            new LayerTrack
            {
                Property = AnimatedProperty.Opacity,
                Keyframes =
                [
                    new KeyframeModel { Time = 0, Value = defaults.Opacity }
                ]
            }
        ];
    }

    private static TimelineLayer CloneLibraryTemplate(TimelineLayer source)
    {
        var clone = DocumentSerializer.Clone(source);
        clone.Id = Guid.NewGuid();
        clone.SourceLibraryItemId = null;
        clone.IsLocked = false;

        foreach (var track in clone.Tracks)
        {
            foreach (var keyframe in track.Keyframes)
            {
                keyframe.Id = Guid.NewGuid();
            }
        }

        return clone;
    }

    private static List<TimelineLayer> CreateSymbolTimelineFromLayer(TimelineLayer source)
    {
        return NormalizeSymbolLayers([CloneLibraryTemplate(source)]);
    }

    private static List<TimelineLayer> NormalizeSymbolLayers(IEnumerable<TimelineLayer> layers)
    {
        var clone = CloneLayerGraph(layers);
        if (clone.Count == 0)
        {
            return [];
        }

        var minX = clone
            .Select(layer => TimelineInterpolationService.SampleLayer(layer, 0))
            .Min(snapshot => snapshot.X);
        var minY = clone
            .Select(layer => TimelineInterpolationService.SampleLayer(layer, 0))
            .Min(snapshot => snapshot.Y);

        foreach (var layer in clone)
        {
            OffsetAnimatedProperty(layer, AnimatedProperty.X, -minX);
            OffsetAnimatedProperty(layer, AnimatedProperty.Y, -minY);
        }

        return clone;
    }

    private static List<TimelineLayer> CloneLayerGraph(IEnumerable<TimelineLayer> layers)
    {
        var clone = DocumentSerializer.Clone(layers.ToList());
        foreach (var layer in clone)
        {
            layer.Id = Guid.NewGuid();
            layer.SourceLibraryItemId = layer.SourceLibraryItemId;
            foreach (var track in layer.Tracks)
            {
                foreach (var keyframe in track.Keyframes)
                {
                    keyframe.Id = Guid.NewGuid();
                }
            }
        }

        return clone;
    }

    private static TimelineLayer BuildSymbolTemplate(
        IReadOnlyList<TimelineLayer> layers,
        LayerKind fallbackKind,
        string fallbackFill,
        string name)
    {
        if (layers.Count == 0)
        {
            return CreateLayer(fallbackKind, name, fallbackFill, string.Empty, 0, 0, 0);
        }

        var snapshots = layers.Select(layer => TimelineInterpolationService.SampleLayer(layer, 0)).ToList();
        var minX = snapshots.Min(snapshot => snapshot.X);
        var minY = snapshots.Min(snapshot => snapshot.Y);
        var maxRight = snapshots.Max(snapshot => snapshot.X + snapshot.Width);
        var maxBottom = snapshots.Max(snapshot => snapshot.Y + snapshot.Height);
        var first = layers[0];

        var template = CreateLayer(
            first.Kind,
            name,
            first.Style.Fill,
            first.Style.Text,
            0,
            0,
            0);
        template.Defaults.X = 0;
        template.Defaults.Y = 0;
        template.Defaults.Width = Math.Max(24, maxRight - minX);
        template.Defaults.Height = Math.Max(24, maxBottom - minY);
        template.Defaults.Rotation = 0;
        template.Defaults.Opacity = 1;
        template.Style = DocumentSerializer.Clone(first.Style);
        if (template.Kind == LayerKind.Text)
        {
            template.Style.Text = name;
        }

        template.Tracks = CreateDefaultTracks(template.Defaults);
        return template;
    }

    private static AvaloniaControlSettings CreateDefaultAvaloniaControlSettings(AvaloniaControlKind controlKind, string text)
    {
        return controlKind switch
        {
            AvaloniaControlKind.Button => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Content = string.IsNullOrWhiteSpace(text) ? "Button" : text
            },
            AvaloniaControlKind.TextBlock => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Content = string.IsNullOrWhiteSpace(text) ? "TextBlock" : text
            },
            AvaloniaControlKind.TextBox => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Content = string.IsNullOrWhiteSpace(text) ? "Type here" : text,
                SecondaryContent = "Watermark"
            },
            AvaloniaControlKind.CheckBox => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Content = string.IsNullOrWhiteSpace(text) ? "CheckBox" : text
            },
            AvaloniaControlKind.ToggleButton => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Content = string.IsNullOrWhiteSpace(text) ? "Toggle" : text
            },
            AvaloniaControlKind.Slider => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Value = 68d,
                Minimum = 0d,
                Maximum = 100d
            },
            AvaloniaControlKind.ProgressBar => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Value = 62d,
                Minimum = 0d,
                Maximum = 100d
            },
            AvaloniaControlKind.Image => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Content = "Image",
                SecondaryContent = "Source"
            },
            AvaloniaControlKind.ComboBox => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Content = string.IsNullOrWhiteSpace(text) ? "Selected Item" : text,
                SecondaryContent = "Option A|Option B|Option C"
            },
            AvaloniaControlKind.ListBox => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Content = string.IsNullOrWhiteSpace(text) ? "Item 1|Item 2|Item 3" : text
            },
            AvaloniaControlKind.TabControl => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Content = string.IsNullOrWhiteSpace(text) ? "Overview|Settings|Export" : text,
                SecondaryContent = "Overview"
            },
            AvaloniaControlKind.Grid => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Content = string.IsNullOrWhiteSpace(text) ? "2x2 Grid" : text
            },
            AvaloniaControlKind.StackPanel => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Content = string.IsNullOrWhiteSpace(text) ? "Vertical Stack" : text,
                SecondaryContent = "Item A|Item B|Item C"
            },
            AvaloniaControlKind.PathIcon => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Content = string.IsNullOrWhiteSpace(text) ? "Icon" : text,
                SecondaryContent = "M 12 2 L 22 22 L 2 22 Z"
            },
            AvaloniaControlKind.Panel => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Content = string.IsNullOrWhiteSpace(text) ? "Panel" : text
            },
            _ => new AvaloniaControlSettings
            {
                Kind = controlKind,
                Content = string.IsNullOrWhiteSpace(text) ? "Border" : text
            }
        };
    }

    private static double GetTimelineDuration(IEnumerable<TimelineLayer> layers)
    {
        var duration = layers
            .SelectMany(layer => layer.Tracks)
            .SelectMany(track => track.Keyframes)
            .DefaultIfEmpty()
            .Max(keyframe => keyframe?.Time ?? 0);
        return Math.Max(0.1d, duration);
    }

    private static List<SymbolButtonStateModel> CreateDefaultButtonStates(IReadOnlyList<TimelineLayer> symbolLayers)
    {
        return
        [
            new SymbolButtonStateModel
            {
                State = ButtonVisualState.Up,
                Layers = CloneLayerGraph(symbolLayers)
            },
            new SymbolButtonStateModel
            {
                State = ButtonVisualState.Over,
                Layers = CloneLayerGraph(symbolLayers)
            },
            new SymbolButtonStateModel
            {
                State = ButtonVisualState.Down,
                Layers = CloneLayerGraph(symbolLayers)
            },
            new SymbolButtonStateModel
            {
                State = ButtonVisualState.Hit,
                Layers = CloneLayerGraph(symbolLayers)
            }
        ];
    }

    private static void EnsureButtonStates(LibraryItem item)
    {
        if (item.ButtonStates.Count == 0)
        {
            item.ButtonStates = CreateDefaultButtonStates(item.Layers.Count > 0 ? item.Layers : [CloneLibraryTemplate(item.Template)]);
        }

        foreach (var state in Enum.GetValues<ButtonVisualState>())
        {
            if (item.ButtonStates.Any(buttonState => buttonState.State == state))
            {
                continue;
            }

            var seed = item.ButtonStates.FirstOrDefault(buttonState => buttonState.State == ButtonVisualState.Up)?.Layers
                ?? item.Layers;
            item.ButtonStates.Add(new SymbolButtonStateModel
            {
                State = state,
                Layers = CloneLayerGraph(seed)
            });
        }
    }

    private static void SetButtonStateLayers(LibraryItem item, ButtonVisualState state, IReadOnlyList<TimelineLayer> layers)
    {
        var targetState = item.ButtonStates.First(buttonState => buttonState.State == state);
        targetState.Layers = CloneLayerGraph(layers);
    }

    private static int SynchronizeLibraryItemInstances(List<TimelineLayer> layers, LibraryItem libraryItem)
    {
        var updated = 0;
        for (var index = 0; index < layers.Count; index++)
        {
            var layer = layers[index];
            if (layer.SourceLibraryItemId != libraryItem.Id)
            {
                continue;
            }

            if (layer.Kind == libraryItem.Template.Kind)
            {
                layer.Style = DocumentSerializer.Clone(libraryItem.Template.Style);
            }

            if (libraryItem.IsComponent)
            {
                layer.Behaviors = InteractionBehaviorService.CloneBehaviors(libraryItem.DefaultBehaviors);
            }

            updated++;
        }

        return updated;
    }

    private static void OffsetAnimatedProperty(TimelineLayer layer, AnimatedProperty property, double delta)
    {
        if (Math.Abs(delta) < TimeTolerance)
        {
            return;
        }

        switch (property)
        {
            case AnimatedProperty.X:
                layer.Defaults.X += delta;
                break;
            case AnimatedProperty.Y:
                layer.Defaults.Y += delta;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(property), property, null);
        }

        var track = layer.Tracks.FirstOrDefault(item => item.Property == property);
        if (track is null)
        {
            return;
        }

        foreach (var keyframe in track.Keyframes)
        {
            keyframe.Value += delta;
        }

        SortTrack(track);
    }

    private static void SortTrack(LayerTrack track)
    {
        track.Keyframes.Sort(static (left, right) => left.Time.CompareTo(right.Time));
    }

    private static void SortShapeKeyframes(TimelineLayer layer)
    {
        layer.ShapeKeyframes.Sort(static (left, right) => left.Time.CompareTo(right.Time));
    }
}
