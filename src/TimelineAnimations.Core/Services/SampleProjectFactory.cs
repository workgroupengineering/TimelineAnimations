using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class SampleProjectFactory
{
    public static TimelineDocument Create()
    {
        var heroCard = CreateHeroCard();
        var orb = CreateOrb();
        var caption = CreateCaption();
        var signalPath = CreateSignalPath();
        var promoClip = CreatePromoClip();
        var ambientTrack = CreateAmbientTrack();
        var promoVideo = CreatePromoVideoLayer(promoClip);
        var ctaComponent = CreateSceneCtaComponent();
        var cueComponent = CreateTimelineCueComponent();
        var soundtrack = CreateSoundtrackLayer(ambientTrack);
        var stageCamera = CreateStageCamera();

        var document = new TimelineDocument
        {
            Name = "Aurora Product Reveal",
            Duration = 6,
            CanvasWidth = 1280,
            CanvasHeight = 720,
            BackgroundFrom = "#09111F",
            BackgroundTo = "#182748",
            Layers =
            [
                heroCard,
                orb,
                caption,
                signalPath,
                promoVideo,
                soundtrack,
                stageCamera
            ],
            MediaAssets =
            [
                promoClip,
                ambientTrack
            ]
        };

        SeedLibrarySymbol(document, heroCard, "Hero Card Symbol", SymbolKind.Graphic);
        SeedLibrarySymbol(document, orb, "Solar Orb Clip", SymbolKind.MovieClip);
        SeedLibrarySymbol(document, caption, "Launch Caption Symbol", SymbolKind.Graphic);
        TimelineEditingService.AddLibraryItem(document, ctaComponent);
        TimelineEditingService.AddLibraryItem(document, cueComponent);

        var introScene = SceneEditingService.CreateSceneFromDocumentState(document, "Intro Reveal");
        introScene.FrameLabels =
        [
            new FrameLabelModel
            {
                Frame = 0,
                Name = "start",
                Script = """
                         trace "Intro scene armed"
                         set_var scene intro
                         """
            },
            new FrameLabelModel
            {
                Frame = 12,
                Name = "reveal",
                Script = """
                         trace "Reveal beat"
                         set_var beat reveal
                         """
            },
            new FrameLabelModel
            {
                Frame = 36,
                Name = "accent",
                Script = """
                         show "Launch Caption"
                         trace "Accent on"
                         """
            }
        ];
        SceneTimelineService.AddOrUpdateMarker(introScene, null, 6, "Intro cue", SceneMarkerKind.Cue, "Hero card enters.", "#57C9FF", FrameTimelineService.GetTotalFrames(introScene.Duration, introScene.FrameRate));
        SceneTimelineService.AddOrUpdateMarker(introScene, null, 18, "Comment", SceneMarkerKind.Comment, "Sync caption reveal with VO.", "#FFD166", FrameTimelineService.GetTotalFrames(introScene.Duration, introScene.FrameRate));
        SceneTimelineService.SetInPoint(introScene, 0, FrameTimelineService.GetTotalFrames(introScene.Duration, introScene.FrameRate));
        SceneTimelineService.SetOutPoint(introScene, 54, FrameTimelineService.GetTotalFrames(introScene.Duration, introScene.FrameRate));
        SceneTimelineService.SetWorkArea(introScene, 6, 42, FrameTimelineService.GetTotalFrames(introScene.Duration, introScene.FrameRate));
        introScene.OutgoingTransition = new SceneTransitionModel
        {
            Kind = SceneTransitionKind.CrossDissolve,
            Duration = 0.45d,
            AccentColor = "#000000"
        };
        introScene.Layers[2].FrameSpans =
        [
            new FrameSpanModel { StartFrame = 0, EndFrame = 9, IsBlank = true },
            new FrameSpanModel { StartFrame = 10, EndFrame = 23, IsBlank = false },
            new FrameSpanModel { StartFrame = 24, EndFrame = 29, IsBlank = true },
            new FrameSpanModel { StartFrame = 30, EndFrame = 60, IsBlank = false }
        ];

        var featureScene = SceneEditingService.DuplicateScene(introScene, "Feature Focus");
        featureScene.BackgroundFrom = "#120C20";
        featureScene.BackgroundTo = "#26174A";
        featureScene.FrameLabels =
        [
            new FrameLabelModel
            {
                Frame = 0,
                Name = "feature",
                Script = """
                         trace "Feature scene live"
                         set_var scene feature
                         """
            },
            new FrameLabelModel
            {
                Frame = 18,
                Name = "detail",
                Script = """
                         trace "Detail cue"
                         set_var cue detail
                         """
            }
        ];
        SceneTimelineService.AddOrUpdateMarker(featureScene, null, 10, "Feature chapter", SceneMarkerKind.Chapter, "Enter focused feature section.", "#24E5C1", FrameTimelineService.GetTotalFrames(featureScene.Duration, featureScene.FrameRate));
        SceneTimelineService.AddOrUpdateMarker(featureScene, null, 30, "Publish note", SceneMarkerKind.PublishNote, "Hold 12f for social export.", "#FF8A4C", FrameTimelineService.GetTotalFrames(featureScene.Duration, featureScene.FrameRate));
        SceneTimelineService.SetInPoint(featureScene, 0, FrameTimelineService.GetTotalFrames(featureScene.Duration, featureScene.FrameRate));
        SceneTimelineService.SetOutPoint(featureScene, 52, FrameTimelineService.GetTotalFrames(featureScene.Duration, featureScene.FrameRate));
        SceneTimelineService.SetWorkArea(featureScene, 8, 38, FrameTimelineService.GetTotalFrames(featureScene.Duration, featureScene.FrameRate));
        featureScene.OutgoingTransition = new SceneTransitionModel
        {
            Kind = SceneTransitionKind.DipToBlack,
            Duration = 0.40d,
            AccentColor = "#09111F"
        };
        featureScene.Layers[0].Defaults.X = 252;
        featureScene.Layers[0].Defaults.Y = 154;
        featureScene.Layers[1].Defaults.X = 822;
        featureScene.Layers[1].Defaults.Y = 184;
        featureScene.Layers[1].Defaults.Width = 248;
        featureScene.Layers[1].Defaults.Height = 248;
        featureScene.Layers[1].FrameSpans =
        [
            new FrameSpanModel { StartFrame = 0, EndFrame = 15, IsBlank = false },
            new FrameSpanModel { StartFrame = 16, EndFrame = 23, IsBlank = true },
            new FrameSpanModel { StartFrame = 24, EndFrame = 60, IsBlank = false }
        ];
        featureScene.Layers[2].Style.Text = "FLASH PARITY";
        featureScene.Layers[4].Media.StartTime = 0.9d;
        featureScene.Layers[4].Media.ClipOffset = 0.12d;
        featureScene.Layers[4].Media.PlaybackMode = MediaPlaybackMode.Start;
        featureScene.Layers[5].Media.StartTime = 0.4d;
        featureScene.Layers[5].Media.Volume = 0.8d;

        ctaComponent.DefaultBehaviors =
        [
            new InteractionBehaviorModel
            {
                Name = "Open Feature Scene",
                Trigger = InteractionTriggerKind.PointerClick,
                Action = InteractionActionKind.GoToScene,
                TargetSceneId = featureScene.Id,
                Script = """
                         trace "CTA clicked"
                         set_var source cta
                         """
            }
        ];
        cueComponent.DefaultBehaviors =
        [
            new InteractionBehaviorModel
            {
                Name = "Jump To Detail",
                Trigger = InteractionTriggerKind.PointerClick,
                Action = InteractionActionKind.GoToFrameLabel,
                TargetFrameLabel = "detail",
                Script = """
                         trace "Jumping to detail"
                         """
            },
            new InteractionBehaviorModel
            {
                Name = "Mark Cue",
                Trigger = InteractionTriggerKind.PointerClick,
                Action = InteractionActionKind.SetVariable,
                VariableName = "cue",
                VariableValue = "detail",
                Script = """
                         set_text "Launch Caption" "DETAIL FOCUS"
                         """
            }
        ];

        introScene.Layers.Add(TimelineEditingService.CreateLayerFromLibraryItem(ctaComponent, 184, 598, 5));
        featureScene.Layers.Add(TimelineEditingService.CreateLayerFromLibraryItem(cueComponent, 184, 592, 5));

        document.Scenes =
        [
            introScene,
            featureScene
        ];
        document.ActiveSceneId = introScene.Id;
        PublishProfileService.EnsureProfiles(document);
        SceneEditingService.ApplySceneToDocument(document, introScene);

        return document;
    }

    private static TimelineLayer CreateHeroCard()
    {
        var layer = new TimelineLayer
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
            Compositing = new LayerCompositeSettings
            {
                ShadowBlur = 18d,
                ShadowOpacity = 0.34d,
                ShadowOffsetX = 20d,
                ShadowOffsetY = 24d,
                TintColor = "#9BFFF0",
                TintStrength = 0.08d,
                ParallaxDepth = 1.18d
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
        }.WithEasings(
            (AnimatedProperty.X, 1, EasingKind.EaseOut),
            (AnimatedProperty.X, 2, EasingKind.EaseInOut),
            (AnimatedProperty.Rotation, 1, EasingKind.BackOut));
        VisualStateEditingService.CaptureState(layer, "CardStates", "Intro", 0d);
        VisualStateEditingService.CaptureState(layer, "CardStates", "Reveal", 1.8d);
        VisualStateEditingService.CaptureState(layer, "CardStates", "Accent", 4.8d);
        return layer;
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
            Compositing = new LayerCompositeSettings
            {
                BlendMode = LayerBlendMode.Screen,
                GlowColor = "#FFB37A",
                GlowOpacity = 0.58d,
                GlowSize = 22d,
                ParallaxDepth = 0.84d
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
        }.WithEasings(
            (AnimatedProperty.X, 1, EasingKind.EaseInOut),
            (AnimatedProperty.Y, 1, EasingKind.BackOut),
            (AnimatedProperty.Height, 1, EasingKind.EaseOut));
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
            Compositing = new LayerCompositeSettings
            {
                ShadowBlur = 10d,
                ShadowOpacity = 0.28d,
                ShadowOffsetX = 8d,
                ShadowOffsetY = 10d,
                Brightness = 0.04d,
                ParallaxDepth = 1.04d
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
        }.WithEasings(
            (AnimatedProperty.X, 1, EasingKind.EaseOut),
            (AnimatedProperty.Opacity, 1, EasingKind.Hold),
            (AnimatedProperty.Opacity, 2, EasingKind.EaseInOut));
    }

    private static TimelineLayer CreateSignalPath()
    {
        var layer = TimelineEditingService.CreatePathLayer(
            "Signal Path",
            "#61E6FF",
            "#9BFFF0",
            [
                new VectorPointModel { X = 622, Y = 238 },
                new VectorPointModel { X = 748, Y = 172 },
                new VectorPointModel { X = 896, Y = 196 },
                new VectorPointModel { X = 1028, Y = 158 },
                new VectorPointModel { X = 1142, Y = 242 }
            ],
            isClosed: false,
            zIndex: 3,
            strokeThickness: 12,
            useGradient: true,
            gradientFrom: "#61E6FF",
            gradientTo: "#FF8A4C");

        layer.Defaults.Opacity = 0;
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Opacity, 0, 0, 6);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Y, 1.2, layer.Defaults.Y - 34, 6);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Y, 4.8, layer.Defaults.Y + 12, 6);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Opacity, 0.6, 0.3, 6);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Opacity, 1.6, 0.78, 6);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Opacity, 5.4, 0.42, 6);
        TimelineEditingService.SetShapeKeyframe(
            layer,
            1.4,
            [
                new VectorPointModel { X = 0.00, Y = 0.78 },
                new VectorPointModel { X = 0.24, Y = 0.12 },
                new VectorPointModel { X = 0.54, Y = 0.30 },
                new VectorPointModel { X = 0.82, Y = 0.02 },
                new VectorPointModel { X = 1.00, Y = 0.92 }
            ],
            isClosed: false,
            duration: 6);
        TimelineEditingService.SetShapeKeyframe(
            layer,
            4.4,
            [
                new VectorPointModel { X = 0.00, Y = 0.56 },
                new VectorPointModel { X = 0.22, Y = 0.88 },
                new VectorPointModel { X = 0.55, Y = 0.14 },
                new VectorPointModel { X = 0.82, Y = 0.94 },
                new VectorPointModel { X = 1.00, Y = 0.42 }
            ],
            isClosed: false,
            duration: 6);
        var customCurve = layer.Tracks
            .Single(track => track.Property == AnimatedProperty.Y)
            .Keyframes[1];
        customCurve.Easing = EasingKind.Custom;
        customCurve.ControlPoint1X = 0.18;
        customCurve.ControlPoint1Y = 0.92;
        customCurve.ControlPoint2X = 0.74;
        customCurve.ControlPoint2Y = 0.18;
        layer.Compositing.BlendMode = LayerBlendMode.Add;
        layer.Compositing.GlowColor = "#61E6FF";
        layer.Compositing.GlowOpacity = 0.34d;
        layer.Compositing.GlowSize = 14d;
        layer.Compositing.ParallaxDepth = 1.22d;
        return layer.WithEasings(
            (AnimatedProperty.Opacity, 1, EasingKind.EaseInOut));
    }

    private static TimelineLayer CreateStageCamera()
    {
        var layer = TimelineEditingService.CreateCameraLayer("Stage Camera", 1120, 630, 8);
        layer.Defaults.X = 48;
        layer.Defaults.Y = 24;
        layer.Tracks =
        [
            Track(AnimatedProperty.X, (0, 48), (2.2, 96), (5.2, 40)),
            Track(AnimatedProperty.Y, (0, 24), (2.2, 60), (5.2, 28)),
            Track(AnimatedProperty.Width, (0, 1120), (2.2, 980), (5.2, 1080)),
            Track(AnimatedProperty.Height, (0, 630), (2.2, 552), (5.2, 608)),
            Track(AnimatedProperty.Rotation, (0, 0)),
            Track(AnimatedProperty.Opacity, (0, 1))
        ];
        return layer.WithEasings(
            (AnimatedProperty.X, 1, EasingKind.EaseInOut),
            (AnimatedProperty.Y, 1, EasingKind.EaseInOut),
            (AnimatedProperty.Width, 1, EasingKind.EaseOut),
            (AnimatedProperty.Height, 1, EasingKind.EaseOut));
    }

    private static MediaAsset CreatePromoClip()
    {
        return new MediaAsset
        {
            Name = "Feature Clip",
            Kind = MediaAssetKind.Video,
            SourceLabel = "Embedded demo sequence",
            Duration = 1.2d,
            FrameRate = 6d,
            Width = 640,
            Height = 360,
            PreviewFill = "#223352",
            PreviewAccent = "#FFD166",
            Frames =
            [
                new MediaFrameModel { Duration = 1d / 6d, Fill = "#223352", Accent = "#61E6FF", Label = "Reveal" },
                new MediaFrameModel { Duration = 1d / 6d, Fill = "#274465", Accent = "#9BFFF0", Label = "Grid" },
                new MediaFrameModel { Duration = 1d / 6d, Fill = "#2B304C", Accent = "#FFD166", Label = "Zoom" },
                new MediaFrameModel { Duration = 1d / 6d, Fill = "#302746", Accent = "#FF8A4C", Label = "Glow" },
                new MediaFrameModel { Duration = 1d / 6d, Fill = "#27304D", Accent = "#F7F5ED", Label = "Text" },
                new MediaFrameModel { Duration = 1d / 6d, Fill = "#223352", Accent = "#24E5C1", Label = "Loop" },
                new MediaFrameModel { Duration = 1d / 6d, Fill = "#21314A", Accent = "#61E6FF", Label = "Panel" }
            ]
        };
    }

    private static MediaAsset CreateAmbientTrack()
    {
        return new MediaAsset
        {
            Name = "Pulse Bed",
            Kind = MediaAssetKind.Audio,
            SourceLabel = "Embedded demo waveform",
            Duration = 6d,
            SampleRate = 48000,
            ChannelCount = 2,
            PreviewFill = "#61E6FF",
            PreviewAccent = "#FF8A4C",
            WaveformSamples =
            [
                .. Enumerable.Range(0, 96).Select(static index =>
                {
                    var t = index / 95d;
                    var bed = Math.Sin(t * Math.PI * 3.2d) * 0.42d;
                    var pulse = Math.Cos(t * Math.PI * 11.4d) * 0.18d;
                    return Math.Clamp(Math.Abs(bed + pulse) + 0.08d, 0.06d, 0.92d);
                })
            ]
        };
    }

    private static TimelineLayer CreatePromoVideoLayer(MediaAsset asset)
    {
        var layer = TimelineEditingService.CreateVideoLayer(asset, 720, 352, 4, 0.7d);
        layer.Name = "Feature Clip";
        layer.Defaults.Width = 404d;
        layer.Defaults.Height = 228d;
        layer.Style.StrokeThickness = 2.2d;
        layer.Compositing.BlendMode = LayerBlendMode.Screen;
        layer.Compositing.GlowColor = "#FFD166";
        layer.Compositing.GlowOpacity = 0.18d;
        layer.Compositing.GlowSize = 14d;
        layer.Compositing.ParallaxDepth = 0.92d;
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.X, 0.7d, 720d, 6d);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.X, 3.2d, 684d, 6d);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.X, 5.4d, 732d, 6d);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Y, 0.7d, 352d, 6d);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Y, 3.2d, 334d, 6d);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Y, 5.4d, 346d, 6d);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Opacity, 0d, 0d, 6d);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Opacity, 0.7d, 0.96d, 6d);
        TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Opacity, 5.5d, 0.84d, 6d);
        return layer.WithEasings(
            (AnimatedProperty.X, 1, EasingKind.EaseInOut),
            (AnimatedProperty.Y, 1, EasingKind.EaseOut),
            (AnimatedProperty.Opacity, 1, EasingKind.EaseOut));
    }

    private static TimelineLayer CreateSoundtrackLayer(MediaAsset asset)
    {
        var layer = TimelineEditingService.CreateAudioLayer(asset, 7, 0d);
        layer.Name = "Soundtrack";
        layer.Media.Volume = 0.72d;
        layer.Media.Loop = false;
        layer.Media.PlaybackMode = MediaPlaybackMode.Stream;
        return layer;
    }

    private static LibraryItem CreateSceneCtaComponent()
    {
        var component = CreateButtonComponent(
            "Feature CTA",
            "OPEN FEATURE",
            "#F7F5ED",
            "#24E5C1",
            "#09111F",
            232d,
            72d);
        component.ComponentCategory = "Navigation";
        component.ComponentDescription = "Button component with scene jump behavior.";
        return component;
    }

    private static LibraryItem CreateTimelineCueComponent()
    {
        var component = CreateButtonComponent(
            "Detail Cue",
            "JUMP TO DETAIL",
            "#182748",
            "#61E6FF",
            "#F7F5ED",
            248d,
            64d);
        component.ComponentCategory = "Playback";
        component.ComponentDescription = "Cue chip that jumps to a labeled frame.";
        return component;
    }

    private static LibraryItem CreateButtonComponent(
        string name,
        string labelText,
        string fill,
        string stroke,
        string textFill,
        double width,
        double height)
    {
        var surface = TimelineEditingService.CreateLayer(LayerKind.Rectangle, $"{name} Surface", fill, string.Empty, 0, 0, 0);
        surface.Defaults.Width = width;
        surface.Defaults.Height = height;
        surface.Style.CornerRadius = height / 2d;
        surface.Style.Fill = fill;
        surface.Style.Stroke = stroke;
        surface.Style.StrokeThickness = 2d;
        surface.Tracks = [];

        var label = TimelineEditingService.CreateLayer(LayerKind.Text, $"{name} Label", textFill, labelText, 22, 14, 1);
        label.Defaults.Width = width - 44d;
        label.Defaults.Height = height - 28d;
        label.Style.Fill = textFill;
        label.Style.Stroke = textFill;
        label.Style.FontSize = 24d;
        label.Style.Text = labelText;
        label.Tracks = [];

        var component = TimelineEditingService.CreateLibraryItemFromLayer(surface, name, SymbolKind.Button);
        component.IsComponent = true;

        var upLayers = new List<TimelineLayer> { surface, label };
        var overLayers = DocumentSerializer.Clone(upLayers);
        overLayers[0].Style.Fill = stroke;
        overLayers[0].Style.Stroke = "#F7F5ED";
        overLayers[1].Style.Fill = "#09111F";
        overLayers[1].Style.Stroke = "#09111F";

        var downLayers = DocumentSerializer.Clone(upLayers);
        downLayers[0].Defaults.Y += 3d;
        downLayers[1].Defaults.Y += 3d;
        downLayers[0].Style.Fill = BlendHex(fill, "#09111F");
        downLayers[1].Style.Fill = "#F7F5ED";
        downLayers[1].Style.Stroke = "#F7F5ED";

        var hitLayers = DocumentSerializer.Clone(upLayers);
        hitLayers[0].Style.Fill = "#FFFFFF";
        hitLayers[0].Style.Stroke = "#FFFFFF";
        hitLayers[1].Style.Fill = "#FFFFFF";
        hitLayers[1].Style.Stroke = "#FFFFFF";

        TimelineEditingService.SetEditableSymbolLayers(component, upLayers, 0.1d, 24d, ButtonVisualState.Up);
        TimelineEditingService.SetEditableSymbolLayers(component, overLayers, 0.1d, 24d, ButtonVisualState.Over);
        TimelineEditingService.SetEditableSymbolLayers(component, downLayers, 0.1d, 24d, ButtonVisualState.Down);
        TimelineEditingService.SetEditableSymbolLayers(component, hitLayers, 0.1d, 24d, ButtonVisualState.Hit);
        return component;
    }

    private static string BlendHex(string baseHex, string tintHex)
    {
        var (baseR, baseG, baseB) = ParseHex(baseHex);
        var (tintR, tintG, tintB) = ParseHex(tintHex);
        var red = (byte)(baseR + ((tintR - baseR) * 0.18d));
        var green = (byte)(baseG + ((tintG - baseG) * 0.18d));
        var blue = (byte)(baseB + ((tintB - baseB) * 0.18d));
        return $"#{red:X2}{green:X2}{blue:X2}";
    }

    private static (byte R, byte G, byte B) ParseHex(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? "FFFFFF"
            : value.Trim().TrimStart('#');
        if (normalized.Length != 6)
        {
            normalized = "FFFFFF";
        }

        return (
            Convert.ToByte(normalized.Substring(0, 2), 16),
            Convert.ToByte(normalized.Substring(2, 2), 16),
            Convert.ToByte(normalized.Substring(4, 2), 16));
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

    private static TimelineLayer WithEasings(this TimelineLayer layer, params (AnimatedProperty Property, int KeyframeIndex, EasingKind Easing)[] settings)
    {
        foreach (var setting in settings)
        {
            var track = layer.Tracks.FirstOrDefault(item => item.Property == setting.Property);
            if (track is null || setting.KeyframeIndex < 0 || setting.KeyframeIndex >= track.Keyframes.Count)
            {
                continue;
            }

            track.Keyframes[setting.KeyframeIndex].Easing = setting.Easing;
        }

        return layer;
    }

    private static void SeedLibrarySymbol(TimelineDocument document, TimelineLayer layer, string symbolName, SymbolKind symbolKind)
    {
        var libraryItem = TimelineEditingService.CreateLibraryItemFromLayer(layer, symbolName, symbolKind);
        TimelineEditingService.AddLibraryItem(document, libraryItem);
        layer.SourceLibraryItemId = libraryItem.Id;
    }
}
