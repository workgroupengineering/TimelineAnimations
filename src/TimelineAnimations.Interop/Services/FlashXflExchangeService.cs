using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TimelineAnimations.Core.Models;
using TimelineAnimations.FlashFormats;

namespace TimelineAnimations.Core.Services;

internal static partial class FlashXflExchangeService
{
    private static readonly XNamespace s_xflNs = "http://ns.adobe.com/xfl/2008/";
    private const double ComparisonTolerance = 0.0001d;

    public static string Export(TimelineDocument document)
    {
        SceneEditingService.EnsureScenes(document);
        LibraryManagementService.EnsureLibraryMetadata(document);
        AnimateDocumentProfileService.EnsureSettings(document);
        PublishProfileService.EnsureProfiles(document);
        NormalizeFlashSupplementalFiles(document);

        var activeScene = GetActiveScene(document);
        var mediaPayloadPlan = BuildMediaPayloadPlan(document.MediaAssets);
        var root = new XElement(
            s_xflNs + "DOMDocument",
            new XAttribute("name", document.Name),
            new XAttribute("xflVersion", "2.971"),
            new XAttribute("creator", "TimelineAnimations Studio"),
            new XAttribute("width", FormatNumber(activeScene.CanvasWidth)),
            new XAttribute("height", FormatNumber(activeScene.CanvasHeight)),
            new XAttribute("frameRate", FormatNumber(activeScene.FrameRate)),
            new XAttribute("currentTimeline", Math.Max(0, document.Scenes.FindIndex(scene => scene.Id == document.ActiveSceneId))),
            new XAttribute("backgroundColor", activeScene.BackgroundFrom),
            new XAttribute("backgroundColor2", activeScene.BackgroundFrom),
            new XAttribute("transparentStageBackground", activeScene.TransparentStageBackground),
            new XAttribute("documentType", document.Animate.Type),
            new XAttribute("units", document.Animate.Units),
            new XAttribute("responsive", document.Animate.IsResponsive),
            new XAttribute("useExternalScriptFile", document.Animate.UseExternalScriptFile),
            new XAttribute("useWebFonts", document.Animate.UseWebFonts),
            new XAttribute("useAdvancedLayers", document.Animate.UseAdvancedLayers),
            new XAttribute("templateName", document.Animate.TemplateName ?? string.Empty),
            new XAttribute("enable3DTransforms", document.Animate.Enable3DTransforms),
            new XAttribute("perspectiveAngle", FormatNumber(document.Animate.PerspectiveAngle)),
            new XAttribute("vanishingPointX", FormatNumber(document.Animate.VanishingPointX)),
            new XAttribute("vanishingPointY", FormatNumber(document.Animate.VanishingPointY)));

        var folders = document.LibraryItems
            .Select(item => LibraryManagementService.NormalizeFolderPath(item.FolderPath))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new XElement(s_xflNs + "DOMFolderItem", new XAttribute("path", path)))
            .ToList();
        if (folders.Count > 0)
        {
            root.Add(new XElement(s_xflNs + "folders", folders));
        }

        if (document.MediaAssets.Count > 0)
        {
            root.Add(
                new XElement(
                    s_xflNs + "media",
                    document.MediaAssets.Select(asset => BuildMediaAssetElement(asset, mediaPayloadPlan))));
        }

        if (document.LibraryItems.Count > 0)
        {
            root.Add(
                new XElement(
                    s_xflNs + "symbols",
                    document.LibraryItems.Select(BuildLibraryItemElement)));
        }

        root.Add(
            new XElement(
                s_xflNs + "timelines",
                document.Scenes.Select(scene => BuildTimelineElement(scene, document.LibraryItems, document.MediaAssets))));

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString();
    }

    public static AnimationExchangeImportResult Import(string content, string sourceLabel)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Flash XFL content is empty.");
        }

        var xfl = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
        return ImportDocument(xfl, sourceLabel, []);
    }

    public static AnimationExchangeImportResult ImportPackage(byte[] data, string sourceLabel)
    {
        if (data.Length == 0)
        {
            throw new InvalidOperationException("Flash package is empty.");
        }

        var package = FlashAuthoringPackageService.Read(data, sourceLabel);
        return ImportPackage(package, sourceLabel);
    }

    public static void ExportPackage(Stream stream, TimelineDocument document)
    {
        var package = BuildPackage(document, FlashAuthoringContainerKind.CompressedFlaArchive, document.Name);
        FlashAuthoringPackageService.WriteArchive(stream, package);
    }

    public static AnimationExchangeImportResult ImportPackageDirectory(string folderPath, string sourceLabel)
    {
        var package = FlashAuthoringPackageService.ReadDirectory(folderPath);
        return ImportPackage(package, sourceLabel);
    }

    public static void ExportPackageDirectory(string folderPath, TimelineDocument document)
    {
        var package = BuildPackage(document, FlashAuthoringContainerKind.UncompressedXflDirectory, folderPath);
        FlashAuthoringPackageService.WriteDirectory(folderPath, package);
    }

    private static AnimationExchangeImportResult ImportPackage(FlashAuthoringPackage package, string sourceLabel)
    {
        var documentContent = Encoding.UTF8.GetString(package.DocumentData).TrimStart('\uFEFF');
        var packagedSymbols = ReadPackagedLibrarySymbolElements(package);
        var xfl = XDocument.Parse(documentContent, LoadOptions.PreserveWhitespace);
        return ImportDocument(xfl, sourceLabel, packagedSymbols, package);
    }

    private static AnimationExchangeImportResult ImportDocument(
        XDocument xfl,
        string sourceLabel,
        IReadOnlyList<XElement> packagedSymbolElements,
        FlashAuthoringPackage? package = null)
    {
        var root = xfl.Root ?? throw new InvalidOperationException("Invalid Flash XFL document.");
        if (!string.Equals(root.Name.LocalName, "DOMDocument", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The XFL document root must be DOMDocument.");
        }

        var issues = new List<AnimationExchangeIssue>
        {
            CreateIssue(AnimationExchangeIssueSeverity.Info, "Flash XFL", "Flash XFL import reconstructs editable authoring data from DOM timelines, packaged library symbols, and explicit motion metadata.")
        };

        var mediaAssets = ParseMediaAssets(root, package);
        var mediaByName = mediaAssets.ToDictionary(asset => asset.Name, StringComparer.OrdinalIgnoreCase);
        var libraryItems = ParseLibraryItems(root, mediaByName, issues, packagedSymbolElements);
        var libraryByName = libraryItems.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);
        var timelines = root.Elements().FirstOrDefault(element => element.Name.LocalName == "timelines");
        var scenes = timelines is null
            ? []
            : timelines.Elements().Where(element => element.Name.LocalName == "DOMTimeline")
                .Select((timeline, index) => ParseScene(timeline, index, libraryByName, mediaByName, issues))
                .ToList();

        if (scenes.Count == 0)
        {
            throw new InvalidOperationException("No Flash timelines were found in the XFL document.");
        }

        var currentTimeline = ParseInt(TryGetAttributeValue(root, "currentTimeline"), 0);
        var activeSceneIndex = Math.Clamp(currentTimeline, 0, scenes.Count - 1);
        var activeScene = scenes[activeSceneIndex];
        var document = new TimelineDocument
        {
            Name = TryGetAttributeValue(root, "name") ?? Path.GetFileNameWithoutExtension(sourceLabel),
            Duration = activeScene.Duration,
            CanvasWidth = activeScene.CanvasWidth,
            CanvasHeight = activeScene.CanvasHeight,
            TransparentStageBackground = activeScene.TransparentStageBackground,
            BackgroundFrom = activeScene.BackgroundFrom,
            BackgroundTo = activeScene.BackgroundTo,
            ActiveSceneId = activeScene.Id,
            Animate = new AnimateDocumentSettings
            {
                Type = ParseDocumentType(TryGetAttributeValue(root, "documentType")),
                Units = ParseDocumentUnits(TryGetAttributeValue(root, "units")),
                IsResponsive = ParseBool(TryGetAttributeValue(root, "responsive"), false),
                UseExternalScriptFile = ParseBool(TryGetAttributeValue(root, "useExternalScriptFile"), false),
                UseWebFonts = ParseBool(TryGetAttributeValue(root, "useWebFonts"), false),
                UseAdvancedLayers = ParseBool(TryGetAttributeValue(root, "useAdvancedLayers"), true),
                TemplateName = TryGetAttributeValue(root, "templateName") ?? "Flash XFL",
                Enable3DTransforms = ParseBool(TryGetAttributeValue(root, "enable3DTransforms"), true),
                PerspectiveAngle = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(root, "perspectiveAngle"), 55d), 1d, 89d),
                VanishingPointX = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(root, "vanishingPointX"), 0.5d), 0d, 1d),
                VanishingPointY = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(root, "vanishingPointY"), 0.5d), 0d, 1d)
            },
            Scenes = scenes,
            Layers = activeScene.Layers,
            LibraryItems = libraryItems,
            MediaAssets = mediaAssets
        };

        ApplyPublishSettings(package, document, issues);
        PublishProfileService.EnsureProfiles(document);
        AnimateDocumentProfileService.EnsureSettings(document);
        LibraryManagementService.EnsureLibraryMetadata(document);
        SceneEditingService.EnsureScenes(document);
        document.FlashAuthoringSupplementalFiles = package is null
            ? []
            : ExtractSupplementalFiles(package, document);

        return new AnimationExchangeImportResult
        {
            Format = AnimationExchangeFormat.FlashXfl,
            Document = document,
            Summary = $"{AnimationExchangeService.GetDisplayName(AnimationExchangeFormat.FlashXfl)} imported as {document.Name}",
            Issues = issues
        };
    }

    private static XElement BuildMediaAssetElement(
        MediaAsset asset,
        IReadOnlyDictionary<Guid, FlashMediaPayloadPlan> payloadPlanByAssetId)
    {
        payloadPlanByAssetId.TryGetValue(asset.Id, out var payloadPlan);
        var elementName = asset.Kind switch
        {
            MediaAssetKind.Audio => "DOMSoundItem",
            MediaAssetKind.Video when asset.Frames.Count <= 1 => "DOMBitmapItem",
            _ => "DOMVideoItem"
        };

        var element = new XElement(
            s_xflNs + elementName,
            new XAttribute("name", asset.Name),
            new XAttribute("assetType", asset.Kind),
            new XAttribute("sourceLabel", asset.SourceLabel ?? string.Empty),
            new XAttribute("durationSeconds", FormatNumber(asset.Duration)),
            new XAttribute("frameRate", FormatNumber(asset.FrameRate)),
            new XAttribute("width", asset.Width),
            new XAttribute("height", asset.Height),
            new XAttribute("sampleRate", asset.SampleRate),
            new XAttribute("channelCount", asset.ChannelCount),
            new XAttribute("previewFill", asset.PreviewFill),
            new XAttribute("previewAccent", asset.PreviewAccent));

        if (payloadPlan is not null)
        {
            if (!string.IsNullOrWhiteSpace(payloadPlan.AssetRelativePath))
            {
                element.SetAttributeValue("relativePath", payloadPlan.AssetRelativePath);
            }

            if (!string.IsNullOrWhiteSpace(payloadPlan.FrameFolderRelativePath))
            {
                element.SetAttributeValue("frameFolder", payloadPlan.FrameFolderRelativePath);
                element.SetAttributeValue("frameCount", payloadPlan.FrameRelativePaths.Count);
                element.SetAttributeValue("frameExtension", payloadPlan.FrameExtension ?? string.Empty);
            }
        }

        return element;
    }

    private static XElement BuildLibraryItemElement(LibraryItem item)
    {
        LibraryManagementService.NormalizeScale9Grid(item);
        ComponentParameterService.NormalizeDefinitions(item);
        var element = new XElement(
            s_xflNs + "DOMSymbolItem",
            new XAttribute("name", item.Name),
            new XAttribute("symbolType", ToFlashSymbolType(item.SymbolKind)),
            new XAttribute("folderPath", item.FolderPath ?? string.Empty),
            new XAttribute("linkageClassName", item.LinkageId ?? string.Empty),
            new XAttribute("linkageBaseClass", item.BaseClassName ?? string.Empty),
            new XAttribute("linkageExportForRuntime", item.ExportForRuntimeSharing),
            new XAttribute("linkageImportForRuntime", item.ImportForRuntimeSharing),
            new XAttribute("linkageExportInFirstFrame", item.ExportInFirstFrame),
            new XAttribute("sharedLibraryPath", item.SharedLibraryPath ?? string.Empty),
            new XAttribute("sharedLibraryAutoUpdate", item.UpdateAutomatically),
            new XAttribute("sourceAssetPath", item.SourceAssetPath ?? string.Empty),
            new XAttribute("durationSeconds", FormatNumber(item.Duration)),
            new XAttribute("frameRate", FormatNumber(item.FrameRate)),
            new XAttribute("isComponent", item.IsComponent),
            new XAttribute("componentCategory", item.ComponentCategory ?? string.Empty),
            new XAttribute("componentDescription", item.ComponentDescription ?? string.Empty),
            new XAttribute("useScale9Grid", item.UseScale9Grid),
            new XAttribute("scaleGridLeft", FormatNumber(item.Scale9Left)),
            new XAttribute("scaleGridTop", FormatNumber(item.Scale9Top)),
            new XAttribute("scaleGridRight", FormatNumber(item.Scale9Right)),
            new XAttribute("scaleGridBottom", FormatNumber(item.Scale9Bottom)),
            new XAttribute("registrationPointX", FormatNumber(item.RegistrationPointX)),
            new XAttribute("registrationPointY", FormatNumber(item.RegistrationPointY)));

        element.Add(BuildBehaviorContainer(item.DefaultBehaviors));
        var componentParameters = BuildComponentParameterCollectionElement(item);
        if (componentParameters is not null)
        {
            element.Add(componentParameters);
        }

        if (item.SymbolKind == SymbolKind.Button)
        {
            element.Add(
                new XElement(
                    s_xflNs + "buttonStates",
                    item.ButtonStates.Select(state =>
                        new XElement(
                            s_xflNs + "DOMButtonState",
                            new XAttribute("state", state.State),
                            BuildLayerCollectionElement(state.Layers, item.Duration, item.FrameRate, [], [])))));
        }
        else
        {
            element.Add(BuildTimelineElement(item.Name, item.Duration, item.FrameRate, item.Layers, null, null));
        }

        return element;
    }

    private static XElement BuildTimelineElement(
        SceneModel scene,
        IReadOnlyList<LibraryItem> libraryItems,
        IReadOnlyList<MediaAsset> mediaAssets)
    {
        var timeline = BuildTimelineElement(scene.Name, scene.Duration, scene.FrameRate, scene.Layers, libraryItems, mediaAssets);
        timeline.SetAttributeValue("canvasWidth", FormatNumber(scene.CanvasWidth));
        timeline.SetAttributeValue("canvasHeight", FormatNumber(scene.CanvasHeight));
        timeline.SetAttributeValue("backgroundColor", scene.BackgroundFrom);
        timeline.SetAttributeValue("backgroundColor2", scene.BackgroundFrom);
        timeline.SetAttributeValue("transparentStageBackground", scene.TransparentStageBackground);
        timeline.SetAttributeValue("inFrame", scene.InFrame);
        timeline.SetAttributeValue("outFrame", scene.OutFrame);
        timeline.SetAttributeValue("workAreaStart", scene.WorkAreaStartFrame);
        timeline.SetAttributeValue("workAreaEnd", scene.WorkAreaEndFrame);

        if (scene.FrameLabels.Count > 0)
        {
            timeline.Add(
                new XElement(
                    s_xflNs + "frameLabels",
                    scene.FrameLabels.Select(label =>
                        new XElement(
                            s_xflNs + "DOMFrameLabel",
                            new XAttribute("frame", label.Frame),
                            new XAttribute("name", label.Name),
                            new XAttribute("script", label.Script ?? string.Empty)))));
        }

        if (scene.Markers.Count > 0)
        {
            timeline.Add(
                new XElement(
                    s_xflNs + "markers",
                    scene.Markers.Select(marker =>
                        new XElement(
                            s_xflNs + "DOMMarker",
                            new XAttribute("frame", marker.Frame),
                            new XAttribute("name", marker.Name),
                            new XAttribute("kind", marker.Kind),
                            new XAttribute("color", marker.Color),
                            new XAttribute("notes", marker.Notes ?? string.Empty)))));
        }

        return timeline;
    }

    private static XElement BuildTimelineElement(
        string name,
        double duration,
        double frameRate,
        IReadOnlyList<TimelineLayer> layers,
        IReadOnlyList<LibraryItem>? libraryItems,
        IReadOnlyList<MediaAsset>? mediaAssets)
    {
        var totalFrames = FrameTimelineService.GetTotalFrames(duration, frameRate);
        return new XElement(
            s_xflNs + "DOMTimeline",
            new XAttribute("name", name),
            new XAttribute("durationSeconds", FormatNumber(duration)),
            new XAttribute("frameRate", FormatNumber(frameRate)),
            new XAttribute("frameCount", totalFrames),
            BuildLayerCollectionElement(layers, duration, frameRate, libraryItems ?? [], mediaAssets ?? []));
    }

    private static XElement BuildLayerCollectionElement(
        IReadOnlyList<TimelineLayer> layers,
        double duration,
        double frameRate,
        IReadOnlyList<LibraryItem> libraryItems,
        IReadOnlyList<MediaAsset> mediaAssets)
    {
        return new XElement(
            s_xflNs + "layers",
            layers.OrderBy(layer => layer.ZIndex).Select(layer => BuildLayerElement(layer, duration, frameRate, libraryItems, mediaAssets)));
    }

    private static XElement BuildLayerElement(
        TimelineLayer layer,
        double duration,
        double frameRate,
        IReadOnlyList<LibraryItem> libraryItems,
        IReadOnlyList<MediaAsset> mediaAssets)
    {
        var totalFrames = FrameTimelineService.GetTotalFrames(duration, frameRate);
        var libraryMap = libraryItems.ToDictionary(item => item.Id, item => item);
        var mediaMap = mediaAssets.ToDictionary(item => item.Id, item => item);
        var frames = new List<XElement>(totalFrames);

        for (var frame = 0; frame < totalFrames; frame++)
        {
            var time = FrameTimelineService.FrameToTime(frame, frameRate);
            var snapshot = FrameTimelineService.SampleLayer(layer, time, frameRate, totalFrames);
            var frameElement = new XElement(
                s_xflNs + "DOMFrame",
                new XAttribute("index", frame),
                new XAttribute("duration", 1),
                new XAttribute("isBlank", snapshot is null));

            if (snapshot is null)
            {
                frames.Add(frameElement);
                continue;
            }

            frameElement.Add(
                new XElement(
                    s_xflNs + "elements",
                    BuildFrameVisualElement(layer, snapshot.Value, libraryMap, mediaMap)));
            frames.Add(frameElement);
        }

        var layerElement = new XElement(
            s_xflNs + "DOMLayer",
            new XAttribute("layerId", layer.Id),
            new XAttribute("name", layer.Name),
            new XAttribute("layerType", ToFlashLayerType(layer)),
            new XAttribute("visible", layer.IsVisible),
            new XAttribute("locked", layer.IsLocked),
            new XAttribute("muted", layer.IsMuted),
            new XAttribute("solo", layer.IsSolo),
            new XAttribute("expanded", layer.IsExpanded),
            new XAttribute("zIndex", layer.ZIndex),
            new XAttribute("parentLayerId", layer.ParentLayerId?.ToString() ?? string.Empty),
            new XAttribute("transformParentLayerId", layer.TransformParenting.ParentLayerId?.ToString() ?? string.Empty),
            new XAttribute("transformBindParentX", FormatNumber(layer.TransformParenting.BindParentX)),
            new XAttribute("transformBindParentY", FormatNumber(layer.TransformParenting.BindParentY)),
            new XAttribute("transformBindParentScaleX", FormatNumber(layer.TransformParenting.BindParentScaleX)),
            new XAttribute("transformBindParentScaleY", FormatNumber(layer.TransformParenting.BindParentScaleY)),
            new XAttribute("transformBindParentRotation", FormatNumber(layer.TransformParenting.BindParentRotation)),
            new XAttribute("transformBindParentOpacity", FormatNumber(layer.TransformParenting.BindParentOpacity)),
            new XAttribute("transformBindChildX", FormatNumber(layer.TransformParenting.BindChildX)),
            new XAttribute("transformBindChildY", FormatNumber(layer.TransformParenting.BindChildY)),
            new XAttribute("guidedByLayerId", layer.GuidedByLayerId?.ToString() ?? string.Empty),
            new XAttribute("orientToGuidePath", layer.OrientToGuidePath),
            new XAttribute("snapToGuidePath", layer.SnapToGuidePath),
            new XAttribute("outline", layer.ShowAsOutline),
            new XAttribute("outlineColor", layer.OutlineColor ?? "#57C9FF"),
            new XAttribute("cacheAsBitmap", layer.CacheAsBitmap),
            new XAttribute("bitmapCacheBackgroundColor", layer.BitmapCacheBackgroundColor ?? "#000000"),
            new XAttribute("kind", layer.Kind),
            new XAttribute("blendMode", layer.Compositing.BlendMode),
            new XAttribute("maskLayerCount", layer.Compositing.MaskLayerCount),
            new XAttribute("parallaxDepth", FormatNumber(layer.Compositing.ParallaxDepth)),
            new XElement(s_xflNs + "frames", frames));

        var behaviors = BuildBehaviorContainer(layer.Behaviors);
        if (behaviors is not null)
        {
            layerElement.Add(behaviors);
        }

        var componentOverrides = BuildComponentOverrideCollectionElement(layer);
        if (componentOverrides is not null)
        {
            layerElement.Add(componentOverrides);
        }

        var compositing = BuildCompositingElement(layer.Compositing);
        if (compositing is not null)
        {
            layerElement.Add(compositing);
        }

        var mediaSettings = BuildMediaSettingsElement(layer);
        if (mediaSettings is not null)
        {
            layerElement.Add(mediaSettings);
        }

        var tracks = BuildTrackCollectionElement(layer);
        if (tracks is not null)
        {
            layerElement.Add(tracks);
        }

        var shapeTweens = BuildShapeKeyframeCollectionElement(layer);
        if (shapeTweens is not null)
        {
            layerElement.Add(shapeTweens);
        }

        var warp = BuildWarpElement(layer.Warp);
        if (warp is not null)
        {
            layerElement.Add(warp);
        }

        var rig = BuildRigElement(layer.Rig);
        if (rig is not null)
        {
            layerElement.Add(rig);
        }

        return layerElement;
    }

    private static XElement BuildFrameVisualElement(
        TimelineLayer layer,
        LayerSnapshot snapshot,
        IReadOnlyDictionary<Guid, LibraryItem> libraryItems,
        IReadOnlyDictionary<Guid, MediaAsset> mediaAssets)
    {
        if (layer.SourceLibraryItemId is Guid sourceLibraryItemId &&
            libraryItems.TryGetValue(sourceLibraryItemId, out var libraryItem))
        {
            var symbolElement = new XElement(
                s_xflNs + "DOMSymbolInstance",
                new XAttribute("libraryItemName", libraryItem.Name),
                new XAttribute("symbolType", ToFlashSymbolType(libraryItem.SymbolKind)),
                new XAttribute("instanceName", layer.InstanceName ?? string.Empty),
                new XAttribute("playbackMode", layer.SymbolPlaybackMode),
                new XAttribute("playbackOffset", FormatNumber(layer.SymbolPlaybackOffset)),
                new XAttribute("lockedFrame", layer.SymbolLockedFrame),
                new XAttribute("buttonState", layer.SymbolButtonState),
                new XAttribute("cacheAsBitmap", layer.CacheAsBitmap),
                new XAttribute("bitmapCacheBackgroundColor", layer.BitmapCacheBackgroundColor ?? "#000000"));
            ApplyCommonVisualAttributes(symbolElement, snapshot);
            ApplyNativeFlashVisualEffects(symbolElement, snapshot.Compositing);
            return symbolElement;
        }

        if (layer.Kind is LayerKind.Video or LayerKind.Audio)
        {
            var mediaName = layer.Media.SourceMediaAssetId is Guid sourceMediaAssetId &&
                            mediaAssets.TryGetValue(sourceMediaAssetId, out var mediaAsset)
                ? mediaAsset.Name
                : layer.Name;
            var mediaElement = new XElement(
                s_xflNs + "DOMMediaInstance",
                new XAttribute("assetName", mediaName),
                new XAttribute("mediaType", layer.Kind == LayerKind.Video ? MediaAssetKind.Video : MediaAssetKind.Audio),
                new XAttribute("startTime", FormatNumber(layer.Media.StartTime)),
                new XAttribute("clipOffset", FormatNumber(layer.Media.ClipOffset)),
                new XAttribute("clipDuration", FormatNumber(layer.Media.ClipDuration)),
                new XAttribute("loop", layer.Media.Loop),
                new XAttribute("volume", FormatNumber(layer.Media.Volume)),
                new XAttribute("gainDb", FormatNumber(layer.Media.GainDb)),
                new XAttribute("pan", FormatNumber(layer.Media.Pan)),
                new XAttribute("fadeIn", FormatNumber(layer.Media.FadeInDuration)),
                new XAttribute("fadeOut", FormatNumber(layer.Media.FadeOutDuration)));
            ApplyCommonVisualAttributes(mediaElement, snapshot);
            ApplyNativeFlashVisualEffects(mediaElement, snapshot.Compositing);
            return mediaElement;
        }

        if (layer.Kind == LayerKind.AvaloniaControl)
        {
            var settings = snapshot.AvaloniaControl;
            var controlElement = new XElement(
                s_xflNs + "DOMComponentInstance",
                new XAttribute("componentType", settings.Kind),
                new XAttribute("content", settings.Content ?? string.Empty),
                new XAttribute("secondaryContent", settings.SecondaryContent ?? string.Empty),
                new XAttribute("isChecked", settings.IsChecked),
                new XAttribute("value", FormatNumber(settings.Value)),
                new XAttribute("minimum", FormatNumber(settings.Minimum)),
                new XAttribute("maximum", FormatNumber(settings.Maximum)),
                new XAttribute("isIndeterminate", settings.IsIndeterminate),
                new XAttribute("source", settings.Source ?? string.Empty));
            ApplyCommonVisualAttributes(controlElement, snapshot);
            ApplyNativeFlashVisualEffects(controlElement, snapshot.Compositing);
            return controlElement;
        }

        if (layer.Kind == LayerKind.Text)
        {
            var elementName = snapshot.TextSettings.FieldKind switch
            {
                FlashTextFieldKind.Input => "DOMInputText",
                FlashTextFieldKind.Dynamic => "DOMDynamicText",
                _ => "DOMStaticText"
            };
            var textElement = new XElement(
                s_xflNs + elementName,
                new XAttribute("text", snapshot.Text ?? string.Empty),
                new XAttribute("fontSize", FormatNumber(snapshot.FontSize)),
                new XAttribute("fieldKind", ToXflTextFieldKind(snapshot.TextSettings.FieldKind)),
                new XAttribute("lineMode", ToXflTextLineMode(snapshot.TextSettings.LineMode)),
                new XAttribute("fontFamily", snapshot.TextSettings.FontFamily ?? string.Empty),
                new XAttribute("bold", snapshot.TextSettings.IsBold),
                new XAttribute("italic", snapshot.TextSettings.IsItalic),
                new XAttribute("alignment", snapshot.TextSettings.Alignment),
                new XAttribute("letterSpacing", FormatNumber(snapshot.TextSettings.LetterSpacing)),
                new XAttribute("lineHeight", FormatNumber(snapshot.TextSettings.LineHeight)),
                new XAttribute("selectable", snapshot.TextSettings.IsSelectable),
                new XAttribute("showBorder", snapshot.TextSettings.ShowBorder),
                new XAttribute("variableName", snapshot.TextSettings.VariableName ?? string.Empty),
                new XAttribute("maxCharacters", snapshot.TextSettings.MaxCharacters),
                new XAttribute("password", snapshot.TextSettings.IsPassword),
                new XAttribute("useDeviceFonts", snapshot.TextSettings.UseDeviceFonts),
                new XAttribute("antiAliasMode", ToXflTextAntiAliasMode(snapshot.TextSettings.AntiAliasMode)),
                new XAttribute("renderAsHtml", snapshot.TextSettings.RenderAsHtml),
                new XAttribute("fill", snapshot.Fill));
            ApplyCommonVisualAttributes(textElement, snapshot);
            ApplyNativeFlashVisualEffects(textElement, snapshot.Compositing);
            return textElement;
        }

        var shapeElement = new XElement(
            s_xflNs + "DOMShape",
            new XAttribute("shapeType", layer.Kind),
            new XAttribute("drawingMode", snapshot.DrawingMode),
            new XAttribute("primitiveShape", snapshot.PrimitiveShape),
            new XAttribute("fill", snapshot.Fill),
            new XAttribute("stroke", snapshot.Stroke),
            new XAttribute("strokeThickness", FormatNumber(snapshot.StrokeThickness)),
            new XAttribute("cornerRadius", FormatNumber(snapshot.CornerRadius)),
            new XAttribute("cornerRadiusTopLeft", FormatNumber(snapshot.CornerRadiusTopLeft)),
            new XAttribute("cornerRadiusTopRight", FormatNumber(snapshot.CornerRadiusTopRight)),
            new XAttribute("cornerRadiusBottomRight", FormatNumber(snapshot.CornerRadiusBottomRight)),
            new XAttribute("cornerRadiusBottomLeft", FormatNumber(snapshot.CornerRadiusBottomLeft)),
            new XAttribute("useGradient", snapshot.UseGradient),
            new XAttribute("gradientFrom", snapshot.GradientFrom),
            new XAttribute("gradientTo", snapshot.GradientTo),
            new XAttribute("isClosed", snapshot.IsClosed),
            new XAttribute("ellipseStartAngle", FormatNumber(snapshot.EllipseStartAngle)),
            new XAttribute("ellipseSweepAngle", FormatNumber(snapshot.EllipseSweepAngle)),
            new XAttribute("polyStarSides", snapshot.PolyStarSides),
            new XAttribute("polyStarInnerRadius", FormatNumber(snapshot.PolyStarInnerRadius)),
            new XAttribute("polyStarIsStar", snapshot.PolyStarIsStar),
            new XAttribute("pathData", BuildPathData(snapshot.PathPoints, snapshot.Width, snapshot.Height, snapshot.IsClosed)));
        ApplyCommonVisualAttributes(shapeElement, snapshot);
        ApplyNativeFlashVisualEffects(shapeElement, snapshot.Compositing);
        return shapeElement;
    }

    private static void ApplyCommonVisualAttributes(XElement element, LayerSnapshot snapshot)
    {
        element.SetAttributeValue("x", FormatNumber(snapshot.X));
        element.SetAttributeValue("y", FormatNumber(snapshot.Y));
        element.SetAttributeValue("width", FormatNumber(snapshot.Width));
        element.SetAttributeValue("height", FormatNumber(snapshot.Height));
        element.SetAttributeValue("scaleX", FormatNumber(snapshot.ScaleX));
        element.SetAttributeValue("scaleY", FormatNumber(snapshot.ScaleY));
        element.SetAttributeValue("skewX", FormatNumber(snapshot.SkewX));
        element.SetAttributeValue("skewY", FormatNumber(snapshot.SkewY));
        element.SetAttributeValue("rotation", FormatNumber(snapshot.Rotation));
        element.SetAttributeValue("rotationX", FormatNumber(snapshot.RotationX));
        element.SetAttributeValue("rotationY", FormatNumber(snapshot.RotationY));
        element.SetAttributeValue("zDepth", FormatNumber(snapshot.ZDepth));
        element.SetAttributeValue("opacity", FormatNumber(snapshot.Opacity));
        element.SetAttributeValue("hasFill", snapshot.HasFill);
        element.SetAttributeValue("hasStroke", snapshot.HasStroke);
        element.SetAttributeValue("gradientKind", ToXflGradientKind(snapshot.GradientKind));
        element.SetAttributeValue("gradientAngle", FormatNumber(snapshot.GradientAngle));
        element.SetAttributeValue("strokeCap", ToXflStrokeCap(snapshot.StrokeCapStyle));
        element.SetAttributeValue("strokeJoin", ToXflStrokeJoin(snapshot.StrokeJoinStyle));
        element.SetAttributeValue("strokeMiterLimit", FormatNumber(snapshot.StrokeMiterLimit));
    }

    private static void ApplyNativeFlashVisualEffects(XElement element, LayerCompositeSettings compositing)
    {
        ApplyNativeFlashColorEffectAttributes(element, compositing);
        var filters = BuildNativeFilterCollectionElement(compositing);
        if (filters is not null)
        {
            element.Add(filters);
        }
    }

    private static void ApplyNativeFlashColorEffectAttributes(XElement element, LayerCompositeSettings compositing)
    {
        if (compositing.FlashColorEffectMode == FlashColorEffectMode.None)
        {
            return;
        }

        element.SetAttributeValue("colorMode", compositing.FlashColorEffectMode.ToString().ToLowerInvariant());
        switch (compositing.FlashColorEffectMode)
        {
            case FlashColorEffectMode.Alpha:
                element.SetAttributeValue("alphaPercent", FormatNumber(compositing.FlashAlphaPercent));
                break;
            case FlashColorEffectMode.Tint:
                element.SetAttributeValue("tintColor", compositing.FlashTintColor);
                element.SetAttributeValue("tintPercent", FormatNumber(compositing.FlashTintPercent));
                break;
            case FlashColorEffectMode.Brightness:
                element.SetAttributeValue("brightnessPercent", FormatNumber(compositing.FlashBrightnessPercent));
                break;
            case FlashColorEffectMode.Advanced:
                element.SetAttributeValue("alphaMultiplier", FormatNumber(compositing.FlashAdvancedAlphaPercent));
                element.SetAttributeValue("redMultiplier", FormatNumber(compositing.FlashRedPercent));
                element.SetAttributeValue("greenMultiplier", FormatNumber(compositing.FlashGreenPercent));
                element.SetAttributeValue("blueMultiplier", FormatNumber(compositing.FlashBluePercent));
                element.SetAttributeValue("alphaOffset", FormatNumber(compositing.FlashAlphaOffset));
                element.SetAttributeValue("redOffset", FormatNumber(compositing.FlashRedOffset));
                element.SetAttributeValue("greenOffset", FormatNumber(compositing.FlashGreenOffset));
                element.SetAttributeValue("blueOffset", FormatNumber(compositing.FlashBlueOffset));
                break;
        }
    }

    private static XElement? BuildNativeFilterCollectionElement(LayerCompositeSettings compositing)
    {
        var filters = new List<XElement>();
        if (compositing.BlurRadius > 0.1d)
        {
            filters.Add(
                new XElement(
                    s_xflNs + "BlurFilter",
                    new XAttribute("blurX", FormatNumber(compositing.BlurRadius)),
                    new XAttribute("blurY", FormatNumber(compositing.BlurRadius)),
                    new XAttribute("quality", (int)compositing.BlurQuality)));
        }

        if (compositing.GlowOpacity > 0d && compositing.GlowSize > 0.1d)
        {
            filters.Add(
                new XElement(
                    s_xflNs + "GlowFilter",
                    new XAttribute("blurX", FormatNumber(compositing.GlowSize)),
                    new XAttribute("blurY", FormatNumber(compositing.GlowSize)),
                    new XAttribute("color", compositing.GlowColor),
                    new XAttribute("alpha", FormatNumber(compositing.GlowOpacity)),
                    new XAttribute("strength", 1),
                    new XAttribute("quality", (int)compositing.GlowQuality),
                    new XAttribute("inner", false),
                    new XAttribute("knockout", compositing.GlowKnockout)));
        }

        if (compositing.ShadowOpacity > 0d &&
            (Math.Abs(compositing.ShadowOffsetX) > ComparisonTolerance ||
             Math.Abs(compositing.ShadowOffsetY) > ComparisonTolerance ||
             compositing.ShadowBlur > 0.1d))
        {
            var angle = Math.Atan2(compositing.ShadowOffsetY, compositing.ShadowOffsetX) * 180d / Math.PI;
            var distance = Math.Sqrt((compositing.ShadowOffsetX * compositing.ShadowOffsetX) + (compositing.ShadowOffsetY * compositing.ShadowOffsetY));
            filters.Add(
                new XElement(
                    s_xflNs + "DropShadowFilter",
                    new XAttribute("blurX", FormatNumber(compositing.ShadowBlur)),
                    new XAttribute("blurY", FormatNumber(compositing.ShadowBlur)),
                    new XAttribute("color", compositing.ShadowColor),
                    new XAttribute("alpha", FormatNumber(compositing.ShadowOpacity)),
                    new XAttribute("angle", FormatNumber(angle)),
                    new XAttribute("distance", FormatNumber(distance)),
                    new XAttribute("strength", 1),
                    new XAttribute("quality", (int)compositing.ShadowQuality),
                    new XAttribute("inner", false),
                    new XAttribute("knockout", compositing.ShadowKnockout),
                    new XAttribute("hideObject", compositing.ShadowHideObject)));
        }

        if (compositing.BevelSize > 0.1d && compositing.BevelStrength > 0.01d)
        {
            filters.Add(
                new XElement(
                    s_xflNs + "BevelFilter",
                    new XAttribute("blurX", FormatNumber(compositing.BevelSize)),
                    new XAttribute("blurY", FormatNumber(compositing.BevelSize)),
                    new XAttribute("angle", FormatNumber(compositing.BevelAngle)),
                    new XAttribute("distance", FormatNumber(compositing.BevelDistance)),
                    new XAttribute("strength", FormatNumber(compositing.BevelStrength)),
                    new XAttribute("highlightColor", compositing.BevelHighlightColor),
                    new XAttribute("shadowColor", compositing.BevelShadowColor),
                    new XAttribute("quality", (int)compositing.BevelQuality),
                    new XAttribute("knockout", compositing.BevelKnockout),
                    new XAttribute("type", compositing.BevelType.ToString().ToLowerInvariant())));
        }

        if (compositing.GradientGlowSize > 0.1d && compositing.GradientGlowStrength > 0.01d)
        {
            filters.Add(
                new XElement(
                    s_xflNs + "GradientGlowFilter",
                    new XAttribute("blurX", FormatNumber(compositing.GradientGlowSize)),
                    new XAttribute("blurY", FormatNumber(compositing.GradientGlowSize)),
                    new XAttribute("angle", FormatNumber(compositing.GradientGlowAngle)),
                    new XAttribute("distance", FormatNumber(compositing.GradientGlowDistance)),
                    new XAttribute("strength", FormatNumber(compositing.GradientGlowStrength)),
                    new XAttribute("fromColor", compositing.GradientGlowFrom),
                    new XAttribute("toColor", compositing.GradientGlowTo),
                    new XAttribute("quality", (int)compositing.GradientGlowQuality),
                    new XAttribute("knockout", compositing.GradientGlowKnockout)));
        }

        if (compositing.GradientBevelSize > 0.1d && compositing.GradientBevelStrength > 0.01d)
        {
            filters.Add(
                new XElement(
                    s_xflNs + "GradientBevelFilter",
                    new XAttribute("blurX", FormatNumber(compositing.GradientBevelSize)),
                    new XAttribute("blurY", FormatNumber(compositing.GradientBevelSize)),
                    new XAttribute("angle", FormatNumber(compositing.GradientBevelAngle)),
                    new XAttribute("distance", FormatNumber(compositing.GradientBevelDistance)),
                    new XAttribute("strength", FormatNumber(compositing.GradientBevelStrength)),
                    new XAttribute("type", compositing.GradientBevelType.ToString().ToLowerInvariant()),
                    new XAttribute("fromColor", compositing.GradientBevelFrom),
                    new XAttribute("toColor", compositing.GradientBevelTo),
                    new XAttribute("quality", (int)compositing.GradientBevelQuality),
                    new XAttribute("knockout", compositing.GradientBevelKnockout)));
        }

        return filters.Count == 0 ? null : new XElement(s_xflNs + "filters", filters);
    }

    private static XElement? BuildBehaviorContainer(IReadOnlyList<InteractionBehaviorModel> behaviors)
    {
        if (behaviors.Count == 0)
        {
            return null;
        }

        return new XElement(
            s_xflNs + "behaviors",
            behaviors.Select(behavior =>
                new XElement(
                    s_xflNs + "DOMBehavior",
                    new XAttribute("name", behavior.Name),
                    new XAttribute("enabled", behavior.IsEnabled),
                    new XAttribute("trigger", behavior.Trigger),
                    new XAttribute("triggerArgument", behavior.TriggerArgument ?? string.Empty),
                    new XAttribute("action", behavior.Action),
                    new XAttribute("targetSceneId", behavior.TargetSceneId?.ToString() ?? string.Empty),
                    new XAttribute("targetFrameLabel", behavior.TargetFrameLabel ?? string.Empty),
                    new XAttribute("targetLayerId", behavior.TargetLayerId?.ToString() ?? string.Empty),
                    new XAttribute("targetButtonState", behavior.TargetButtonState),
                    new XAttribute("boolValue", behavior.BoolValue),
                    new XAttribute("variableName", behavior.VariableName ?? string.Empty),
                    new XAttribute("variableValue", behavior.VariableValue ?? string.Empty),
                    new XAttribute("targetVisualStateGroup", behavior.TargetVisualStateGroup ?? string.Empty),
                    new XAttribute("targetVisualState", behavior.TargetVisualState ?? string.Empty),
                    new XAttribute("script", behavior.Script ?? string.Empty))));
    }

    private static XElement? BuildComponentParameterCollectionElement(LibraryItem item)
    {
        if (!item.IsComponent || item.ComponentParameters.Count == 0)
        {
            return null;
        }

        return new XElement(
            s_xflNs + "componentParameters",
            item.ComponentParameters.Select(parameter =>
                new XElement(
                    s_xflNs + "DOMComponentParameter",
                    new XAttribute("name", parameter.Name),
                    new XAttribute("kind", parameter.Kind),
                    new XAttribute("bindingTarget", parameter.BindingTarget),
                    new XAttribute("targetLayerName", parameter.TargetLayerName ?? string.Empty),
                    new XAttribute("defaultValue", parameter.DefaultValue ?? string.Empty),
                    new XAttribute("description", parameter.Description ?? string.Empty))));
    }

    private static XElement? BuildComponentOverrideCollectionElement(TimelineLayer layer)
    {
        if (layer.ComponentParameterOverrides.Count == 0)
        {
            return null;
        }

        return new XElement(
            s_xflNs + "componentOverrides",
            layer.ComponentParameterOverrides.Select(parameter =>
                new XElement(
                    s_xflNs + "DOMComponentOverride",
                    new XAttribute("name", parameter.Name ?? string.Empty),
                    new XAttribute("value", parameter.Value ?? string.Empty))));
    }

    private static XElement? BuildCompositingElement(LayerCompositeSettings compositing)
    {
        if (compositing.Role == LayerCompositeRole.Normal &&
            compositing.BlendMode == LayerBlendMode.Normal &&
            compositing.MaskLayerCount == 1 &&
            NearlyEqual(compositing.BlurRadius, 0d) &&
            NearlyEqual(compositing.GlowOpacity, 0d) &&
            NearlyEqual(compositing.GlowSize, 18d) &&
            NearlyEqual(compositing.ShadowBlur, 0d) &&
            NearlyEqual(compositing.ShadowOpacity, 0d) &&
            NearlyEqual(compositing.ShadowOffsetX, 0d) &&
            NearlyEqual(compositing.ShadowOffsetY, 0d) &&
            NearlyEqual(compositing.TintStrength, 0d) &&
            NearlyEqual(compositing.Brightness, 0d) &&
            NearlyEqual(compositing.Contrast, 0d) &&
            NearlyEqual(compositing.HueShift, 0d) &&
            NearlyEqual(compositing.Saturation, 1d) &&
            NearlyEqual(compositing.BevelSize, 0d) &&
            NearlyEqual(compositing.BevelStrength, 0d) &&
            NearlyEqual(compositing.GradientGlowSize, 0d) &&
            NearlyEqual(compositing.GradientGlowStrength, 0d) &&
            NearlyEqual(compositing.GradientBevelSize, 0d) &&
            NearlyEqual(compositing.GradientBevelStrength, 0d) &&
            NearlyEqual(compositing.ParallaxDepth, 1d))
        {
            return null;
        }

        return new XElement(
            s_xflNs + "compositing",
            new XAttribute("role", compositing.Role),
            new XAttribute("blendMode", ToXflBlendMode(compositing.BlendMode)),
            new XAttribute("maskLayerCount", compositing.MaskLayerCount),
            new XAttribute("blurRadius", FormatNumber(compositing.BlurRadius)),
            new XAttribute("blurQuality", (int)compositing.BlurQuality),
            new XAttribute("glowSize", FormatNumber(compositing.GlowSize)),
            new XAttribute("glowOpacity", FormatNumber(compositing.GlowOpacity)),
            new XAttribute("glowColor", compositing.GlowColor),
            new XAttribute("glowQuality", (int)compositing.GlowQuality),
            new XAttribute("glowKnockout", compositing.GlowKnockout),
            new XAttribute("shadowBlur", FormatNumber(compositing.ShadowBlur)),
            new XAttribute("shadowOpacity", FormatNumber(compositing.ShadowOpacity)),
            new XAttribute("shadowOffsetX", FormatNumber(compositing.ShadowOffsetX)),
            new XAttribute("shadowOffsetY", FormatNumber(compositing.ShadowOffsetY)),
            new XAttribute("shadowColor", compositing.ShadowColor),
            new XAttribute("shadowQuality", (int)compositing.ShadowQuality),
            new XAttribute("shadowKnockout", compositing.ShadowKnockout),
            new XAttribute("shadowHideObject", compositing.ShadowHideObject),
            new XAttribute("tintStrength", FormatNumber(compositing.TintStrength)),
            new XAttribute("tintColor", compositing.TintColor),
            new XAttribute("brightness", FormatNumber(compositing.Brightness)),
            new XAttribute("contrast", FormatNumber(compositing.Contrast)),
            new XAttribute("hueShift", FormatNumber(compositing.HueShift)),
            new XAttribute("saturation", FormatNumber(compositing.Saturation)),
            new XAttribute("bevelType", compositing.BevelType),
            new XAttribute("bevelSize", FormatNumber(compositing.BevelSize)),
            new XAttribute("bevelStrength", FormatNumber(compositing.BevelStrength)),
            new XAttribute("bevelAngle", FormatNumber(compositing.BevelAngle)),
            new XAttribute("bevelDistance", FormatNumber(compositing.BevelDistance)),
            new XAttribute("bevelHighlightColor", compositing.BevelHighlightColor),
            new XAttribute("bevelShadowColor", compositing.BevelShadowColor),
            new XAttribute("bevelQuality", (int)compositing.BevelQuality),
            new XAttribute("bevelKnockout", compositing.BevelKnockout),
            new XAttribute("gradientGlowSize", FormatNumber(compositing.GradientGlowSize)),
            new XAttribute("gradientGlowStrength", FormatNumber(compositing.GradientGlowStrength)),
            new XAttribute("gradientGlowAngle", FormatNumber(compositing.GradientGlowAngle)),
            new XAttribute("gradientGlowDistance", FormatNumber(compositing.GradientGlowDistance)),
            new XAttribute("gradientGlowFrom", compositing.GradientGlowFrom),
            new XAttribute("gradientGlowTo", compositing.GradientGlowTo),
            new XAttribute("gradientGlowQuality", (int)compositing.GradientGlowQuality),
            new XAttribute("gradientGlowKnockout", compositing.GradientGlowKnockout),
            new XAttribute("gradientBevelType", compositing.GradientBevelType),
            new XAttribute("gradientBevelSize", FormatNumber(compositing.GradientBevelSize)),
            new XAttribute("gradientBevelStrength", FormatNumber(compositing.GradientBevelStrength)),
            new XAttribute("gradientBevelAngle", FormatNumber(compositing.GradientBevelAngle)),
            new XAttribute("gradientBevelDistance", FormatNumber(compositing.GradientBevelDistance)),
            new XAttribute("gradientBevelFrom", compositing.GradientBevelFrom),
            new XAttribute("gradientBevelTo", compositing.GradientBevelTo),
            new XAttribute("gradientBevelQuality", (int)compositing.GradientBevelQuality),
            new XAttribute("gradientBevelKnockout", compositing.GradientBevelKnockout),
            new XAttribute("parallaxDepth", FormatNumber(compositing.ParallaxDepth)));
    }

    private static XElement? BuildMediaSettingsElement(TimelineLayer layer)
    {
        if (layer.Kind is not LayerKind.Video and not LayerKind.Audio)
        {
            return null;
        }

        return new XElement(
            s_xflNs + "mediaSettings",
            new XAttribute("playbackMode", layer.Media.PlaybackMode),
            new XAttribute("startTime", FormatNumber(layer.Media.StartTime)),
            new XAttribute("clipOffset", FormatNumber(layer.Media.ClipOffset)),
            new XAttribute("clipDuration", FormatNumber(layer.Media.ClipDuration)),
            new XAttribute("loop", layer.Media.Loop),
            new XAttribute("repeatCount", Math.Max(1, layer.Media.RepeatCount)),
            new XAttribute("repeatForever", layer.Media.RepeatForever || layer.Media.Loop),
            new XAttribute("volume", FormatNumber(layer.Media.Volume)),
            new XAttribute("gainDb", FormatNumber(layer.Media.GainDb)),
            new XAttribute("pan", FormatNumber(layer.Media.Pan)),
            new XAttribute("fadeIn", FormatNumber(layer.Media.FadeInDuration)),
            new XAttribute("fadeOut", FormatNumber(layer.Media.FadeOutDuration)));
    }

    private static XElement? BuildTrackCollectionElement(TimelineLayer layer)
    {
        var tracks = layer.Tracks
            .Where(track => track.Keyframes.Count > 0)
            .OrderBy(track => track.Property)
            .ToList();
        if (tracks.Count == 0)
        {
            return null;
        }

        return new XElement(
            s_xflNs + "motionTracks",
            tracks.Select(track =>
                new XElement(
                    s_xflNs + "DOMPropertyTrack",
                    new XAttribute("property", track.Property),
                    track.Keyframes.OrderBy(keyframe => keyframe.Time).Select(keyframe =>
                        new XElement(
                            s_xflNs + "DOMKeyframe",
                            new XAttribute("time", FormatNumber(keyframe.Time)),
                            new XAttribute("value", FormatNumber(keyframe.Value)),
                            new XAttribute("easing", keyframe.Easing),
                            new XAttribute("cp1x", FormatNumber(keyframe.ControlPoint1X)),
                            new XAttribute("cp1y", FormatNumber(keyframe.ControlPoint1Y)),
                            new XAttribute("cp2x", FormatNumber(keyframe.ControlPoint2X)),
                            new XAttribute("cp2y", FormatNumber(keyframe.ControlPoint2Y)))))));
    }

    private static XElement? BuildShapeKeyframeCollectionElement(TimelineLayer layer)
    {
        if (layer.ShapeKeyframes.Count == 0)
        {
            return null;
        }

        return new XElement(
            s_xflNs + "shapeTweens",
            layer.ShapeKeyframes.OrderBy(keyframe => keyframe.Time).Select(keyframe =>
                new XElement(
                    s_xflNs + "DOMShapeKeyframe",
                    new XAttribute("time", FormatNumber(keyframe.Time)),
                    new XAttribute("easing", keyframe.Easing),
                    new XAttribute("cp1x", FormatNumber(keyframe.ControlPoint1X)),
                    new XAttribute("cp1y", FormatNumber(keyframe.ControlPoint1Y)),
                    new XAttribute("cp2x", FormatNumber(keyframe.ControlPoint2X)),
                    new XAttribute("cp2y", FormatNumber(keyframe.ControlPoint2Y)),
                    new XAttribute("isClosed", keyframe.IsClosed),
                    new XAttribute("pathData", BuildPathData(keyframe.PathPoints, 1d, 1d, keyframe.IsClosed)))));
    }

    private static XElement? BuildWarpElement(LayerWarpSettings warp)
    {
        if (!warp.IsEnabled && warp.Pins.Count == 0)
        {
            return null;
        }

        return new XElement(
            s_xflNs + "warp",
            new XAttribute("enabled", warp.IsEnabled),
            new XAttribute("meshColumns", warp.MeshColumns),
            new XAttribute("meshRows", warp.MeshRows),
            warp.Pins.Select(pin =>
                new XElement(
                    s_xflNs + "pin",
                    new XAttribute("id", pin.Id),
                    new XAttribute("bindX", FormatNumber(pin.BindX)),
                    new XAttribute("bindY", FormatNumber(pin.BindY)),
                    new XAttribute("x", FormatNumber(pin.X)),
                    new XAttribute("y", FormatNumber(pin.Y)),
                    new XAttribute("radius", FormatNumber(pin.Radius)),
                    new XAttribute("strength", FormatNumber(pin.Strength)))));
    }

    private static XElement? BuildRigElement(LayerRigSettings rig)
    {
        if (!rig.IsEnabled && rig.Bones.Count == 0 && rig.Poses.Count == 0)
        {
            return null;
        }

        return new XElement(
            s_xflNs + "rig",
            new XAttribute("enabled", rig.IsEnabled),
            new XElement(
                s_xflNs + "bones",
                rig.Bones.Select(bone =>
                    new XElement(
                        s_xflNs + "bone",
                        new XAttribute("id", bone.Id),
                        new XAttribute("name", bone.Name ?? string.Empty),
                        new XAttribute("parentBoneId", bone.ParentBoneId?.ToString() ?? string.Empty),
                        new XAttribute("bindStartX", FormatNumber(bone.BindStartX)),
                        new XAttribute("bindStartY", FormatNumber(bone.BindStartY)),
                        new XAttribute("startX", FormatNumber(bone.StartX)),
                        new XAttribute("startY", FormatNumber(bone.StartY)),
                        new XAttribute("length", FormatNumber(bone.Length)),
                        new XAttribute("rotation", FormatNumber(bone.Rotation)),
                        new XAttribute("envelope", FormatNumber(bone.Envelope))))),
            new XElement(
                s_xflNs + "poses",
                rig.Poses.Select(pose =>
                    new XElement(
                        s_xflNs + "pose",
                        new XAttribute("id", pose.Id),
                        new XAttribute("name", pose.Name ?? string.Empty),
                        new XAttribute("time", FormatNumber(pose.Time)),
                        pose.Bones.Select(poseBone =>
                            new XElement(
                                s_xflNs + "bonePose",
                                new XAttribute("boneId", poseBone.BoneId),
                                new XAttribute("startX", FormatNumber(poseBone.StartX)),
                                new XAttribute("startY", FormatNumber(poseBone.StartY)),
                                new XAttribute("length", FormatNumber(poseBone.Length)),
                                new XAttribute("rotation", FormatNumber(poseBone.Rotation))))))));
    }

    private static List<MediaAsset> ParseMediaAssets(XElement root, FlashAuthoringPackage? package)
    {
        var manifestItems = ParseMediaManifestItems(package);
        var media = root.Elements().FirstOrDefault(element => element.Name.LocalName == "media");
        if (media is null)
        {
            return manifestItems.Count == 0
                ? []
                : manifestItems.Select(item => ParseMediaAssetFromManifest(item, package)).ToList();
        }

        var assets = media.Elements()
            .Where(element => element.Name.LocalName is "DOMMediaItem" or "DOMSoundItem" or "DOMBitmapItem" or "DOMVideoItem")
            .Select(element => ParseMediaAsset(element, package, manifestItems))
            .ToList();
        var knownAssetNames = new HashSet<string>(assets.Select(asset => asset.Name), StringComparer.OrdinalIgnoreCase);
        foreach (var manifestItem in manifestItems.Where(item => !knownAssetNames.Contains(item.Name)))
        {
            assets.Add(ParseMediaAssetFromManifest(manifestItem, package));
        }

        return assets;
    }

    private static MediaAsset ParseMediaAsset(
        XElement element,
        FlashAuthoringPackage? package,
        IReadOnlyList<FlashMediaManifestItem> manifestItems)
    {
        var kind = ParseMediaAssetKind(element);
        var relativePath = NormalizePackageRelativePath(TryGetAttributeValue(element, "relativePath"));
        var frameFolder = NormalizePackageRelativePath(TryGetAttributeValue(element, "frameFolder"));
        var manifestItem = FindMediaManifestItem(
            manifestItems,
            TryGetAttributeValue(element, "name"),
            relativePath,
            frameFolder,
            kind);
        var asset = new MediaAsset
        {
            Name = TryGetAttributeValue(element, "name") ?? "Media",
            Kind = kind,
            SourceLabel = TryGetAttributeValue(element, "sourceLabel") ?? manifestItem?.SourceLabel ?? string.Empty,
            PackageRelativePath = relativePath ?? manifestItem?.RelativePath ?? string.Empty,
            Duration = ParseDouble(TryGetAttributeValue(element, "durationSeconds"), manifestItem?.Duration ?? 1d),
            FrameRate = ParseDouble(TryGetAttributeValue(element, "frameRate"), manifestItem?.FrameRate ?? 12d),
            Width = ParseInt(TryGetAttributeValue(element, "width"), manifestItem?.Width ?? 0),
            Height = ParseInt(TryGetAttributeValue(element, "height"), manifestItem?.Height ?? 0),
            SampleRate = ParseInt(TryGetAttributeValue(element, "sampleRate"), manifestItem?.SampleRate ?? 0),
            ChannelCount = ParseInt(TryGetAttributeValue(element, "channelCount"), manifestItem?.ChannelCount ?? 1),
            PreviewFill = TryGetAttributeValue(element, "previewFill") ?? manifestItem?.PreviewFill ?? "#61E6FF",
            PreviewAccent = TryGetAttributeValue(element, "previewAccent") ?? manifestItem?.PreviewAccent ?? "#FFD166"
        };

        if (package is not null)
        {
            HydrateMediaAssetFromPackage(
                asset,
                package,
                relativePath ?? manifestItem?.RelativePath,
                frameFolder ?? manifestItem?.FrameFolder,
                TryGetAttributeValue(element, "frameExtension") ?? manifestItem?.FrameExtension);
        }

        return asset;
    }

    private static MediaAsset ParseMediaAssetFromManifest(FlashMediaManifestItem manifestItem, FlashAuthoringPackage? package)
    {
        var asset = new MediaAsset
        {
            Name = manifestItem.Name,
            Kind = manifestItem.Kind,
            SourceLabel = manifestItem.SourceLabel,
            PackageRelativePath = manifestItem.RelativePath ?? manifestItem.FrameFolder ?? string.Empty,
            Duration = manifestItem.Duration,
            FrameRate = manifestItem.FrameRate,
            Width = manifestItem.Width,
            Height = manifestItem.Height,
            SampleRate = manifestItem.SampleRate,
            ChannelCount = manifestItem.ChannelCount,
            PreviewFill = manifestItem.PreviewFill,
            PreviewAccent = manifestItem.PreviewAccent
        };

        if (package is not null)
        {
            HydrateMediaAssetFromPackage(asset, package, manifestItem.RelativePath, manifestItem.FrameFolder, manifestItem.FrameExtension);
        }

        return asset;
    }

    private static List<FlashMediaManifestItem> ParseMediaManifestItems(FlashAuthoringPackage? package)
    {
        if (package is null)
        {
            return [];
        }

        var manifestEntry = package.Entries.FirstOrDefault(static entry => entry.Kind == FlashAuthoringPackageEntryKind.MediaManifest);
        if (manifestEntry is null || manifestEntry.Data.Length == 0)
        {
            return [];
        }

        var xml = Encoding.UTF8.GetString(manifestEntry.Data).TrimStart('\uFEFF');
        if (string.IsNullOrWhiteSpace(xml))
        {
            return [];
        }

        var root = XDocument.Parse(xml, LoadOptions.PreserveWhitespace).Root;
        if (root is null)
        {
            return [];
        }

        return root.Elements()
            .Where(static element => element.Name.LocalName == "Item")
            .Select(ParseMediaManifestItem)
            .ToList();
    }

    private static FlashMediaManifestItem ParseMediaManifestItem(XElement element)
    {
        var relativePath = NormalizePackageRelativePath(TryGetAttributeValue(element, "relativePath"));
        var frameFolder = NormalizePackageRelativePath(TryGetAttributeValue(element, "frameFolder"));
        return new FlashMediaManifestItem(
            TryGetAttributeValue(element, "name") ?? "Media",
            Enum.TryParse<MediaAssetKind>(TryGetAttributeValue(element, "type"), true, out var kind) ? kind : MediaAssetKind.Video,
            ParseDouble(TryGetAttributeValue(element, "durationSeconds"), 1d),
            ParseDouble(TryGetAttributeValue(element, "frameRate"), 12d),
            ParseInt(TryGetAttributeValue(element, "width"), 0),
            ParseInt(TryGetAttributeValue(element, "height"), 0),
            ParseInt(TryGetAttributeValue(element, "sampleRate"), 0),
            ParseInt(TryGetAttributeValue(element, "channelCount"), 1),
            TryGetAttributeValue(element, "sourceLabel") ?? string.Empty,
            relativePath,
            frameFolder,
            ParseInt(TryGetAttributeValue(element, "frameCount"), 0),
            TryGetAttributeValue(element, "frameExtension"),
            TryGetAttributeValue(element, "previewFill") ?? "#61E6FF",
            TryGetAttributeValue(element, "previewAccent") ?? "#FFD166");
    }

    private static FlashMediaManifestItem? FindMediaManifestItem(
        IReadOnlyList<FlashMediaManifestItem> manifestItems,
        string? name,
        string? relativePath,
        string? frameFolder,
        MediaAssetKind kind)
    {
        var normalizedRelativePath = NormalizePackageRelativePath(relativePath);
        var normalizedFrameFolder = NormalizePackageRelativePath(frameFolder);
        return manifestItems.FirstOrDefault(item =>
                   item.Kind == kind &&
                   !string.IsNullOrWhiteSpace(name) &&
                   string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)) ??
               manifestItems.FirstOrDefault(item =>
                   item.Kind == kind &&
                   !string.IsNullOrWhiteSpace(normalizedRelativePath) &&
                   string.Equals(item.RelativePath, normalizedRelativePath, StringComparison.OrdinalIgnoreCase)) ??
               manifestItems.FirstOrDefault(item =>
                   item.Kind == kind &&
                   !string.IsNullOrWhiteSpace(normalizedFrameFolder) &&
                   string.Equals(item.FrameFolder, normalizedFrameFolder, StringComparison.OrdinalIgnoreCase));
    }

    private static List<LibraryItem> ParseLibraryItems(
        XElement root,
        IReadOnlyDictionary<string, MediaAsset> mediaByName,
        List<AnimationExchangeIssue> issues,
        IReadOnlyList<XElement> packagedSymbolElements)
    {
        var symbols = root.Elements().FirstOrDefault(element => element.Name.LocalName == "symbols");
        if (symbols is null && packagedSymbolElements.Count == 0)
        {
            return [];
        }

        var symbolElements = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
        if (symbols is not null)
        {
            foreach (var symbolElement in symbols.Elements().Where(element => element.Name.LocalName == "DOMSymbolItem"))
            {
                var name = TryGetAttributeValue(symbolElement, "name");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    symbolElements[name] = symbolElement;
                }
            }
        }

        foreach (var packagedSymbolElement in packagedSymbolElements)
        {
            var name = TryGetAttributeValue(packagedSymbolElement, "name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                symbolElements[name] = packagedSymbolElement;
            }
        }

        var orderedElements = symbolElements.Values.OrderBy(element => TryGetAttributeValue(element, "name"), StringComparer.OrdinalIgnoreCase).ToList();
        var items = new List<LibraryItem>(orderedElements.Count);
        var itemByName = new Dictionary<string, LibraryItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var symbolElement in orderedElements)
        {
            var item = new LibraryItem
            {
                Name = TryGetAttributeValue(symbolElement, "name") ?? "Symbol",
                SymbolKind = ParseFlashSymbolType(TryGetAttributeValue(symbolElement, "symbolType")),
                FolderPath = LibraryManagementService.NormalizeFolderPath(TryGetAttributeValue(symbolElement, "folderPath")),
                LinkageId = TryGetAttributeValue(symbolElement, "linkageClassName") ?? string.Empty,
                BaseClassName = LibraryManagementService.NormalizeClassName(TryGetAttributeValue(symbolElement, "linkageBaseClass")),
                ExportForRuntimeSharing = ParseBool(TryGetAttributeValue(symbolElement, "linkageExportForRuntime"), false),
                ImportForRuntimeSharing = ParseBool(TryGetAttributeValue(symbolElement, "linkageImportForRuntime"), false),
                ExportInFirstFrame = ParseBool(TryGetAttributeValue(symbolElement, "linkageExportInFirstFrame"), true),
                SharedLibraryPath = LibraryManagementService.NormalizeSharedLibraryPath(TryGetAttributeValue(symbolElement, "sharedLibraryPath")),
                UpdateAutomatically = ParseBool(TryGetAttributeValue(symbolElement, "sharedLibraryAutoUpdate"), false),
                SourceAssetPath = TryGetAttributeValue(symbolElement, "sourceAssetPath") ?? string.Empty,
                Duration = ParseDouble(TryGetAttributeValue(symbolElement, "durationSeconds"), 2d),
                FrameRate = ParseDouble(TryGetAttributeValue(symbolElement, "frameRate"), 24d),
                IsComponent = ParseBool(TryGetAttributeValue(symbolElement, "isComponent"), false),
                ComponentCategory = TryGetAttributeValue(symbolElement, "componentCategory") ?? string.Empty,
                ComponentDescription = TryGetAttributeValue(symbolElement, "componentDescription") ?? string.Empty,
                UseScale9Grid = ParseBool(TryGetAttributeValue(symbolElement, "useScale9Grid"), false),
                Scale9Left = ParseDouble(TryGetAttributeValue(symbolElement, "scaleGridLeft"), 16d),
                Scale9Top = ParseDouble(TryGetAttributeValue(symbolElement, "scaleGridTop"), 16d),
                Scale9Right = ParseDouble(TryGetAttributeValue(symbolElement, "scaleGridRight"), 144d),
                Scale9Bottom = ParseDouble(TryGetAttributeValue(symbolElement, "scaleGridBottom"), 84d),
                RegistrationPointX = ParseDouble(TryGetAttributeValue(symbolElement, "registrationPointX"), 0d),
                RegistrationPointY = ParseDouble(TryGetAttributeValue(symbolElement, "registrationPointY"), 0d)
            };
            LibraryManagementService.NormalizeScale9Grid(item);
            LibraryManagementService.NormalizeRegistrationPoint(item);
            item.ComponentParameters = ParseComponentParameters(symbolElement);
            items.Add(item);
            itemByName[item.Name] = item;
        }

        foreach (var symbolElement in orderedElements)
        {
            var name = TryGetAttributeValue(symbolElement, "name") ?? "Symbol";
            if (!itemByName.TryGetValue(name, out var item))
            {
                continue;
            }

            item.DefaultBehaviors = ParseBehaviors(symbolElement);
            if (item.SymbolKind == SymbolKind.Button)
            {
                var buttonStates = symbolElement.Elements().FirstOrDefault(element => element.Name.LocalName == "buttonStates");
                if (buttonStates is not null)
                {
                    foreach (var stateElement in buttonStates.Elements().Where(element => element.Name.LocalName == "DOMButtonState"))
                    {
                        var state = Enum.TryParse<ButtonVisualState>(TryGetAttributeValue(stateElement, "state"), true, out var parsedState)
                            ? parsedState
                            : ButtonVisualState.Up;
                        var layers = ParseLayers(
                            stateElement.Elements().FirstOrDefault(element => element.Name.LocalName == "layers"),
                            item.Duration,
                            item.FrameRate,
                            itemByName,
                            mediaByName,
                            issues);
                        TimelineEditingService.SetEditableSymbolLayers(item, layers, item.Duration, item.FrameRate, state);
                    }
                }
            }
            else
            {
                var timelineElement = symbolElement.Elements().FirstOrDefault(element => element.Name.LocalName == "DOMTimeline");
                if (timelineElement is not null)
                {
                    var layers = ParseLayers(
                        timelineElement.Elements().FirstOrDefault(element => element.Name.LocalName == "layers"),
                        item.Duration,
                        item.FrameRate,
                        itemByName,
                        mediaByName,
                        issues);
                    TimelineEditingService.SetEditableSymbolLayers(item, layers, item.Duration, item.FrameRate, ButtonVisualState.Up);
                }
            }
        }

        return items;
    }

    private static SceneModel ParseScene(
        XElement timeline,
        int index,
        IReadOnlyDictionary<string, LibraryItem> libraryByName,
        IReadOnlyDictionary<string, MediaAsset> mediaByName,
        List<AnimationExchangeIssue> issues)
    {
        var frameRate = ParseDouble(TryGetAttributeValue(timeline, "frameRate"), 24d);
        var duration = ParseDouble(TryGetAttributeValue(timeline, "durationSeconds"), 6d);
        var scene = new SceneModel
        {
            Name = TryGetAttributeValue(timeline, "name") ?? $"Scene {index + 1}",
            FrameRate = frameRate,
            Duration = duration,
            CanvasWidth = ParseDouble(TryGetAttributeValue(timeline, "canvasWidth"), 1280d),
            CanvasHeight = ParseDouble(TryGetAttributeValue(timeline, "canvasHeight"), 720d),
            TransparentStageBackground = ParseBool(TryGetAttributeValue(timeline, "transparentStageBackground"), false),
            BackgroundFrom = TryGetAttributeValue(timeline, "backgroundColor") ?? "#09111F",
            BackgroundTo = TryGetAttributeValue(timeline, "backgroundColor2") ?? TryGetAttributeValue(timeline, "backgroundColor") ?? "#09111F",
            InFrame = ParseInt(TryGetAttributeValue(timeline, "inFrame"), 0),
            OutFrame = ParseInt(TryGetAttributeValue(timeline, "outFrame"), 0),
            WorkAreaStartFrame = ParseInt(TryGetAttributeValue(timeline, "workAreaStart"), 0),
            WorkAreaEndFrame = ParseInt(TryGetAttributeValue(timeline, "workAreaEnd"), 0)
        };

        var totalFrames = FrameTimelineService.GetTotalFrames(scene.Duration, scene.FrameRate);
        scene.OutFrame = scene.OutFrame <= 0 ? totalFrames - 1 : Math.Min(scene.OutFrame, totalFrames - 1);
        scene.WorkAreaEndFrame = scene.WorkAreaEndFrame <= 0 ? totalFrames - 1 : Math.Min(scene.WorkAreaEndFrame, totalFrames - 1);

        var labels = timeline.Elements().FirstOrDefault(element => element.Name.LocalName == "frameLabels");
        if (labels is not null)
        {
            scene.FrameLabels = labels.Elements()
                .Where(element => element.Name.LocalName == "DOMFrameLabel")
                .Select(element => new FrameLabelModel
                {
                    Frame = ParseInt(TryGetAttributeValue(element, "frame"), 0),
                    Name = TryGetAttributeValue(element, "name") ?? string.Empty,
                    Script = TryGetAttributeValue(element, "script") ?? string.Empty
                })
                .OrderBy(label => label.Frame)
                .ToList();
        }

        var markers = timeline.Elements().FirstOrDefault(element => element.Name.LocalName == "markers");
        if (markers is not null)
        {
            scene.Markers = markers.Elements()
                .Where(element => element.Name.LocalName == "DOMMarker")
                .Select(element => new SceneMarkerModel
                {
                    Frame = ParseInt(TryGetAttributeValue(element, "frame"), 0),
                    Name = TryGetAttributeValue(element, "name") ?? string.Empty,
                    Kind = Enum.TryParse<SceneMarkerKind>(TryGetAttributeValue(element, "kind"), true, out var parsedKind) ? parsedKind : SceneMarkerKind.Cue,
                    Color = TryGetAttributeValue(element, "color") ?? "#57C9FF",
                    Notes = TryGetAttributeValue(element, "notes") ?? string.Empty
                })
                .OrderBy(marker => marker.Frame)
                .ToList();
        }

        scene.Layers = ParseLayers(
            timeline.Elements().FirstOrDefault(element => element.Name.LocalName == "layers"),
            scene.Duration,
            scene.FrameRate,
            libraryByName,
            mediaByName,
            issues);

        return scene;
    }

    private static List<TimelineLayer> ParseLayers(
        XElement? layersElement,
        double duration,
        double frameRate,
        IReadOnlyDictionary<string, LibraryItem> libraryByName,
        IReadOnlyDictionary<string, MediaAsset> mediaByName,
        List<AnimationExchangeIssue> issues)
    {
        if (layersElement is null)
        {
            return [];
        }

        var totalFrames = FrameTimelineService.GetTotalFrames(duration, frameRate);
        return layersElement.Elements()
            .Where(element => element.Name.LocalName == "DOMLayer")
            .Select((element, index) => ParseLayer(element, index, duration, frameRate, totalFrames, libraryByName, mediaByName, issues))
            .OrderBy(layer => layer.ZIndex)
            .ToList();
    }

    private static TimelineLayer ParseLayer(
        XElement element,
        int index,
        double duration,
        double frameRate,
        int totalFrames,
        IReadOnlyDictionary<string, LibraryItem> libraryByName,
        IReadOnlyDictionary<string, MediaAsset> mediaByName,
        List<AnimationExchangeIssue> issues)
    {
        var framesElement = element.Elements().FirstOrDefault(child => child.Name.LocalName == "frames");
        var parsedFrames = ParseFrames(framesElement, totalFrames, libraryByName, mediaByName, issues);
        var firstVisibleFrame = parsedFrames.FirstOrDefault(frame => frame.Sample is not null);
        var baseSample = firstVisibleFrame?.Sample;

        TimelineLayer layer;
        if (baseSample is null)
        {
            layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, TryGetAttributeValue(element, "name") ?? $"Layer {index + 1}", "#24E5C1", string.Empty, 0d, 0d, index);
        }
        else
        {
            layer = CreateLayerFromSample(baseSample, index, mediaByName, libraryByName);
            ApplyLayerSnapshot(layer, baseSample);
        }

        layer.Name = TryGetAttributeValue(element, "name") ?? layer.Name;
        if (Guid.TryParse(TryGetAttributeValue(element, "layerId"), out var layerId))
        {
            layer.Id = layerId;
        }
        layer.Kind = Enum.TryParse<LayerKind>(TryGetAttributeValue(element, "kind"), true, out var declaredKind) ? declaredKind : layer.Kind;
        layer.IsVisible = ParseBool(TryGetAttributeValue(element, "visible"), true);
        layer.IsLocked = ParseBool(TryGetAttributeValue(element, "locked"), false);
        layer.IsMuted = ParseBool(TryGetAttributeValue(element, "muted"), false);
        layer.IsSolo = ParseBool(TryGetAttributeValue(element, "solo"), false);
        layer.IsExpanded = ParseBool(TryGetAttributeValue(element, "expanded"), true);
        layer.ZIndex = ParseInt(TryGetAttributeValue(element, "zIndex"), index);
        if (Guid.TryParse(TryGetAttributeValue(element, "parentLayerId"), out var parentLayerId))
        {
            layer.ParentLayerId = parentLayerId;
        }
        ParseTransformParenting(element, layer);
        if (Guid.TryParse(TryGetAttributeValue(element, "guidedByLayerId"), out var guidedByLayerId))
        {
            layer.GuidedByLayerId = guidedByLayerId;
        }
        layer.OrientToGuidePath = ParseBool(TryGetAttributeValue(element, "orientToGuidePath"), false);
        layer.SnapToGuidePath = ParseBool(TryGetAttributeValue(element, "snapToGuidePath"), false);
        layer.ShowAsOutline = ParseBool(TryGetAttributeValue(element, "outline"), false);
        layer.OutlineColor = TryGetAttributeValue(element, "outlineColor") ?? "#57C9FF";
        layer.CacheAsBitmap = ParseBool(TryGetAttributeValue(element, "cacheAsBitmap"), false);
        layer.BitmapCacheBackgroundColor = TryGetAttributeValue(element, "bitmapCacheBackgroundColor") ?? "#000000";

        layer.Behaviors = ParseBehaviors(element);
        ParseComponentOverrides(element, layer, libraryByName);
        layer.FrameSpans = BuildFrameSpans(parsedFrames);
        ParseCompositing(element, layer.Compositing);
        ParseMediaSettings(element, layer);
        ParseWarp(element, layer);
        ParseRig(element, layer);

        FlashFrameVisual? previousVisible = null;
        foreach (var parsedFrame in parsedFrames)
        {
            if (parsedFrame.Sample is null)
            {
                continue;
            }

            var sample = parsedFrame.Sample;
            var time = FrameTimelineService.FrameToTime(parsedFrame.Frame, frameRate);
            ApplySampleKeyframes(layer, previousVisible, sample, time, duration);
            previousVisible = sample;
        }

        ParseExplicitTracks(element, layer);
        ParseExplicitShapeKeyframes(element, layer);

        return layer;
    }

    private static List<ParsedFrameRecord> ParseFrames(
        XElement? framesElement,
        int totalFrames,
        IReadOnlyDictionary<string, LibraryItem> libraryByName,
        IReadOnlyDictionary<string, MediaAsset> mediaByName,
        List<AnimationExchangeIssue> issues)
    {
        var parsedFrames = Enumerable.Range(0, totalFrames)
            .Select(frame => new ParsedFrameRecord(frame, null, true))
            .ToArray();

        if (framesElement is null)
        {
            return [.. parsedFrames];
        }

        foreach (var frameElement in framesElement.Elements().Where(element => element.Name.LocalName == "DOMFrame"))
        {
            var start = Math.Clamp(ParseInt(TryGetAttributeValue(frameElement, "index"), 0), 0, totalFrames - 1);
            var duration = Math.Max(1, ParseInt(TryGetAttributeValue(frameElement, "duration"), 1));
            var isBlank = ParseBool(TryGetAttributeValue(frameElement, "isBlank"), false);
            var visual = isBlank
                ? null
                : ParseFrameVisual(frameElement, libraryByName, mediaByName, issues);

            for (var frame = start; frame < Math.Min(totalFrames, start + duration); frame++)
            {
                parsedFrames[frame] = new ParsedFrameRecord(frame, visual, isBlank || visual is null);
            }
        }

        return [.. parsedFrames];
    }

    private static FlashFrameVisual? ParseFrameVisual(
        XElement frameElement,
        IReadOnlyDictionary<string, LibraryItem> libraryByName,
        IReadOnlyDictionary<string, MediaAsset> mediaByName,
        List<AnimationExchangeIssue> issues)
    {
        var visualElement = frameElement.Descendants().FirstOrDefault(element =>
            element.Name.LocalName is "DOMSymbolInstance" or "DOMShape" or "DOMStaticText" or "DOMDynamicText" or "DOMInputText" or "DOMComponentInstance" or "DOMMediaInstance");
        if (visualElement is null)
        {
            return null;
        }

        var x = ParseDouble(TryGetAttributeValue(visualElement, "x"), 0d);
        var y = ParseDouble(TryGetAttributeValue(visualElement, "y"), 0d);
        var width = ParseDouble(TryGetAttributeValue(visualElement, "width"), 240d);
        var height = ParseDouble(TryGetAttributeValue(visualElement, "height"), 140d);
        var scaleX = ParseDouble(TryGetAttributeValue(visualElement, "scaleX"), 1d);
        var scaleY = ParseDouble(TryGetAttributeValue(visualElement, "scaleY"), 1d);
        var skewX = ParseDouble(TryGetAttributeValue(visualElement, "skewX"), 0d);
        var skewY = ParseDouble(TryGetAttributeValue(visualElement, "skewY"), 0d);
        var rotation = ParseDouble(TryGetAttributeValue(visualElement, "rotation"), 0d);
        var rotationX = ParseDouble(TryGetAttributeValue(visualElement, "rotationX"), 0d);
        var rotationY = ParseDouble(TryGetAttributeValue(visualElement, "rotationY"), 0d);
        var zDepth = ParseDouble(TryGetAttributeValue(visualElement, "zDepth"), 0d);
        var opacity = ParseDouble(TryGetAttributeValue(visualElement, "opacity"), 1d);
        var fill = TryGetAttributeValue(visualElement, "fill") ?? "#24E5C1";
        var stroke = TryGetAttributeValue(visualElement, "stroke") ?? "#FFFFFF";
        var strokeThickness = ParseDouble(TryGetAttributeValue(visualElement, "strokeThickness"), 1.6d);
        var cornerRadius = ParseDouble(TryGetAttributeValue(visualElement, "cornerRadius"), 28d);
        var fontSize = ParseDouble(TryGetAttributeValue(visualElement, "fontSize"), 42d);
        var text = TryGetAttributeValue(visualElement, "text") ?? string.Empty;
        var hasFill = ParseBool(TryGetAttributeValue(visualElement, "hasFill"), true);
        var hasStroke = ParseBool(TryGetAttributeValue(visualElement, "hasStroke"), true);
        var useGradient = ParseBool(TryGetAttributeValue(visualElement, "useGradient"), false);
        var gradientKind = ParseGradientKind(TryGetAttributeValue(visualElement, "gradientKind"));
        var gradientAngle = ParseDouble(TryGetAttributeValue(visualElement, "gradientAngle"), LayerSnapshotDefaults.DefaultGradientAngle);
        var gradientFrom = TryGetAttributeValue(visualElement, "gradientFrom") ?? fill;
        var gradientTo = TryGetAttributeValue(visualElement, "gradientTo") ?? stroke;
        var strokeCapStyle = ParseStrokeCapStyle(TryGetAttributeValue(visualElement, "strokeCap"));
        var strokeJoinStyle = ParseStrokeJoinStyle(TryGetAttributeValue(visualElement, "strokeJoin"));
        var strokeMiterLimit = ParseDouble(TryGetAttributeValue(visualElement, "strokeMiterLimit"), LayerSnapshotDefaults.DefaultStrokeMiterLimit);
        var drawingMode = ParseShapeDrawingMode(TryGetAttributeValue(visualElement, "drawingMode"));
        var primitiveShape = ParsePrimitiveShapeType(TryGetAttributeValue(visualElement, "primitiveShape"));
        var cornerRadiusTopLeft = ParseDouble(TryGetAttributeValue(visualElement, "cornerRadiusTopLeft"), cornerRadius);
        var cornerRadiusTopRight = ParseDouble(TryGetAttributeValue(visualElement, "cornerRadiusTopRight"), cornerRadius);
        var cornerRadiusBottomRight = ParseDouble(TryGetAttributeValue(visualElement, "cornerRadiusBottomRight"), cornerRadius);
        var cornerRadiusBottomLeft = ParseDouble(TryGetAttributeValue(visualElement, "cornerRadiusBottomLeft"), cornerRadius);
        var ellipseStartAngle = ParseDouble(TryGetAttributeValue(visualElement, "ellipseStartAngle"), 0d);
        var ellipseSweepAngle = ParseDouble(TryGetAttributeValue(visualElement, "ellipseSweepAngle"), 360d);
        var pathData = TryGetAttributeValue(visualElement, "pathData");
        var pathPoints = ParsePathPoints(pathData);
        var isClosed = ParseBool(TryGetAttributeValue(visualElement, "isClosed"), IsClosedPath(pathData));
        var polyStarSides = Math.Clamp(ParseInt(TryGetAttributeValue(visualElement, "polyStarSides"), 5), 3, 16);
        var polyStarInnerRadius = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(visualElement, "polyStarInnerRadius"), 0.46d), 0.08d, 0.95d);
        var polyStarIsStar = ParseBool(TryGetAttributeValue(visualElement, "polyStarIsStar"), true);
        var textSettings = ParseTextSettings(visualElement);
        var visualCompositing = ParseNativeVisualCompositing(visualElement);

        return visualElement.Name.LocalName switch
        {
            "DOMSymbolInstance" => ParseSymbolVisual(
                visualElement,
                libraryByName,
                x,
                y,
                width,
                height,
                scaleX,
                scaleY,
                skewX,
                skewY,
                rotation,
                rotationX,
                rotationY,
                zDepth,
                opacity,
                fill,
                stroke,
                strokeThickness,
                cornerRadius,
                fontSize,
                text,
                hasFill,
                hasStroke,
                useGradient,
                gradientKind,
                gradientAngle,
                gradientFrom,
                gradientTo,
                strokeCapStyle,
                strokeJoinStyle,
                strokeMiterLimit,
                pathPoints,
                isClosed,
                textSettings,
                visualCompositing,
                issues),
            "DOMShape" => new FlashFrameVisual(
                Kind: Enum.TryParse<LayerKind>(TryGetAttributeValue(visualElement, "shapeType"), true, out var shapeKind) ? shapeKind : LayerKind.Rectangle,
                Snapshot: new LayerSnapshot(
                    x,
                    y,
                    width,
                    height,
                    scaleX,
                    scaleY,
                    skewX,
                    skewY,
                    rotation,
                    rotationX,
                    rotationY,
                    zDepth,
                    opacity,
                    cornerRadius,
                    cornerRadiusTopLeft,
                    cornerRadiusTopRight,
                    cornerRadiusBottomRight,
                    cornerRadiusBottomLeft,
                    fill,
                    stroke,
                    string.Empty,
                    fontSize,
                    strokeThickness,
                    useGradient,
                    gradientFrom,
                    gradientTo,
                    drawingMode,
                    primitiveShape == PrimitiveShapeType.None
                        ? shapeKind is LayerKind.Rectangle
                            ? PrimitiveShapeType.Rectangle
                            : shapeKind is LayerKind.Ellipse ? PrimitiveShapeType.Ellipse : PrimitiveShapeType.None
                        : primitiveShape,
                    isClosed,
                    ellipseStartAngle,
                    ellipseSweepAngle,
                    false,
                    "#57C9FF",
                    pathPoints,
                    polyStarSides,
                    polyStarInnerRadius,
                    polyStarIsStar,
                    new AvaloniaControlSettings(),
                    visualCompositing.Clone(),
                    new LayerTextSettings())
                {
                    HasFill = hasFill,
                    HasStroke = hasStroke,
                    GradientKind = gradientKind,
                    GradientAngle = gradientAngle,
                    StrokeCapStyle = strokeCapStyle,
                    StrokeJoinStyle = strokeJoinStyle,
                    StrokeMiterLimit = strokeMiterLimit
                },
                LibraryItemName: null,
                InstanceName: string.Empty,
                SymbolPlaybackMode: SymbolPlaybackMode.SceneTime,
                SymbolPlaybackOffset: 0d,
                SymbolLockedFrame: 0,
                SymbolButtonState: ButtonVisualState.Up,
                CacheAsBitmap: false,
                BitmapCacheBackgroundColor: "#000000",
                MediaName: null,
                MediaKind: null),
            "DOMStaticText" or "DOMDynamicText" or "DOMInputText" => new FlashFrameVisual(
                Kind: LayerKind.Text,
                Snapshot: new LayerSnapshot(
                    x,
                    y,
                    width,
                    height,
                    scaleX,
                    scaleY,
                    skewX,
                    skewY,
                    rotation,
                    rotationX,
                    rotationY,
                    zDepth,
                    opacity,
                    cornerRadius,
                    cornerRadius,
                    cornerRadius,
                    cornerRadius,
                    cornerRadius,
                    fill,
                    stroke,
                    text,
                    fontSize,
                    strokeThickness,
                    useGradient,
                    gradientFrom,
                    gradientTo,
                    ShapeDrawingMode.Merge,
                    PrimitiveShapeType.None,
                    false,
                    0d,
                    360d,
                    false,
                    "#57C9FF",
                    [],
                    5,
                    0.46d,
                    true,
                    new AvaloniaControlSettings(),
                    visualCompositing.Clone(),
                    textSettings)
                {
                    HasFill = hasFill,
                    HasStroke = hasStroke,
                    GradientKind = gradientKind,
                    GradientAngle = gradientAngle,
                    StrokeCapStyle = strokeCapStyle,
                    StrokeJoinStyle = strokeJoinStyle,
                    StrokeMiterLimit = strokeMiterLimit
                },
                LibraryItemName: null,
                InstanceName: string.Empty,
                SymbolPlaybackMode: SymbolPlaybackMode.SceneTime,
                SymbolPlaybackOffset: 0d,
                SymbolLockedFrame: 0,
                SymbolButtonState: ButtonVisualState.Up,
                CacheAsBitmap: false,
                BitmapCacheBackgroundColor: "#000000",
                MediaName: null,
                MediaKind: null),
            "DOMComponentInstance" => ParseControlVisual(visualElement, x, y, width, height, scaleX, scaleY, skewX, skewY, rotation, rotationX, rotationY, zDepth, opacity, fill, stroke, strokeThickness, cornerRadius, hasFill, hasStroke, useGradient, gradientKind, gradientAngle, gradientFrom, gradientTo, strokeCapStyle, strokeJoinStyle, strokeMiterLimit, visualCompositing),
            "DOMMediaInstance" => ParseMediaVisual(visualElement, x, y, width, height, scaleX, scaleY, skewX, skewY, rotation, rotationX, rotationY, zDepth, opacity, fill, stroke, strokeThickness, cornerRadius, hasFill, hasStroke, useGradient, gradientKind, gradientAngle, gradientFrom, gradientTo, strokeCapStyle, strokeJoinStyle, strokeMiterLimit, visualCompositing),
            _ => null
        };
    }

    private static FlashFrameVisual ParseSymbolVisual(
        XElement element,
        IReadOnlyDictionary<string, LibraryItem> libraryByName,
        double x,
        double y,
        double width,
        double height,
        double scaleX,
        double scaleY,
        double skewX,
        double skewY,
        double rotation,
        double rotationX,
        double rotationY,
        double zDepth,
        double opacity,
        string fill,
        string stroke,
        double strokeThickness,
        double cornerRadius,
        double fontSize,
        string text,
        bool hasFill,
        bool hasStroke,
        bool useGradient,
        LayerGradientKind gradientKind,
        double gradientAngle,
        string gradientFrom,
        string gradientTo,
        LayerStrokeCapStyle strokeCapStyle,
        LayerStrokeJoinStyle strokeJoinStyle,
        double strokeMiterLimit,
        IReadOnlyList<VectorPointModel> pathPoints,
        bool isClosed,
        LayerTextSettings textSettings,
        LayerCompositeSettings compositing,
        List<AnimationExchangeIssue> issues)
    {
        var libraryItemName = TryGetAttributeValue(element, "libraryItemName");
        if (!string.IsNullOrWhiteSpace(libraryItemName) && !libraryByName.ContainsKey(libraryItemName))
        {
            issues.Add(CreateIssue(AnimationExchangeIssueSeverity.Warning, "Flash XFL", $"Symbol '{libraryItemName}' was referenced by a layer but was not present in the symbol library."));
        }

        var kind = !string.IsNullOrWhiteSpace(libraryItemName) && libraryByName.TryGetValue(libraryItemName, out var libraryItem)
            ? libraryItem.Template.Kind
            : LayerKind.Rectangle;

        return new FlashFrameVisual(
            Kind: kind,
            Snapshot: new LayerSnapshot(
                x,
                y,
                width,
                height,
                scaleX,
                scaleY,
                skewX,
                skewY,
                rotation,
                rotationX,
                rotationY,
                zDepth,
                opacity,
                cornerRadius,
                cornerRadius,
                cornerRadius,
                cornerRadius,
                cornerRadius,
                fill,
                stroke,
                text,
                fontSize,
                strokeThickness,
                useGradient,
                gradientFrom,
                gradientTo,
                ShapeDrawingMode.Object,
                kind is LayerKind.Rectangle
                    ? PrimitiveShapeType.Rectangle
                    : kind is LayerKind.Ellipse ? PrimitiveShapeType.Ellipse : PrimitiveShapeType.None,
                isClosed,
                0d,
                360d,
                false,
                "#57C9FF",
                pathPoints,
                5,
                0.46d,
                true,
                new AvaloniaControlSettings(),
                compositing.Clone(),
                textSettings.Clone())
            {
                HasFill = hasFill,
                HasStroke = hasStroke,
                GradientKind = gradientKind,
                GradientAngle = gradientAngle,
                StrokeCapStyle = strokeCapStyle,
                StrokeJoinStyle = strokeJoinStyle,
                StrokeMiterLimit = strokeMiterLimit
            },
            LibraryItemName: libraryItemName,
            InstanceName: TryGetAttributeValue(element, "instanceName") ?? string.Empty,
            SymbolPlaybackMode: Enum.TryParse<SymbolPlaybackMode>(TryGetAttributeValue(element, "playbackMode"), true, out var playbackMode) ? playbackMode : SymbolPlaybackMode.SceneTime,
            SymbolPlaybackOffset: ParseDouble(TryGetAttributeValue(element, "playbackOffset"), 0d),
            SymbolLockedFrame: ParseInt(TryGetAttributeValue(element, "lockedFrame"), 0),
            SymbolButtonState: Enum.TryParse<ButtonVisualState>(TryGetAttributeValue(element, "buttonState"), true, out var buttonState) ? buttonState : ButtonVisualState.Up,
            CacheAsBitmap: ParseBool(TryGetAttributeValue(element, "cacheAsBitmap"), false),
            BitmapCacheBackgroundColor: TryGetAttributeValue(element, "bitmapCacheBackgroundColor") ?? "#000000",
            MediaName: null,
            MediaKind: null);
    }

    private static FlashFrameVisual ParseControlVisual(
        XElement element,
        double x,
        double y,
        double width,
        double height,
        double scaleX,
        double scaleY,
        double skewX,
        double skewY,
        double rotation,
        double rotationX,
        double rotationY,
        double zDepth,
        double opacity,
        string fill,
        string stroke,
        double strokeThickness,
        double cornerRadius,
        bool hasFill,
        bool hasStroke,
        bool useGradient,
        LayerGradientKind gradientKind,
        double gradientAngle,
        string gradientFrom,
        string gradientTo,
        LayerStrokeCapStyle strokeCapStyle,
        LayerStrokeJoinStyle strokeJoinStyle,
        double strokeMiterLimit,
        LayerCompositeSettings compositing)
    {
        var controlKind = Enum.TryParse<AvaloniaControlKind>(TryGetAttributeValue(element, "componentType"), true, out var parsedKind)
            ? parsedKind
            : AvaloniaControlKind.Border;
        var control = new AvaloniaControlSettings
        {
            Kind = controlKind,
            Content = TryGetAttributeValue(element, "content") ?? string.Empty,
            SecondaryContent = TryGetAttributeValue(element, "secondaryContent") ?? string.Empty,
            IsChecked = ParseBool(TryGetAttributeValue(element, "isChecked"), false),
            Value = ParseDouble(TryGetAttributeValue(element, "value"), 50d),
            Minimum = ParseDouble(TryGetAttributeValue(element, "minimum"), 0d),
            Maximum = ParseDouble(TryGetAttributeValue(element, "maximum"), 100d),
            IsIndeterminate = ParseBool(TryGetAttributeValue(element, "isIndeterminate"), false),
            Source = TryGetAttributeValue(element, "source") ?? string.Empty
        };

        return new FlashFrameVisual(
            Kind: LayerKind.AvaloniaControl,
            Snapshot: new LayerSnapshot(
                x,
                y,
                width,
                height,
                scaleX,
                scaleY,
                skewX,
                skewY,
                rotation,
                rotationX,
                rotationY,
                zDepth,
                opacity,
                cornerRadius,
                cornerRadius,
                cornerRadius,
                cornerRadius,
                cornerRadius,
                fill,
                stroke,
                control.Content,
                18d,
                strokeThickness,
                useGradient,
                gradientFrom,
                gradientTo,
                ShapeDrawingMode.Object,
                PrimitiveShapeType.None,
                false,
                0d,
                360d,
                false,
                "#57C9FF",
                [],
                5,
                0.46d,
                true,
                control,
                compositing.Clone(),
                new LayerTextSettings())
            {
                HasFill = hasFill,
                HasStroke = hasStroke,
                GradientKind = gradientKind,
                GradientAngle = gradientAngle,
                StrokeCapStyle = strokeCapStyle,
                StrokeJoinStyle = strokeJoinStyle,
                StrokeMiterLimit = strokeMiterLimit
            },
            LibraryItemName: null,
            InstanceName: TryGetAttributeValue(element, "instanceName") ?? string.Empty,
            SymbolPlaybackMode: SymbolPlaybackMode.SceneTime,
            SymbolPlaybackOffset: 0d,
            SymbolLockedFrame: 0,
            SymbolButtonState: ButtonVisualState.Up,
            CacheAsBitmap: false,
            BitmapCacheBackgroundColor: "#000000",
            MediaName: null,
            MediaKind: null);
    }

    private static FlashFrameVisual ParseMediaVisual(
        XElement element,
        double x,
        double y,
        double width,
        double height,
        double scaleX,
        double scaleY,
        double skewX,
        double skewY,
        double rotation,
        double rotationX,
        double rotationY,
        double zDepth,
        double opacity,
        string fill,
        string stroke,
        double strokeThickness,
        double cornerRadius,
        bool hasFill,
        bool hasStroke,
        bool useGradient,
        LayerGradientKind gradientKind,
        double gradientAngle,
        string gradientFrom,
        string gradientTo,
        LayerStrokeCapStyle strokeCapStyle,
        LayerStrokeJoinStyle strokeJoinStyle,
        double strokeMiterLimit,
        LayerCompositeSettings compositing)
    {
        var mediaKind = Enum.TryParse<MediaAssetKind>(TryGetAttributeValue(element, "mediaType"), true, out var parsedKind)
            ? parsedKind
            : MediaAssetKind.Video;

        return new FlashFrameVisual(
            Kind: mediaKind == MediaAssetKind.Audio ? LayerKind.Audio : LayerKind.Video,
            Snapshot: new LayerSnapshot(
                x,
                y,
                width,
                height,
                scaleX,
                scaleY,
                skewX,
                skewY,
                rotation,
                rotationX,
                rotationY,
                zDepth,
                opacity,
                cornerRadius,
                cornerRadius,
                cornerRadius,
                cornerRadius,
                cornerRadius,
                fill,
                stroke,
                string.Empty,
                18d,
                strokeThickness,
                useGradient,
                gradientFrom,
                gradientTo,
                ShapeDrawingMode.Object,
                PrimitiveShapeType.None,
                false,
                0d,
                360d,
                false,
                "#57C9FF",
                [],
                5,
                0.46d,
                true,
                new AvaloniaControlSettings(),
                compositing.Clone(),
                new LayerTextSettings())
            {
                HasFill = hasFill,
                HasStroke = hasStroke,
                GradientKind = gradientKind,
                GradientAngle = gradientAngle,
                StrokeCapStyle = strokeCapStyle,
                StrokeJoinStyle = strokeJoinStyle,
                StrokeMiterLimit = strokeMiterLimit
            },
            LibraryItemName: null,
            InstanceName: string.Empty,
            SymbolPlaybackMode: SymbolPlaybackMode.SceneTime,
            SymbolPlaybackOffset: 0d,
            SymbolLockedFrame: 0,
            SymbolButtonState: ButtonVisualState.Up,
            CacheAsBitmap: false,
            BitmapCacheBackgroundColor: "#000000",
            MediaName: TryGetAttributeValue(element, "assetName"),
            MediaKind: mediaKind);
    }

    private static TimelineLayer CreateLayerFromSample(
        FlashFrameVisual sample,
        int zIndex,
        IReadOnlyDictionary<string, MediaAsset> mediaByName,
        IReadOnlyDictionary<string, LibraryItem> libraryByName)
    {
        if (!string.IsNullOrWhiteSpace(sample.LibraryItemName) &&
            libraryByName.TryGetValue(sample.LibraryItemName, out var libraryItem))
        {
            var symbolLayer = TimelineEditingService.CreateLayerFromLibraryItem(libraryItem, sample.Snapshot.X, sample.Snapshot.Y, zIndex);
            symbolLayer.InstanceName = sample.InstanceName;
            symbolLayer.SymbolPlaybackMode = sample.SymbolPlaybackMode;
            symbolLayer.SymbolPlaybackOffset = sample.SymbolPlaybackOffset;
            symbolLayer.SymbolLockedFrame = sample.SymbolLockedFrame;
            symbolLayer.SymbolButtonState = sample.SymbolButtonState;
            symbolLayer.CacheAsBitmap = sample.CacheAsBitmap;
            symbolLayer.BitmapCacheBackgroundColor = sample.BitmapCacheBackgroundColor;
            return symbolLayer;
        }

        if (!string.IsNullOrWhiteSpace(sample.MediaName) &&
            mediaByName.TryGetValue(sample.MediaName, out var mediaAsset))
        {
            return mediaAsset.Kind == MediaAssetKind.Audio
                ? TimelineEditingService.CreateAudioLayer(mediaAsset, zIndex, 0d)
                : TimelineEditingService.CreateVideoLayer(mediaAsset, sample.Snapshot.X, sample.Snapshot.Y, zIndex, 0d);
        }

        return sample.Kind switch
        {
            LayerKind.Path when sample.Snapshot.PathPoints.Count >= 2 => TimelineEditingService.CreatePathLayer(
                $"Path {zIndex + 1}",
                sample.Snapshot.Fill,
                sample.Snapshot.Stroke,
                sample.Snapshot.PathPoints,
                sample.Snapshot.IsClosed,
                zIndex,
                sample.Snapshot.StrokeThickness,
                sample.Snapshot.UseGradient,
                sample.Snapshot.GradientFrom,
                sample.Snapshot.GradientTo,
                sample.Snapshot.GradientKind,
                sample.Snapshot.GradientAngle,
                sample.Snapshot.HasFill,
                sample.Snapshot.HasStroke,
                sample.Snapshot.StrokeCapStyle,
                sample.Snapshot.StrokeJoinStyle,
                sample.Snapshot.StrokeMiterLimit),
            LayerKind.AvaloniaControl => TimelineEditingService.CreateAvaloniaControlLayer(
                sample.Snapshot.AvaloniaControl.Kind,
                $"{sample.Snapshot.AvaloniaControl.Kind} {zIndex + 1}",
                sample.Snapshot.Fill,
                sample.Snapshot.Stroke,
                sample.Snapshot.AvaloniaControl.Content,
                sample.Snapshot.X,
                sample.Snapshot.Y,
                zIndex),
            _ => TimelineEditingService.CreateLayer(
                sample.Kind,
                $"{sample.Kind} {zIndex + 1}",
                sample.Snapshot.Fill,
                sample.Snapshot.Text,
                sample.Snapshot.X,
                sample.Snapshot.Y,
                zIndex)
        };
    }

    private static void ApplyLayerSnapshot(TimelineLayer layer, FlashFrameVisual sample)
    {
        layer.Defaults.X = sample.Snapshot.X;
        layer.Defaults.Y = sample.Snapshot.Y;
        layer.Defaults.Width = Math.Max(16d, sample.Snapshot.Width);
        layer.Defaults.Height = Math.Max(16d, sample.Snapshot.Height);
        layer.Defaults.ScaleX = sample.Snapshot.ScaleX;
        layer.Defaults.ScaleY = sample.Snapshot.ScaleY;
        layer.Defaults.SkewX = sample.Snapshot.SkewX;
        layer.Defaults.SkewY = sample.Snapshot.SkewY;
        layer.Defaults.Rotation = sample.Snapshot.Rotation;
        layer.Defaults.RotationX = sample.Snapshot.RotationX;
        layer.Defaults.RotationY = sample.Snapshot.RotationY;
        layer.Defaults.ZDepth = sample.Snapshot.ZDepth;
        layer.Defaults.Opacity = TimelineMath.Clamp(sample.Snapshot.Opacity, 0d, 1d);
        layer.Style.Fill = sample.Snapshot.Fill;
        layer.Style.Stroke = sample.Snapshot.Stroke;
        layer.Style.StrokeThickness = Math.Max(0.5d, sample.Snapshot.StrokeThickness);
        layer.Style.Text = sample.Kind == LayerKind.Text ? sample.Snapshot.Text : layer.Style.Text;
        layer.Style.FontSize = sample.Snapshot.FontSize;
        layer.Style.TextSettings = sample.Snapshot.TextSettings.Clone();
        layer.Style.CornerRadius = sample.Snapshot.CornerRadius;
        layer.Style.CornerRadiusTopLeft = sample.Snapshot.CornerRadiusTopLeft;
        layer.Style.CornerRadiusTopRight = sample.Snapshot.CornerRadiusTopRight;
        layer.Style.CornerRadiusBottomRight = sample.Snapshot.CornerRadiusBottomRight;
        layer.Style.CornerRadiusBottomLeft = sample.Snapshot.CornerRadiusBottomLeft;
        layer.Style.HasFill = sample.Snapshot.HasFill;
        layer.Style.HasStroke = sample.Snapshot.HasStroke;
        layer.Style.UseGradient = sample.Snapshot.UseGradient;
        layer.Style.GradientKind = sample.Snapshot.GradientKind;
        layer.Style.GradientAngle = sample.Snapshot.GradientAngle;
        layer.Style.GradientFrom = sample.Snapshot.GradientFrom;
        layer.Style.GradientTo = sample.Snapshot.GradientTo;
        layer.Style.DrawingMode = sample.Snapshot.DrawingMode;
        layer.Style.PrimitiveShape = sample.Snapshot.PrimitiveShape;
        layer.Style.StrokeCapStyle = sample.Snapshot.StrokeCapStyle;
        layer.Style.StrokeJoinStyle = sample.Snapshot.StrokeJoinStyle;
        layer.Style.StrokeMiterLimit = sample.Snapshot.StrokeMiterLimit;
        layer.Style.IsClosed = sample.Snapshot.IsClosed;
        layer.Style.EllipseStartAngle = sample.Snapshot.EllipseStartAngle;
        layer.Style.EllipseSweepAngle = sample.Snapshot.EllipseSweepAngle;
        layer.Style.PathPoints = VectorPathService.ClonePoints(sample.Snapshot.PathPoints);
        layer.Style.PolyStarSides = sample.Snapshot.PolyStarSides;
        layer.Style.PolyStarInnerRadius = sample.Snapshot.PolyStarInnerRadius;
        layer.Style.PolyStarIsStar = sample.Snapshot.PolyStarIsStar;
        layer.Style.AvaloniaControl = sample.Snapshot.AvaloniaControl.Clone();
        PrimitiveShapeService.NormalizeStyle(layer.Style);
        layer.Compositing = sample.Snapshot.Compositing.Clone();
        layer.ShowAsOutline = sample.Snapshot.ShowAsOutline;
        layer.OutlineColor = sample.Snapshot.OutlineColor;
        layer.InstanceName = sample.InstanceName;
        layer.CacheAsBitmap = sample.CacheAsBitmap;
        layer.BitmapCacheBackgroundColor = sample.BitmapCacheBackgroundColor;
        if (sample.MediaName is not null)
        {
            layer.Media.StartTime = 0d;
            layer.Media.ClipDuration = 0d;
        }
    }

    private static List<InteractionBehaviorModel> ParseBehaviors(XElement parent)
    {
        var behaviors = parent.Elements().FirstOrDefault(element => element.Name.LocalName == "behaviors");
        if (behaviors is null)
        {
            return [];
        }

        return behaviors.Elements()
            .Where(element => element.Name.LocalName == "DOMBehavior")
            .Select(element => new InteractionBehaviorModel
            {
                Name = TryGetAttributeValue(element, "name") ?? "Behavior",
                IsEnabled = ParseBool(TryGetAttributeValue(element, "enabled"), true),
                Trigger = Enum.TryParse<InteractionTriggerKind>(TryGetAttributeValue(element, "trigger"), true, out var trigger) ? trigger : InteractionTriggerKind.PointerClick,
                TriggerArgument = TryGetAttributeValue(element, "triggerArgument") ?? string.Empty,
                Action = Enum.TryParse<InteractionActionKind>(TryGetAttributeValue(element, "action"), true, out var action) ? action : InteractionActionKind.Play,
                TargetSceneId = Guid.TryParse(TryGetAttributeValue(element, "targetSceneId"), out var targetSceneId) ? targetSceneId : null,
                TargetFrameLabel = TryGetAttributeValue(element, "targetFrameLabel") ?? string.Empty,
                TargetLayerId = Guid.TryParse(TryGetAttributeValue(element, "targetLayerId"), out var targetLayerId) ? targetLayerId : null,
                TargetButtonState = Enum.TryParse<ButtonVisualState>(TryGetAttributeValue(element, "targetButtonState"), true, out var buttonState) ? buttonState : ButtonVisualState.Up,
                BoolValue = ParseBool(TryGetAttributeValue(element, "boolValue"), true),
                VariableName = TryGetAttributeValue(element, "variableName") ?? string.Empty,
                VariableValue = TryGetAttributeValue(element, "variableValue") ?? string.Empty,
                TargetVisualStateGroup = TryGetAttributeValue(element, "targetVisualStateGroup") ?? string.Empty,
                TargetVisualState = TryGetAttributeValue(element, "targetVisualState") ?? string.Empty,
                Script = TryGetAttributeValue(element, "script") ?? string.Empty
            })
            .ToList();
    }

    private static List<ComponentParameterDefinition> ParseComponentParameters(XElement parent)
    {
        var parameters = parent.Elements().FirstOrDefault(element => element.Name.LocalName == "componentParameters");
        if (parameters is null)
        {
            return [];
        }

        return parameters.Elements()
            .Where(element => element.Name.LocalName == "DOMComponentParameter")
            .Select(element => new ComponentParameterDefinition
            {
                Name = TryGetAttributeValue(element, "name") ?? "parameter",
                Kind = Enum.TryParse<ComponentParameterKind>(TryGetAttributeValue(element, "kind"), true, out var kind)
                    ? kind
                    : ComponentParameterKind.String,
                BindingTarget = Enum.TryParse<ComponentParameterBindingTarget>(TryGetAttributeValue(element, "bindingTarget"), true, out var bindingTarget)
                    ? bindingTarget
                    : ComponentParameterBindingTarget.Text,
                TargetLayerName = TryGetAttributeValue(element, "targetLayerName") ?? string.Empty,
                DefaultValue = TryGetAttributeValue(element, "defaultValue") ?? string.Empty,
                Description = TryGetAttributeValue(element, "description") ?? string.Empty
            })
            .ToList();
    }

    private static void ParseComponentOverrides(
        XElement parent,
        TimelineLayer layer,
        IReadOnlyDictionary<string, LibraryItem> libraryByName)
    {
        var overrides = parent.Elements().FirstOrDefault(element => element.Name.LocalName == "componentOverrides");
        if (overrides is null)
        {
            if (layer.SourceLibraryItemId is Guid sourceLibraryItemId)
            {
                var libraryItem = libraryByName.Values.FirstOrDefault(item => item.Id == sourceLibraryItemId);
                if (libraryItem is not null)
                {
                    ComponentParameterService.EnsureOverrides(layer, libraryItem);
                }
            }

            return;
        }

        layer.ComponentParameterOverrides = overrides.Elements()
            .Where(element => element.Name.LocalName == "DOMComponentOverride")
            .Select(element => new ComponentParameterOverride
            {
                Name = TryGetAttributeValue(element, "name") ?? string.Empty,
                Value = TryGetAttributeValue(element, "value") ?? string.Empty
            })
            .ToList();

        if (layer.SourceLibraryItemId is Guid componentLibraryItemId)
        {
            var libraryItem = libraryByName.Values.FirstOrDefault(item => item.Id == componentLibraryItemId);
            if (libraryItem is not null)
            {
                ComponentParameterService.EnsureOverrides(layer, libraryItem);
            }
        }
    }

    private static void ParseCompositing(XElement layerElement, LayerCompositeSettings compositing)
    {
        compositing.Role = (TryGetAttributeValue(layerElement, "layerType") ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "guide" => LayerCompositeRole.Guide,
            "mask" => LayerCompositeRole.Mask,
            "camera" => LayerCompositeRole.Camera,
            _ => compositing.Role
        };
        compositing.BlendMode = ParseXflBlendMode(TryGetAttributeValue(layerElement, "blendMode"), compositing.BlendMode);
        compositing.MaskLayerCount = Math.Max(1, ParseInt(TryGetAttributeValue(layerElement, "maskLayerCount"), compositing.MaskLayerCount));
        compositing.ParallaxDepth = Math.Max(0.2d, ParseDouble(TryGetAttributeValue(layerElement, "parallaxDepth"), compositing.ParallaxDepth));

        var element = layerElement.Elements().FirstOrDefault(candidate => candidate.Name.LocalName == "compositing");
        if (element is null)
        {
            return;
        }

        compositing.Role = Enum.TryParse<LayerCompositeRole>(TryGetAttributeValue(element, "role"), true, out var role)
            ? role
            : compositing.Role;
        compositing.BlendMode = ParseXflBlendMode(TryGetAttributeValue(element, "blendMode"), compositing.BlendMode);
        compositing.MaskLayerCount = Math.Max(1, ParseInt(TryGetAttributeValue(element, "maskLayerCount"), compositing.MaskLayerCount));
        compositing.BlurRadius = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "blurRadius"), compositing.BlurRadius));
        compositing.BlurQuality = ParseFilterQuality(TryGetAttributeValue(element, "blurQuality"), compositing.BlurQuality);
        compositing.GlowSize = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "glowSize"), compositing.GlowSize));
        compositing.GlowOpacity = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(element, "glowOpacity"), compositing.GlowOpacity), 0d, 1d);
        compositing.GlowColor = TryGetAttributeValue(element, "glowColor") ?? compositing.GlowColor;
        compositing.GlowQuality = ParseFilterQuality(TryGetAttributeValue(element, "glowQuality"), compositing.GlowQuality);
        compositing.GlowKnockout = ParseBool(TryGetAttributeValue(element, "glowKnockout"), compositing.GlowKnockout);
        compositing.ShadowBlur = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "shadowBlur"), compositing.ShadowBlur));
        compositing.ShadowOpacity = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(element, "shadowOpacity"), compositing.ShadowOpacity), 0d, 1d);
        compositing.ShadowOffsetX = ParseDouble(TryGetAttributeValue(element, "shadowOffsetX"), compositing.ShadowOffsetX);
        compositing.ShadowOffsetY = ParseDouble(TryGetAttributeValue(element, "shadowOffsetY"), compositing.ShadowOffsetY);
        compositing.ShadowColor = TryGetAttributeValue(element, "shadowColor") ?? compositing.ShadowColor;
        compositing.ShadowQuality = ParseFilterQuality(TryGetAttributeValue(element, "shadowQuality"), compositing.ShadowQuality);
        compositing.ShadowKnockout = ParseBool(TryGetAttributeValue(element, "shadowKnockout"), compositing.ShadowKnockout);
        compositing.ShadowHideObject = ParseBool(TryGetAttributeValue(element, "shadowHideObject"), compositing.ShadowHideObject);
        compositing.TintStrength = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(element, "tintStrength"), compositing.TintStrength), 0d, 1d);
        compositing.TintColor = TryGetAttributeValue(element, "tintColor") ?? compositing.TintColor;
        compositing.Brightness = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(element, "brightness"), compositing.Brightness), -1d, 1d);
        compositing.Contrast = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(element, "contrast"), compositing.Contrast), -1d, 1d);
        compositing.HueShift = ParseDouble(TryGetAttributeValue(element, "hueShift"), compositing.HueShift);
        compositing.Saturation = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "saturation"), compositing.Saturation));
        compositing.BevelType = Enum.TryParse<LayerBevelType>(TryGetAttributeValue(element, "bevelType"), true, out var bevelType)
            ? bevelType
            : compositing.BevelType;
        compositing.BevelSize = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "bevelSize"), compositing.BevelSize));
        compositing.BevelStrength = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "bevelStrength"), compositing.BevelStrength));
        compositing.BevelAngle = ParseDouble(TryGetAttributeValue(element, "bevelAngle"), compositing.BevelAngle);
        compositing.BevelDistance = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "bevelDistance"), compositing.BevelDistance));
        compositing.BevelHighlightColor = TryGetAttributeValue(element, "bevelHighlightColor") ?? compositing.BevelHighlightColor;
        compositing.BevelShadowColor = TryGetAttributeValue(element, "bevelShadowColor") ?? compositing.BevelShadowColor;
        compositing.BevelQuality = ParseFilterQuality(TryGetAttributeValue(element, "bevelQuality"), compositing.BevelQuality);
        compositing.BevelKnockout = ParseBool(TryGetAttributeValue(element, "bevelKnockout"), compositing.BevelKnockout);
        compositing.GradientGlowSize = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "gradientGlowSize"), compositing.GradientGlowSize));
        compositing.GradientGlowStrength = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "gradientGlowStrength"), compositing.GradientGlowStrength));
        compositing.GradientGlowAngle = ParseDouble(TryGetAttributeValue(element, "gradientGlowAngle"), compositing.GradientGlowAngle);
        compositing.GradientGlowDistance = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "gradientGlowDistance"), compositing.GradientGlowDistance));
        compositing.GradientGlowFrom = TryGetAttributeValue(element, "gradientGlowFrom") ?? compositing.GradientGlowFrom;
        compositing.GradientGlowTo = TryGetAttributeValue(element, "gradientGlowTo") ?? compositing.GradientGlowTo;
        compositing.GradientGlowQuality = ParseFilterQuality(TryGetAttributeValue(element, "gradientGlowQuality"), compositing.GradientGlowQuality);
        compositing.GradientGlowKnockout = ParseBool(TryGetAttributeValue(element, "gradientGlowKnockout"), compositing.GradientGlowKnockout);
        compositing.GradientBevelType = Enum.TryParse<LayerBevelType>(TryGetAttributeValue(element, "gradientBevelType"), true, out var gradientBevelType)
            ? gradientBevelType
            : compositing.GradientBevelType;
        compositing.GradientBevelSize = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "gradientBevelSize"), compositing.GradientBevelSize));
        compositing.GradientBevelStrength = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "gradientBevelStrength"), compositing.GradientBevelStrength));
        compositing.GradientBevelAngle = ParseDouble(TryGetAttributeValue(element, "gradientBevelAngle"), compositing.GradientBevelAngle);
        compositing.GradientBevelDistance = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "gradientBevelDistance"), compositing.GradientBevelDistance));
        compositing.GradientBevelFrom = TryGetAttributeValue(element, "gradientBevelFrom") ?? compositing.GradientBevelFrom;
        compositing.GradientBevelTo = TryGetAttributeValue(element, "gradientBevelTo") ?? compositing.GradientBevelTo;
        compositing.GradientBevelQuality = ParseFilterQuality(TryGetAttributeValue(element, "gradientBevelQuality"), compositing.GradientBevelQuality);
        compositing.GradientBevelKnockout = ParseBool(TryGetAttributeValue(element, "gradientBevelKnockout"), compositing.GradientBevelKnockout);
        compositing.ParallaxDepth = Math.Max(0.2d, ParseDouble(TryGetAttributeValue(element, "parallaxDepth"), compositing.ParallaxDepth));
    }

    private static void ParseMediaSettings(XElement layerElement, TimelineLayer layer)
    {
        if (layer.Kind is not LayerKind.Video and not LayerKind.Audio)
        {
            return;
        }

        var element = layerElement.Elements().FirstOrDefault(candidate => candidate.Name.LocalName == "mediaSettings");
        if (element is null)
        {
            return;
        }

        layer.Media.PlaybackMode = Enum.TryParse<MediaPlaybackMode>(TryGetAttributeValue(element, "playbackMode"), true, out var playbackMode)
            ? playbackMode
            : layer.Media.PlaybackMode;
        layer.Media.StartTime = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "startTime"), layer.Media.StartTime));
        layer.Media.ClipOffset = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "clipOffset"), layer.Media.ClipOffset));
        layer.Media.ClipDuration = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "clipDuration"), layer.Media.ClipDuration));
        layer.Media.Loop = ParseBool(TryGetAttributeValue(element, "loop"), layer.Media.Loop);
        layer.Media.RepeatCount = Math.Max(1, ParseInt(TryGetAttributeValue(element, "repeatCount"), layer.Media.RepeatCount));
        layer.Media.RepeatForever = ParseBool(TryGetAttributeValue(element, "repeatForever"), layer.Media.RepeatForever || layer.Media.Loop);
        layer.Media.Volume = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(element, "volume"), layer.Media.Volume), 0d, 2d);
        layer.Media.GainDb = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(element, "gainDb"), layer.Media.GainDb), -24d, 24d);
        layer.Media.Pan = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(element, "pan"), layer.Media.Pan), -1d, 1d);
        layer.Media.FadeInDuration = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "fadeIn"), layer.Media.FadeInDuration));
        layer.Media.FadeOutDuration = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "fadeOut"), layer.Media.FadeOutDuration));
    }

    private static void ParseTransformParenting(XElement layerElement, TimelineLayer layer)
    {
        var settings = new TransformParentingSettings
        {
            BindParentX = ParseDouble(TryGetAttributeValue(layerElement, "transformBindParentX"), layer.TransformParenting.BindParentX),
            BindParentY = ParseDouble(TryGetAttributeValue(layerElement, "transformBindParentY"), layer.TransformParenting.BindParentY),
            BindParentScaleX = ParseDouble(TryGetAttributeValue(layerElement, "transformBindParentScaleX"), layer.TransformParenting.BindParentScaleX),
            BindParentScaleY = ParseDouble(TryGetAttributeValue(layerElement, "transformBindParentScaleY"), layer.TransformParenting.BindParentScaleY),
            BindParentRotation = ParseDouble(TryGetAttributeValue(layerElement, "transformBindParentRotation"), layer.TransformParenting.BindParentRotation),
            BindParentOpacity = ParseDouble(TryGetAttributeValue(layerElement, "transformBindParentOpacity"), layer.TransformParenting.BindParentOpacity),
            BindChildX = ParseDouble(TryGetAttributeValue(layerElement, "transformBindChildX"), layer.TransformParenting.BindChildX),
            BindChildY = ParseDouble(TryGetAttributeValue(layerElement, "transformBindChildY"), layer.TransformParenting.BindChildY)
        };

        if (Guid.TryParse(TryGetAttributeValue(layerElement, "transformParentLayerId"), out var parentLayerId))
        {
            settings.ParentLayerId = parentLayerId;
        }

        layer.TransformParenting = settings;
    }

    private static void ParseWarp(XElement layerElement, TimelineLayer layer)
    {
        var element = layerElement.Elements().FirstOrDefault(candidate => candidate.Name.LocalName == "warp");
        if (element is null)
        {
            return;
        }

        layer.Warp.IsEnabled = ParseBool(TryGetAttributeValue(element, "enabled"), layer.Warp.IsEnabled);
        layer.Warp.MeshColumns = Math.Clamp(ParseInt(TryGetAttributeValue(element, "meshColumns"), layer.Warp.MeshColumns), 2, 12);
        layer.Warp.MeshRows = Math.Clamp(ParseInt(TryGetAttributeValue(element, "meshRows"), layer.Warp.MeshRows), 2, 12);
        layer.Warp.Pins =
        [
            .. element.Elements().Where(candidate => candidate.Name.LocalName == "pin").Select(pinElement =>
                new WarpPinModel
                {
                    Id = Guid.TryParse(TryGetAttributeValue(pinElement, "id"), out var pinId) ? pinId : Guid.NewGuid(),
                    BindX = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(pinElement, "bindX"), 0d), 0d, 1d),
                    BindY = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(pinElement, "bindY"), 0d), 0d, 1d),
                    X = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(pinElement, "x"), 0d), 0d, 1d),
                    Y = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(pinElement, "y"), 0d), 0d, 1d),
                    Radius = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(pinElement, "radius"), 0.2d), 0.05d, 1.5d),
                    Strength = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(pinElement, "strength"), 1d), 0d, 2d)
                })
        ];
    }

    private static void ParseRig(XElement layerElement, TimelineLayer layer)
    {
        var element = layerElement.Elements().FirstOrDefault(candidate => candidate.Name.LocalName == "rig");
        if (element is null)
        {
            return;
        }

        layer.Rig.IsEnabled = ParseBool(TryGetAttributeValue(element, "enabled"), layer.Rig.IsEnabled);

        var bonesElement = element.Elements().FirstOrDefault(candidate => candidate.Name.LocalName == "bones");
        if (bonesElement is not null)
        {
            layer.Rig.Bones =
            [
                .. bonesElement.Elements().Where(candidate => candidate.Name.LocalName == "bone").Select(boneElement =>
                    new RigBoneModel
                    {
                        Id = Guid.TryParse(TryGetAttributeValue(boneElement, "id"), out var boneId) ? boneId : Guid.NewGuid(),
                        Name = TryGetAttributeValue(boneElement, "name") ?? string.Empty,
                        ParentBoneId = Guid.TryParse(TryGetAttributeValue(boneElement, "parentBoneId"), out var parentBoneId) ? parentBoneId : null,
                        BindStartX = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(boneElement, "bindStartX"), 0d), 0d, 1d),
                        BindStartY = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(boneElement, "bindStartY"), 0d), 0d, 1d),
                        StartX = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(boneElement, "startX"), 0d), 0d, 1d),
                        StartY = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(boneElement, "startY"), 0d), 0d, 1d),
                        Length = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(boneElement, "length"), 0.2d), 0.05d, 1.5d),
                        Rotation = ParseDouble(TryGetAttributeValue(boneElement, "rotation"), 0d),
                        Envelope = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(boneElement, "envelope"), 0.3d), 0.08d, 1.5d)
                    })
            ];
        }

        var posesElement = element.Elements().FirstOrDefault(candidate => candidate.Name.LocalName == "poses");
        if (posesElement is not null)
        {
            layer.Rig.Poses =
            [
                .. posesElement.Elements().Where(candidate => candidate.Name.LocalName == "pose").Select(poseElement =>
                    new RigPoseModel
                    {
                        Id = Guid.TryParse(TryGetAttributeValue(poseElement, "id"), out var poseId) ? poseId : Guid.NewGuid(),
                        Name = TryGetAttributeValue(poseElement, "name") ?? string.Empty,
                        Time = Math.Max(0d, ParseDouble(TryGetAttributeValue(poseElement, "time"), 0d)),
                        Bones =
                        [
                            .. poseElement.Elements().Where(candidate => candidate.Name.LocalName == "bonePose").Select(bonePoseElement =>
                                new RigBonePoseModel
                                {
                                    BoneId = Guid.TryParse(TryGetAttributeValue(bonePoseElement, "boneId"), out var boneId) ? boneId : Guid.Empty,
                                    StartX = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(bonePoseElement, "startX"), 0d), 0d, 1d),
                                    StartY = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(bonePoseElement, "startY"), 0d), 0d, 1d),
                                    Length = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(bonePoseElement, "length"), 0.2d), 0.05d, 1.5d),
                                    Rotation = ParseDouble(TryGetAttributeValue(bonePoseElement, "rotation"), 0d)
                                })
                        ]
                    })
            ];
        }
    }

    private static LayerCompositeSettings ParseNativeVisualCompositing(XElement visualElement)
    {
        var compositing = new LayerCompositeSettings();
        ParseNativeFlashColorEffect(visualElement, compositing);
        ParseNativeFilters(visualElement, compositing);
        return compositing;
    }

    private static void ParseNativeFlashColorEffect(XElement visualElement, LayerCompositeSettings compositing)
    {
        var mode = (TryGetAttributeValue(visualElement, "colorMode") ?? string.Empty).Trim().ToLowerInvariant();
        compositing.FlashColorEffectMode = mode switch
        {
            "alpha" => FlashColorEffectMode.Alpha,
            "tint" => FlashColorEffectMode.Tint,
            "brightness" => FlashColorEffectMode.Brightness,
            "advanced" => FlashColorEffectMode.Advanced,
            _ => FlashColorEffectMode.None
        };

        switch (compositing.FlashColorEffectMode)
        {
            case FlashColorEffectMode.Alpha:
                compositing.FlashAlphaPercent = ParseDouble(TryGetAttributeValue(visualElement, "alphaPercent"), 100d);
                break;
            case FlashColorEffectMode.Tint:
                compositing.FlashTintColor = TryGetAttributeValue(visualElement, "tintColor") ?? "#FFFFFF";
                compositing.FlashTintPercent = ParseDouble(TryGetAttributeValue(visualElement, "tintPercent"), 0d);
                break;
            case FlashColorEffectMode.Brightness:
                compositing.FlashBrightnessPercent = ParseDouble(TryGetAttributeValue(visualElement, "brightnessPercent"), 0d);
                break;
            case FlashColorEffectMode.Advanced:
                compositing.FlashAdvancedAlphaPercent = ParseDouble(TryGetAttributeValue(visualElement, "alphaMultiplier"), 100d);
                compositing.FlashRedPercent = ParseDouble(TryGetAttributeValue(visualElement, "redMultiplier"), 100d);
                compositing.FlashGreenPercent = ParseDouble(TryGetAttributeValue(visualElement, "greenMultiplier"), 100d);
                compositing.FlashBluePercent = ParseDouble(TryGetAttributeValue(visualElement, "blueMultiplier"), 100d);
                compositing.FlashAlphaOffset = ParseDouble(TryGetAttributeValue(visualElement, "alphaOffset"), 0d);
                compositing.FlashRedOffset = ParseDouble(TryGetAttributeValue(visualElement, "redOffset"), 0d);
                compositing.FlashGreenOffset = ParseDouble(TryGetAttributeValue(visualElement, "greenOffset"), 0d);
                compositing.FlashBlueOffset = ParseDouble(TryGetAttributeValue(visualElement, "blueOffset"), 0d);
                break;
        }
    }

    private static void ParseNativeFilters(XElement visualElement, LayerCompositeSettings compositing)
    {
        var filtersElement = visualElement.Elements().FirstOrDefault(candidate => candidate.Name.LocalName == "filters");
        if (filtersElement is null)
        {
            return;
        }

        foreach (var filterElement in filtersElement.Elements())
        {
            switch (filterElement.Name.LocalName)
            {
                case "BlurFilter":
                    compositing.BlurRadius = Math.Max(
                        compositing.BlurRadius,
                        (ParseDouble(TryGetAttributeValue(filterElement, "blurX"), 0d) +
                         ParseDouble(TryGetAttributeValue(filterElement, "blurY"), 0d)) / 2d);
                    compositing.BlurQuality = ParseFilterQuality(TryGetAttributeValue(filterElement, "quality"), compositing.BlurQuality);
                    break;
                case "GlowFilter":
                    compositing.GlowSize = Math.Max(
                        compositing.GlowSize,
                        (ParseDouble(TryGetAttributeValue(filterElement, "blurX"), compositing.GlowSize) +
                         ParseDouble(TryGetAttributeValue(filterElement, "blurY"), compositing.GlowSize)) / 2d);
                    compositing.GlowOpacity = Math.Max(compositing.GlowOpacity, ParseDouble(TryGetAttributeValue(filterElement, "alpha"), compositing.GlowOpacity));
                    compositing.GlowColor = TryGetAttributeValue(filterElement, "color") ?? compositing.GlowColor;
                    compositing.GlowQuality = ParseFilterQuality(TryGetAttributeValue(filterElement, "quality"), compositing.GlowQuality);
                    compositing.GlowKnockout = ParseBool(TryGetAttributeValue(filterElement, "knockout"), compositing.GlowKnockout);
                    break;
                case "DropShadowFilter":
                    var angle = ParseDouble(TryGetAttributeValue(filterElement, "angle"), 0d) * Math.PI / 180d;
                    var distance = ParseDouble(TryGetAttributeValue(filterElement, "distance"), 0d);
                    compositing.ShadowBlur = Math.Max(
                        compositing.ShadowBlur,
                        (ParseDouble(TryGetAttributeValue(filterElement, "blurX"), compositing.ShadowBlur) +
                         ParseDouble(TryGetAttributeValue(filterElement, "blurY"), compositing.ShadowBlur)) / 2d);
                    compositing.ShadowOpacity = Math.Max(compositing.ShadowOpacity, ParseDouble(TryGetAttributeValue(filterElement, "alpha"), compositing.ShadowOpacity));
                    compositing.ShadowColor = TryGetAttributeValue(filterElement, "color") ?? compositing.ShadowColor;
                    compositing.ShadowOffsetX = Math.Cos(angle) * distance;
                    compositing.ShadowOffsetY = Math.Sin(angle) * distance;
                    compositing.ShadowQuality = ParseFilterQuality(TryGetAttributeValue(filterElement, "quality"), compositing.ShadowQuality);
                    compositing.ShadowKnockout = ParseBool(TryGetAttributeValue(filterElement, "knockout"), compositing.ShadowKnockout);
                    compositing.ShadowHideObject = ParseBool(TryGetAttributeValue(filterElement, "hideObject"), compositing.ShadowHideObject);
                    break;
                case "BevelFilter":
                    compositing.BevelSize = Math.Max(
                        compositing.BevelSize,
                        (ParseDouble(TryGetAttributeValue(filterElement, "blurX"), compositing.BevelSize) +
                         ParseDouble(TryGetAttributeValue(filterElement, "blurY"), compositing.BevelSize)) / 2d);
                    compositing.BevelStrength = Math.Max(compositing.BevelStrength, ParseDouble(TryGetAttributeValue(filterElement, "strength"), compositing.BevelStrength));
                    compositing.BevelAngle = ParseDouble(TryGetAttributeValue(filterElement, "angle"), compositing.BevelAngle);
                    compositing.BevelDistance = Math.Max(compositing.BevelDistance, ParseDouble(TryGetAttributeValue(filterElement, "distance"), compositing.BevelDistance));
                    compositing.BevelHighlightColor = TryGetAttributeValue(filterElement, "highlightColor") ?? compositing.BevelHighlightColor;
                    compositing.BevelShadowColor = TryGetAttributeValue(filterElement, "shadowColor") ?? compositing.BevelShadowColor;
                    compositing.BevelType = Enum.TryParse<LayerBevelType>(TryGetAttributeValue(filterElement, "type"), true, out var bevelType)
                        ? bevelType
                        : compositing.BevelType;
                    compositing.BevelQuality = ParseFilterQuality(TryGetAttributeValue(filterElement, "quality"), compositing.BevelQuality);
                    compositing.BevelKnockout = ParseBool(TryGetAttributeValue(filterElement, "knockout"), compositing.BevelKnockout);
                    break;
                case "GradientGlowFilter":
                    compositing.GradientGlowSize = Math.Max(
                        compositing.GradientGlowSize,
                        (ParseDouble(TryGetAttributeValue(filterElement, "blurX"), compositing.GradientGlowSize) +
                         ParseDouble(TryGetAttributeValue(filterElement, "blurY"), compositing.GradientGlowSize)) / 2d);
                    compositing.GradientGlowStrength = Math.Max(compositing.GradientGlowStrength, ParseDouble(TryGetAttributeValue(filterElement, "strength"), compositing.GradientGlowStrength));
                    compositing.GradientGlowAngle = ParseDouble(TryGetAttributeValue(filterElement, "angle"), compositing.GradientGlowAngle);
                    compositing.GradientGlowDistance = Math.Max(compositing.GradientGlowDistance, ParseDouble(TryGetAttributeValue(filterElement, "distance"), compositing.GradientGlowDistance));
                    compositing.GradientGlowFrom = TryGetAttributeValue(filterElement, "fromColor") ?? compositing.GradientGlowFrom;
                    compositing.GradientGlowTo = TryGetAttributeValue(filterElement, "toColor") ?? compositing.GradientGlowTo;
                    compositing.GradientGlowQuality = ParseFilterQuality(TryGetAttributeValue(filterElement, "quality"), compositing.GradientGlowQuality);
                    compositing.GradientGlowKnockout = ParseBool(TryGetAttributeValue(filterElement, "knockout"), compositing.GradientGlowKnockout);
                    break;
                case "GradientBevelFilter":
                    compositing.GradientBevelSize = Math.Max(
                        compositing.GradientBevelSize,
                        (ParseDouble(TryGetAttributeValue(filterElement, "blurX"), compositing.GradientBevelSize) +
                         ParseDouble(TryGetAttributeValue(filterElement, "blurY"), compositing.GradientBevelSize)) / 2d);
                    compositing.GradientBevelStrength = Math.Max(compositing.GradientBevelStrength, ParseDouble(TryGetAttributeValue(filterElement, "strength"), compositing.GradientBevelStrength));
                    compositing.GradientBevelAngle = ParseDouble(TryGetAttributeValue(filterElement, "angle"), compositing.GradientBevelAngle);
                    compositing.GradientBevelDistance = Math.Max(compositing.GradientBevelDistance, ParseDouble(TryGetAttributeValue(filterElement, "distance"), compositing.GradientBevelDistance));
                    compositing.GradientBevelFrom = TryGetAttributeValue(filterElement, "fromColor") ?? compositing.GradientBevelFrom;
                    compositing.GradientBevelTo = TryGetAttributeValue(filterElement, "toColor") ?? compositing.GradientBevelTo;
                    compositing.GradientBevelType = Enum.TryParse<LayerBevelType>(TryGetAttributeValue(filterElement, "type"), true, out var gradientBevelType)
                        ? gradientBevelType
                        : compositing.GradientBevelType;
                    compositing.GradientBevelQuality = ParseFilterQuality(TryGetAttributeValue(filterElement, "quality"), compositing.GradientBevelQuality);
                    compositing.GradientBevelKnockout = ParseBool(TryGetAttributeValue(filterElement, "knockout"), compositing.GradientBevelKnockout);
                    break;
            }
        }
    }

    private static void ParseExplicitTracks(XElement layerElement, TimelineLayer layer)
    {
        var tracksElement = layerElement.Elements().FirstOrDefault(candidate => candidate.Name.LocalName == "motionTracks");
        if (tracksElement is null)
        {
            return;
        }

        foreach (var trackElement in tracksElement.Elements().Where(candidate => candidate.Name.LocalName == "DOMPropertyTrack"))
        {
            if (!Enum.TryParse<AnimatedProperty>(TryGetAttributeValue(trackElement, "property"), true, out var property))
            {
                continue;
            }

            var track = TimelineEditingService.GetOrCreateTrack(layer, property);
            track.Keyframes =
            [
                .. trackElement.Elements()
                    .Where(candidate => candidate.Name.LocalName == "DOMKeyframe")
                    .Select(ParseKeyframe)
                    .OrderBy(keyframe => keyframe.Time)
            ];
        }
    }

    private static void ParseExplicitShapeKeyframes(XElement layerElement, TimelineLayer layer)
    {
        var shapeElement = layerElement.Elements().FirstOrDefault(candidate => candidate.Name.LocalName == "shapeTweens");
        if (shapeElement is null)
        {
            return;
        }

        layer.ShapeKeyframes =
        [
            .. shapeElement.Elements()
                .Where(candidate => candidate.Name.LocalName == "DOMShapeKeyframe")
                .Select(ParseShapeKeyframe)
                .OrderBy(keyframe => keyframe.Time)
        ];

        if (layer.ShapeKeyframes.Count > 0)
        {
            var first = layer.ShapeKeyframes[0];
            layer.Style.IsClosed = first.IsClosed;
            layer.Style.PathPoints = VectorPathService.ClonePoints(first.PathPoints);
        }
    }

    private static List<FrameSpanModel> BuildFrameSpans(IReadOnlyList<ParsedFrameRecord> frames)
    {
        if (frames.Count == 0)
        {
            return [];
        }

        var spans = new List<FrameSpanModel>();
        var spanStart = 0;
        var currentBlank = frames[0].IsBlank;

        for (var frame = 1; frame < frames.Count; frame++)
        {
            if (frames[frame].IsBlank == currentBlank)
            {
                continue;
            }

            spans.Add(new FrameSpanModel
            {
                StartFrame = spanStart,
                EndFrame = frame - 1,
                IsBlank = currentBlank
            });
            spanStart = frame;
            currentBlank = frames[frame].IsBlank;
        }

        spans.Add(new FrameSpanModel
        {
            StartFrame = spanStart,
            EndFrame = frames.Count - 1,
            IsBlank = currentBlank
        });

        return spans;
    }

    private static void ApplySampleKeyframes(
        TimelineLayer layer,
        FlashFrameVisual? previousVisible,
        FlashFrameVisual current,
        double time,
        double duration)
    {
        ApplyPropertyKeyframe(layer, previousVisible?.Snapshot.X, current.Snapshot.X, AnimatedProperty.X, time, duration);
        ApplyPropertyKeyframe(layer, previousVisible?.Snapshot.Y, current.Snapshot.Y, AnimatedProperty.Y, time, duration);
        ApplyPropertyKeyframe(layer, previousVisible?.Snapshot.Width, current.Snapshot.Width, AnimatedProperty.Width, time, duration);
        ApplyPropertyKeyframe(layer, previousVisible?.Snapshot.Height, current.Snapshot.Height, AnimatedProperty.Height, time, duration);
        ApplyPropertyKeyframe(layer, previousVisible?.Snapshot.ScaleX, current.Snapshot.ScaleX, AnimatedProperty.ScaleX, time, duration);
        ApplyPropertyKeyframe(layer, previousVisible?.Snapshot.ScaleY, current.Snapshot.ScaleY, AnimatedProperty.ScaleY, time, duration);
        ApplyPropertyKeyframe(layer, previousVisible?.Snapshot.SkewX, current.Snapshot.SkewX, AnimatedProperty.SkewX, time, duration);
        ApplyPropertyKeyframe(layer, previousVisible?.Snapshot.SkewY, current.Snapshot.SkewY, AnimatedProperty.SkewY, time, duration);
        ApplyPropertyKeyframe(layer, previousVisible?.Snapshot.Rotation, current.Snapshot.Rotation, AnimatedProperty.Rotation, time, duration);
        ApplyPropertyKeyframe(layer, previousVisible?.Snapshot.RotationX, current.Snapshot.RotationX, AnimatedProperty.RotationX, time, duration);
        ApplyPropertyKeyframe(layer, previousVisible?.Snapshot.RotationY, current.Snapshot.RotationY, AnimatedProperty.RotationY, time, duration);
        ApplyPropertyKeyframe(layer, previousVisible?.Snapshot.ZDepth, current.Snapshot.ZDepth, AnimatedProperty.ZDepth, time, duration);
        ApplyPropertyKeyframe(layer, previousVisible?.Snapshot.Opacity, current.Snapshot.Opacity, AnimatedProperty.Opacity, time, duration);

        if (layer.Kind == LayerKind.Path &&
            (previousVisible is null || !PathPointsEqual(previousVisible.Snapshot.PathPoints, current.Snapshot.PathPoints, current.Snapshot.IsClosed, previousVisible.Snapshot.IsClosed)))
        {
            TimelineEditingService.SetShapeKeyframe(layer, time, current.Snapshot.PathPoints, current.Snapshot.IsClosed, duration);
        }

        if (layer.Kind == LayerKind.AvaloniaControl)
        {
            layer.Style.AvaloniaControl = current.Snapshot.AvaloniaControl.Clone();
        }
    }

    private static void ApplyPropertyKeyframe(
        TimelineLayer layer,
        double? previousValue,
        double currentValue,
        AnimatedProperty property,
        double time,
        double duration)
    {
        if (previousValue is not null && NearlyEqual(previousValue.Value, currentValue))
        {
            return;
        }

        TimelineEditingService.SetKeyframe(layer, property, time, currentValue, duration);
    }

    private static KeyframeModel ParseKeyframe(XElement element)
    {
        return new KeyframeModel
        {
            Time = ParseDouble(TryGetAttributeValue(element, "time"), 0d),
            Value = ParseDouble(TryGetAttributeValue(element, "value"), 0d),
            Easing = Enum.TryParse<EasingKind>(TryGetAttributeValue(element, "easing"), true, out var easing) ? easing : EasingKind.Linear,
            ControlPoint1X = ParseDouble(TryGetAttributeValue(element, "cp1x"), 0.25d),
            ControlPoint1Y = ParseDouble(TryGetAttributeValue(element, "cp1y"), 0.1d),
            ControlPoint2X = ParseDouble(TryGetAttributeValue(element, "cp2x"), 0.25d),
            ControlPoint2Y = ParseDouble(TryGetAttributeValue(element, "cp2y"), 1d)
        };
    }

    private static ShapeKeyframeModel ParseShapeKeyframe(XElement element)
    {
        var pathData = TryGetAttributeValue(element, "pathData");
        return new ShapeKeyframeModel
        {
            Time = ParseDouble(TryGetAttributeValue(element, "time"), 0d),
            Easing = Enum.TryParse<EasingKind>(TryGetAttributeValue(element, "easing"), true, out var easing) ? easing : EasingKind.Linear,
            ControlPoint1X = ParseDouble(TryGetAttributeValue(element, "cp1x"), 0.25d),
            ControlPoint1Y = ParseDouble(TryGetAttributeValue(element, "cp1y"), 0.1d),
            ControlPoint2X = ParseDouble(TryGetAttributeValue(element, "cp2x"), 0.25d),
            ControlPoint2Y = ParseDouble(TryGetAttributeValue(element, "cp2y"), 1d),
            IsClosed = ParseBool(TryGetAttributeValue(element, "isClosed"), IsClosedPath(pathData)),
            PathPoints = ParsePathPoints(pathData)
        };
    }

    private static LayerTextSettings ParseTextSettings(XElement element)
    {
        return new LayerTextSettings
        {
            FieldKind = ParseTextFieldKind(TryGetAttributeValue(element, "fieldKind"), element.Name.LocalName),
            LineMode = ParseTextLineMode(TryGetAttributeValue(element, "lineMode")),
            FontFamily = TryGetAttributeValue(element, "fontFamily") ?? string.Empty,
            IsBold = ParseBool(TryGetAttributeValue(element, "bold"), false),
            IsItalic = ParseBool(TryGetAttributeValue(element, "italic"), false),
            Alignment = ParseTextAlignment(TryGetAttributeValue(element, "alignment")),
            LetterSpacing = ParseDouble(TryGetAttributeValue(element, "letterSpacing"), 0d),
            LineHeight = Math.Max(0d, ParseDouble(TryGetAttributeValue(element, "lineHeight"), 0d)),
            IsSelectable = ParseBool(TryGetAttributeValue(element, "selectable"), true),
            ShowBorder = ParseBool(TryGetAttributeValue(element, "showBorder"), false),
            VariableName = TryGetAttributeValue(element, "variableName") ?? string.Empty,
            MaxCharacters = Math.Max(0, ParseInt(TryGetAttributeValue(element, "maxCharacters"), 0)),
            IsPassword = ParseBool(TryGetAttributeValue(element, "password"), false),
            UseDeviceFonts = ParseBool(TryGetAttributeValue(element, "useDeviceFonts"), false),
            AntiAliasMode = ParseTextAntiAliasMode(TryGetAttributeValue(element, "antiAliasMode")),
            RenderAsHtml = ParseBool(TryGetAttributeValue(element, "renderAsHtml"), false)
        };
    }

    private static string BuildLibraryManifest(IReadOnlyList<LibraryItem> items)
    {
        var root = new XElement(
            s_xflNs + "LibraryManifest",
            items.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).Select(item =>
                new XElement(
                    s_xflNs + "Item",
                    new XAttribute("name", item.Name),
                    new XAttribute("href", $"LIBRARY/{GetLibraryPackageFileName(item)}"),
                    new XAttribute("symbolType", ToFlashSymbolType(item.SymbolKind)),
                    new XAttribute("folderPath", item.FolderPath ?? string.Empty),
                    new XAttribute("linkageClassName", item.LinkageId ?? string.Empty),
                    new XAttribute("linkageBaseClass", item.BaseClassName ?? string.Empty),
                    new XAttribute("linkageExportForRuntime", item.ExportForRuntimeSharing),
                    new XAttribute("linkageImportForRuntime", item.ImportForRuntimeSharing),
                    new XAttribute("sharedLibraryPath", item.SharedLibraryPath ?? string.Empty),
                    new XAttribute("useScale9Grid", item.UseScale9Grid),
                    new XAttribute("registrationPointX", FormatNumber(item.RegistrationPointX)),
                    new XAttribute("registrationPointY", FormatNumber(item.RegistrationPointY)))));
        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString();
    }

    private static string BuildStandaloneLibraryItemDocument(LibraryItem item)
    {
        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), BuildLibraryItemElement(item)).ToString();
    }

    private static string BuildMediaManifest(IReadOnlyList<MediaAsset> items)
    {
        var payloadPlanByAssetId = BuildMediaPayloadPlan(items);
        var root = new XElement(
            s_xflNs + "MediaManifest",
            items.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).Select(item =>
            {
                payloadPlanByAssetId.TryGetValue(item.Id, out var payloadPlan);
                return new XElement(
                    s_xflNs + "Item",
                    new XAttribute("name", item.Name),
                    new XAttribute("type", item.Kind),
                    new XAttribute("durationSeconds", FormatNumber(item.Duration)),
                    new XAttribute("frameRate", FormatNumber(item.FrameRate)),
                    new XAttribute("width", item.Width),
                    new XAttribute("height", item.Height),
                    new XAttribute("sampleRate", item.SampleRate),
                    new XAttribute("channelCount", item.ChannelCount),
                    new XAttribute("sourceLabel", item.SourceLabel ?? string.Empty),
                    new XAttribute("relativePath", payloadPlan?.AssetRelativePath ?? string.Empty),
                    new XAttribute("frameFolder", payloadPlan?.FrameFolderRelativePath ?? string.Empty),
                    new XAttribute("frameCount", payloadPlan?.FrameRelativePaths.Count ?? 0),
                    new XAttribute("frameExtension", payloadPlan?.FrameExtension ?? string.Empty),
                    new XAttribute("previewFill", item.PreviewFill),
                    new XAttribute("previewAccent", item.PreviewAccent));
            }));
        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString();
    }

    private static string BuildPublishSettingsDocument(TimelineDocument document)
    {
        var scene = GetActiveScene(document);
        var root = new XElement(
            s_xflNs + "PublishSettings",
            new XAttribute("documentName", document.Name),
            new XAttribute("frameRate", FormatNumber(scene.FrameRate)),
            new XAttribute("width", FormatNumber(scene.CanvasWidth)),
            new XAttribute("height", FormatNumber(scene.CanvasHeight)),
            new XAttribute("backgroundColor", scene.BackgroundFrom),
            new XAttribute("backgroundColor2", scene.BackgroundFrom),
            new XAttribute("transparentStageBackground", scene.TransparentStageBackground),
            new XAttribute("documentType", document.Animate.Type),
            new XAttribute("units", document.Animate.Units),
            new XAttribute("responsive", document.Animate.IsResponsive),
            new XAttribute("useExternalScriptFile", document.Animate.UseExternalScriptFile),
            new XAttribute("useWebFonts", document.Animate.UseWebFonts),
            new XAttribute("useAdvancedLayers", document.Animate.UseAdvancedLayers),
            new XAttribute("templateName", document.Animate.TemplateName ?? string.Empty),
            new XAttribute("enable3DTransforms", document.Animate.Enable3DTransforms),
            new XAttribute("perspectiveAngle", FormatNumber(document.Animate.PerspectiveAngle)),
            new XAttribute("vanishingPointX", FormatNumber(document.Animate.VanishingPointX)),
            new XAttribute("vanishingPointY", FormatNumber(document.Animate.VanishingPointY)));

        root.Add(
            new XElement(
                s_xflNs + "profiles",
                document.PublishProfiles.Select(profile =>
                    new XElement(
                        s_xflNs + "profile",
                        new XAttribute("name", profile.Name),
                        new XAttribute("outputKind", profile.OutputKind),
                        new XAttribute("width", profile.Width),
                        new XAttribute("height", profile.Height),
                        new XAttribute("frameRate", FormatNumber(profile.FrameRate)),
                        new XAttribute("playAllScenes", profile.PlayAllScenes),
                        new XAttribute("transparentBackground", profile.TransparentBackground),
                        new XAttribute("includeSceneGraph", profile.IncludeSceneGraph),
                        new XAttribute("includeValidationReport", profile.IncludeValidationReport),
                        new XAttribute("includeAudio", profile.IncludeAudio),
                        new XAttribute("spriteSheetColumns", profile.SpriteSheetColumns),
                        new XAttribute("quality", profile.Quality)))));
        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString();
    }

    private static string BuildPackageMetadataDocument(TimelineDocument document)
    {
        var scene = GetActiveScene(document);
        var root = new XElement(
            "metadata",
            new XAttribute("documentName", document.Name),
            new XAttribute("generator", "TimelineAnimations Studio"),
            new XAttribute("xflVersion", "2.971"),
            new XAttribute("documentType", document.Animate.Type),
            new XAttribute("width", FormatNumber(scene.CanvasWidth)),
            new XAttribute("height", FormatNumber(scene.CanvasHeight)),
            new XAttribute("frameRate", FormatNumber(scene.FrameRate)),
            new XAttribute("createdUtc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)));
        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString();
    }

    private static string BuildMobileSettingsDocument(TimelineDocument document)
    {
        var scene = GetActiveScene(document);
        var root = new XElement(
            "MobileSettings",
            new XAttribute("documentType", document.Animate.Type),
            new XAttribute("width", FormatNumber(scene.CanvasWidth)),
            new XAttribute("height", FormatNumber(scene.CanvasHeight)),
            new XAttribute("frameRate", FormatNumber(scene.FrameRate)),
            new XAttribute("responsive", document.Animate.IsResponsive),
            new XAttribute("transparentStageBackground", scene.TransparentStageBackground));
        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString();
    }

    private static FlashAuthoringPackage BuildPackage(
        TimelineDocument document,
        FlashAuthoringContainerKind kind,
        string sourceLabel)
    {
        var documentXml = Export(document);
        var mediaPayloadPlan = BuildMediaPayloadPlan(document.MediaAssets);
        var entries = new List<FlashAuthoringPackageEntry>();

        if (document.LibraryItems.Count > 0)
        {
            foreach (var item in document.LibraryItems.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                entries.Add(new FlashAuthoringPackageEntry
                {
                    RelativePath = $"LIBRARY/{GetLibraryPackageFileName(item)}",
                    Data = Encoding.UTF8.GetBytes(BuildStandaloneLibraryItemDocument(item)),
                    Kind = FlashAuthoringPackageEntryKind.LibrarySymbol
                });
            }

            entries.Add(new FlashAuthoringPackageEntry
            {
                RelativePath = "LIBRARY/manifest.xml",
                Data = Encoding.UTF8.GetBytes(BuildLibraryManifest(document.LibraryItems)),
                Kind = FlashAuthoringPackageEntryKind.LibraryManifest
            });
        }

        if (document.MediaAssets.Count > 0)
        {
            entries.Add(new FlashAuthoringPackageEntry
            {
                RelativePath = "MEDIA/manifest.xml",
                Data = Encoding.UTF8.GetBytes(BuildMediaManifest(document.MediaAssets)),
                Kind = FlashAuthoringPackageEntryKind.MediaManifest
            });

            entries.AddRange(BuildMediaPayloadEntries(document.MediaAssets, mediaPayloadPlan));
        }

        entries.Add(new FlashAuthoringPackageEntry
        {
            RelativePath = "PublishSettings.xml",
            Data = Encoding.UTF8.GetBytes(BuildPublishSettingsDocument(document)),
            Kind = FlashAuthoringPackageEntryKind.PublishSettings
        });

        NormalizeFlashSupplementalFiles(document);
        foreach (var supplementalFile in document.FlashAuthoringSupplementalFiles)
        {
            if (string.IsNullOrWhiteSpace(supplementalFile.RelativePath) || supplementalFile.Data.Length == 0)
            {
                continue;
            }

            if (entries.Any(entry => string.Equals(entry.RelativePath, supplementalFile.RelativePath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            entries.Add(new FlashAuthoringPackageEntry
            {
                RelativePath = supplementalFile.RelativePath,
                Data = supplementalFile.Data,
                Kind = FlashAuthoringPackageEntryKind.Other
            });
        }

        AddPackageEntryIfMissing(
            entries,
            "mimetype",
            Encoding.UTF8.GetBytes(FlashAuthoringPackageService.DefaultMimeType),
            FlashAuthoringPackageEntryKind.MimeType);
        AddPackageEntryIfMissing(
            entries,
            "META-INF/metadata.xml",
            Encoding.UTF8.GetBytes(BuildPackageMetadataDocument(document)),
            FlashAuthoringPackageEntryKind.Metadata);
        AddPackageEntryIfMissing(
            entries,
            "MobileSettings.xml",
            Encoding.UTF8.GetBytes(BuildMobileSettingsDocument(document)),
            FlashAuthoringPackageEntryKind.MobileSettings);
        AddPackageEntryIfMissing(
            entries,
            $"{SanitizePackageName(document.Name)}.xfl",
            Encoding.UTF8.GetBytes(documentXml),
            FlashAuthoringPackageEntryKind.ProjectFile);

        return FlashAuthoringPackageService.CreatePackage(sourceLabel, documentXml, entries, kind);
    }

    private static void AddPackageEntryIfMissing(
        ICollection<FlashAuthoringPackageEntry> entries,
        string relativePath,
        byte[] data,
        FlashAuthoringPackageEntryKind kind)
    {
        if (entries.Any(entry => string.Equals(entry.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        entries.Add(new FlashAuthoringPackageEntry
        {
            RelativePath = relativePath,
            Data = data,
            Kind = kind
        });
    }

    private static IReadOnlyDictionary<Guid, FlashMediaPayloadPlan> BuildMediaPayloadPlan(IReadOnlyList<MediaAsset> mediaAssets)
    {
        var reservedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var planByAssetId = new Dictionary<Guid, FlashMediaPayloadPlan>();

        foreach (var asset in mediaAssets.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (asset.Kind == MediaAssetKind.Audio)
            {
                var preferredPath = NormalizePackageRelativePath(asset.PackageRelativePath);
                var extension = GetPreferredFileExtension(preferredPath ?? asset.SourceLabel, ".wav");
                var assetPath = ReserveUniqueFilePath(preferredPath, $"MEDIA/{SanitizePackageName(asset.Name)}{extension}", reservedPaths);
                planByAssetId[asset.Id] = new FlashMediaPayloadPlan(assetPath, string.Empty, [], extension);
                continue;
            }

            if (asset.Frames.Count > 0)
            {
                if (asset.Frames.Count == 1)
                {
                    var preferredPath = NormalizePackageRelativePath(asset.Frames[0].PackageRelativePath);
                    var frameExtension = GetPreferredFileExtension(preferredPath ?? asset.PackageRelativePath ?? asset.SourceLabel, ".png");
                    var framePath = ReserveUniqueFilePath(preferredPath, $"MEDIA/{SanitizePackageName(asset.Name)}{frameExtension}", reservedPaths);
                    planByAssetId[asset.Id] = new FlashMediaPayloadPlan(framePath, string.Empty, [framePath], frameExtension);
                    continue;
                }

                var preferredFolder = NormalizePackageRelativePath(asset.PackageRelativePath);
                var folderPath = ReserveUniqueFolderPath(preferredFolder, $"MEDIA/{SanitizePackageName(asset.Name)}", reservedPaths);
                var extension = GetPreferredFileExtension(
                    asset.Frames.FirstOrDefault(static frame => !string.IsNullOrWhiteSpace(frame.PackageRelativePath))?.PackageRelativePath ??
                    asset.SourceLabel,
                    ".png");
                var framePaths = new List<string>(asset.Frames.Count);
                for (var frameIndex = 0; frameIndex < asset.Frames.Count; frameIndex++)
                {
                    var preferredPath = NormalizePackageRelativePath(asset.Frames[frameIndex].PackageRelativePath);
                    var fallbackPath = $"{folderPath}/frame-{frameIndex + 1:D4}{extension}";
                    framePaths.Add(ReserveUniqueFilePath(preferredPath, fallbackPath, reservedPaths));
                }

                planByAssetId[asset.Id] = new FlashMediaPayloadPlan(string.Empty, folderPath, framePaths, extension);
                continue;
            }

            var existingPath = NormalizePackageRelativePath(asset.PackageRelativePath);
            if (!string.IsNullOrWhiteSpace(existingPath))
            {
                planByAssetId[asset.Id] = new FlashMediaPayloadPlan(
                    ReserveUniqueFilePath(existingPath, existingPath, reservedPaths),
                    string.Empty,
                    [],
                    Path.GetExtension(existingPath));
            }
        }

        return planByAssetId;
    }

    private static IEnumerable<FlashAuthoringPackageEntry> BuildMediaPayloadEntries(
        IReadOnlyList<MediaAsset> mediaAssets,
        IReadOnlyDictionary<Guid, FlashMediaPayloadPlan> payloadPlanByAssetId)
    {
        foreach (var asset in mediaAssets)
        {
            if (!payloadPlanByAssetId.TryGetValue(asset.Id, out var payloadPlan))
            {
                continue;
            }

            if (asset.Kind == MediaAssetKind.Audio)
            {
                if (string.IsNullOrWhiteSpace(asset.AudioBase64) || string.IsNullOrWhiteSpace(payloadPlan.AssetRelativePath))
                {
                    continue;
                }

                yield return new FlashAuthoringPackageEntry
                {
                    RelativePath = payloadPlan.AssetRelativePath,
                    Data = Convert.FromBase64String(asset.AudioBase64),
                    Kind = FlashAuthoringPackageEntryKind.Other
                };
                continue;
            }

            for (var frameIndex = 0; frameIndex < Math.Min(asset.Frames.Count, payloadPlan.FrameRelativePaths.Count); frameIndex++)
            {
                var frame = asset.Frames[frameIndex];
                if (string.IsNullOrWhiteSpace(frame.ImageBase64))
                {
                    continue;
                }

                yield return new FlashAuthoringPackageEntry
                {
                    RelativePath = payloadPlan.FrameRelativePaths[frameIndex],
                    Data = Convert.FromBase64String(frame.ImageBase64),
                    Kind = FlashAuthoringPackageEntryKind.Other
                };
            }
        }
    }

    private static void ApplyPublishSettings(
        FlashAuthoringPackage? package,
        TimelineDocument document,
        List<AnimationExchangeIssue> issues)
    {
        if (package is null)
        {
            return;
        }

        var publishSettingsEntry = package.Entries.FirstOrDefault(static entry => entry.Kind == FlashAuthoringPackageEntryKind.PublishSettings);
        if (publishSettingsEntry is null || publishSettingsEntry.Data.Length == 0)
        {
            return;
        }

        var xml = Encoding.UTF8.GetString(publishSettingsEntry.Data).TrimStart('\uFEFF');
        if (string.IsNullOrWhiteSpace(xml))
        {
            return;
        }

        var documentElement = XDocument.Parse(xml, LoadOptions.PreserveWhitespace).Root;
        if (documentElement is null)
        {
            return;
        }

        document.Animate.Type = ParseDocumentType(TryGetAttributeValue(documentElement, "documentType"), document.Animate.Type);
        document.Animate.Units = ParseDocumentUnits(TryGetAttributeValue(documentElement, "units"), document.Animate.Units);
        document.Animate.IsResponsive = ParseBool(TryGetAttributeValue(documentElement, "responsive"), document.Animate.IsResponsive);
        document.Animate.UseExternalScriptFile = ParseBool(TryGetAttributeValue(documentElement, "useExternalScriptFile"), document.Animate.UseExternalScriptFile);
        document.Animate.UseWebFonts = ParseBool(TryGetAttributeValue(documentElement, "useWebFonts"), document.Animate.UseWebFonts);
        document.Animate.UseAdvancedLayers = ParseBool(TryGetAttributeValue(documentElement, "useAdvancedLayers"), document.Animate.UseAdvancedLayers);
        document.Animate.TemplateName = TryGetAttributeValue(documentElement, "templateName") ?? document.Animate.TemplateName;
        document.Animate.Enable3DTransforms = ParseBool(TryGetAttributeValue(documentElement, "enable3DTransforms"), document.Animate.Enable3DTransforms);
        document.Animate.PerspectiveAngle = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(documentElement, "perspectiveAngle"), document.Animate.PerspectiveAngle), 1d, 89d);
        document.Animate.VanishingPointX = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(documentElement, "vanishingPointX"), document.Animate.VanishingPointX), 0d, 1d);
        document.Animate.VanishingPointY = TimelineMath.Clamp(ParseDouble(TryGetAttributeValue(documentElement, "vanishingPointY"), document.Animate.VanishingPointY), 0d, 1d);

        var profilesElement = documentElement.Elements().FirstOrDefault(static element => element.Name.LocalName == "profiles");
        if (profilesElement is not null)
        {
            document.PublishProfiles = profilesElement.Elements()
                .Where(static element => element.Name.LocalName == "profile")
                .Select(ParsePublishProfile)
                .ToList();
        }

        issues.Add(CreateIssue(AnimationExchangeIssueSeverity.Info, "Flash XFL", "Imported PublishSettings.xml document-target and publish-profile metadata."));
    }

    private static PublishProfile ParsePublishProfile(XElement element)
    {
        return new PublishProfile
        {
            Name = TryGetAttributeValue(element, "name") ?? "Publish",
            OutputKind = Enum.TryParse<PublishOutputKind>(TryGetAttributeValue(element, "outputKind"), true, out var outputKind)
                ? outputKind
                : PublishOutputKind.PngSequence,
            Width = Math.Max(1, ParseInt(TryGetAttributeValue(element, "width"), 1280)),
            Height = Math.Max(1, ParseInt(TryGetAttributeValue(element, "height"), 720)),
            FrameRate = Math.Max(1d, ParseDouble(TryGetAttributeValue(element, "frameRate"), 24d)),
            PlayAllScenes = ParseBool(TryGetAttributeValue(element, "playAllScenes"), false),
            TransparentBackground = ParseBool(TryGetAttributeValue(element, "transparentBackground"), false),
            IncludeSceneGraph = ParseBool(TryGetAttributeValue(element, "includeSceneGraph"), true),
            IncludeValidationReport = ParseBool(TryGetAttributeValue(element, "includeValidationReport"), true),
            IncludeAudio = ParseBool(TryGetAttributeValue(element, "includeAudio"), true),
            SpriteSheetColumns = Math.Max(1, ParseInt(TryGetAttributeValue(element, "spriteSheetColumns"), 6)),
            Quality = Math.Clamp(ParseInt(TryGetAttributeValue(element, "quality"), 90), 1, 100)
        };
    }

    private static List<FlashAuthoringSupplementalFile> ExtractSupplementalFiles(
        FlashAuthoringPackage package,
        TimelineDocument document)
    {
        var consumedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            NormalizePackageRelativePath(package.DocumentRelativePath) ?? "DOMDocument.xml",
            "LIBRARY/manifest.xml",
            "MEDIA/manifest.xml",
            "PublishSettings.xml"
        };

        foreach (var entry in package.Entries.Where(static entry =>
                     entry.RelativePath.StartsWith("LIBRARY/", StringComparison.OrdinalIgnoreCase) &&
                     entry.RelativePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
        {
            consumedPaths.Add(entry.RelativePath);
        }

        foreach (var mediaAsset in document.MediaAssets)
        {
            if (!string.IsNullOrWhiteSpace(mediaAsset.PackageRelativePath))
            {
                consumedPaths.Add(mediaAsset.PackageRelativePath);
            }

            foreach (var frame in mediaAsset.Frames)
            {
                if (!string.IsNullOrWhiteSpace(frame.PackageRelativePath))
                {
                    consumedPaths.Add(frame.PackageRelativePath);
                }
            }
        }

        return package.Entries
            .Where(static entry => entry.Kind != FlashAuthoringPackageEntryKind.Document)
            .Where(entry => !consumedPaths.Contains(entry.RelativePath))
            .Select(entry => new FlashAuthoringSupplementalFile
            {
                RelativePath = entry.RelativePath,
                Data = entry.Data
            })
            .ToList();
    }

    private static void NormalizeFlashSupplementalFiles(TimelineDocument document)
    {
        document.FlashAuthoringSupplementalFiles = document.FlashAuthoringSupplementalFiles
            .Where(file => !string.IsNullOrWhiteSpace(file.RelativePath) && file.Data.Length > 0)
            .GroupBy(file => NormalizePackageRelativePath(file.RelativePath) ?? file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(static group =>
            {
                var file = group.Last();
                file.RelativePath = group.Key;
                return file;
            })
            .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void HydrateMediaAssetFromPackage(
        MediaAsset asset,
        FlashAuthoringPackage package,
        string? relativePath,
        string? frameFolder,
        string? frameExtension)
    {
        if (asset.Kind == MediaAssetKind.Audio &&
            TryResolveMediaEntry(package, relativePath, asset.Name, asset.Kind, out var audioEntry))
        {
            asset.AudioBase64 = Convert.ToBase64String(audioEntry.Data);
            asset.PackageRelativePath = audioEntry.RelativePath;
            return;
        }

        var frameEntries = ResolveFrameEntries(package, relativePath, frameFolder, asset.Name, frameExtension);
        if (frameEntries.Count > 0)
        {
            var frameDuration = 1d / Math.Max(1d, asset.FrameRate);
            asset.PackageRelativePath = NormalizePackageRelativePath(frameFolder) ??
                                        Path.GetDirectoryName(frameEntries[0].RelativePath)?.Replace('\\', '/') ??
                                        frameEntries[0].RelativePath;
            asset.Frames = frameEntries.Select((entry, index) => new MediaFrameModel
            {
                Duration = frameDuration,
                PackageRelativePath = entry.RelativePath,
                ImageBase64 = Convert.ToBase64String(entry.Data),
                Fill = asset.PreviewFill,
                Accent = asset.PreviewAccent,
                Label = $"F{index + 1:00}"
            }).ToList();
            return;
        }

        if (TryResolveMediaEntry(package, relativePath, asset.Name, asset.Kind, out var singleEntry) &&
            LooksLikeImagePath(singleEntry.RelativePath))
        {
            asset.PackageRelativePath = singleEntry.RelativePath;
            asset.Frames =
            [
                new MediaFrameModel
                {
                    Duration = Math.Max(1d / Math.Max(1d, asset.FrameRate), asset.Duration),
                    PackageRelativePath = singleEntry.RelativePath,
                    ImageBase64 = Convert.ToBase64String(singleEntry.Data),
                    Fill = asset.PreviewFill,
                    Accent = asset.PreviewAccent,
                    Label = "F01"
                }
            ];
        }
    }

    private static bool TryGetPackageEntry(FlashAuthoringPackage package, string? relativePath, out FlashAuthoringPackageEntry entry)
    {
        var normalizedPath = NormalizePackageRelativePath(relativePath);
        entry = package.Entries.FirstOrDefault(candidate => string.Equals(candidate.RelativePath, normalizedPath, StringComparison.OrdinalIgnoreCase))!;
        return entry is not null;
    }

    private static bool TryResolveMediaEntry(
        FlashAuthoringPackage package,
        string? relativePath,
        string? assetName,
        MediaAssetKind kind,
        out FlashAuthoringPackageEntry entry)
    {
        if (TryGetPackageEntry(package, relativePath, out entry))
        {
            return true;
        }

        var fileName = Path.GetFileName(NormalizePackageRelativePath(relativePath) ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            entry = package.Entries.FirstOrDefault(candidate =>
                string.Equals(Path.GetFileName(candidate.RelativePath), fileName, StringComparison.OrdinalIgnoreCase))!;
            if (entry is not null)
            {
                return true;
            }
        }

        var sanitizedName = SanitizePackageName(assetName ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(sanitizedName))
        {
            entry = package.Entries.FirstOrDefault(candidate =>
                candidate.RelativePath.StartsWith("MEDIA/", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Path.GetFileNameWithoutExtension(candidate.RelativePath), sanitizedName, StringComparison.OrdinalIgnoreCase) &&
                IsMatchingMediaEntry(candidate.RelativePath, kind))!;
            if (entry is not null)
            {
                return true;
            }
        }

        entry = null!;
        return false;
    }

    private static List<FlashAuthoringPackageEntry> ResolveFrameEntries(
        FlashAuthoringPackage package,
        string? relativePath,
        string? frameFolder,
        string? assetName,
        string? frameExtension)
    {
        var normalizedFrameFolder = NormalizePackageRelativePath(frameFolder);
        if (!string.IsNullOrWhiteSpace(normalizedFrameFolder))
        {
            var directFolderEntries = package.Entries
                .Where(entry => entry.RelativePath.StartsWith(normalizedFrameFolder + "/", StringComparison.OrdinalIgnoreCase))
                .Where(entry => MatchesFrameExtension(entry.RelativePath, frameExtension))
                .OrderBy(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (directFolderEntries.Count > 0)
            {
                return directFolderEntries;
            }
        }

        var normalizedRelativePath = NormalizePackageRelativePath(relativePath);
        if (!string.IsNullOrWhiteSpace(normalizedRelativePath) &&
            TryGetPackageEntry(package, normalizedRelativePath, out var directEntry) &&
            LooksLikeImagePath(directEntry.RelativePath))
        {
            return [directEntry];
        }

        var sanitizedName = SanitizePackageName(assetName ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(sanitizedName))
        {
            var nameFolderEntries = package.Entries
                .Where(entry => entry.RelativePath.StartsWith($"MEDIA/{sanitizedName}/", StringComparison.OrdinalIgnoreCase))
                .Where(entry => MatchesFrameExtension(entry.RelativePath, frameExtension))
                .OrderBy(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (nameFolderEntries.Count > 0)
            {
                return nameFolderEntries;
            }

            var nameFileEntries = package.Entries
                .Where(entry => entry.RelativePath.StartsWith("MEDIA/", StringComparison.OrdinalIgnoreCase))
                .Where(entry => string.Equals(Path.GetFileNameWithoutExtension(entry.RelativePath), sanitizedName, StringComparison.OrdinalIgnoreCase))
                .Where(entry => LooksLikeImagePath(entry.RelativePath))
                .OrderBy(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (nameFileEntries.Count > 0)
            {
                return nameFileEntries;
            }
        }

        return [];
    }

    private static bool MatchesFrameExtension(string path, string? frameExtension)
    {
        return string.IsNullOrWhiteSpace(frameExtension) ||
               path.EndsWith(frameExtension, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMatchingMediaEntry(string relativePath, MediaAssetKind kind)
    {
        var extension = Path.GetExtension(relativePath);
        return kind switch
        {
            MediaAssetKind.Audio => extension.Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                                    extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase),
            _ => LooksLikeImagePath(relativePath)
        };
    }

    private static MediaAssetKind ParseMediaAssetKind(XElement element)
    {
        if (Enum.TryParse<MediaAssetKind>(TryGetAttributeValue(element, "assetType"), true, out var kind))
        {
            return kind;
        }

        return element.Name.LocalName switch
        {
            "DOMSoundItem" => MediaAssetKind.Audio,
            _ => MediaAssetKind.Video
        };
    }

    private static AnimateDocumentType ParseDocumentType(string? value, AnimateDocumentType fallback = AnimateDocumentType.ActionScript3)
    {
        return Enum.TryParse<AnimateDocumentType>(value, true, out var type) ? type : fallback;
    }

    private static AnimateDocumentUnits ParseDocumentUnits(string? value, AnimateDocumentUnits fallback = AnimateDocumentUnits.Pixels)
    {
        return Enum.TryParse<AnimateDocumentUnits>(value, true, out var units) ? units : fallback;
    }

    private static string? NormalizePackageRelativePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Replace('\\', '/').TrimStart('/');
    }

    private static bool LooksLikeImagePath(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".gif", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPreferredFileExtension(string? path, string fallback)
    {
        var extension = Path.GetExtension(path ?? string.Empty);
        return string.IsNullOrWhiteSpace(extension) ? fallback : extension;
    }

    private static string ReserveUniqueFilePath(string? preferredPath, string fallbackPath, HashSet<string> reservedPaths)
    {
        var path = NormalizePackageRelativePath(preferredPath) ?? NormalizePackageRelativePath(fallbackPath) ?? fallbackPath;
        var directory = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        var candidate = path;
        var suffix = 2;
        while (reservedPaths.Contains(candidate))
        {
            candidate = string.IsNullOrWhiteSpace(directory)
                ? $"{fileNameWithoutExtension}-{suffix}{extension}"
                : $"{directory}/{fileNameWithoutExtension}-{suffix}{extension}";
            suffix++;
        }

        reservedPaths.Add(candidate);
        return candidate;
    }

    private static string ReserveUniqueFolderPath(string? preferredPath, string fallbackPath, HashSet<string> reservedPaths)
    {
        var folder = NormalizePackageRelativePath(preferredPath) ?? NormalizePackageRelativePath(fallbackPath) ?? fallbackPath;
        var candidate = folder;
        var suffix = 2;
        while (reservedPaths.Any(path => path.StartsWith(candidate + "/", StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{folder}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static string SanitizePackageName(string value)
    {
        var sanitized = Regex.Replace(value, "[^A-Za-z0-9_-]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(sanitized) ? "media" : sanitized;
    }

    private static IReadOnlyList<XElement> ReadPackagedLibrarySymbolElements(FlashAuthoringPackage package)
    {
        var elements = new List<XElement>();
        foreach (var entry in package.Entries.Where(static entry =>
                     entry.RelativePath.StartsWith("LIBRARY/", StringComparison.OrdinalIgnoreCase) &&
                     entry.RelativePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(Path.GetFileName(entry.RelativePath), "manifest.xml", StringComparison.OrdinalIgnoreCase)))
        {
            var content = Encoding.UTF8.GetString(entry.Data).TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var document = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
            var element = document.Root?.Name.LocalName == "DOMSymbolItem"
                ? document.Root
                : document.Descendants().FirstOrDefault(candidate => candidate.Name.LocalName == "DOMSymbolItem");
            if (element is not null)
            {
                elements.Add(element);
            }
        }

        return elements;
    }

    private static string GetLibraryPackageFileName(LibraryItem item)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(item.Name.Length);
        foreach (var character in item.Name)
        {
            builder.Append(invalidCharacters.Contains(character) ? '_' : character);
        }

        var normalized = builder.Length == 0 ? "symbol" : builder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "symbol";
        }

        return $"{normalized}.xml";
    }

    private static SceneModel GetActiveScene(TimelineDocument document)
    {
        return document.Scenes.FirstOrDefault(scene => scene.Id == document.ActiveSceneId)
            ?? document.Scenes.First();
    }

    private static string ToFlashLayerType(TimelineLayer layer)
    {
        if (layer.Kind == LayerKind.Folder)
        {
            return "folder";
        }

        return layer.Compositing.Role switch
        {
            LayerCompositeRole.Guide => "guide",
            LayerCompositeRole.Mask => "mask",
            LayerCompositeRole.Camera => "camera",
            _ => "normal"
        };
    }

    private static string ToFlashSymbolType(SymbolKind symbolKind)
    {
        return symbolKind switch
        {
            SymbolKind.MovieClip => "movie clip",
            SymbolKind.Button => "button",
            _ => "graphic"
        };
    }

    private static SymbolKind ParseFlashSymbolType(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "movie clip" or "movieclip" => SymbolKind.MovieClip,
            "button" => SymbolKind.Button,
            _ => SymbolKind.Graphic
        };
    }

    private static LayerTextAlignment ParseTextAlignment(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "center" => LayerTextAlignment.Center,
            "right" => LayerTextAlignment.Right,
            "justify" => LayerTextAlignment.Justify,
            _ => LayerTextAlignment.Left
        };
    }

    private static FlashTextFieldKind ParseTextFieldKind(string? value, string? elementName = null)
    {
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<FlashTextFieldKind>(value, true, out var parsed))
        {
            return parsed;
        }

        if (string.Equals(elementName, "DOMInputText", StringComparison.OrdinalIgnoreCase))
        {
            return FlashTextFieldKind.Input;
        }

        return string.Equals(elementName, "DOMDynamicText", StringComparison.OrdinalIgnoreCase)
            ? FlashTextFieldKind.Dynamic
            : FlashTextFieldKind.Static;
    }

    private static FlashTextLineMode ParseTextLineMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "multiline" => FlashTextLineMode.Multiline,
            "multilinenowrap" or "multiline_no_wrap" or "multiline-no-wrap" => FlashTextLineMode.MultilineNoWrap,
            _ => FlashTextLineMode.SingleLine
        };
    }

    private static FlashTextAntiAliasMode ParseTextAntiAliasMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "readability" => FlashTextAntiAliasMode.Readability,
            "device" => FlashTextAntiAliasMode.Device,
            _ => FlashTextAntiAliasMode.Animation
        };
    }

    private static string ToXflTextFieldKind(FlashTextFieldKind kind)
    {
        return kind switch
        {
            FlashTextFieldKind.Dynamic => "dynamic",
            FlashTextFieldKind.Input => "input",
            _ => "static"
        };
    }

    private static string ToXflTextLineMode(FlashTextLineMode mode)
    {
        return mode switch
        {
            FlashTextLineMode.Multiline => "multiline",
            FlashTextLineMode.MultilineNoWrap => "multilineNoWrap",
            _ => "singleLine"
        };
    }

    private static string ToXflTextAntiAliasMode(FlashTextAntiAliasMode mode)
    {
        return mode switch
        {
            FlashTextAntiAliasMode.Readability => "readability",
            FlashTextAntiAliasMode.Device => "device",
            _ => "animation"
        };
    }

    private static string ToXflGradientKind(LayerGradientKind kind)
    {
        return kind switch
        {
            LayerGradientKind.Radial => "radial",
            _ => "linear"
        };
    }

    private static string ToXflBlendMode(LayerBlendMode mode)
    {
        return mode switch
        {
            LayerBlendMode.Layer => "layer",
            LayerBlendMode.Add => "add",
            LayerBlendMode.Screen => "screen",
            LayerBlendMode.Multiply => "multiply",
            LayerBlendMode.Overlay => "overlay",
            LayerBlendMode.Darken => "darken",
            LayerBlendMode.Lighten => "lighten",
            LayerBlendMode.Difference => "difference",
            LayerBlendMode.HardLight => "hardlight",
            LayerBlendMode.Subtract => "subtract",
            LayerBlendMode.Invert => "invert",
            LayerBlendMode.Alpha => "alpha",
            LayerBlendMode.Erase => "erase",
            _ => "normal"
        };
    }

    private static ShapeDrawingMode ParseShapeDrawingMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "object" => ShapeDrawingMode.Object,
            "primitive" => ShapeDrawingMode.Primitive,
            _ => ShapeDrawingMode.Merge
        };
    }

    private static PrimitiveShapeType ParsePrimitiveShapeType(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "rectangle" => PrimitiveShapeType.Rectangle,
            "ellipse" => PrimitiveShapeType.Ellipse,
            "polystar" or "poly_star" or "poly-star" => PrimitiveShapeType.PolyStar,
            _ => PrimitiveShapeType.None
        };
    }

    private static string ToXflStrokeCap(LayerStrokeCapStyle cap)
    {
        return cap switch
        {
            LayerStrokeCapStyle.Butt => "butt",
            LayerStrokeCapStyle.Square => "square",
            _ => "round"
        };
    }

    private static string ToXflStrokeJoin(LayerStrokeJoinStyle join)
    {
        return join switch
        {
            LayerStrokeJoinStyle.Bevel => "bevel",
            LayerStrokeJoinStyle.Round => "round",
            _ => "miter"
        };
    }

    private static LayerGradientKind ParseGradientKind(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "radial" => LayerGradientKind.Radial,
            _ => LayerGradientKind.Linear
        };
    }

    private static LayerBlendMode ParseXflBlendMode(string? value, LayerBlendMode fallback)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "normal" => LayerBlendMode.Normal,
            "layer" => LayerBlendMode.Layer,
            "add" => LayerBlendMode.Add,
            "screen" => LayerBlendMode.Screen,
            "multiply" => LayerBlendMode.Multiply,
            "overlay" => LayerBlendMode.Overlay,
            "darken" => LayerBlendMode.Darken,
            "lighten" => LayerBlendMode.Lighten,
            "difference" => LayerBlendMode.Difference,
            "hardlight" or "hardLight" => LayerBlendMode.HardLight,
            "subtract" => LayerBlendMode.Subtract,
            "invert" => LayerBlendMode.Invert,
            "alpha" => LayerBlendMode.Alpha,
            "erase" => LayerBlendMode.Erase,
            _ when Enum.TryParse<LayerBlendMode>(value, true, out var parsed) => parsed,
            _ => fallback
        };
    }

    private static LayerFilterQuality ParseFilterQuality(string? value, LayerFilterQuality fallback)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            return fallback;
        }

        return numeric switch
        {
            >= 3 => LayerFilterQuality.High,
            2 => LayerFilterQuality.Medium,
            1 => LayerFilterQuality.Low,
            _ => fallback
        };
    }

    private static LayerStrokeCapStyle ParseStrokeCapStyle(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "butt" => LayerStrokeCapStyle.Butt,
            "square" => LayerStrokeCapStyle.Square,
            _ => LayerStrokeCapStyle.Round
        };
    }

    private static LayerStrokeJoinStyle ParseStrokeJoinStyle(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "bevel" => LayerStrokeJoinStyle.Bevel,
            "round" => LayerStrokeJoinStyle.Round,
            _ => LayerStrokeJoinStyle.Miter
        };
    }

    private static bool PathPointsEqual(
        IReadOnlyList<VectorPointModel> left,
        IReadOnlyList<VectorPointModel> right,
        bool leftClosed,
        bool rightClosed)
    {
        if (leftClosed != rightClosed || left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (!NearlyEqual(left[index].X, right[index].X) || !NearlyEqual(left[index].Y, right[index].Y))
            {
                return false;
            }
        }

        return true;
    }

    private static bool NearlyEqual(double left, double right)
    {
        return Math.Abs(left - right) <= ComparisonTolerance;
    }

    private static string BuildPathData(IReadOnlyList<VectorPointModel> points, double width, double height, bool isClosed)
    {
        if (points.Count < 2)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("M ").Append(FormatNumber(points[0].X * width)).Append(',').Append(FormatNumber(points[0].Y * height));
        for (var index = 1; index < points.Count; index++)
        {
            builder.Append(" L ").Append(FormatNumber(points[index].X * width)).Append(',').Append(FormatNumber(points[index].Y * height));
        }

        if (isClosed)
        {
            builder.Append(" Z");
        }

        return builder.ToString();
    }

    private static List<VectorPointModel> ParsePathPoints(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return [];
        }

        var numbers = NumberRegex().Matches(data)
            .Select(match => ParseDouble(match.Value, double.NaN))
            .Where(value => !double.IsNaN(value))
            .ToList();
        if (numbers.Count < 4)
        {
            return [];
        }

        var points = new List<VectorPointModel>(numbers.Count / 2);
        for (var index = 0; index + 1 < numbers.Count; index += 2)
        {
            points.Add(new VectorPointModel { X = numbers[index], Y = numbers[index + 1] });
        }

        return points;
    }

    private static bool IsClosedPath(string? data)
    {
        return !string.IsNullOrWhiteSpace(data) && data.Contains('Z', StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static double ParseDouble(string? value, double fallback)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static int ParseInt(string? value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static bool ParseBool(string? value, bool fallback)
    {
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static string? TryGetAttributeValue(XElement? element, params string[] names)
    {
        if (element is null)
        {
            return null;
        }

        foreach (var attribute in element.Attributes())
        {
            foreach (var name in names)
            {
                if (string.Equals(attribute.Name.LocalName, name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(attribute.Name.ToString(), name, StringComparison.OrdinalIgnoreCase))
                {
                    return attribute.Value;
                }
            }
        }

        return null;
    }

    private static AnimationExchangeIssue CreateIssue(AnimationExchangeIssueSeverity severity, string source, string message)
    {
        return new AnimationExchangeIssue
        {
            Severity = severity,
            Source = source,
            Message = message
        };
    }

    [GeneratedRegex(@"-?\d+(?:\.\d+)?", RegexOptions.Compiled)]
    private static partial Regex NumberRegex();

    private sealed record FlashMediaManifestItem(
        string Name,
        MediaAssetKind Kind,
        double Duration,
        double FrameRate,
        int Width,
        int Height,
        int SampleRate,
        int ChannelCount,
        string SourceLabel,
        string? RelativePath,
        string? FrameFolder,
        int FrameCount,
        string? FrameExtension,
        string PreviewFill,
        string PreviewAccent);

    private sealed record FlashMediaPayloadPlan(
        string AssetRelativePath,
        string FrameFolderRelativePath,
        IReadOnlyList<string> FrameRelativePaths,
        string? FrameExtension);

    private sealed record FlashFrameVisual(
        LayerKind Kind,
        LayerSnapshot Snapshot,
        string? LibraryItemName,
        string InstanceName,
        SymbolPlaybackMode SymbolPlaybackMode,
        double SymbolPlaybackOffset,
        int SymbolLockedFrame,
        ButtonVisualState SymbolButtonState,
        bool CacheAsBitmap,
        string BitmapCacheBackgroundColor,
        string? MediaName,
        MediaAssetKind? MediaKind);

    private sealed record ParsedFrameRecord(int Frame, FlashFrameVisual? Sample, bool IsBlank);
}
