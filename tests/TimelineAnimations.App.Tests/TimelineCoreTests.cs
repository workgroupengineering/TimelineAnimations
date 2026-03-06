using System.Text.Json;
using TimelineAnimations.App.Services;
using TimelineAnimations.App.ViewModels;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Tests;

public class TimelineCoreTests
{
    [Fact]
    public void SampleProperty_Interpolates_BetweenNeighbouringKeyframes()
    {
        var layer = new TimelineLayer
        {
            Defaults = new LayerDefaults { X = 120 },
            Tracks =
            [
                new LayerTrack
                {
                    Property = AnimatedProperty.X,
                    Keyframes =
                    [
                        new KeyframeModel { Time = 1, Value = 200 },
                        new KeyframeModel { Time = 3, Value = 400 }
                    ]
                }
            ]
        };

        var beforeFirst = TimelineInterpolationService.SampleProperty(layer, AnimatedProperty.X, 0.5);
        var between = TimelineInterpolationService.SampleProperty(layer, AnimatedProperty.X, 2);
        var afterLast = TimelineInterpolationService.SampleProperty(layer, AnimatedProperty.X, 4);

        Assert.Equal(120, beforeFirst);
        Assert.Equal(300, between);
        Assert.Equal(400, afterLast);
    }

    [Fact]
    public void MoveKeyframe_ReordersAndMergesConflictingTimes()
    {
        var layer = new TimelineLayer
        {
            Tracks =
            [
                new LayerTrack
                {
                    Property = AnimatedProperty.Opacity,
                    Keyframes =
                    [
                        new KeyframeModel { Time = 0, Value = 0.2 },
                        new KeyframeModel { Time = 1, Value = 0.5 },
                        new KeyframeModel { Time = 2, Value = 1.0 }
                    ]
                }
            ]
        };

        var movedId = layer.Tracks[0].Keyframes[2].Id;
        var moved = TimelineEditingService.MoveKeyframe(layer, AnimatedProperty.Opacity, movedId, 1, 6);

        Assert.True(moved);
        Assert.Equal(2, layer.Tracks[0].Keyframes.Count);
        Assert.Collection(
            layer.Tracks[0].Keyframes,
            first => Assert.Equal(0, first.Time),
            second => Assert.Equal(1, second.Time));
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_SampleComposition()
    {
        var document = SampleProjectFactory.Create();
        document.Layers[0].IsLocked = true;
        document.Layers[1].IsVisible = false;
        var json = DocumentSerializer.ToJson(document);
        var restored = DocumentSerializer.FromJson(json);

        Assert.Equal(document.Name, restored.Name);
        Assert.Equal(document.Duration, restored.Duration);
        Assert.Equal(document.Layers.Count, restored.Layers.Count);
        Assert.Equal(document.Scenes.Count, restored.Scenes.Count);
        Assert.Equal(document.LibraryItems.Count, restored.LibraryItems.Count);
        Assert.Equal(document.MediaAssets.Count, restored.MediaAssets.Count);
        Assert.Equal(document.ActiveSceneId, restored.ActiveSceneId);
        Assert.True(restored.Layers[0].IsLocked);
        Assert.False(restored.Layers[1].IsVisible);
        Assert.Equal(document.Layers[0].SourceLibraryItemId, restored.Layers[0].SourceLibraryItemId);
        Assert.Equal(document.Layers[2].Style.Text, restored.Layers[2].Style.Text);
        Assert.Equal(document.Layers[1].Tracks[0].Keyframes.Count, restored.Layers[1].Tracks[0].Keyframes.Count);
        Assert.Equal(document.Scenes[0].FrameRate, restored.Scenes[0].FrameRate);
        Assert.Equal(document.Scenes[0].FrameLabels.Count, restored.Scenes[0].FrameLabels.Count);
        Assert.Equal(document.Scenes[0].Layers[2].FrameSpans.Count, restored.Scenes[0].Layers[2].FrameSpans.Count);
        Assert.Equal(document.Layers[4].Media.SourceMediaAssetId, restored.Layers[4].Media.SourceMediaAssetId);
        Assert.Equal(document.Layers[5].Kind, restored.Layers[5].Kind);
    }

    [Fact]
    public void UndoRedoStack_TraversesRecordedStates()
    {
        var history = new UndoRedoStack<int>(1);

        Assert.True(history.Record(2));
        Assert.True(history.Record(3));
        Assert.True(history.TryUndo(out var undoState));
        Assert.Equal(2, undoState);
        Assert.True(history.TryUndo(out undoState));
        Assert.Equal(1, undoState);
        Assert.True(history.TryRedo(out var redoState));
        Assert.Equal(2, redoState);
    }

    [Fact]
    public void SampleProperty_UsesHoldEasing_FromNextKeyframe()
    {
        var layer = new TimelineLayer
        {
            Defaults = new LayerDefaults { Opacity = 0.1 },
            Tracks =
            [
                new LayerTrack
                {
                    Property = AnimatedProperty.Opacity,
                    Keyframes =
                    [
                        new KeyframeModel { Time = 0, Value = 0.1 },
                        new KeyframeModel { Time = 1, Value = 1.0, Easing = EasingKind.Hold }
                    ]
                }
            ]
        };

        var sampled = TimelineInterpolationService.SampleProperty(layer, AnimatedProperty.Opacity, 0.6);

        Assert.Equal(0.1, sampled);
    }

    [Fact]
    public void TimelineEasingService_BackOut_OvershootsLinearCurve()
    {
        var linear = TimelineEasingService.Apply(EasingKind.Linear, 0.75);
        var backOut = TimelineEasingService.Apply(EasingKind.BackOut, 0.75);

        Assert.True(backOut > linear);
    }

    [Fact]
    public void CanvasSnapService_SnapsMove_ToStageCenter()
    {
        var snapped = CanvasSnapService.SnapMove(
            x: 495,
            y: 214,
            width: 300,
            height: 100,
            canvasWidth: 1280,
            canvasHeight: 720,
            snapEnabled: true);

        Assert.Equal(490, snapped.X);
        Assert.Equal(220, snapped.Y);
        Assert.Equal(640, snapped.VerticalGuide);
        Assert.Null(snapped.HorizontalGuide);
    }

    [Fact]
    public void CanvasSnapService_SnapsResize_ToGrid()
    {
        var snapped = CanvasSnapService.SnapResize(
            x: 143,
            y: 118,
            width: 237,
            height: 151,
            canvasWidth: 1280,
            canvasHeight: 720,
            snapEnabled: true);

        Assert.Equal(140, snapped.X);
        Assert.Equal(120, snapped.Y);
        Assert.Equal(240, snapped.Width);
        Assert.Equal(160, snapped.Height);
    }

    [Fact]
    public void CreateLayerFromLibraryItem_OffsetsPositionTracks()
    {
        var sourceLayer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 100, 80, 0);
        TimelineEditingService.SetKeyframe(sourceLayer, AnimatedProperty.X, 1, 180, 6);
        TimelineEditingService.SetKeyframe(sourceLayer, AnimatedProperty.Y, 1, 140, 6);
        var libraryItem = TimelineEditingService.CreateLibraryItemFromLayer(sourceLayer, "Card Symbol", SymbolKind.Graphic);

        var instance = TimelineEditingService.CreateLayerFromLibraryItem(libraryItem, 300, 240, 1);
        var document = new TimelineDocument
        {
            Duration = 2,
            LibraryItems = [libraryItem],
            Layers = [instance]
        };
        var rendered = SymbolRenderService.BuildRenderSamples(document, document.Layers, 1, document.Duration, 24);
        var sample = Assert.Single(rendered);

        Assert.Equal(libraryItem.Id, instance.SourceLibraryItemId);
        Assert.Equal(300, instance.Defaults.X);
        Assert.Equal(240, instance.Defaults.Y);
        Assert.Equal(380, sample.Snapshot.X);
        Assert.Equal(300, sample.Snapshot.Y);
    }

    [Fact]
    public void SynchronizeLibraryItemInstances_UpdatesLinkedLayers()
    {
        var document = new TimelineDocument();
        var sourceLayer = TimelineEditingService.CreateLayer(LayerKind.Text, "Caption", "#F7F5ED", "HELLO", 120, 90, 0);
        TimelineEditingService.SetKeyframe(sourceLayer, AnimatedProperty.X, 1, 200, 6);
        var libraryItem = TimelineEditingService.CreateLibraryItemFromLayer(sourceLayer, "Caption Symbol", SymbolKind.MovieClip);
        TimelineEditingService.AddLibraryItem(document, libraryItem);

        var instance = TimelineEditingService.CreateLayerFromLibraryItem(libraryItem, 420, 220, 0);
        instance.Name = "Caption Instance";
        TimelineEditingService.AddLayer(document, instance);

        sourceLayer.Style.Fill = "#61E6FF";
        TimelineEditingService.SetKeyframe(sourceLayer, AnimatedProperty.X, 1, 240, 6);
        TimelineEditingService.UpdateLibraryItemFromLayer(libraryItem, sourceLayer);

        var updatedInstances = TimelineEditingService.SynchronizeLibraryItemInstances(document, libraryItem.Id);
        var synchronized = document.Layers.Single();
        var rendered = SymbolRenderService.BuildRenderSamples(document, document.Layers, 0.5, 3, 24);
        var sample = Assert.Single(rendered);

        Assert.Equal(1, updatedInstances);
        Assert.Equal("Caption Instance", synchronized.Name);
        Assert.Equal("#61E6FF", sample.Snapshot.Fill);
        Assert.Equal(420, synchronized.Defaults.X);
        Assert.Equal(480, sample.Snapshot.X);
    }

    [Fact]
    public void SceneEditingService_EnsuresLegacyDocumentIntoSingleScene()
    {
        var document = new TimelineDocument
        {
            Name = "Legacy",
            Duration = 5,
            CanvasWidth = 1024,
            CanvasHeight = 576,
            BackgroundFrom = "#111111",
            BackgroundTo = "#222222",
            Layers =
            [
                TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 100, 80, 0)
            ]
        };

        SceneEditingService.EnsureScenes(document);

        Assert.Single(document.Scenes);
        Assert.Equal(document.ActiveSceneId, document.Scenes[0].Id);
        Assert.Equal(5, document.Scenes[0].Duration);
        Assert.Single(document.Scenes[0].Layers);
    }

    [Fact]
    public void DuplicateScene_CreatesIndependentLayerGraph()
    {
        var scene = new SceneModel
        {
            Name = "Intro",
            Duration = 4,
            Layers =
            [
                TimelineEditingService.CreateLayer(LayerKind.Text, "Caption", "#FFFFFF", "HELLO", 100, 80, 0)
            ]
        };

        var duplicate = SceneEditingService.DuplicateScene(scene, "Intro Copy");

        Assert.Equal("Intro Copy", duplicate.Name);
        Assert.Single(duplicate.Layers);
        Assert.NotEqual(scene.Layers[0].Id, duplicate.Layers[0].Id);
        Assert.NotEqual(
            scene.Layers[0].Tracks[0].Keyframes[0].Id,
            duplicate.Layers[0].Tracks[0].Keyframes[0].Id);
    }

    [Fact]
    public void InsertContentKeyframe_SplitsContentSpan()
    {
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 120, 80, 0);

        var span = FrameTimelineService.InsertContentKeyframe(layer, 12, 49);
        var spans = FrameTimelineService.GetEffectiveSpans(layer, 49);

        Assert.False(span.IsBlank);
        Assert.Equal(2, spans.Count);
        Assert.Equal((0, 11, false), (spans[0].StartFrame, spans[0].EndFrame, spans[0].IsBlank));
        Assert.Equal((12, 48, false), (spans[1].StartFrame, spans[1].EndFrame, spans[1].IsBlank));
    }

    [Fact]
    public void GetSampleTime_UsesContentSpanStartForHeldFrames()
    {
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 120, 80, 0);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.X, 0, 100, 2);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.X, 0.5, 220, 2);
        FrameTimelineService.InsertContentKeyframe(layer, 12, 49);

        var heldTime = FrameTimelineService.GetSampleTime(layer, 0.75, 24, 49);
        var sampled = FrameTimelineService.SampleProperty(layer, AnimatedProperty.X, 0.75, 24, 49);

        Assert.Equal(0.5, heldTime);
        Assert.Equal(220, sampled);
    }

    [Fact]
    public void CopyPasteRange_CopiesExposureAndTrackKeys()
    {
        var source = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 120, 80, 0);
        TimelineEditingService.SetKeyframe(source, AnimatedProperty.X, 0, 120, 3);
        TimelineEditingService.SetKeyframe(source, AnimatedProperty.X, 0.5, 320, 3);
        TimelineEditingService.SetKeyframe(source, AnimatedProperty.X, 1, 520, 3);
        FrameTimelineService.InsertBlankKeyframe(source, 12, 73);
        FrameTimelineService.InsertContentKeyframe(source, 24, 73);

        var clipboard = FrameTimelineService.CopyRange(source, 12, 24, 73, 24);

        var target = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Target", "#24E5C1", string.Empty, 120, 80, 0);
        TimelineEditingService.SetKeyframe(target, AnimatedProperty.X, 0, 40, 3);
        FrameTimelineService.PasteRange(target, 36, clipboard, 73, 24);

        var pastedSpans = FrameTimelineService.GetEffectiveSpans(target, 73)
            .Where(span => span.EndFrame >= 36 && span.StartFrame <= 48)
            .ToList();
        var pastedValue = FrameTimelineService.SampleProperty(target, AnimatedProperty.X, 2.0, 24, 73);

        Assert.Equal(2, pastedSpans.Count);
        Assert.True(pastedSpans[0].IsBlank);
        Assert.False(pastedSpans[1].IsBlank);
        Assert.Equal(520, pastedValue);
    }

    [Fact]
    public void SetFrameLabel_AddsAndRemovesSceneMarkers()
    {
        var scene = new SceneModel { FrameRate = 24, Duration = 2 };

        Assert.True(FrameTimelineService.SetFrameLabel(scene, 12, "intro", 49));
        Assert.Equal("intro", FrameTimelineService.GetFrameLabel(scene, 12)?.Name);
        Assert.True(FrameTimelineService.RemoveFrameLabel(scene, 12));
        Assert.Null(FrameTimelineService.GetFrameLabel(scene, 12));
    }

    [Fact]
    public void CreateLibraryItemFromLayer_LocalizesNestedTimeline()
    {
        var source = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 100, 80, 0);
        TimelineEditingService.SetKeyframe(source, AnimatedProperty.X, 1, 180, 2);
        TimelineEditingService.SetKeyframe(source, AnimatedProperty.Y, 1, 140, 2);

        var libraryItem = TimelineEditingService.CreateLibraryItemFromLayer(source, "Card Symbol", SymbolKind.Graphic);
        var nestedLayer = libraryItem.Layers.Single();

        Assert.Equal(0, libraryItem.Template.Defaults.X);
        Assert.Equal(0, libraryItem.Template.Defaults.Y);
        Assert.Equal(0, nestedLayer.Defaults.X);
        Assert.Equal(0, nestedLayer.Defaults.Y);
        Assert.Equal(80, nestedLayer.Tracks.Single(track => track.Property == AnimatedProperty.X).Keyframes.Last().Value);
        Assert.Equal(60, nestedLayer.Tracks.Single(track => track.Property == AnimatedProperty.Y).Keyframes.Last().Value);
    }

    [Fact]
    public void ButtonSymbol_PersistsIndependentStateLayers()
    {
        var source = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Button", "#24E5C1", string.Empty, 80, 60, 0);
        var libraryItem = TimelineEditingService.CreateLibraryItemFromLayer(source, "CTA Button", SymbolKind.Button);
        var downLayers = TimelineEditingService.GetEditableSymbolLayers(libraryItem, ButtonVisualState.Down);

        downLayers[0].Style.Fill = "#FF8A4C";
        TimelineEditingService.SetEditableSymbolLayers(libraryItem, downLayers, 1.5, 24, ButtonVisualState.Down);

        Assert.Equal(4, libraryItem.ButtonStates.Count);
        Assert.Equal("#24E5C1", libraryItem.ButtonStates.First(state => state.State == ButtonVisualState.Up).Layers[0].Style.Fill);
        Assert.Equal("#FF8A4C", libraryItem.ButtonStates.First(state => state.State == ButtonVisualState.Down).Layers[0].Style.Fill);
    }

    [Fact]
    public void SymbolRenderService_RendersMovieClipInstancesWithIndependentTimeline()
    {
        var source = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Pulse", "#24E5C1", string.Empty, 100, 80, 0);
        TimelineEditingService.SetKeyframe(source, AnimatedProperty.X, 1, 220, 2);
        var libraryItem = TimelineEditingService.CreateLibraryItemFromLayer(source, "Pulse Clip", SymbolKind.MovieClip);

        var document = new TimelineDocument
        {
            Duration = 4,
            LibraryItems = [libraryItem]
        };

        var instance = TimelineEditingService.CreateLayerFromLibraryItem(libraryItem, 300, 160, 0);
        TimelineEditingService.AddLayer(document, instance);

        var rendered = SymbolRenderService.BuildRenderSamples(document, document.Layers, 2.5, document.Duration, 24);
        var sample = Assert.Single(rendered);

        Assert.Equal(LayerKind.Rectangle, sample.Kind);
        Assert.Equal(360, sample.Snapshot.X);
    }

    [Fact]
    public void CreatePathLayer_NormalizesPointsIntoLayerBounds()
    {
        var layer = TimelineEditingService.CreatePathLayer(
            "Stroke",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 120, Y = 80 },
                new VectorPointModel { X = 260, Y = 140 },
                new VectorPointModel { X = 520, Y = 180 }
            ],
            isClosed: false,
            zIndex: 0,
            strokeThickness: 6);

        Assert.Equal(LayerKind.Path, layer.Kind);
        Assert.Equal(120, layer.Defaults.X);
        Assert.Equal(80, layer.Defaults.Y);
        Assert.Equal(400, layer.Defaults.Width);
        Assert.Equal(100, layer.Defaults.Height);
        Assert.Equal(3, layer.Style.PathPoints.Count);
        Assert.Equal(0, layer.Style.PathPoints[0].X);
        Assert.Equal(0.35, layer.Style.PathPoints[1].X, 2);
        Assert.Equal(1, layer.Style.PathPoints[2].X);
    }

    [Fact]
    public void VectorPathService_TryMovePoint_UpdatesNormalizedPoint()
    {
        var layer = TimelineEditingService.CreatePathLayer(
            "Stroke",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 100, Y = 50 },
                new VectorPointModel { X = 300, Y = 150 }
            ],
            isClosed: false,
            zIndex: 0,
            strokeThickness: 4);
        var snapshot = TimelineInterpolationService.SampleLayer(layer, 0);

        var changed = VectorPathService.TryMovePoint(
            layer,
            snapshot,
            1,
            new VectorPointModel { X = 250, Y = 100 });

        Assert.True(changed);
        Assert.Equal(0.75, layer.Style.PathPoints[1].X, 2);
        Assert.Equal(0.5, layer.Style.PathPoints[1].Y, 2);
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_VectorLayerStyle()
    {
        var document = new TimelineDocument
        {
            Layers =
            [
                TimelineEditingService.CreatePathLayer(
                    "Stroke",
                    "#24E5C1",
                    "#93A6D8",
                    [
                        new VectorPointModel { X = 100, Y = 50 },
                        new VectorPointModel { X = 200, Y = 160 },
                        new VectorPointModel { X = 320, Y = 90 }
                    ],
                    isClosed: true,
                    zIndex: 0,
                    strokeThickness: 8,
                    useGradient: true,
                    gradientFrom: "#24E5C1",
                    gradientTo: "#61E6FF")
            ]
        };

        var json = DocumentSerializer.ToJson(document);
        var restored = DocumentSerializer.FromJson(json);
        var pathLayer = Assert.Single(restored.Layers);

        Assert.Equal(LayerKind.Path, pathLayer.Kind);
        Assert.True(pathLayer.Style.UseGradient);
        Assert.True(pathLayer.Style.IsClosed);
        Assert.Equal(8, pathLayer.Style.StrokeThickness);
        Assert.Equal(3, pathLayer.Style.PathPoints.Count);
        Assert.Equal("#61E6FF", pathLayer.Style.GradientTo);
    }

    [Fact]
    public void TimelineEasingService_CustomBezier_UsesControlPoints()
    {
        var keyframe = new KeyframeModel
        {
            Easing = EasingKind.Custom,
            ControlPoint1X = 0.15,
            ControlPoint1Y = 0.9,
            ControlPoint2X = 0.8,
            ControlPoint2Y = 0.1
        };

        var linear = TimelineEasingService.Apply(EasingKind.Linear, 0.35);
        var custom = TimelineEasingService.Apply(keyframe, 0.35);

        Assert.NotEqual(linear, custom);
        Assert.InRange(custom, 0d, 1d);
    }

    [Fact]
    public void SampleLayer_Interpolates_PathShapeKeyframes()
    {
        var layer = TimelineEditingService.CreatePathLayer(
            "Morph",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 100, Y = 100 },
                new VectorPointModel { X = 300, Y = 100 },
                new VectorPointModel { X = 300, Y = 220 }
            ],
            isClosed: false,
            zIndex: 0,
            strokeThickness: 6);

        TimelineEditingService.SetShapeKeyframe(
            layer,
            0,
            [
                new VectorPointModel { X = 0, Y = 0 },
                new VectorPointModel { X = 1, Y = 0 },
                new VectorPointModel { X = 1, Y = 1 }
            ],
            isClosed: false,
            duration: 2);
        TimelineEditingService.SetShapeKeyframe(
            layer,
            1,
            [
                new VectorPointModel { X = 0, Y = 0.5 },
                new VectorPointModel { X = 1, Y = 0.25 },
                new VectorPointModel { X = 0.5, Y = 1 }
            ],
            isClosed: true,
            duration: 2);

        var sampled = TimelineInterpolationService.SampleLayer(layer, 0.5);

        Assert.Equal(3, sampled.PathPoints.Count);
        Assert.Equal(0.25, sampled.PathPoints[0].Y, 2);
        Assert.Equal(0.75, sampled.PathPoints[2].X, 2);
        Assert.False(sampled.IsClosed);
    }

    [Fact]
    public void CopyPasteRange_PreservesShapeMorphKeys()
    {
        var source = TimelineEditingService.CreatePathLayer(
            "Morph",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 100, Y = 100 },
                new VectorPointModel { X = 300, Y = 100 },
                new VectorPointModel { X = 300, Y = 220 }
            ],
            isClosed: false,
            zIndex: 0,
            strokeThickness: 6);
        TimelineEditingService.SetShapeKeyframe(
            source,
            0.5,
            [
                new VectorPointModel { X = 0, Y = 0.4 },
                new VectorPointModel { X = 1, Y = 0.2 },
                new VectorPointModel { X = 0.7, Y = 1 }
            ],
            isClosed: true,
            duration: 3);

        var clipboard = FrameTimelineService.CopyRange(source, 0, 24, 73, 24);
        var target = TimelineEditingService.CreatePathLayer(
            "Target",
            "#24E5C1",
            "#FFFFFF",
            [
                new VectorPointModel { X = 100, Y = 100 },
                new VectorPointModel { X = 300, Y = 100 },
                new VectorPointModel { X = 300, Y = 220 }
            ],
            isClosed: false,
            zIndex: 0,
            strokeThickness: 6);

        FrameTimelineService.PasteRange(target, 24, clipboard, 73, 24);

        Assert.NotEmpty(target.ShapeKeyframes);
        Assert.Contains(target.ShapeKeyframes, keyframe => Math.Abs(keyframe.Time - 1.0) < 0.0001d);
        Assert.True(target.ShapeKeyframes.Last().IsClosed);
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_CompositingSettings()
    {
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 120, 80, 0);
        layer.Compositing.Role = LayerCompositeRole.Mask;
        layer.Compositing.MaskLayerCount = 2;
        layer.Compositing.BlendMode = LayerBlendMode.Screen;
        layer.Compositing.GlowColor = "#FFD166";
        layer.Compositing.GlowOpacity = 0.55d;
        layer.Compositing.ShadowOpacity = 0.32d;
        layer.Compositing.ShadowOffsetX = 18d;
        layer.Compositing.ShadowOffsetY = 12d;
        layer.Compositing.TintColor = "#61E6FF";
        layer.Compositing.TintStrength = 0.2d;
        layer.Compositing.Brightness = 0.08d;
        layer.Compositing.Saturation = 1.4d;
        layer.Compositing.ParallaxDepth = 1.3d;

        var restored = DocumentSerializer.FromJson(DocumentSerializer.ToJson(new TimelineDocument { Layers = [layer] }));
        var compositing = Assert.Single(restored.Layers).Compositing;

        Assert.Equal(LayerCompositeRole.Mask, compositing.Role);
        Assert.Equal(2, compositing.MaskLayerCount);
        Assert.Equal(LayerBlendMode.Screen, compositing.BlendMode);
        Assert.Equal("#FFD166", compositing.GlowColor);
        Assert.Equal(0.32d, compositing.ShadowOpacity);
        Assert.Equal(1.3d, compositing.ParallaxDepth);
    }

    [Fact]
    public void CompositeFrameRenderer_TransformsSnapshotWithCameraAndParallax()
    {
        var snapshot = new LayerSnapshot(
            200,
            120,
            240,
            140,
            0,
            1,
            28,
            "#24E5C1",
            "#FFFFFF",
            string.Empty,
            42,
            2,
            false,
            "#24E5C1",
            "#FFFFFF",
            false,
            [],
            new LayerCompositeSettings { ParallaxDepth = 2d });
        var camera = new LayerSnapshot(
            80,
            40,
            640,
            360,
            0,
            1,
            0,
            "#15223A",
            "#FFD166",
            string.Empty,
            42,
            2,
            false,
            "#15223A",
            "#FFD166",
            false,
            [],
            new LayerCompositeSettings { Role = LayerCompositeRole.Camera });

        var transformed = CompositeFrameRenderer.TransformSnapshot(snapshot, camera, 1280, 720, snapshot.Compositing.ParallaxDepth);

        Assert.Equal(320d, transformed.X, 3);
        Assert.Equal(200d, transformed.Y, 3);
        Assert.Equal(480d, transformed.Width, 3);
        Assert.Equal(280d, transformed.Height, 3);
    }

    [Fact]
    public void CompositeFrameRenderer_ResolvesTopmostCameraSample()
    {
        var baseSettings = new LayerCompositeSettings();
        var cameraSettings = new LayerCompositeSettings { Role = LayerCompositeRole.Camera };
        var firstCamera = new RenderableLayerSample
        {
            SourceLayerId = Guid.NewGuid(),
            Kind = LayerKind.Rectangle,
            ZIndex = 0,
            Snapshot = new LayerSnapshot(0, 0, 1280, 720, 0, 1, 0, "#000000", "#000000", string.Empty, 0, 1, false, "#000000", "#000000", false, [], cameraSettings)
        };
        var topCamera = new RenderableLayerSample
        {
            SourceLayerId = Guid.NewGuid(),
            Kind = LayerKind.Rectangle,
            ZIndex = 2,
            Snapshot = new LayerSnapshot(80, 24, 1120, 630, 0, 1, 0, "#000000", "#000000", string.Empty, 0, 1, false, "#000000", "#000000", false, [], new LayerCompositeSettings { Role = LayerCompositeRole.Camera })
        };
        var content = new RenderableLayerSample
        {
            SourceLayerId = Guid.NewGuid(),
            Kind = LayerKind.Rectangle,
            ZIndex = 1,
            Snapshot = new LayerSnapshot(120, 80, 320, 180, 0, 1, 28, "#24E5C1", "#FFFFFF", string.Empty, 0, 1, false, "#24E5C1", "#FFFFFF", false, [], baseSettings)
        };

        var resolved = CompositeFrameRenderer.ResolveActiveCamera([firstCamera, content, topCamera]);

        Assert.NotNull(resolved);
        Assert.Equal(80, resolved.Value.X);
        Assert.Equal(1120, resolved.Value.Width);
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_MediaAssetsAndLayerSettings()
    {
        var video = new MediaAsset
        {
            Name = "Promo",
            Kind = MediaAssetKind.Video,
            Duration = 2.4d,
            FrameRate = 12d,
            Width = 640,
            Height = 360,
            PreviewFill = "#223352",
            PreviewAccent = "#FFD166",
            Frames =
            [
                new MediaFrameModel { Duration = 0.2d, Fill = "#223352", Accent = "#61E6FF", Label = "A" },
                new MediaFrameModel { Duration = 0.2d, Fill = "#30435F", Accent = "#FFD166", Label = "B" }
            ]
        };
        var audio = new MediaAsset
        {
            Name = "Bed",
            Kind = MediaAssetKind.Audio,
            Duration = 3d,
            SampleRate = 48000,
            ChannelCount = 2,
            WaveformSamples = [0.12d, 0.42d, 0.28d, 0.58d]
        };

        var videoLayer = TimelineEditingService.CreateVideoLayer(video, 120, 80, 0, 0.5d);
        videoLayer.Media.PlaybackMode = MediaPlaybackMode.Start;
        videoLayer.Media.ClipOffset = 0.2d;
        videoLayer.Media.ClipDuration = 1.4d;
        var audioLayer = TimelineEditingService.CreateAudioLayer(audio, 1, 0.25d);
        audioLayer.Media.Loop = true;
        audioLayer.Media.Volume = 0.7d;

        var restored = DocumentSerializer.FromJson(DocumentSerializer.ToJson(new TimelineDocument
        {
            MediaAssets = [video, audio],
            Layers = [videoLayer, audioLayer]
        }));

        Assert.Equal(2, restored.MediaAssets.Count);
        Assert.Equal(MediaAssetKind.Video, restored.MediaAssets[0].Kind);
        Assert.Equal(2, restored.MediaAssets[0].Frames.Count);
        Assert.Equal(MediaAssetKind.Audio, restored.MediaAssets[1].Kind);
        Assert.Equal(4, restored.MediaAssets[1].WaveformSamples.Count);
        Assert.Equal(MediaPlaybackMode.Start, restored.Layers[0].Media.PlaybackMode);
        Assert.Equal(0.2d, restored.Layers[0].Media.ClipOffset);
        Assert.True(restored.Layers[1].Media.Loop);
        Assert.Equal(0.7d, restored.Layers[1].Media.Volume);
    }

    [Fact]
    public void MediaTimelineService_ResolvesStreamClipTime()
    {
        var asset = new MediaAsset
        {
            Name = "Promo",
            Kind = MediaAssetKind.Video,
            Duration = 2d,
            FrameRate = 10d
        };
        var layer = TimelineEditingService.CreateVideoLayer(asset, 100, 80, 0, 0.5d);
        layer.Media.ClipOffset = 0.25d;
        layer.Media.ClipDuration = 1d;
        layer.Media.PlaybackMode = MediaPlaybackMode.Stream;

        Assert.Null(MediaTimelineService.ResolveMediaTime(layer, asset, 0.4d));
        Assert.Equal(0.5d, MediaTimelineService.ResolveMediaTime(layer, asset, 0.75d));
        Assert.Null(MediaTimelineService.ResolveMediaTime(layer, asset, 1.8d));
    }

    [Fact]
    public void MediaTimelineService_ResolvesLoopingClipTime()
    {
        var asset = new MediaAsset
        {
            Name = "Loop",
            Kind = MediaAssetKind.Audio,
            Duration = 1.2d,
            SampleRate = 44100
        };
        var layer = TimelineEditingService.CreateAudioLayer(asset, 0, 0d);
        layer.Media.ClipOffset = 0.4d;
        layer.Media.ClipDuration = 3d;
        layer.Media.Loop = true;

        var mediaTime = MediaTimelineService.ResolveMediaTime(layer, asset, 2.2d);

        Assert.NotNull(mediaTime);
        Assert.Equal(0.2d, mediaTime!.Value, 3);
    }

    [Fact]
    public void SymbolRenderService_ActivatesVideoSamplesOnlyWhenClipIsPlaying()
    {
        var asset = new MediaAsset
        {
            Name = "Promo",
            Kind = MediaAssetKind.Video,
            Duration = 1d,
            FrameRate = 6d,
            Width = 640,
            Height = 360,
            Frames =
            [
                new MediaFrameModel { Duration = 1d / 6d, Fill = "#223352", Accent = "#61E6FF", Label = "A" }
            ]
        };
        var layer = TimelineEditingService.CreateVideoLayer(asset, 120, 90, 0, 0.6d);
        var document = new TimelineDocument
        {
            Duration = 3d,
            MediaAssets = [asset],
            Layers = [layer]
        };

        var beforeClip = SymbolRenderService.BuildRenderSamples(document, document.Layers, 0.2d, document.Duration, 24d);
        var duringClip = SymbolRenderService.BuildRenderSamples(document, document.Layers, 0.9d, document.Duration, 24d);

        Assert.Empty(beforeClip);
        var sample = Assert.Single(duringClip);
        Assert.Equal(LayerKind.Video, sample.Kind);
        Assert.Equal(asset.Id, sample.SourceMediaAssetId);
        Assert.NotNull(sample.MediaTime);
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_ComponentBehaviors()
    {
        var component = TimelineEditingService.CreateLibraryItemFromLayer(
            TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 100, 80, 0),
            "CTA",
            SymbolKind.Button);
        component.IsComponent = true;
        component.ComponentCategory = "Controls";
        component.ComponentDescription = "Reusable button";
        component.DefaultBehaviors =
        [
            new InteractionBehaviorModel
            {
                Name = "Jump",
                Trigger = InteractionTriggerKind.PointerClick,
                Action = InteractionActionKind.GoToFrameLabel,
                TargetFrameLabel = "reveal"
            }
        ];

        var instance = TimelineEditingService.CreateLayerFromLibraryItem(component, 220, 180, 0);
        var restored = DocumentSerializer.FromJson(DocumentSerializer.ToJson(new TimelineDocument
        {
            LibraryItems = [component],
            Layers = [instance]
        }));

        Assert.True(restored.LibraryItems[0].IsComponent);
        Assert.Equal("Controls", restored.LibraryItems[0].ComponentCategory);
        Assert.Single(restored.LibraryItems[0].DefaultBehaviors);
        Assert.Single(restored.Layers[0].Behaviors);
        Assert.Equal("reveal", restored.Layers[0].Behaviors[0].TargetFrameLabel);
    }

    [Fact]
    public void CreateLayerFromLibraryItem_ClonesComponentBehaviors()
    {
        var component = TimelineEditingService.CreateLibraryItemFromLayer(
            TimelineEditingService.CreateLayer(LayerKind.Rectangle, "Card", "#24E5C1", string.Empty, 120, 80, 0),
            "Toggle Card",
            SymbolKind.Button);
        component.IsComponent = true;
        component.DefaultBehaviors =
        [
            new InteractionBehaviorModel
            {
                Name = "Hide Card",
                Trigger = InteractionTriggerKind.PointerClick,
                Action = InteractionActionKind.SetLayerVisibility,
                BoolValue = false
            }
        ];

        var instance = TimelineEditingService.CreateLayerFromLibraryItem(component, 320, 220, 0);
        component.DefaultBehaviors[0].VariableName = "mutated";

        Assert.Single(instance.Behaviors);
        Assert.Equal("Hide Card", instance.Behaviors[0].Name);
        Assert.True(string.IsNullOrWhiteSpace(instance.Behaviors[0].VariableName));
    }

    [Fact]
    public void InteractionBehaviorService_MatchesFrameLabelTrigger()
    {
        var behavior = new InteractionBehaviorModel
        {
            Trigger = InteractionTriggerKind.FrameLabelReached,
            TriggerArgument = "detail"
        };

        Assert.True(InteractionBehaviorService.MatchesTrigger(behavior, InteractionTriggerKind.FrameLabelReached, "detail"));
        Assert.False(InteractionBehaviorService.MatchesTrigger(behavior, InteractionTriggerKind.FrameLabelReached, "intro"));
        Assert.False(InteractionBehaviorService.MatchesTrigger(behavior, InteractionTriggerKind.PointerClick, null));
    }

    [Fact]
    public void SampleProjectFactory_CreatesComponentLibraryItems()
    {
        var document = SampleProjectFactory.Create();

        Assert.Contains(document.LibraryItems, item => item.IsComponent);
        Assert.Contains(document.Scenes[0].Layers, layer => layer.Behaviors.Count > 0);
    }

    [Fact]
    public void MainWindowViewModel_PrototypeInteraction_RestoresEditorStateOnExit()
    {
        var viewModel = new MainWindowViewModel();
        var introSceneId = viewModel.SelectedScene?.Id;
        var triggerLayer = viewModel.Document.Layers.First(layer => layer.Behaviors.Count > 0);

        viewModel.TogglePrototypeModeCommand.Execute(null);
        viewModel.HandlePrototypeTrigger(triggerLayer.Id, InteractionTriggerKind.PointerClick);

        Assert.True(viewModel.IsPrototypeMode);
        Assert.NotEqual(introSceneId, viewModel.SelectedScene?.Id);

        viewModel.TogglePrototypeModeCommand.Execute(null);

        Assert.False(viewModel.IsPrototypeMode);
        Assert.Equal(introSceneId, viewModel.SelectedScene?.Id);
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_PublishProfiles()
    {
        var document = SampleProjectFactory.Create();
        PublishProfileService.EnsureProfiles(document);
        document.PublishProfiles[0].OutputKind = PublishOutputKind.Gif;
        document.PublishProfiles[0].Width = 1920;
        document.PublishProfiles[0].Height = 1080;
        document.PublishProfiles[0].FrameRate = 30;
        document.PublishProfiles[0].TransparentBackground = true;
        document.PublishProfiles[0].SpriteSheetColumns = 8;
        document.PublishProfiles[0].Quality = 72;

        var restored = DocumentSerializer.FromJson(DocumentSerializer.ToJson(document));
        var restoredProfile = Assert.Single(restored.PublishProfiles, item => item.Id == document.PublishProfiles[0].Id);

        Assert.Equal(document.PublishProfiles.Count, restored.PublishProfiles.Count);
        Assert.Equal(PublishOutputKind.Gif, restoredProfile.OutputKind);
        Assert.Equal(1920, restoredProfile.Width);
        Assert.Equal(1080, restoredProfile.Height);
        Assert.Equal(30, restoredProfile.FrameRate);
        Assert.True(restoredProfile.TransparentBackground);
        Assert.Equal(8, restoredProfile.SpriteSheetColumns);
        Assert.Equal(72, restoredProfile.Quality);
    }

    [Fact]
    public void PublishValidationService_FlagsMissingBehaviorTargets()
    {
        var document = SampleProjectFactory.Create();
        PublishProfileService.EnsureProfiles(document);
        var triggerLayer = document.Scenes[0].Layers.First();
        triggerLayer.Behaviors.Add(new InteractionBehaviorModel
        {
            Name = "Missing Scene",
            Trigger = InteractionTriggerKind.PointerClick,
            Action = InteractionActionKind.GoToScene,
            TargetSceneId = Guid.NewGuid()
        });
        triggerLayer.Behaviors.Add(new InteractionBehaviorModel
        {
            Name = "Missing Label",
            Trigger = InteractionTriggerKind.PointerClick,
            Action = InteractionActionKind.GoToFrameLabel,
            TargetFrameLabel = "missing_label"
        });
        triggerLayer.Behaviors.Add(new InteractionBehaviorModel
        {
            Name = "Missing Layer",
            Trigger = InteractionTriggerKind.PointerClick,
            Action = InteractionActionKind.SetLayerVisibility,
            TargetLayerId = Guid.NewGuid(),
            BoolValue = false
        });

        var issues = PublishValidationService.Validate(document, document.PublishProfiles[0]);

        Assert.Contains(issues, issue => issue.Message.Contains("targets a missing scene", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(issues, issue => issue.Message.Contains("targets a frame label that does not exist", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(issues, issue => issue.Message.Contains("targets a layer that does not exist", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PublishExportService_ExportsSceneGraphJson()
    {
        var document = SampleProjectFactory.Create();
        PublishProfileService.EnsureProfiles(document);
        var profile = document.PublishProfiles[0].Clone();
        profile.Name = "Scene Graph Review";
        profile.OutputKind = PublishOutputKind.JsonSceneGraph;

        var outputRoot = Path.Combine(Path.GetTempPath(), $"timeline_publish_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputRoot);
        var outputPath = Path.Combine(outputRoot, "scene-graph.json");

        try
        {
            var result = await PublishExportService.ExportAsync(document, profile, outputPath);

            Assert.True(File.Exists(outputPath));
            Assert.Equal(outputPath, result.PrimaryOutputPath);
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            Assert.Equal(document.Name, json.RootElement.GetProperty("document").GetProperty("name").GetString());
            Assert.Equal("Scene Graph Review", json.RootElement.GetProperty("profile").GetProperty("name").GetString());
            Assert.True(json.RootElement.GetProperty("scenes").GetArrayLength() >= 1);
            Assert.True(json.RootElement.GetProperty("library").GetArrayLength() >= 1);
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void ActionScriptService_ParsesQuotedCommands()
    {
        var script = """
                     set_text "Launch Caption" "FLASH READY"
                     set_button this Over
                     trace "Cue fired"
                     """;

        var result = ActionScriptService.Parse(script);

        Assert.False(result.HasErrors);
        Assert.Collection(
            result.Commands,
            first =>
            {
                Assert.Equal(ActionScriptCommandKind.SetText, first.Kind);
                Assert.Equal(["Launch Caption", "FLASH READY"], first.Arguments);
            },
            second =>
            {
                Assert.Equal(ActionScriptCommandKind.SetButtonState, second.Kind);
                Assert.Equal(["this", "Over"], second.Arguments);
            },
            third =>
            {
                Assert.Equal(ActionScriptCommandKind.Trace, third.Kind);
                Assert.Equal(["Cue fired"], third.Arguments);
            });
    }

    [Fact]
    public void DocumentSerializer_RoundTrips_ActionScripts()
    {
        var document = SampleProjectFactory.Create();
        document.Scenes[0].FrameLabels[0].Script = "trace \"intro\"";
        document.Scenes[0].Layers.First(layer => layer.Behaviors.Count > 0).Behaviors[0].Script = "set_var beat one";

        var restored = DocumentSerializer.FromJson(DocumentSerializer.ToJson(document));

        Assert.Equal("trace \"intro\"", restored.Scenes[0].FrameLabels[0].Script);
        Assert.Equal("set_var beat one", restored.Scenes[0].Layers.First(layer => layer.Behaviors.Count > 0).Behaviors[0].Script);
    }

    [Fact]
    public void PublishValidationService_FlagsInvalidActionScripts()
    {
        var document = SampleProjectFactory.Create();
        PublishProfileService.EnsureProfiles(document);
        document.Scenes[0].FrameLabels[0].Script = "goto_scene \"Missing Scene\"\nunknown_cmd";
        document.Scenes[0].Layers.First().Behaviors.Add(new InteractionBehaviorModel
        {
            Name = "Bad Script",
            Trigger = InteractionTriggerKind.PointerClick,
            Action = InteractionActionKind.Play,
            Script = "set_button \"Hero Card\" InvalidState"
        });

        var issues = PublishValidationService.Validate(document, document.PublishProfiles[0]);

        Assert.Contains(issues, issue => issue.Message.Contains("unknown action command", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(issues, issue => issue.Message.Contains("missing scene", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(issues, issue => issue.Message.Contains("invalid button state", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MainWindowViewModel_PrototypeBehaviorScript_ExecutesCommands()
    {
        var viewModel = new MainWindowViewModel();
        var triggerLayer = viewModel.Document.Layers.First(layer => layer.Behaviors.Count > 0);
        triggerLayer.Behaviors[0].Script = """
                                          set_var mode scripted
                                          trace "CTA fired"
                                          """;

        viewModel.TogglePrototypeModeCommand.Execute(null);
        viewModel.HandlePrototypeTrigger(triggerLayer.Id, InteractionTriggerKind.PointerClick);

        Assert.True(viewModel.IsPrototypeMode);
        Assert.Equal("Feature Focus", viewModel.SelectedScene?.Name);
        Assert.Contains("mode=scripted", viewModel.PrototypeVariableSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CTA fired", viewModel.PrototypeSummary, StringComparison.OrdinalIgnoreCase);
    }
}
