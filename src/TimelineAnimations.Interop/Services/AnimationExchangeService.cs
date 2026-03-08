using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static partial class AnimationExchangeService
{
    private const string MetadataMarker = "TimelineAnimationsMetadata:";
    private const string HtmlMetadataName = "timeline-animations-metadata";
    private const string SvgMetadataId = "TimelineAnimationsMetadata";

    private static readonly XNamespace s_avaloniaNs = "https://github.com/avaloniaui";
    private static readonly XNamespace s_xamlNs = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly XNamespace s_svgNs = "http://www.w3.org/2000/svg";
    private static readonly XNamespace s_htmlNs = "http://www.w3.org/1999/xhtml";

    public static string GetDisplayName(AnimationExchangeFormat format)
    {
        return format switch
        {
            AnimationExchangeFormat.AvaloniaXaml => "Avalonia XAML Animation",
            AnimationExchangeFormat.FlashXfl => "Adobe Animate / Flash XFL",
            AnimationExchangeFormat.SvgSmil => "SVG / SMIL Animation",
            AnimationExchangeFormat.HtmlCss => "HTML / CSS Animation",
            _ => format.ToString()
        };
    }

    public static string GetSuggestedExtension(AnimationExchangeFormat format)
    {
        return format switch
        {
            AnimationExchangeFormat.AvaloniaXaml => "axaml",
            AnimationExchangeFormat.FlashXfl => "xfl",
            AnimationExchangeFormat.SvgSmil => "svg",
            AnimationExchangeFormat.HtmlCss => "html",
            _ => "txt"
        };
    }

    public static AnimationExchangeExportResult Export(TimelineDocument document, AnimationExchangeFormat format)
    {
        var snapshot = DocumentSerializer.Clone(document);
        SceneEditingService.EnsureScenes(snapshot);
        LibraryManagementService.EnsureLibraryMetadata(snapshot);
        var issues = CollectExportIssues(snapshot, format);

        var content = format switch
        {
            AnimationExchangeFormat.AvaloniaXaml => ExportAvaloniaXaml(snapshot),
            AnimationExchangeFormat.FlashXfl => FlashXflExchangeService.Export(snapshot),
            AnimationExchangeFormat.SvgSmil => ExportSvg(snapshot),
            AnimationExchangeFormat.HtmlCss => ExportHtml(snapshot),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        return new AnimationExchangeExportResult
        {
            Format = format,
            Content = content,
            SuggestedFileName = $"{SanitizeFileName(snapshot.Name)}.{GetSuggestedExtension(format)}",
            Summary = $"{GetDisplayName(format)} exported from {GetActiveScene(snapshot).Name}",
            Issues = issues
        };
    }

    public static AnimationExchangeImportResult Import(AnimationExchangeFormat format, string content, string sourceLabel = "Imported Animation")
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Animation content is empty.");
        }

        var issues = new List<AnimationExchangeIssue>();
        var embedded = TryImportEmbeddedDocument(format, content);
        TimelineDocument document;
        if (embedded is not null)
        {
            document = embedded;
            issues.Add(CreateIssue(AnimationExchangeIssueSeverity.Info, "Legacy Metadata", "Legacy embedded TimelineAnimations metadata was detected and used during import."));
        }
        else
        {
            var fallback = format switch
            {
                AnimationExchangeFormat.AvaloniaXaml => ImportAvaloniaXaml(content, sourceLabel),
                AnimationExchangeFormat.FlashXfl => FlashXflExchangeService.Import(content, sourceLabel),
                AnimationExchangeFormat.SvgSmil => ImportSvg(content, sourceLabel),
                AnimationExchangeFormat.HtmlCss => ImportHtml(content, sourceLabel),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
            document = fallback.Document;
            issues.AddRange(fallback.Issues);
        }

        SceneEditingService.EnsureScenes(document);
        LibraryManagementService.EnsureLibraryMetadata(document);
        PublishProfileService.EnsureProfiles(document);

        if (string.IsNullOrWhiteSpace(document.Name))
        {
            document.Name = Path.GetFileNameWithoutExtension(sourceLabel);
        }

        return new AnimationExchangeImportResult
        {
            Format = format,
            Document = document,
            Summary = $"{GetDisplayName(format)} imported as {document.Name}",
            Issues = issues
        };
    }

    private static string ExportAvaloniaXaml(TimelineDocument document)
    {
        var scene = GetActiveScene(document);
        var canvas = new XElement(
            s_avaloniaNs + "Canvas",
            new XAttribute("Width", FormatNumber(scene.CanvasWidth)),
            new XAttribute("Height", FormatNumber(scene.CanvasHeight)),
            new XAttribute("ClipToBounds", "True"));

        canvas.Add(
            new XElement(
                s_avaloniaNs + "Canvas.Background",
                new XElement(
                    s_avaloniaNs + "SolidColorBrush",
                    new XAttribute("Color", scene.TransparentStageBackground ? "#00FFFFFF" : scene.BackgroundFrom))));

        var styles = new List<XElement>();
        foreach (var layer in scene.Layers.OrderBy(layer => layer.ZIndex))
        {
            var samples = BuildLayerSamples(layer, scene);
            var baseSample = samples[0].Snapshot;
            var elementName = GetElementName(layer.Id);
            var element = BuildAvaloniaVisualElement(layer, baseSample, elementName);
            canvas.Add(element);

            if (samples.Count > 1)
            {
                styles.Add(BuildAvaloniaAnimationStyle(element.Name.LocalName, elementName, samples, scene.Duration));
            }
        }

        var root = new XElement(
            s_avaloniaNs + "UserControl",
            new XAttribute(XNamespace.Xmlns + "x", s_xamlNs),
            new XAttribute("Width", FormatNumber(scene.CanvasWidth)),
            new XAttribute("Height", FormatNumber(scene.CanvasHeight)));

        if (styles.Count > 0)
        {
            root.Add(new XElement(s_avaloniaNs + "UserControl.Styles", styles));
        }

        root.Add(canvas);
        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString();
    }

    private static string ExportSvg(TimelineDocument document)
    {
        var scene = GetActiveScene(document);
        var root = new XElement(
            s_svgNs + "svg",
            new XAttribute("width", FormatNumber(scene.CanvasWidth)),
            new XAttribute("height", FormatNumber(scene.CanvasHeight)),
            new XAttribute("viewBox", FormattableString.Invariant($"0 0 {scene.CanvasWidth:0.###} {scene.CanvasHeight:0.###}")));

        root.Add(
            new XElement(
                s_svgNs + "rect",
                new XAttribute("width", FormatNumber(scene.CanvasWidth)),
                new XAttribute("height", FormatNumber(scene.CanvasHeight)),
                new XAttribute("fill", scene.TransparentStageBackground ? "none" : scene.BackgroundFrom)));

        foreach (var layer in scene.Layers.OrderBy(layer => layer.ZIndex))
        {
            var samples = BuildLayerSamples(layer, scene);
            root.Add(BuildSvgLayer(layer, samples, scene.Duration));
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString();
    }

    private static string ExportHtml(TimelineDocument document)
    {
        var scene = GetActiveScene(document);
        var css = new StringBuilder();
        css.AppendLine("html, body { margin: 0; padding: 0; background: #0b111d; }");
        css.AppendLine("body { font-family: Segoe UI, Arial, sans-serif; color: #f4f7fb; }");
        css.AppendLine(".stage { position: relative; overflow: hidden; }");
        css.AppendLine(".layer { position: absolute; box-sizing: border-box; transform-origin: center center; }");
        css.AppendLine(".layer.text { display: flex; align-items: center; justify-content: center; white-space: pre-wrap; }");
        css.AppendLine(".layer.button, .layer.textbox, .layer.checkbox, .layer.toggle, .layer.slider, .layer.progress, .layer.panel, .layer.media { display: flex; align-items: center; justify-content: center; }");
        css.AppendLine(".layer.checkbox, .layer.toggle { justify-content: flex-start; gap: 12px; padding: 0 14px; }");
        css.AppendLine(".layer.slider input, .layer.progress progress { width: calc(100% - 28px); }");
        css.AppendLine(".layer.media { text-transform: uppercase; letter-spacing: 0.08em; font-size: 12px; }");

        var stageBackground = scene.TransparentStageBackground ? "transparent" : scene.BackgroundFrom;
        var stage = new XElement(
            s_htmlNs + "div",
            new XAttribute("class", "stage"),
            new XAttribute("id", "stage"),
            new XAttribute("data-canvas-width", FormatNumber(scene.CanvasWidth)),
            new XAttribute("data-canvas-height", FormatNumber(scene.CanvasHeight)),
            new XAttribute("style", $"width:{FormatNumber(scene.CanvasWidth)}px;height:{FormatNumber(scene.CanvasHeight)}px;background:{stageBackground};"));

        foreach (var layer in scene.Layers.OrderBy(layer => layer.ZIndex))
        {
            var samples = BuildLayerSamples(layer, scene);
            var baseSample = samples[0].Snapshot;
            var elementId = GetElementName(layer.Id);
            var animationName = $"anim_{elementId}";
            css.Append(BuildCssAnimation(animationName, samples));
            stage.Add(BuildHtmlVisualElement(layer, baseSample, elementId, animationName, scene.Duration));
        }

        var html = new XElement(
            s_htmlNs + "html",
            new XElement(
                s_htmlNs + "head",
                new XElement(s_htmlNs + "meta", new XAttribute("charset", "utf-8")),
                new XElement(s_htmlNs + "title", scene.Name),
                new XElement(s_htmlNs + "style", new XCData(css.ToString()))),
            new XElement(s_htmlNs + "body", stage));

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), html).ToString();
    }

    private static TimelineDocument? TryImportEmbeddedDocument(AnimationExchangeFormat format, string content)
    {
        var metadata = format switch
        {
            AnimationExchangeFormat.AvaloniaXaml => ExtractMetadataFromComment(content),
            AnimationExchangeFormat.FlashXfl => null,
            AnimationExchangeFormat.SvgSmil => ExtractMetadataFromSvg(content),
            AnimationExchangeFormat.HtmlCss => ExtractMetadataFromHtml(content),
            _ => null
        };

        if (string.IsNullOrWhiteSpace(metadata))
        {
            return null;
        }

        return DocumentSerializer.FromJson(Encoding.UTF8.GetString(Convert.FromBase64String(metadata)));
    }

    private static AnimationExchangeImportResult ImportAvaloniaXaml(string content, string sourceLabel)
    {
        var issues = new List<AnimationExchangeIssue>();
        var xaml = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
        var (surface, surfaceIssue) = ResolveAvaloniaImportSurface(xaml);
        if (surfaceIssue is not null)
        {
            issues.Add(surfaceIssue);
        }

        var root = xaml.Root!;
        var width = ParseDouble(TryGetAttributeValue(surface, "Width"), ParseDouble(TryGetAttributeValue(root, "Width"), 1280d));
        var height = ParseDouble(TryGetAttributeValue(surface, "Height"), ParseDouble(TryGetAttributeValue(root, "Height"), 720d));
        var scene = new SceneModel
        {
            Name = Path.GetFileNameWithoutExtension(sourceLabel),
            Duration = 6d,
            CanvasWidth = width,
            CanvasHeight = height,
            TransparentStageBackground = ExtractXamlTransparentBackground(surface) || ExtractXamlTransparentBackground(root),
            BackgroundFrom = ExtractXamlSolidBackground(surface) ?? ExtractXamlSolidBackground(root) ?? ExtractXamlBackground(surface, 0) ?? ExtractXamlBackground(root, 0) ?? "#FFFFFF",
            BackgroundTo = ExtractXamlSolidBackground(surface) ?? ExtractXamlSolidBackground(root) ?? ExtractXamlBackground(surface, 0) ?? ExtractXamlBackground(root, 0) ?? "#FFFFFF",
            FrameRate = 24d
        };

        var styles = BuildAvaloniaStyleMap(xaml);
        foreach (var element in surface.Elements().Where(IsImportableAvaloniaElement))
        {
            var layer = BuildLayerFromAvaloniaElement(element, scene.Layers.Count);
            ApplyAvaloniaAnimation(styles, element, layer, scene);
            scene.Layers.Add(layer);
            scene.Duration = Math.Max(scene.Duration, GetLayerDuration(layer, scene.Duration));
        }

        return new AnimationExchangeImportResult
        {
            Format = AnimationExchangeFormat.AvaloniaXaml,
            Document = CreateImportedDocument(scene, sourceLabel),
            Summary = $"{GetDisplayName(AnimationExchangeFormat.AvaloniaXaml)} imported as {Path.GetFileNameWithoutExtension(sourceLabel)}",
            Issues = issues
        };
    }

    private static AnimationExchangeImportResult ImportSvg(string content, string sourceLabel)
    {
        var issues = new List<AnimationExchangeIssue>
        {
            CreateIssue(AnimationExchangeIssueSeverity.Info, "SVG", "SVG fallback import reconstructs motion from sampled transforms and placeholders for media/control internals.")
        };
        var svg = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
        var root = svg.Root ?? throw new InvalidOperationException("Invalid SVG animation file.");
        var width = ParseSvgLength(TryGetAttributeValue(root, "width"), 1280d);
        var height = ParseSvgLength(TryGetAttributeValue(root, "height"), 720d);
        var scene = new SceneModel
        {
            Name = Path.GetFileNameWithoutExtension(sourceLabel),
            Duration = 6d,
            CanvasWidth = width,
            CanvasHeight = height,
            BackgroundFrom = "#FFFFFF",
            BackgroundTo = "#FFFFFF",
            FrameRate = 24d
        };

        var backgroundRect = root.Elements().FirstOrDefault(element =>
            element.Name.LocalName == "rect" &&
            string.Equals(TryGetAttributeValue(element, "width"), FormatNumber(width), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(TryGetAttributeValue(element, "height"), FormatNumber(height), StringComparison.OrdinalIgnoreCase));
        if (backgroundRect is not null)
        {
            var fill = TryGetAttributeValue(backgroundRect, "fill");
            if (string.Equals(fill, "none", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fill, "transparent", StringComparison.OrdinalIgnoreCase))
            {
                scene.TransparentStageBackground = true;
            }
            else if (!string.IsNullOrWhiteSpace(fill))
            {
                scene.BackgroundFrom = fill;
                scene.BackgroundTo = fill;
            }
        }

        foreach (var element in root.Elements().Where(IsImportableSvgElement))
        {
            if (ReferenceEquals(element, backgroundRect))
            {
                continue;
            }

            var layer = BuildLayerFromSvgElement(element, scene.Layers.Count);
            scene.Duration = Math.Max(scene.Duration, ApplySvgAnimation(element, layer, scene.Duration));
            scene.Layers.Add(layer);
        }

        return new AnimationExchangeImportResult
        {
            Format = AnimationExchangeFormat.SvgSmil,
            Document = CreateImportedDocument(scene, sourceLabel),
            Summary = $"{GetDisplayName(AnimationExchangeFormat.SvgSmil)} imported as {Path.GetFileNameWithoutExtension(sourceLabel)}",
            Issues = issues
        };
    }

    private static AnimationExchangeImportResult ImportHtml(string content, string sourceLabel)
    {
        var issues = new List<AnimationExchangeIssue>
        {
            CreateIssue(AnimationExchangeIssueSeverity.Info, "HTML/CSS", "HTML fallback import reconstructs motion from CSS keyframes and preserves control hints when available.")
        };
        var html = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
        var css = string.Join(Environment.NewLine, html.Descendants().Where(element => element.Name.LocalName == "style").Select(element => element.Value));
        var cssAnimations = ParseCssAnimations(css);
        var stage = html.Descendants().FirstOrDefault(element => string.Equals(TryGetAttributeValue(element, "id"), "stage", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("No stage element found in the HTML animation file.");

        var width = ParseDouble(TryGetAttributeValue(stage, "data-canvas-width"), 1280d);
        var height = ParseDouble(TryGetAttributeValue(stage, "data-canvas-height"), 720d);
        var stageStyle = ParseInlineStyle(TryGetAttributeValue(stage, "style"));
        var cssBackground = ExtractCssBackgroundColor(stageStyle);
        var scene = new SceneModel
        {
            Name = Path.GetFileNameWithoutExtension(sourceLabel),
            Duration = 6d,
            CanvasWidth = width,
            CanvasHeight = height,
            TransparentStageBackground = string.Equals(cssBackground, "transparent", StringComparison.OrdinalIgnoreCase),
            BackgroundFrom = string.Equals(cssBackground, "transparent", StringComparison.OrdinalIgnoreCase)
                ? "#FFFFFF"
                : cssBackground ?? ExtractCssGradientColor(stageStyle, 0) ?? "#FFFFFF",
            BackgroundTo = string.Equals(cssBackground, "transparent", StringComparison.OrdinalIgnoreCase)
                ? "#FFFFFF"
                : cssBackground ?? ExtractCssGradientColor(stageStyle, 0) ?? "#FFFFFF",
            FrameRate = 24d
        };

        foreach (var element in stage.Elements().Where(IsImportableHtmlElement))
        {
            var layer = BuildLayerFromHtmlElement(element, scene.Layers.Count);
            var elementStyle = ParseInlineStyle(TryGetAttributeValue(element, "style"));
            var animationName = ResolveHtmlAnimationName(elementStyle);
            var animationDuration = GetCssAnimationDuration(elementStyle);
            if (!string.IsNullOrWhiteSpace(animationName) && cssAnimations.TryGetValue(animationName, out var frames))
            {
                ApplyCssAnimation(layer, frames, animationDuration);
                scene.Duration = Math.Max(scene.Duration, animationDuration);
            }

            scene.Layers.Add(layer);
        }

        return new AnimationExchangeImportResult
        {
            Format = AnimationExchangeFormat.HtmlCss,
            Document = CreateImportedDocument(scene, sourceLabel),
            Summary = $"{GetDisplayName(AnimationExchangeFormat.HtmlCss)} imported as {Path.GetFileNameWithoutExtension(sourceLabel)}",
            Issues = issues
        };
    }

    private static XElement BuildAvaloniaVisualElement(TimelineLayer layer, LayerSnapshot snapshot, string elementName)
    {
        var element = layer.Kind switch
        {
            LayerKind.Rectangle => new XElement(
                s_avaloniaNs + "Border",
                new XAttribute("Background", ResolveFill(snapshot)),
                new XAttribute("BorderBrush", snapshot.Stroke),
                new XAttribute("BorderThickness", FormatNumber(snapshot.StrokeThickness)),
                new XAttribute("CornerRadius", FormatNumber(snapshot.CornerRadius))),
            LayerKind.Ellipse => new XElement(
                s_avaloniaNs + "Ellipse",
                new XAttribute("Fill", ResolveFill(snapshot)),
                new XAttribute("Stroke", snapshot.Stroke),
                new XAttribute("StrokeThickness", FormatNumber(snapshot.StrokeThickness))),
            LayerKind.Text => new XElement(
                s_avaloniaNs + "TextBlock",
                new XAttribute("Text", snapshot.Text),
                new XAttribute("Foreground", snapshot.Fill),
                new XAttribute("FontSize", FormatNumber(snapshot.FontSize)),
                new XAttribute("TextAlignment", "Center"),
                new XAttribute("VerticalAlignment", "Center"),
                new XAttribute("HorizontalAlignment", "Center")),
            LayerKind.Path => new XElement(
                s_avaloniaNs + "Path",
                new XAttribute("Data", BuildAvaloniaPathData(snapshot)),
                new XAttribute("Fill", snapshot.IsClosed ? ResolveFill(snapshot) : "Transparent"),
                new XAttribute("Stroke", snapshot.Stroke),
                new XAttribute("StrokeThickness", FormatNumber(snapshot.StrokeThickness)),
                new XAttribute("Stretch", "Fill")),
            LayerKind.AvaloniaControl => BuildAvaloniaControlElement(snapshot),
            LayerKind.Video or LayerKind.Audio => BuildAvaloniaMediaElement(layer, snapshot),
            _ => new XElement(s_avaloniaNs + "Border")
        };

        element.SetAttributeValue(s_xamlNs + "Name", elementName);
        element.SetAttributeValue("Canvas.Left", FormatNumber(snapshot.X));
        element.SetAttributeValue("Canvas.Top", FormatNumber(snapshot.Y));
        element.SetAttributeValue("Width", FormatNumber(snapshot.Width));
        element.SetAttributeValue("Height", FormatNumber(snapshot.Height));
        element.SetAttributeValue("Opacity", FormatNumber(snapshot.Opacity));
        element.SetAttributeValue("IsVisible", layer.IsVisible ? "True" : "False");
        element.SetAttributeValue("RenderTransformOrigin", "0.5,0.5");
        element.SetAttributeValue("Tag", layer.Kind == LayerKind.AvaloniaControl
            ? $"control:{snapshot.AvaloniaControl.Kind}"
            : layer.Kind.ToString());

        if (Math.Abs(snapshot.Rotation) > 0.0001d || layer.Tracks.Any(track => track.Property == AnimatedProperty.Rotation && track.Keyframes.Count > 0))
        {
            element.Add(
                new XElement(
                    s_avaloniaNs + $"{element.Name.LocalName}.RenderTransform",
                    new XElement(s_avaloniaNs + "RotateTransform", new XAttribute("Angle", FormatNumber(snapshot.Rotation)))));
        }

        return element;
    }

    private static XElement BuildAvaloniaAnimationStyle(string targetTypeName, string elementName, IReadOnlyList<LayerFrameSample> samples, double duration)
    {
        var animation = new XElement(
            s_avaloniaNs + "Animation",
            new XAttribute("Duration", TimeSpan.FromSeconds(Math.Max(0.05d, duration)).ToString("c", CultureInfo.InvariantCulture)),
            new XAttribute("FillMode", "Forward"),
            new XAttribute("IterationCount", "1"));

        foreach (var sample in samples)
        {
            animation.Add(
                new XElement(
                    s_avaloniaNs + "KeyFrame",
                    new XAttribute("Cue", FormattableString.Invariant($"{sample.Cue * 100d:0.###}%")),
                    new XElement(s_avaloniaNs + "Setter", new XAttribute("Property", "Canvas.Left"), new XAttribute("Value", FormatNumber(sample.Snapshot.X))),
                    new XElement(s_avaloniaNs + "Setter", new XAttribute("Property", "Canvas.Top"), new XAttribute("Value", FormatNumber(sample.Snapshot.Y))),
                    new XElement(s_avaloniaNs + "Setter", new XAttribute("Property", "Width"), new XAttribute("Value", FormatNumber(sample.Snapshot.Width))),
                    new XElement(s_avaloniaNs + "Setter", new XAttribute("Property", "Height"), new XAttribute("Value", FormatNumber(sample.Snapshot.Height))),
                    new XElement(s_avaloniaNs + "Setter", new XAttribute("Property", "Opacity"), new XAttribute("Value", FormatNumber(sample.IsVisible ? sample.Snapshot.Opacity : 0d))),
                    new XElement(s_avaloniaNs + "Setter", new XAttribute("Property", "RotateTransform.Angle"), new XAttribute("Value", FormatNumber(sample.Snapshot.Rotation)))));
        }

        return new XElement(
            s_avaloniaNs + "Style",
            new XAttribute("Selector", $"{targetTypeName}#{elementName}"),
            new XElement(
                s_avaloniaNs + "Style.Animations",
                animation));
    }

    private static XElement BuildSvgLayer(TimelineLayer layer, IReadOnlyList<LayerFrameSample> samples, double duration)
    {
        var baseSample = samples[0].Snapshot;
        var group = new XElement(
            s_svgNs + "g",
            new XAttribute("id", GetElementName(layer.Id)),
            new XAttribute("data-layer-kind", layer.Kind.ToString()),
            new XAttribute("data-base-width", FormatNumber(baseSample.Width)),
            new XAttribute("data-base-height", FormatNumber(baseSample.Height)),
            new XAttribute("visibility", layer.IsVisible ? "visible" : "hidden"));

        if (layer.Kind == LayerKind.AvaloniaControl)
        {
            group.SetAttributeValue("data-control-kind", baseSample.AvaloniaControl.Kind.ToString());
        }

        group.Add(BuildSvgVisual(layer, baseSample));

        if (samples.Count > 1)
        {
            group.Add(
                new XElement(
                    s_svgNs + "animateTransform",
                    new XAttribute("attributeName", "transform"),
                    new XAttribute("type", "translate"),
                    new XAttribute("dur", $"{Math.Max(0.05d, duration):0.###}s"),
                    new XAttribute("fill", "freeze"),
                    new XAttribute("calcMode", "linear"),
                    new XAttribute("additive", "sum"),
                    new XAttribute("keyTimes", BuildKeyTimes(samples)),
                    new XAttribute("values", string.Join(";", samples.Select(sample => $"{FormatNumber(sample.Snapshot.X)} {FormatNumber(sample.Snapshot.Y)}")))));
            group.Add(
                new XElement(
                    s_svgNs + "animateTransform",
                    new XAttribute("attributeName", "transform"),
                    new XAttribute("type", "scale"),
                    new XAttribute("dur", $"{Math.Max(0.05d, duration):0.###}s"),
                    new XAttribute("fill", "freeze"),
                    new XAttribute("calcMode", "linear"),
                    new XAttribute("additive", "sum"),
                    new XAttribute("keyTimes", BuildKeyTimes(samples)),
                    new XAttribute("values", string.Join(";", samples.Select(sample => $"{FormatNumber(sample.Snapshot.Width / Math.Max(1d, baseSample.Width))} {FormatNumber(sample.Snapshot.Height / Math.Max(1d, baseSample.Height))}")))));
            group.Add(
                new XElement(
                    s_svgNs + "animateTransform",
                    new XAttribute("attributeName", "transform"),
                    new XAttribute("type", "rotate"),
                    new XAttribute("dur", $"{Math.Max(0.05d, duration):0.###}s"),
                    new XAttribute("fill", "freeze"),
                    new XAttribute("calcMode", "linear"),
                    new XAttribute("additive", "sum"),
                    new XAttribute("keyTimes", BuildKeyTimes(samples)),
                    new XAttribute("values", string.Join(";", samples.Select(sample => $"{FormatNumber(sample.Snapshot.Rotation)} {FormatNumber(baseSample.Width / 2d)} {FormatNumber(baseSample.Height / 2d)}")))));
            group.Add(
                new XElement(
                    s_svgNs + "animate",
                    new XAttribute("attributeName", "opacity"),
                    new XAttribute("dur", $"{Math.Max(0.05d, duration):0.###}s"),
                    new XAttribute("fill", "freeze"),
                    new XAttribute("calcMode", "linear"),
                    new XAttribute("keyTimes", BuildKeyTimes(samples)),
                    new XAttribute("values", string.Join(";", samples.Select(sample => FormatNumber(sample.IsVisible ? sample.Snapshot.Opacity : 0d))))));
        }
        else
        {
            group.SetAttributeValue("transform", $"translate({FormatNumber(baseSample.X)} {FormatNumber(baseSample.Y)})");
            group.SetAttributeValue("opacity", FormatNumber(baseSample.Opacity));
        }

        return group;
    }

    private static XElement BuildHtmlVisualElement(TimelineLayer layer, LayerSnapshot snapshot, string elementId, string animationName, double duration)
    {
        var style = BuildHtmlStyle(snapshot, animationName, layer.IsVisible, duration);
        var cssClass = layer.Kind switch
        {
            LayerKind.Rectangle => "layer rectangle",
            LayerKind.Ellipse => "layer ellipse",
            LayerKind.Text => "layer text",
            LayerKind.Path => "layer path",
            LayerKind.AvaloniaControl => $"layer {snapshot.AvaloniaControl.Kind.ToString().ToLowerInvariant()}",
            LayerKind.Video or LayerKind.Audio => "layer media",
            _ => "layer"
        };

        return layer.Kind switch
        {
            LayerKind.Rectangle or LayerKind.Ellipse => new XElement(
                s_htmlNs + "div",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", layer.Kind.ToString()),
                new XAttribute("style", $"{style};background:{ResolveFill(snapshot)};border:{FormatNumber(snapshot.StrokeThickness)}px solid {snapshot.Stroke};border-radius:{FormatNumber(layer.Kind == LayerKind.Ellipse ? 999 : snapshot.CornerRadius)}px;")),
            LayerKind.Text => new XElement(
                s_htmlNs + "div",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", layer.Kind.ToString()),
                new XAttribute("style", $"{style};color:{snapshot.Fill};font-size:{FormatNumber(snapshot.FontSize)}px;"),
                snapshot.Text),
            LayerKind.Path => BuildHtmlPathElement(layer, snapshot, elementId, style, cssClass),
            LayerKind.AvaloniaControl => BuildHtmlControlElement(snapshot, elementId, style, cssClass),
            LayerKind.Video or LayerKind.Audio => new XElement(
                s_htmlNs + "div",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", layer.Kind.ToString()),
                new XAttribute("style", $"{style};background:{ResolveFill(snapshot)};border:1px solid {snapshot.Stroke};border-radius:{FormatNumber(snapshot.CornerRadius)}px;"),
                layer.Kind == LayerKind.Video ? "Video Clip" : "Audio Clip"),
            _ => new XElement(s_htmlNs + "div", new XAttribute("id", elementId), new XAttribute("class", cssClass), new XAttribute("style", style))
        };
    }

    private static XElement BuildSvgVisual(TimelineLayer layer, LayerSnapshot snapshot)
    {
        return layer.Kind switch
        {
            LayerKind.Rectangle => new XElement(
                s_svgNs + "rect",
                new XAttribute("width", FormatNumber(snapshot.Width)),
                new XAttribute("height", FormatNumber(snapshot.Height)),
                new XAttribute("rx", FormatNumber(snapshot.CornerRadius)),
                new XAttribute("fill", ResolveFill(snapshot)),
                new XAttribute("stroke", snapshot.Stroke),
                new XAttribute("stroke-width", FormatNumber(snapshot.StrokeThickness))),
            LayerKind.Ellipse => new XElement(
                s_svgNs + "ellipse",
                new XAttribute("cx", FormatNumber(snapshot.Width / 2d)),
                new XAttribute("cy", FormatNumber(snapshot.Height / 2d)),
                new XAttribute("rx", FormatNumber(snapshot.Width / 2d)),
                new XAttribute("ry", FormatNumber(snapshot.Height / 2d)),
                new XAttribute("fill", ResolveFill(snapshot)),
                new XAttribute("stroke", snapshot.Stroke),
                new XAttribute("stroke-width", FormatNumber(snapshot.StrokeThickness))),
            LayerKind.Text => new XElement(
                s_svgNs + "text",
                new XAttribute("x", "0"),
                new XAttribute("y", FormatNumber(Math.Max(snapshot.FontSize, snapshot.Height / 2d))),
                new XAttribute("fill", snapshot.Fill),
                new XAttribute("font-size", FormatNumber(snapshot.FontSize)),
                snapshot.Text),
            LayerKind.Path => new XElement(
                s_svgNs + "path",
                new XAttribute("d", BuildSvgPathData(snapshot)),
                new XAttribute("fill", snapshot.IsClosed ? ResolveFill(snapshot) : "transparent"),
                new XAttribute("stroke", snapshot.Stroke),
                new XAttribute("stroke-width", FormatNumber(snapshot.StrokeThickness))),
            LayerKind.AvaloniaControl => BuildSvgControlElement(snapshot),
            LayerKind.Video or LayerKind.Audio => new XElement(
                s_svgNs + "g",
                new XElement(
                    s_svgNs + "rect",
                    new XAttribute("width", FormatNumber(snapshot.Width)),
                    new XAttribute("height", FormatNumber(snapshot.Height)),
                    new XAttribute("rx", FormatNumber(snapshot.CornerRadius)),
                    new XAttribute("fill", ResolveFill(snapshot)),
                    new XAttribute("stroke", snapshot.Stroke)),
                new XElement(
                    s_svgNs + "text",
                    new XAttribute("x", FormatNumber(snapshot.Width / 2d)),
                    new XAttribute("y", FormatNumber((snapshot.Height / 2d) + 4d)),
                    new XAttribute("fill", "#FFFFFF"),
                    new XAttribute("font-size", "14"),
                    new XAttribute("text-anchor", "middle"),
                    layer.Kind == LayerKind.Video ? "VIDEO" : "AUDIO")),
            _ => new XElement(s_svgNs + "g")
        };
    }

    private static XElement BuildAvaloniaControlElement(LayerSnapshot snapshot)
    {
        var settings = snapshot.AvaloniaControl;
        return settings.Kind switch
        {
            AvaloniaControlKind.Button => new XElement(
                s_avaloniaNs + "Button",
                new XAttribute("Content", settings.Content),
                new XAttribute("Background", ResolveFill(snapshot)),
                new XAttribute("Foreground", snapshot.Stroke)),
            AvaloniaControlKind.TextBlock => new XElement(
                s_avaloniaNs + "TextBlock",
                new XAttribute("Text", settings.Content),
                new XAttribute("Foreground", ResolveFill(snapshot)),
                new XAttribute("FontSize", FormatNumber(snapshot.FontSize))),
            AvaloniaControlKind.TextBox => new XElement(
                s_avaloniaNs + "TextBox",
                new XAttribute("Text", settings.Content),
                new XAttribute("Watermark", settings.SecondaryContent)),
            AvaloniaControlKind.CheckBox => new XElement(
                s_avaloniaNs + "CheckBox",
                new XAttribute("Content", settings.Content),
                new XAttribute("IsChecked", settings.IsChecked ? "True" : "False")),
            AvaloniaControlKind.ToggleButton => new XElement(
                s_avaloniaNs + "ToggleButton",
                new XAttribute("Content", settings.Content),
                new XAttribute("IsChecked", settings.IsChecked ? "True" : "False")),
            AvaloniaControlKind.Slider => new XElement(
                s_avaloniaNs + "Slider",
                new XAttribute("Minimum", FormatNumber(settings.Minimum)),
                new XAttribute("Maximum", FormatNumber(settings.Maximum)),
                new XAttribute("Value", FormatNumber(settings.Value))),
            AvaloniaControlKind.ProgressBar => new XElement(
                s_avaloniaNs + "ProgressBar",
                new XAttribute("Minimum", FormatNumber(settings.Minimum)),
                new XAttribute("Maximum", FormatNumber(settings.Maximum)),
                new XAttribute("Value", FormatNumber(settings.Value)),
                new XAttribute("IsIndeterminate", settings.IsIndeterminate ? "True" : "False")),
            AvaloniaControlKind.ComboBox => new XElement(
                s_avaloniaNs + "ComboBox",
                new XAttribute("SelectedIndex", "0"),
                SplitControlItems(settings.SecondaryContent, "Option A", "Option B", "Option C")
                    .Select(item => new XElement(s_avaloniaNs + "ComboBoxItem", new XAttribute("Content", item)))),
            AvaloniaControlKind.ListBox => new XElement(
                s_avaloniaNs + "ListBox",
                SplitControlItems(settings.Content, "Item 1", "Item 2", "Item 3")
                    .Select(item => new XElement(s_avaloniaNs + "ListBoxItem", new XAttribute("Content", item)))),
            AvaloniaControlKind.TabControl => new XElement(
                s_avaloniaNs + "TabControl",
                SplitControlItems(settings.Content, "Overview", "Settings", "Export")
                    .Select(item => new XElement(
                        s_avaloniaNs + "TabItem",
                        new XAttribute("Header", item),
                        new XElement(s_avaloniaNs + "Border", new XAttribute("Padding", "12"), new XElement(s_avaloniaNs + "TextBlock", new XAttribute("Text", $"{item} content")))))),
            AvaloniaControlKind.Grid => BuildAvaloniaLayoutElement(snapshot, settings, "Grid"),
            AvaloniaControlKind.StackPanel => BuildAvaloniaLayoutElement(snapshot, settings, "StackPanel"),
            AvaloniaControlKind.PathIcon => new XElement(
                s_avaloniaNs + "PathIcon",
                new XAttribute("Foreground", ResolveFill(snapshot)),
                new XAttribute("Data", string.IsNullOrWhiteSpace(settings.SecondaryContent) ? "M 12 2 L 22 22 L 2 22 Z" : settings.SecondaryContent)),
            AvaloniaControlKind.Image => new XElement(
                s_avaloniaNs + "Border",
                new XAttribute("Background", ResolveFill(snapshot)),
                new XAttribute("BorderBrush", snapshot.Stroke),
                new XAttribute("BorderThickness", "1"),
                new XElement(s_avaloniaNs + "TextBlock", new XAttribute("Text", string.IsNullOrWhiteSpace(settings.Source) ? "Image" : settings.Source), new XAttribute("HorizontalAlignment", "Center"), new XAttribute("VerticalAlignment", "Center"))),
            AvaloniaControlKind.Panel => new XElement(
                s_avaloniaNs + "Border",
                new XAttribute("Background", ResolveFill(snapshot)),
                new XAttribute("BorderBrush", snapshot.Stroke),
                new XAttribute("BorderThickness", "1")),
            _ => new XElement(
                s_avaloniaNs + "Border",
                new XAttribute("Background", ResolveFill(snapshot)),
                new XAttribute("BorderBrush", snapshot.Stroke),
                new XAttribute("BorderThickness", "1"))
        };
    }

    private static XElement BuildAvaloniaLayoutElement(LayerSnapshot snapshot, AvaloniaControlSettings settings, string elementName)
    {
        var items = SplitControlItems(settings.SecondaryContent, "Item A", "Item B", "Item C", "Item D");
        if (string.Equals(elementName, "Grid", StringComparison.Ordinal))
        {
            return new XElement(
                s_avaloniaNs + "Grid",
                new XAttribute("Background", ResolveFill(snapshot)),
                new XAttribute("RowDefinitions", "*,*"),
                new XAttribute("ColumnDefinitions", "*,*"),
                Enumerable.Range(0, 4).Select(index => new XElement(
                    s_avaloniaNs + "Border",
                    new XAttribute("Grid.Row", index / 2),
                    new XAttribute("Grid.Column", index % 2),
                    new XAttribute("Margin", "4"),
                    new XAttribute("CornerRadius", "8"),
                    new XAttribute("Background", "#22FFFFFF"),
                    new XElement(
                        s_avaloniaNs + "TextBlock",
                        new XAttribute("Text", items.ElementAtOrDefault(index) ?? $"Cell {index + 1}"),
                        new XAttribute("HorizontalAlignment", "Center"),
                        new XAttribute("VerticalAlignment", "Center")))));
        }

        return new XElement(
            s_avaloniaNs + elementName,
            new XAttribute("Background", ResolveFill(snapshot)),
            new XAttribute("Orientation", "Vertical"),
            new XAttribute("Spacing", "8"),
            SplitControlItems(settings.SecondaryContent, "Item A", "Item B", "Item C").Select(item => new XElement(
                s_avaloniaNs + "Border",
                new XAttribute("Background", "#22FFFFFF"),
                new XAttribute("CornerRadius", "8"),
                new XAttribute("Padding", "10,8"),
                new XElement(s_avaloniaNs + "TextBlock", new XAttribute("Text", item)))));
    }

    private static XElement BuildAvaloniaMediaElement(TimelineLayer layer, LayerSnapshot snapshot)
    {
        return new XElement(
            s_avaloniaNs + "Border",
            new XAttribute("Background", ResolveFill(snapshot)),
            new XAttribute("BorderBrush", snapshot.Stroke),
            new XAttribute("BorderThickness", "1"),
            new XAttribute("CornerRadius", FormatNumber(snapshot.CornerRadius)),
            new XElement(
                s_avaloniaNs + "TextBlock",
                new XAttribute("Text", layer.Kind == LayerKind.Video ? "Video Clip" : "Audio Clip"),
                new XAttribute("HorizontalAlignment", "Center"),
                new XAttribute("VerticalAlignment", "Center"),
                new XAttribute("Foreground", "#FFFFFF")));
    }

    private static XElement BuildSvgControlElement(LayerSnapshot snapshot)
    {
        var settings = snapshot.AvaloniaControl;
        var label = string.IsNullOrWhiteSpace(settings.Content) ? settings.Kind.ToString() : settings.Content;
        return new XElement(
            s_svgNs + "g",
            new XAttribute("data-control-kind", settings.Kind.ToString()),
            new XElement(
                s_svgNs + "rect",
                new XAttribute("width", FormatNumber(snapshot.Width)),
                new XAttribute("height", FormatNumber(snapshot.Height)),
                new XAttribute("rx", FormatNumber(snapshot.CornerRadius)),
                new XAttribute("fill", ResolveFill(snapshot)),
                new XAttribute("stroke", snapshot.Stroke),
                new XAttribute("stroke-width", "1.4")),
            new XElement(
                s_svgNs + "text",
                new XAttribute("x", FormatNumber(snapshot.Width / 2d)),
                new XAttribute("y", FormatNumber((snapshot.Height / 2d) + 4d)),
                new XAttribute("fill", "#FFFFFF"),
                new XAttribute("font-size", "14"),
                new XAttribute("text-anchor", "middle"),
                label));
    }

    private static XElement BuildHtmlPathElement(TimelineLayer layer, LayerSnapshot snapshot, string elementId, string style, string cssClass)
    {
        var svg = new XElement(
            s_svgNs + "svg",
            new XAttribute("xmlns", s_svgNs),
            new XAttribute("viewBox", FormattableString.Invariant($"0 0 {snapshot.Width:0.###} {snapshot.Height:0.###}")),
            new XAttribute("width", FormatNumber(snapshot.Width)),
            new XAttribute("height", FormatNumber(snapshot.Height)),
            new XElement(
                s_svgNs + "path",
                new XAttribute("d", BuildSvgPathData(snapshot)),
                new XAttribute("fill", snapshot.IsClosed ? ResolveFill(snapshot) : "transparent"),
                new XAttribute("stroke", snapshot.Stroke),
                new XAttribute("stroke-width", FormatNumber(snapshot.StrokeThickness))));

        return new XElement(
            s_htmlNs + "div",
            new XAttribute("id", elementId),
            new XAttribute("class", cssClass),
            new XAttribute("data-layer-kind", layer.Kind.ToString()),
            new XAttribute("style", style),
            svg);
    }

    private static XElement BuildHtmlControlElement(LayerSnapshot snapshot, string elementId, string style, string cssClass)
    {
        var settings = snapshot.AvaloniaControl;
        var rootStyle = $"{style};background:{ResolveFill(snapshot)};border:1px solid {snapshot.Stroke};border-radius:{FormatNumber(snapshot.CornerRadius)}px;";
        return settings.Kind switch
        {
            AvaloniaControlKind.Button => new XElement(
                s_htmlNs + "button",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", LayerKind.AvaloniaControl.ToString()),
                new XAttribute("data-control-kind", settings.Kind.ToString()),
                new XAttribute("style", rootStyle),
                settings.Content),
            AvaloniaControlKind.TextBlock => new XElement(
                s_htmlNs + "div",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", LayerKind.AvaloniaControl.ToString()),
                new XAttribute("data-control-kind", settings.Kind.ToString()),
                new XAttribute("style", $"{style};color:{ResolveFill(snapshot)};font-size:{FormatNumber(snapshot.FontSize)}px;"),
                settings.Content),
            AvaloniaControlKind.TextBox => new XElement(
                s_htmlNs + "input",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("type", "text"),
                new XAttribute("value", settings.Content),
                new XAttribute("placeholder", settings.SecondaryContent),
                new XAttribute("data-layer-kind", LayerKind.AvaloniaControl.ToString()),
                new XAttribute("data-control-kind", settings.Kind.ToString()),
                new XAttribute("style", rootStyle)),
            AvaloniaControlKind.CheckBox or AvaloniaControlKind.ToggleButton => new XElement(
                s_htmlNs + "label",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", LayerKind.AvaloniaControl.ToString()),
                new XAttribute("data-control-kind", settings.Kind.ToString()),
                new XAttribute("data-checked", settings.IsChecked ? "true" : "false"),
                new XAttribute("style", rootStyle),
                new XElement(s_htmlNs + "input", new XAttribute("type", "checkbox"), settings.IsChecked ? new XAttribute("checked", "checked") : null),
                new XElement(s_htmlNs + "span", settings.Content)),
            AvaloniaControlKind.Slider => new XElement(
                s_htmlNs + "div",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", LayerKind.AvaloniaControl.ToString()),
                new XAttribute("data-control-kind", settings.Kind.ToString()),
                new XAttribute("style", rootStyle),
                new XElement(
                    s_htmlNs + "input",
                    new XAttribute("type", "range"),
                    new XAttribute("min", FormatNumber(settings.Minimum)),
                    new XAttribute("max", FormatNumber(settings.Maximum)),
                    new XAttribute("value", FormatNumber(settings.Value)))),
            AvaloniaControlKind.ProgressBar => new XElement(
                s_htmlNs + "div",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", LayerKind.AvaloniaControl.ToString()),
                new XAttribute("data-control-kind", settings.Kind.ToString()),
                new XAttribute("data-indeterminate", settings.IsIndeterminate ? "true" : "false"),
                new XAttribute("style", rootStyle),
                new XElement(
                    s_htmlNs + "progress",
                    new XAttribute("max", FormatNumber(settings.Maximum)),
                    new XAttribute("value", FormatNumber(settings.Value)))),
            AvaloniaControlKind.ComboBox => new XElement(
                s_htmlNs + "select",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", LayerKind.AvaloniaControl.ToString()),
                new XAttribute("data-control-kind", settings.Kind.ToString()),
                new XAttribute("style", rootStyle),
                SplitControlItems(settings.SecondaryContent, "Option A", "Option B", "Option C")
                    .Select((item, index) => new XElement(
                        s_htmlNs + "option",
                        index == 0 ? new XAttribute("selected", "selected") : null,
                        item))),
            AvaloniaControlKind.ListBox => new XElement(
                s_htmlNs + "ul",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", LayerKind.AvaloniaControl.ToString()),
                new XAttribute("data-control-kind", settings.Kind.ToString()),
                new XAttribute("style", $"{rootStyle};margin:0;padding:10px 18px;list-style:none;display:block;"),
                SplitControlItems(settings.Content, "Item 1", "Item 2", "Item 3")
                    .Select(item => new XElement(s_htmlNs + "li", item))),
            AvaloniaControlKind.TabControl => new XElement(
                s_htmlNs + "div",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", LayerKind.AvaloniaControl.ToString()),
                new XAttribute("data-control-kind", settings.Kind.ToString()),
                new XAttribute("style", $"{rootStyle};display:block;"),
                new XElement(
                    s_htmlNs + "div",
                    new XAttribute("style", "display:flex;gap:6px;padding:8px 8px 0 8px;"),
                    SplitControlItems(settings.Content, "Overview", "Settings", "Export")
                        .Select((item, index) => new XElement(
                            s_htmlNs + "span",
                            new XAttribute("style", $"padding:6px 10px;border-radius:8px;background:{(index == 0 ? "#3B516E" : "#263548")};"),
                            item))),
                new XElement(
                    s_htmlNs + "div",
                    new XAttribute("style", "padding:12px;"),
                    $"{(string.IsNullOrWhiteSpace(settings.SecondaryContent) ? "Overview" : settings.SecondaryContent)} content")),
            AvaloniaControlKind.Grid => new XElement(
                s_htmlNs + "div",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", LayerKind.AvaloniaControl.ToString()),
                new XAttribute("data-control-kind", settings.Kind.ToString()),
                new XAttribute("style", $"{rootStyle};display:grid;grid-template-columns:1fr 1fr;grid-template-rows:1fr 1fr;gap:8px;padding:8px;"),
                Enumerable.Range(1, 4).Select(index => new XElement(s_htmlNs + "div", new XAttribute("style", "background:rgba(255,255,255,0.08);border-radius:8px;"), $"Cell {index}"))),
            AvaloniaControlKind.StackPanel => new XElement(
                s_htmlNs + "div",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", LayerKind.AvaloniaControl.ToString()),
                new XAttribute("data-control-kind", settings.Kind.ToString()),
                new XAttribute("style", $"{rootStyle};display:flex;flex-direction:column;gap:8px;padding:8px;"),
                SplitControlItems(settings.SecondaryContent, "Item A", "Item B", "Item C")
                    .Select(item => new XElement(s_htmlNs + "div", new XAttribute("style", "background:rgba(255,255,255,0.08);border-radius:8px;padding:6px 8px;"), item))),
            AvaloniaControlKind.PathIcon => new XElement(
                s_htmlNs + "div",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", LayerKind.AvaloniaControl.ToString()),
                new XAttribute("data-control-kind", settings.Kind.ToString()),
                new XAttribute("data-path", string.IsNullOrWhiteSpace(settings.SecondaryContent) ? "M 12 2 L 22 22 L 2 22 Z" : settings.SecondaryContent),
                new XAttribute("style", $"{rootStyle};display:flex;align-items:center;justify-content:center;"),
                new XElement(
                    s_svgNs + "svg",
                    new XAttribute("viewBox", "0 0 24 24"),
                    new XAttribute("width", "70%"),
                    new XAttribute("height", "70%"),
                    new XElement(
                        s_svgNs + "path",
                        new XAttribute("d", string.IsNullOrWhiteSpace(settings.SecondaryContent) ? "M 12 2 L 22 22 L 2 22 Z" : settings.SecondaryContent),
                        new XAttribute("fill", ResolveFill(snapshot))))),
            AvaloniaControlKind.Image => new XElement(
                s_htmlNs + "img",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("src", string.IsNullOrWhiteSpace(settings.Source) ? "about:blank" : settings.Source),
                new XAttribute("alt", settings.Content),
                new XAttribute("data-layer-kind", LayerKind.AvaloniaControl.ToString()),
                new XAttribute("data-control-kind", settings.Kind.ToString()),
                new XAttribute("style", rootStyle)),
            _ => new XElement(
                s_htmlNs + "div",
                new XAttribute("id", elementId),
                new XAttribute("class", cssClass),
                new XAttribute("data-layer-kind", LayerKind.AvaloniaControl.ToString()),
                new XAttribute("data-control-kind", settings.Kind.ToString()),
                new XAttribute("style", rootStyle),
                settings.Content)
        };
    }

    private static Dictionary<string, AvaloniaStyleAnimation> BuildAvaloniaStyleMap(XDocument xaml)
    {
        var styles = new Dictionary<string, AvaloniaStyleAnimation>(StringComparer.OrdinalIgnoreCase);
        foreach (var style in xaml.Descendants().Where(element => element.Name.LocalName == "Style"))
        {
            var selector = TryGetAttributeValue(style, "Selector");
            var selectorId = ParseAvaloniaStyleSelectorId(selector);
            if (string.IsNullOrWhiteSpace(selectorId))
            {
                continue;
            }

            var animationElement = style.Descendants().FirstOrDefault(element => element.Name.LocalName == "Animation");
            if (animationElement is null)
            {
                continue;
            }

            var duration = ParseDurationSeconds(TryGetAttributeValue(animationElement, "Duration"), 6d);
            var keyframes = new List<AvaloniaAnimationFrame>();
            foreach (var keyframe in animationElement.Elements().Where(element => element.Name.LocalName == "KeyFrame"))
            {
                var cue = ParseCue(TryGetAttributeValue(keyframe, "Cue"));
                var setters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                foreach (var setter in keyframe.Elements().Where(element => element.Name.LocalName == "Setter"))
                {
                    var property = TryGetAttributeValue(setter, "Property");
                    var value = ParseDouble(TryGetAttributeValue(setter, "Value"), double.NaN);
                    if (!string.IsNullOrWhiteSpace(property) && !double.IsNaN(value))
                    {
                        setters[property] = value;
                    }
                }

                keyframes.Add(new AvaloniaAnimationFrame(cue, setters));
            }

            styles[selectorId] = new AvaloniaStyleAnimation(duration, keyframes);
        }

        return styles;
    }

    private static string? ParseAvaloniaStyleSelectorId(string? selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            return null;
        }

        var hashIndex = selector.LastIndexOf('#');
        if (hashIndex < 0 || hashIndex == selector.Length - 1)
        {
            return null;
        }

        var id = selector[(hashIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var stopIndex = id.IndexOfAny([' ', ':', '.', '[', '>']);
        return stopIndex >= 0 ? id[..stopIndex] : id;
    }

    private static TimelineLayer BuildLayerFromAvaloniaElement(XElement element, int zIndex)
    {
        var kind = GetAvaloniaImportedLayerKind(element);
        var x = ParseDouble(TryGetAttributeValue(element, "Canvas.Left", "Left"), 0d);
        var y = ParseDouble(TryGetAttributeValue(element, "Canvas.Top", "Top"), 0d);
        var width = ParseDouble(TryGetAttributeValue(element, "Width"), 240d);
        var height = ParseDouble(TryGetAttributeValue(element, "Height"), 140d);
        var opacity = ParseDouble(TryGetAttributeValue(element, "Opacity"), 1d);
        var rotation = ExtractRotation(element);
        var name = TryGetAttributeValue(element, "Name") ?? $"{kind} {zIndex + 1}";
        var fill = TryGetAttributeValue(element, "Background", "Fill", "Foreground") ?? "#24E5C1";
        var stroke = TryGetAttributeValue(element, "BorderBrush", "Stroke") ?? "#FFFFFF";
        var strokeThickness = ParseDouble(TryGetAttributeValue(element, "BorderThickness", "StrokeThickness"), 1.6d);
        var text = TryGetAttributeValue(element, "Text", "Content", "Watermark") ?? element.Value.Trim();

        TimelineLayer layer;
        if (kind == LayerKind.Path)
        {
            var points = ParsePathPoints(TryGetAttributeValue(element, "Data"));
            layer = points.Count >= 2
                ? TimelineEditingService.CreatePathLayer(name, fill, stroke, points, IsClosedPath(TryGetAttributeValue(element, "Data")), zIndex, strokeThickness)
                : TimelineEditingService.CreateLayer(LayerKind.Path, name, fill, string.Empty, x, y, zIndex);
        }
        else if (kind == LayerKind.AvaloniaControl)
        {
            var controlKind = GetAvaloniaImportedControlKind(element);
            layer = TimelineEditingService.CreateAvaloniaControlLayer(controlKind, name, fill, stroke, text, x, y, zIndex);
            layer.Style.AvaloniaControl.SecondaryContent = TryGetAttributeValue(element, "Watermark", "Placeholder") ?? string.Empty;
            layer.Style.AvaloniaControl.IsChecked = ParseBool(TryGetAttributeValue(element, "IsChecked"), false);
            layer.Style.AvaloniaControl.Minimum = ParseDouble(TryGetAttributeValue(element, "Minimum"), 0d);
            layer.Style.AvaloniaControl.Maximum = ParseDouble(TryGetAttributeValue(element, "Maximum"), 100d);
            layer.Style.AvaloniaControl.Value = ParseDouble(TryGetAttributeValue(element, "Value"), 50d);
            layer.Style.AvaloniaControl.IsIndeterminate = ParseBool(TryGetAttributeValue(element, "IsIndeterminate"), false);
            layer.Style.AvaloniaControl.Source = TryGetAttributeValue(element, "Source") ?? string.Empty;

            switch (controlKind)
            {
                case AvaloniaControlKind.ComboBox:
                    var comboItems = element.Elements()
                        .Where(child => child.Name.LocalName == "ComboBoxItem")
                        .Select(child => TryGetAttributeValue(child, "Content") ?? child.Value.Trim())
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .ToList();
                    layer.Style.AvaloniaControl.Content = comboItems.FirstOrDefault() ?? layer.Style.AvaloniaControl.Content;
                    layer.Style.AvaloniaControl.SecondaryContent = string.Join("|", comboItems);
                    break;
                case AvaloniaControlKind.ListBox:
                    layer.Style.AvaloniaControl.Content = string.Join("|", element.Elements()
                        .Where(child => child.Name.LocalName == "ListBoxItem")
                        .Select(child => TryGetAttributeValue(child, "Content") ?? child.Value.Trim())
                        .Where(item => !string.IsNullOrWhiteSpace(item)));
                    break;
                case AvaloniaControlKind.TabControl:
                    var tabs = element.Elements()
                        .Where(child => child.Name.LocalName == "TabItem")
                        .Select(child => TryGetAttributeValue(child, "Header") ?? child.Value.Trim())
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .ToList();
                    layer.Style.AvaloniaControl.Content = string.Join("|", tabs);
                    layer.Style.AvaloniaControl.SecondaryContent = element.Elements().FirstOrDefault(child => child.Name.LocalName == "TabItem") is { } tab
                        ? ResolveElementText(tab)
                        : layer.Style.AvaloniaControl.SecondaryContent;
                    break;
                case AvaloniaControlKind.Grid:
                    var gridItems = element.Elements()
                        .Select(ResolveElementText)
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .ToList();
                    layer.Style.AvaloniaControl.Content = string.IsNullOrWhiteSpace(text) ? $"{gridItems.Count} cells" : text;
                    layer.Style.AvaloniaControl.SecondaryContent = string.Join("|", gridItems);
                    break;
                case AvaloniaControlKind.StackPanel:
                    layer.Style.AvaloniaControl.SecondaryContent = string.Join("|", element.Elements()
                        .Select(ResolveElementText)
                        .Where(item => !string.IsNullOrWhiteSpace(item)));
                    break;
                case AvaloniaControlKind.PathIcon:
                    layer.Style.AvaloniaControl.SecondaryContent = TryGetAttributeValue(element, "Data") ?? layer.Style.AvaloniaControl.SecondaryContent;
                    break;
            }
        }
        else
        {
            layer = TimelineEditingService.CreateLayer(kind, name, fill, text, x, y, zIndex);
        }

        layer.Defaults.X = x;
        layer.Defaults.Y = y;
        layer.Defaults.Width = Math.Max(16d, width);
        layer.Defaults.Height = Math.Max(16d, height);
        layer.Defaults.Opacity = TimelineMath.Clamp(opacity, 0d, 1d);
        layer.Defaults.Rotation = rotation;
        layer.Style.Fill = fill;
        layer.Style.Stroke = stroke;
        layer.Style.StrokeThickness = Math.Max(0.5d, strokeThickness);
        layer.Style.Text = kind == LayerKind.Text ? text : layer.Style.Text;
        layer.Style.FontSize = ParseDouble(TryGetAttributeValue(element, "FontSize"), layer.Style.FontSize);
        layer.Style.CornerRadius = ParseDouble(TryGetAttributeValue(element, "CornerRadius", "RadiusX"), layer.Style.CornerRadius);
        layer.IsVisible = ParseBool(TryGetAttributeValue(element, "IsVisible"), true);
        return layer;
    }

    private static void ApplyAvaloniaAnimation(Dictionary<string, AvaloniaStyleAnimation> styles, XElement element, TimelineLayer layer, SceneModel scene)
    {
        var elementName = TryGetAttributeValue(element, "Name");
        if (string.IsNullOrWhiteSpace(elementName) || !styles.TryGetValue(elementName, out var styleAnimation))
        {
            return;
        }

        scene.Duration = Math.Max(scene.Duration, styleAnimation.Duration);
        foreach (var keyframe in styleAnimation.Keyframes.OrderBy(item => item.Cue))
        {
            var time = keyframe.Cue * Math.Max(0.05d, styleAnimation.Duration);
            foreach (var setter in keyframe.Setters)
            {
                var property = MapImportedAnimatedProperty(setter.Key);
                if (property is null)
                {
                    continue;
                }

                TimelineEditingService.SetKeyframe(layer, property.Value, time, setter.Value, styleAnimation.Duration);
            }
        }
    }

    private static TimelineLayer BuildLayerFromSvgElement(XElement element, int zIndex)
    {
        if (element.Name.LocalName != "g")
        {
            return BuildLayerFromStandaloneSvgElement(element, zIndex);
        }

        var kind = ParseSvgLayerKind(TryGetAttributeValue(element, "data-layer-kind"));
        if (kind == LayerKind.AvaloniaControl)
        {
            var controlKind = Enum.TryParse<AvaloniaControlKind>(TryGetAttributeValue(element, "data-control-kind"), true, out var parsedControlKind)
                ? parsedControlKind
                : AvaloniaControlKind.Border;
            var baseWidth = ParseDouble(TryGetAttributeValue(element, "data-base-width"), 240d);
            var baseHeight = ParseDouble(TryGetAttributeValue(element, "data-base-height"), 140d);
            var layer = TimelineEditingService.CreateAvaloniaControlLayer(controlKind, $"{controlKind} {zIndex + 1}", "#30445E", "#FFFFFF", controlKind.ToString(), 0d, 0d, zIndex);
            layer.Defaults.Width = baseWidth;
            layer.Defaults.Height = baseHeight;
            var visual = element.Elements().FirstOrDefault();
            if (visual is not null)
            {
                layer.Style.Fill = TryGetAttributeValue(visual.DescendantsAndSelf().FirstOrDefault(node => node.Name.LocalName == "rect") ?? visual, "fill") ?? layer.Style.Fill;
                layer.Style.Stroke = TryGetAttributeValue(visual.DescendantsAndSelf().FirstOrDefault(node => node.Name.LocalName == "rect") ?? visual, "stroke") ?? layer.Style.Stroke;
            }

            ApplySvgTransforms(element, layer, 6d);
            layer.IsVisible = !string.Equals(TryGetAttributeValue(element, "visibility"), "hidden", StringComparison.OrdinalIgnoreCase);
            return layer;
        }

        if (kind == LayerKind.Video || kind == LayerKind.Audio)
        {
            var mediaLayer = TimelineEditingService.CreateLayer(kind, $"{kind} {zIndex + 1}", "#30445E", string.Empty, 0d, 0d, zIndex);
            mediaLayer.Defaults.Width = ParseDouble(TryGetAttributeValue(element, "data-base-width"), 240d);
            mediaLayer.Defaults.Height = ParseDouble(TryGetAttributeValue(element, "data-base-height"), 140d);
            ApplySvgTransforms(element, mediaLayer, 6d);
            mediaLayer.IsVisible = !string.Equals(TryGetAttributeValue(element, "visibility"), "hidden", StringComparison.OrdinalIgnoreCase);
            return mediaLayer;
        }

        var child = element.Elements().FirstOrDefault() ?? throw new InvalidOperationException("SVG layer group is empty.");
        var layerFromChild = BuildLayerFromStandaloneSvgElement(child, zIndex, kind);
        layerFromChild.Defaults.Width = ParseDouble(TryGetAttributeValue(element, "data-base-width"), layerFromChild.Defaults.Width);
        layerFromChild.Defaults.Height = ParseDouble(TryGetAttributeValue(element, "data-base-height"), layerFromChild.Defaults.Height);
        ApplySvgTransforms(element, layerFromChild, 6d);
        layerFromChild.IsVisible = !string.Equals(TryGetAttributeValue(element, "visibility"), "hidden", StringComparison.OrdinalIgnoreCase);
        return layerFromChild;
    }

    private static TimelineLayer BuildLayerFromStandaloneSvgElement(XElement element, int zIndex, LayerKind? forcedKind = null)
    {
        var kind = forcedKind ?? ParseSvgElementKind(element);
        return kind switch
        {
            LayerKind.Rectangle => BuildSvgRectangleLayer(element, zIndex),
            LayerKind.Ellipse => BuildSvgEllipseLayer(element, zIndex),
            LayerKind.Text => BuildSvgTextLayer(element, zIndex),
            LayerKind.Path => BuildSvgPathLayer(element, zIndex),
            _ => TimelineEditingService.CreateLayer(LayerKind.Rectangle, $"Layer {zIndex + 1}", "#24E5C1", string.Empty, 0d, 0d, zIndex)
        };
    }

    private static TimelineLayer BuildSvgRectangleLayer(XElement element, int zIndex)
    {
        var x = ParseDouble(TryGetAttributeValue(element, "x"), 0d);
        var y = ParseDouble(TryGetAttributeValue(element, "y"), 0d);
        var width = ParseDouble(TryGetAttributeValue(element, "width"), 240d);
        var height = ParseDouble(TryGetAttributeValue(element, "height"), 152d);
        var layer = TimelineEditingService.CreateLayer(LayerKind.Rectangle, $"Rectangle {zIndex + 1}", TryGetAttributeValue(element, "fill") ?? "#24E5C1", string.Empty, x, y, zIndex);
        layer.Defaults.Width = width;
        layer.Defaults.Height = height;
        layer.Style.Stroke = TryGetAttributeValue(element, "stroke") ?? layer.Style.Stroke;
        layer.Style.StrokeThickness = ParseDouble(TryGetAttributeValue(element, "stroke-width"), layer.Style.StrokeThickness);
        layer.Style.CornerRadius = ParseDouble(TryGetAttributeValue(element, "rx"), layer.Style.CornerRadius);
        return layer;
    }

    private static TimelineLayer BuildSvgEllipseLayer(XElement element, int zIndex)
    {
        var rx = ParseDouble(TryGetAttributeValue(element, "rx"), 92d);
        var ry = ParseDouble(TryGetAttributeValue(element, "ry"), rx);
        var cx = ParseDouble(TryGetAttributeValue(element, "cx"), rx);
        var cy = ParseDouble(TryGetAttributeValue(element, "cy"), ry);
        var layer = TimelineEditingService.CreateLayer(LayerKind.Ellipse, $"Ellipse {zIndex + 1}", TryGetAttributeValue(element, "fill") ?? "#FF8A4C", string.Empty, cx - rx, cy - ry, zIndex);
        layer.Defaults.Width = rx * 2d;
        layer.Defaults.Height = ry * 2d;
        layer.Style.Stroke = TryGetAttributeValue(element, "stroke") ?? layer.Style.Stroke;
        layer.Style.StrokeThickness = ParseDouble(TryGetAttributeValue(element, "stroke-width"), layer.Style.StrokeThickness);
        return layer;
    }

    private static TimelineLayer BuildSvgTextLayer(XElement element, int zIndex)
    {
        var x = ParseDouble(TryGetAttributeValue(element, "x"), 0d);
        var y = ParseDouble(TryGetAttributeValue(element, "y"), 0d);
        var fontSize = ParseDouble(TryGetAttributeValue(element, "font-size"), 42d);
        var layer = TimelineEditingService.CreateLayer(LayerKind.Text, $"Text {zIndex + 1}", TryGetAttributeValue(element, "fill") ?? "#FFFFFF", element.Value, x, Math.Max(0d, y - fontSize), zIndex);
        layer.Style.FontSize = fontSize;
        return layer;
    }

    private static TimelineLayer BuildSvgPathLayer(XElement element, int zIndex)
    {
        var points = ParsePathPoints(TryGetAttributeValue(element, "d"));
        var isClosed = IsClosedPath(TryGetAttributeValue(element, "d"));
        var layer = points.Count >= 2
            ? TimelineEditingService.CreatePathLayer($"Path {zIndex + 1}", TryGetAttributeValue(element, "fill") ?? "#4FE1FF", TryGetAttributeValue(element, "stroke") ?? "#FFFFFF", points, isClosed, zIndex, ParseDouble(TryGetAttributeValue(element, "stroke-width"), 2d))
            : TimelineEditingService.CreateLayer(LayerKind.Path, $"Path {zIndex + 1}", "#4FE1FF", string.Empty, 0d, 0d, zIndex);
        return layer;
    }

    private static double ApplySvgAnimation(XElement element, TimelineLayer layer, double defaultDuration)
    {
        if (element.Name.LocalName != "g")
        {
            return defaultDuration;
        }

        ApplySvgTransforms(element, layer, defaultDuration);
        return ResolveSvgDuration(element, defaultDuration);
    }

    private static void ApplySvgTransforms(XElement group, TimelineLayer layer, double defaultDuration)
    {
        var baseWidth = Math.Max(1d, layer.Defaults.Width);
        var baseHeight = Math.Max(1d, layer.Defaults.Height);
        var duration = ResolveSvgDuration(group, defaultDuration);
        var translate = group.Elements().FirstOrDefault(element => element.Name.LocalName == "animateTransform" && string.Equals(TryGetAttributeValue(element, "type"), "translate", StringComparison.OrdinalIgnoreCase));
        var scale = group.Elements().FirstOrDefault(element => element.Name.LocalName == "animateTransform" && string.Equals(TryGetAttributeValue(element, "type"), "scale", StringComparison.OrdinalIgnoreCase));
        var rotate = group.Elements().FirstOrDefault(element => element.Name.LocalName == "animateTransform" && string.Equals(TryGetAttributeValue(element, "type"), "rotate", StringComparison.OrdinalIgnoreCase));
        var opacity = group.Elements().FirstOrDefault(element => element.Name.LocalName == "animate" && string.Equals(TryGetAttributeValue(element, "attributeName"), "opacity", StringComparison.OrdinalIgnoreCase));

        var keyTimes = ParseNumericList(TryGetAttributeValue(translate ?? scale ?? rotate ?? opacity, "keyTimes"));
        if (keyTimes.Count == 0)
        {
            keyTimes = [0d];
        }

        var translateValues = ParseTupleList(TryGetAttributeValue(translate, "values"), 2);
        var scaleValues = ParseTupleList(TryGetAttributeValue(scale, "values"), 2);
        var rotateValues = ParseTupleList(TryGetAttributeValue(rotate, "values"), 3);
        var opacityValues = ParseNumericList(TryGetAttributeValue(opacity, "values"));

        for (var index = 0; index < keyTimes.Count; index++)
        {
            var time = keyTimes[index] * duration;
            if (index < translateValues.Count)
            {
                TimelineEditingService.SetKeyframe(layer, AnimatedProperty.X, time, translateValues[index][0], duration);
                TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Y, time, translateValues[index][1], duration);
                if (index == 0)
                {
                    layer.Defaults.X = translateValues[index][0];
                    layer.Defaults.Y = translateValues[index][1];
                }
            }

            if (index < scaleValues.Count)
            {
                var width = baseWidth * scaleValues[index][0];
                var height = baseHeight * scaleValues[index][1];
                TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Width, time, width, duration);
                TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Height, time, height, duration);
                if (index == 0)
                {
                    layer.Defaults.Width = width;
                    layer.Defaults.Height = height;
                }
            }

            if (index < rotateValues.Count)
            {
                TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Rotation, time, rotateValues[index][0], duration);
                if (index == 0)
                {
                    layer.Defaults.Rotation = rotateValues[index][0];
                }
            }

            if (index < opacityValues.Count)
            {
                TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Opacity, time, opacityValues[index], duration);
                if (index == 0)
                {
                    layer.Defaults.Opacity = opacityValues[index];
                }
            }
        }
    }

    private static TimelineLayer BuildLayerFromHtmlElement(XElement element, int zIndex)
    {
        var kind = ParseHtmlLayerKind(element);
        var style = ParseInlineStyle(TryGetAttributeValue(element, "style"));
        var transform = style.TryGetValue("transform", out var transformValue) ? transformValue : string.Empty;
        var translate = ExtractCssTranslate(transform);
        var scale = ExtractCssScale(transform);
        var x = ParseCssSize(style, "left", 0d) + translate.X;
        var y = ParseCssSize(style, "top", 0d) + translate.Y;
        var width = ParseCssSize(style, "width", 240d) * scale.X;
        var height = ParseCssSize(style, "height", 140d) * scale.Y;
        var rotation = ParseCssRotation(style);
        var opacity = ParseCssDouble(style, "opacity", 1d);
        var fill = ParseCssColor(style, "background") ?? "#24E5C1";
        var stroke = ParseCssBorderColor(style) ?? "#FFFFFF";
        var text = element.Value.Trim();
        TimelineLayer layer;

        if (kind == LayerKind.AvaloniaControl)
        {
            var controlKind = ParseHtmlControlKind(element);
            layer = TimelineEditingService.CreateAvaloniaControlLayer(controlKind, $"{controlKind} {zIndex + 1}", fill, stroke, ResolveHtmlControlText(element), x, y, zIndex);
            layer.Style.AvaloniaControl.SecondaryContent = TryGetAttributeValue(element, "placeholder") ?? string.Empty;
            layer.Style.AvaloniaControl.IsChecked = ParseBool(TryGetAttributeValue(element, "data-checked"), TryGetAttributeValue(element, "checked") is not null);
            layer.Style.AvaloniaControl.Minimum = ParseDouble(TryGetAttributeValue(element, "min"), 0d);
            layer.Style.AvaloniaControl.Maximum = ParseDouble(TryGetAttributeValue(element, "max"), 100d);
            layer.Style.AvaloniaControl.Value = ParseDouble(TryGetAttributeValue(element, "value"), 50d);
            layer.Style.AvaloniaControl.IsIndeterminate = ParseBool(TryGetAttributeValue(element, "data-indeterminate"), false);
            layer.Style.AvaloniaControl.Source = TryGetAttributeValue(element, "src") ?? string.Empty;

            switch (controlKind)
            {
                case AvaloniaControlKind.ComboBox:
                    var options = element.Elements()
                        .Where(child => child.Name.LocalName == "option")
                        .Select(child => TryGetAttributeValue(child, "value") ?? child.Value.Trim())
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .ToList();
                    var selectedOption = element.Elements().FirstOrDefault(child =>
                        child.Name.LocalName == "option" &&
                        child.Attributes().Any(attribute => string.Equals(attribute.Name.LocalName, "selected", StringComparison.OrdinalIgnoreCase)));
                    layer.Style.AvaloniaControl.Content = TryGetAttributeValue(selectedOption, "value") ?? selectedOption?.Value.Trim() ?? options.FirstOrDefault() ?? layer.Style.AvaloniaControl.Content;
                    layer.Style.AvaloniaControl.SecondaryContent = string.Join("|", options);
                    break;
                case AvaloniaControlKind.ListBox:
                    layer.Style.AvaloniaControl.Content = string.Join("|", element.Elements()
                        .Where(child => child.Name.LocalName == "li")
                        .Select(ResolveElementText)
                        .Where(item => !string.IsNullOrWhiteSpace(item)));
                    break;
                case AvaloniaControlKind.TabControl:
                    var headers = element.Descendants()
                        .Where(child => child.Name.LocalName == "span")
                        .Select(ResolveElementText)
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .ToList();
                    var content = element.Elements()
                        .Where(child => child.Name.LocalName == "div")
                        .Select(ResolveElementText)
                        .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
                    layer.Style.AvaloniaControl.Content = string.Join("|", headers);
                    layer.Style.AvaloniaControl.SecondaryContent = content ?? layer.Style.AvaloniaControl.SecondaryContent;
                    break;
                case AvaloniaControlKind.Grid:
                    var gridItems = element.Elements()
                        .Select(ResolveElementText)
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .ToList();
                    layer.Style.AvaloniaControl.Content = string.IsNullOrWhiteSpace(text) ? $"{gridItems.Count} cells" : text;
                    layer.Style.AvaloniaControl.SecondaryContent = string.Join("|", gridItems);
                    break;
                case AvaloniaControlKind.StackPanel:
                    layer.Style.AvaloniaControl.SecondaryContent = string.Join("|", element.Elements()
                        .Select(ResolveElementText)
                        .Where(item => !string.IsNullOrWhiteSpace(item)));
                    break;
                case AvaloniaControlKind.PathIcon:
                    layer.Style.AvaloniaControl.SecondaryContent = TryGetAttributeValue(element, "data-path") ?? layer.Style.AvaloniaControl.SecondaryContent;
                    break;
            }
        }
        else if (kind == LayerKind.Path)
        {
            var path = element.Descendants().FirstOrDefault(node => node.Name.LocalName == "path");
            var points = ParsePathPoints(TryGetAttributeValue(path, "d"));
            layer = points.Count >= 2
                ? TimelineEditingService.CreatePathLayer($"Path {zIndex + 1}", TryGetAttributeValue(path, "fill") ?? fill, TryGetAttributeValue(path, "stroke") ?? stroke, points, IsClosedPath(TryGetAttributeValue(path, "d")), zIndex, ParseDouble(TryGetAttributeValue(path, "stroke-width"), 2d))
                : TimelineEditingService.CreateLayer(LayerKind.Path, $"Path {zIndex + 1}", fill, string.Empty, x, y, zIndex);
        }
        else
        {
            layer = TimelineEditingService.CreateLayer(kind, $"{kind} {zIndex + 1}", fill, text, x, y, zIndex);
        }

        layer.Defaults.X = x;
        layer.Defaults.Y = y;
        layer.Defaults.Width = Math.Max(16d, width);
        layer.Defaults.Height = Math.Max(16d, height);
        layer.Defaults.Rotation = rotation;
        layer.Defaults.Opacity = opacity;
        layer.Style.Fill = fill;
        layer.Style.Stroke = stroke;
        layer.Style.Text = kind == LayerKind.Text ? text : layer.Style.Text;
        layer.Style.FontSize = ParseCssSize(style, "font-size", layer.Style.FontSize);
        layer.Style.CornerRadius = ParseCssSize(style, "border-radius", layer.Style.CornerRadius);
        return layer;
    }

    private static void ApplyCssAnimation(TimelineLayer layer, IReadOnlyList<CssAnimationFrame> frames, double defaultDuration)
    {
        var baseX = layer.Defaults.X;
        var baseY = layer.Defaults.Y;
        var baseWidth = layer.Defaults.Width;
        var baseHeight = layer.Defaults.Height;
        foreach (var frame in frames.OrderBy(item => item.Cue))
        {
            var time = frame.Cue * defaultDuration;
            if (frame.Declarations.TryGetValue("left", out var left))
            {
                TimelineEditingService.SetKeyframe(layer, AnimatedProperty.X, time, ParseCssPixelValue(left), defaultDuration);
            }

            if (frame.Declarations.TryGetValue("top", out var top))
            {
                TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Y, time, ParseCssPixelValue(top), defaultDuration);
            }

            if (frame.Declarations.TryGetValue("width", out var width))
            {
                TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Width, time, ParseCssPixelValue(width), defaultDuration);
            }

            if (frame.Declarations.TryGetValue("height", out var height))
            {
                TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Height, time, ParseCssPixelValue(height), defaultDuration);
            }

            if (frame.Declarations.TryGetValue("opacity", out var opacity))
            {
                TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Opacity, time, ParseDouble(opacity, 1d), defaultDuration);
            }

            if (frame.Declarations.TryGetValue("transform", out var transform))
            {
                var translate = ExtractCssTranslate(transform);
                var scale = ExtractCssScale(transform);
                var rotation = ExtractDegrees(transform);
                if (Math.Abs(translate.X) > 0.0001d)
                {
                    TimelineEditingService.SetKeyframe(layer, AnimatedProperty.X, time, baseX + translate.X, defaultDuration);
                }

                if (Math.Abs(translate.Y) > 0.0001d)
                {
                    TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Y, time, baseY + translate.Y, defaultDuration);
                }

                if (Math.Abs(scale.X - 1d) > 0.0001d)
                {
                    TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Width, time, baseWidth * scale.X, defaultDuration);
                }

                if (Math.Abs(scale.Y - 1d) > 0.0001d)
                {
                    TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Height, time, baseHeight * scale.Y, defaultDuration);
                }

                TimelineEditingService.SetKeyframe(layer, AnimatedProperty.Rotation, time, rotation, defaultDuration);
            }
        }
    }

    private static Dictionary<string, List<CssAnimationFrame>> ParseCssAnimations(string css)
    {
        var animations = new Dictionary<string, List<CssAnimationFrame>>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        while (index < css.Length)
        {
            var keyframesIndex = css.IndexOf("@keyframes", index, StringComparison.OrdinalIgnoreCase);
            if (keyframesIndex < 0)
            {
                break;
            }

            var nameStart = keyframesIndex + "@keyframes".Length;
            while (nameStart < css.Length && char.IsWhiteSpace(css[nameStart]))
            {
                nameStart++;
            }

            var nameEnd = nameStart;
            while (nameEnd < css.Length && !char.IsWhiteSpace(css[nameEnd]) && css[nameEnd] != '{')
            {
                nameEnd++;
            }

            var name = css[nameStart..nameEnd].Trim();
            var bodyStart = css.IndexOf('{', nameEnd);
            if (bodyStart < 0)
            {
                break;
            }

            var bodyEnd = FindMatchingBrace(css, bodyStart);
            if (bodyEnd <= bodyStart)
            {
                break;
            }

            var body = css[(bodyStart + 1)..bodyEnd];
            animations[name] = ParseCssAnimationFrames(body);
            index = bodyEnd + 1;
        }

        return animations;
    }

    private static List<CssAnimationFrame> ParseCssAnimationFrames(string body)
    {
        var frames = new List<CssAnimationFrame>();
        foreach (Match match in CssFrameRegex().Matches(body))
        {
            var cue = ParseDouble(match.Groups["cue"].Value, 0d) / 100d;
            var declarations = ParseInlineStyle(match.Groups["decl"].Value);
            frames.Add(new CssAnimationFrame(cue, declarations));
        }

        return frames;
    }

    private static IReadOnlyList<LayerFrameSample> BuildLayerSamples(TimelineLayer layer, SceneModel scene)
    {
        var totalFrames = FrameTimelineService.GetTotalFrames(scene.Duration, scene.FrameRate);
        var samples = new List<LayerFrameSample>(totalFrames);
        var fallback = TimelineInterpolationService.SampleLayer(layer, 0d);
        var lastVisible = fallback;

        for (var frame = 0; frame < totalFrames; frame++)
        {
            var time = FrameTimelineService.FrameToTime(frame, scene.FrameRate);
            var sampled = FrameTimelineService.SampleLayer(layer, time, scene.FrameRate, totalFrames);
            if (sampled is not null)
            {
                lastVisible = sampled.Value;
            }

            var snapshot = sampled ?? lastVisible;
            samples.Add(new LayerFrameSample(
                Time: time,
                Cue: scene.Duration <= 0d ? 0d : TimelineMath.Clamp(time / scene.Duration, 0d, 1d),
                Snapshot: snapshot,
                IsVisible: sampled is not null && layer.IsVisible));
        }

        return samples;
    }

    private static SceneModel GetActiveScene(TimelineDocument document)
    {
        return document.Scenes.FirstOrDefault(scene => scene.Id == document.ActiveSceneId)
            ?? document.Scenes.First();
    }

    private static TimelineDocument CreateImportedDocument(SceneModel scene, string sourceLabel)
    {
        var document = new TimelineDocument
        {
            Name = Path.GetFileNameWithoutExtension(sourceLabel),
            Duration = scene.Duration,
            CanvasWidth = scene.CanvasWidth,
            CanvasHeight = scene.CanvasHeight,
            TransparentStageBackground = scene.TransparentStageBackground,
            BackgroundFrom = scene.BackgroundFrom,
            BackgroundTo = scene.BackgroundTo,
            Layers = scene.Layers,
            Scenes = [scene],
            ActiveSceneId = scene.Id
        };
        PublishProfileService.EnsureProfiles(document);
        return document;
    }

    private static LayerKind GetAvaloniaImportedLayerKind(XElement element)
    {
        if (ParseAvaloniaControlKind(TryGetAttributeValue(element, "Tag")) is not null)
        {
            return LayerKind.AvaloniaControl;
        }

        return element.Name.LocalName switch
        {
            "Border" => ParseAvaloniaTagKind(TryGetAttributeValue(element, "Tag")) ?? LayerKind.Rectangle,
            "Ellipse" => LayerKind.Ellipse,
            "TextBlock" => ParseAvaloniaTagKind(TryGetAttributeValue(element, "Tag")) ?? LayerKind.Text,
            "Path" => LayerKind.Path,
            "Button" or "TextBox" or "CheckBox" or "ToggleButton" or "Slider" or "ProgressBar" or "ComboBox" or "ListBox" or "TabControl" or "Grid" or "StackPanel" or "PathIcon" => LayerKind.AvaloniaControl,
            _ => LayerKind.Rectangle
        };
    }

    private static AvaloniaControlKind GetAvaloniaImportedControlKind(XElement element)
    {
        if (ParseAvaloniaControlKind(TryGetAttributeValue(element, "Tag")) is { } tagged)
        {
            return tagged;
        }

        return element.Name.LocalName switch
        {
            "Button" => AvaloniaControlKind.Button,
            "TextBlock" => AvaloniaControlKind.TextBlock,
            "TextBox" => AvaloniaControlKind.TextBox,
            "CheckBox" => AvaloniaControlKind.CheckBox,
            "ToggleButton" => AvaloniaControlKind.ToggleButton,
            "Slider" => AvaloniaControlKind.Slider,
            "ProgressBar" => AvaloniaControlKind.ProgressBar,
            "ComboBox" => AvaloniaControlKind.ComboBox,
            "ListBox" => AvaloniaControlKind.ListBox,
            "TabControl" => AvaloniaControlKind.TabControl,
            "Grid" => AvaloniaControlKind.Grid,
            "StackPanel" => AvaloniaControlKind.StackPanel,
            "PathIcon" => AvaloniaControlKind.PathIcon,
            _ => AvaloniaControlKind.Border
        };
    }

    private static LayerKind ParseSvgLayerKind(string? value)
    {
        return Enum.TryParse<LayerKind>(value, true, out var kind) ? kind : LayerKind.Rectangle;
    }

    private static LayerKind ParseSvgElementKind(XElement element)
    {
        return element.Name.LocalName switch
        {
            "rect" => LayerKind.Rectangle,
            "ellipse" or "circle" => LayerKind.Ellipse,
            "text" => LayerKind.Text,
            "path" => LayerKind.Path,
            _ => LayerKind.Rectangle
        };
    }

    private static LayerKind ParseHtmlLayerKind(XElement element)
    {
        if (Enum.TryParse<LayerKind>(TryGetAttributeValue(element, "data-layer-kind"), true, out var declared))
        {
            return declared;
        }

        if (!string.IsNullOrWhiteSpace(TryGetAttributeValue(element, "data-control-kind")))
        {
            return LayerKind.AvaloniaControl;
        }

        return element.Name.LocalName switch
        {
            "button" or "input" or "label" or "progress" or "img" or "select" or "ul" or "ol" => LayerKind.AvaloniaControl,
            "svg" => LayerKind.Path,
            _ => LayerKind.Rectangle
        };
    }

    private static AvaloniaControlKind ParseHtmlControlKind(XElement element)
    {
        if (Enum.TryParse<AvaloniaControlKind>(TryGetAttributeValue(element, "data-control-kind"), true, out var declared))
        {
            return declared;
        }

        return element.Name.LocalName switch
        {
            "button" => AvaloniaControlKind.Button,
            "input" when string.Equals(TryGetAttributeValue(element, "type"), "text", StringComparison.OrdinalIgnoreCase) => AvaloniaControlKind.TextBox,
            "input" when string.Equals(TryGetAttributeValue(element, "type"), "range", StringComparison.OrdinalIgnoreCase) => AvaloniaControlKind.Slider,
            "select" => AvaloniaControlKind.ComboBox,
            "ul" or "ol" => AvaloniaControlKind.ListBox,
            "label" => AvaloniaControlKind.CheckBox,
            "progress" => AvaloniaControlKind.ProgressBar,
            "img" => AvaloniaControlKind.Image,
            _ => AvaloniaControlKind.Border
        };
    }

    private static bool IsImportableAvaloniaElement(XElement element)
    {
        return element.Name.LocalName is "Border" or "Ellipse" or "TextBlock" or "Path" or "Button" or "TextBox" or "CheckBox" or "ToggleButton" or "Slider" or "ProgressBar" or "ComboBox" or "ListBox" or "TabControl" or "Grid" or "StackPanel" or "PathIcon";
    }

    private static bool IsImportableSvgElement(XElement element)
    {
        return element.Name.LocalName is "g" or "rect" or "ellipse" or "circle" or "text" or "path";
    }

    private static bool IsImportableHtmlElement(XElement element)
    {
        return !string.Equals(TryGetAttributeValue(element, "id"), "stage", StringComparison.OrdinalIgnoreCase);
    }

    private static AnimatedProperty? MapImportedAnimatedProperty(string property)
    {
        return property switch
        {
            "Canvas.Left" or "Left" => AnimatedProperty.X,
            "Canvas.Top" or "Top" => AnimatedProperty.Y,
            "Width" => AnimatedProperty.Width,
            "Height" => AnimatedProperty.Height,
            "Opacity" => AnimatedProperty.Opacity,
            "RotateTransform.Angle" or "Rotation" => AnimatedProperty.Rotation,
            _ => null
        };
    }

    private static string? ExtractMetadataFromComment(string content)
    {
        var markerIndex = content.IndexOf(MetadataMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return null;
        }

        var start = markerIndex + MetadataMarker.Length;
        var end = content.IndexOf("-->", start, StringComparison.Ordinal);
        if (end < 0)
        {
            return null;
        }

        return content[start..end].Trim();
    }

    private static string? ExtractMetadataFromSvg(string content)
    {
        var svg = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
        return svg.Descendants().FirstOrDefault(element =>
            element.Name.LocalName == "metadata" &&
            string.Equals(TryGetAttributeValue(element, "id"), SvgMetadataId, StringComparison.OrdinalIgnoreCase))?.Value.Trim();
    }

    private static string? ExtractMetadataFromHtml(string content)
    {
        var html = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
        return html.Descendants().FirstOrDefault(element =>
            element.Name.LocalName == "meta" &&
            string.Equals(TryGetAttributeValue(element, "name"), HtmlMetadataName, StringComparison.OrdinalIgnoreCase))
            ?.Attribute("content")?.Value
            ?.Trim();
    }

    private static double GetLayerDuration(TimelineLayer layer, double fallbackDuration)
    {
        var trackDuration = layer.Tracks.SelectMany(track => track.Keyframes).Select(keyframe => keyframe.Time).DefaultIfEmpty(0d).Max();
        return Math.Max(fallbackDuration, trackDuration);
    }

    private static IReadOnlyList<AnimationExchangeIssue> CollectExportIssues(TimelineDocument document, AnimationExchangeFormat format)
    {
        var issues = new List<AnimationExchangeIssue>();

        if (document.Scenes.Count > 1)
        {
            issues.Add(CreateIssue(AnimationExchangeIssueSeverity.Warning, "Scenes", "Single-file animation interchange exports only the active scene surface."));
        }

        if (document.Layers.Any(layer => layer.Kind is LayerKind.Video or LayerKind.Audio))
        {
            issues.Add(format == AnimationExchangeFormat.FlashXfl
                ? CreateIssue(AnimationExchangeIssueSeverity.Info, "Media", "Flash XFL exports media as authoring references and timeline placeholders without embedding binary payloads.")
                : CreateIssue(AnimationExchangeIssueSeverity.Warning, "Media", "Audio and video layers export as styled placeholders in interchange formats."));
        }

        if (format == AnimationExchangeFormat.FlashXfl)
        {
            issues.Add(CreateIssue(AnimationExchangeIssueSeverity.Info, "Flash XFL", "Flash XFL export writes DOM timelines plus linkage-sharing metadata, Scale-9 grids, instance names, classic text settings, explicit tracks, easing, shape-tween, effect, and media-sync metadata for editable round-trip without binary SWF data."));
        }

        if (format is AnimationExchangeFormat.SvgSmil or AnimationExchangeFormat.HtmlCss)
        {
            issues.Add(CreateIssue(AnimationExchangeIssueSeverity.Info, "Motion", "SVG and HTML/CSS exports bake motion into sampled transforms for stable playback."));
        }

        if (format is not AnimationExchangeFormat.AvaloniaXaml &&
            document.Layers.Any(layer =>
                layer.Kind == LayerKind.AvaloniaControl &&
                layer.Style.AvaloniaControl.Kind is AvaloniaControlKind.Grid or AvaloniaControlKind.StackPanel or AvaloniaControlKind.TabControl or AvaloniaControlKind.PathIcon))
        {
            issues.Add(CreateIssue(AnimationExchangeIssueSeverity.Info, "Controls", "Complex Avalonia controls export as styled approximation visuals outside Avalonia XAML."));
        }

        return issues;
    }

    private static (XElement Surface, AnimationExchangeIssue? Issue) ResolveAvaloniaImportSurface(XDocument xaml)
    {
        var root = xaml.Root ?? throw new InvalidOperationException("Invalid Avalonia animation file.");
        var canvas = root.DescendantsAndSelf().FirstOrDefault(element => element.Name.LocalName == "Canvas");
        if (canvas is not null)
        {
            return (canvas, null);
        }

        var surface = root.DescendantsAndSelf().FirstOrDefault(element => element.Name.LocalName is "Grid" or "StackPanel" or "Border" or "Panel");
        if (surface is not null)
        {
            return
            (
                surface,
                CreateIssue(AnimationExchangeIssueSeverity.Warning, "Avalonia", "No Canvas surface was found. Imported the first layout surface and preserved its child visuals best-effort.")
            );
        }

        throw new InvalidOperationException("No supported Avalonia visual surface was found in the animation file.");
    }

    private static List<string> SplitControlItems(string? value, params string[] fallback)
    {
        var items = string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(['|', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        return items.Count > 0 ? items : [.. fallback];
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

    private static double ResolveSvgDuration(XElement element, double fallback)
    {
        var durationAttribute = element.Descendants()
            .Select(candidate => TryGetAttributeValue(candidate, "dur"))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        return ParseDurationSeconds(durationAttribute, fallback);
    }

    private static string ResolveFill(LayerSnapshot snapshot)
    {
        return snapshot.UseGradient ? snapshot.GradientFrom : snapshot.Fill;
    }

    private static string BuildCssAnimation(string animationName, IReadOnlyList<LayerFrameSample> samples)
    {
        var builder = new StringBuilder();
        builder.Append("@keyframes ").Append(animationName).AppendLine(" {");
        foreach (var sample in samples)
        {
            builder.Append("  ")
                .Append(FormatNumber(sample.Cue * 100d))
                .Append("% { left:")
                .Append(FormatNumber(sample.Snapshot.X))
                .Append("px; top:")
                .Append(FormatNumber(sample.Snapshot.Y))
                .Append("px; width:")
                .Append(FormatNumber(sample.Snapshot.Width))
                .Append("px; height:")
                .Append(FormatNumber(sample.Snapshot.Height))
                .Append("px; opacity:")
                .Append(FormatNumber(sample.IsVisible ? sample.Snapshot.Opacity : 0d))
                .Append("; transform:rotate(")
                .Append(FormatNumber(sample.Snapshot.Rotation))
                .AppendLine("deg); }");
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string BuildHtmlStyle(LayerSnapshot snapshot, string animationName, bool isVisible, double duration)
    {
        return string.Join(
            ';',
            [
                "position:absolute",
                $"left:{FormatNumber(snapshot.X)}px",
                $"top:{FormatNumber(snapshot.Y)}px",
                $"width:{FormatNumber(snapshot.Width)}px",
                $"height:{FormatNumber(snapshot.Height)}px",
                $"opacity:{FormatNumber(isVisible ? snapshot.Opacity : 0d)}",
                $"transform:rotate({FormatNumber(snapshot.Rotation)}deg)",
                $"animation-name:{animationName}",
                "animation-fill-mode:both",
                "animation-timing-function:linear",
                $"animation-duration:{FormatNumber(Math.Max(0.05d, duration))}s"
            ]);
    }

    private static string BuildKeyTimes(IReadOnlyList<LayerFrameSample> samples)
    {
        return string.Join(";", samples.Select(sample => sample.Cue.ToString("0.######", CultureInfo.InvariantCulture)));
    }

    private static string BuildAvaloniaPathData(LayerSnapshot snapshot)
    {
        return BuildPathData(snapshot.PathPoints, snapshot.Width, snapshot.Height, snapshot.IsClosed);
    }

    private static string BuildSvgPathData(LayerSnapshot snapshot)
    {
        return BuildPathData(snapshot.PathPoints, snapshot.Width, snapshot.Height, snapshot.IsClosed);
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

        var points = new List<VectorPointModel>();
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

    private static string? ExtractXamlBackground(XElement canvas, int stopIndex)
    {
        var brush = canvas.Elements().FirstOrDefault(element => element.Name.LocalName == "Canvas.Background");
        if (brush is null)
        {
            return null;
        }

        var stops = brush.Descendants().Where(element => element.Name.LocalName == "GradientStop").ToList();
        return stops.Count > stopIndex ? TryGetAttributeValue(stops[stopIndex], "Color") : null;
    }

    private static string? ExtractXamlSolidBackground(XElement canvas)
    {
        var brush = canvas.Elements().FirstOrDefault(element => element.Name.LocalName == "Canvas.Background");
        var solidColorBrush = brush?.Descendants().FirstOrDefault(element => element.Name.LocalName == "SolidColorBrush");
        return solidColorBrush is null ? null : TryGetAttributeValue(solidColorBrush, "Color");
    }

    private static bool ExtractXamlTransparentBackground(XElement canvas)
    {
        var color = ExtractXamlSolidBackground(canvas);
        return string.Equals(color, "#00FFFFFF", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(color, "Transparent", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractCssGradientColor(IReadOnlyDictionary<string, string> style, int stopIndex)
    {
        if (!style.TryGetValue("background", out var background) && !style.TryGetValue("background-image", out background))
        {
            return null;
        }

        var colors = HexColorRegex().Matches(background).Select(match => match.Value).ToList();
        return colors.Count > stopIndex ? colors[stopIndex] : null;
    }

    private static string? ExtractCssBackgroundColor(IReadOnlyDictionary<string, string> style)
    {
        if (!style.TryGetValue("background", out var background) && !style.TryGetValue("background-color", out background))
        {
            return null;
        }

        var normalized = background.Trim();
        if (string.Equals(normalized, "transparent", StringComparison.OrdinalIgnoreCase))
        {
            return "transparent";
        }

        return HexColorRegex().Match(normalized) is { Success: true } match ? match.Value : null;
    }

    private static string ResolveHtmlAnimationName(IReadOnlyDictionary<string, string> style)
    {
        if (style.TryGetValue("animation-name", out var animationName))
        {
            return animationName;
        }

        if (style.TryGetValue("animation", out var animation))
        {
            return animation.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        }

        return string.Empty;
    }

    private static double GetCssAnimationDuration(IReadOnlyDictionary<string, string> style)
    {
        if (style.TryGetValue("animation-duration", out var duration))
        {
            return ParseDurationSeconds(duration, 6d);
        }

        if (style.TryGetValue("animation", out var animation))
        {
            var token = animation.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(part => part.EndsWith('s'));
            return ParseDurationSeconds(token, 6d);
        }

        return 6d;
    }

    private static string ResolveHtmlControlText(XElement element)
    {
        return element.Name.LocalName switch
        {
            "input" => TryGetAttributeValue(element, "value") ?? string.Empty,
            "label" => element.Descendants().FirstOrDefault(child => child.Name.LocalName == "span")?.Value ?? element.Value.Trim(),
            "select" => element.Elements().FirstOrDefault(child => child.Name.LocalName == "option")?.Value.Trim() ?? string.Empty,
            "ul" or "ol" => string.Join("|", element.Elements().Where(child => child.Name.LocalName == "li").Select(ResolveElementText).Where(item => !string.IsNullOrWhiteSpace(item))),
            _ => element.Value.Trim()
        };
    }

    private static string ResolveElementText(XElement element)
    {
        return element.Name.LocalName switch
        {
            "input" => TryGetAttributeValue(element, "value") ?? string.Empty,
            "img" => TryGetAttributeValue(element, "alt") ?? string.Empty,
            _ => TryGetAttributeValue(element, "Text", "Content", "Header", "value") ?? element.Value.Trim()
        };
    }

    private static LayerKind? ParseAvaloniaTagKind(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        return Enum.TryParse<LayerKind>(tag, true, out var kind) ? kind : null;
    }

    private static AvaloniaControlKind? ParseAvaloniaControlKind(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        if (!tag.Contains("control:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var suffix = tag[(tag.IndexOf(':') + 1)..];
        return Enum.TryParse<AvaloniaControlKind>(suffix, true, out var controlKind) ? controlKind : null;
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
                    string.Equals(attribute.Name.ToString(), name, StringComparison.OrdinalIgnoreCase) ||
                    attribute.Name.ToString().EndsWith($".{name}", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains('.') && attribute.Name.ToString().EndsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    return attribute.Value;
                }
            }
        }

        return null;
    }

    private static Dictionary<string, string> ParseInlineStyle(string? style)
    {
        var declarations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(style))
        {
            return declarations;
        }

        foreach (var declaration in style.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = declaration.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            declarations[declaration[..separatorIndex].Trim()] = declaration[(separatorIndex + 1)..].Trim();
        }

        return declarations;
    }

    private static double ExtractRotation(XElement element)
    {
        var rotateTransform = element.Descendants().FirstOrDefault(node => node.Name.LocalName == "RotateTransform");
        return ParseDouble(TryGetAttributeValue(rotateTransform, "Angle"), 0d);
    }

    private static double ParseCssRotation(IReadOnlyDictionary<string, string> style)
    {
        return style.TryGetValue("transform", out var transform) ? ExtractDegrees(transform) : 0d;
    }

    private static (double X, double Y) ExtractCssTranslate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (0d, 0d);
        }

        var translateMatch = Regex.Match(input, @"translate\(\s*([-+]?\d*\.?\d+)px(?:\s*,\s*|\s+)([-+]?\d*\.?\d+)px\)", RegexOptions.IgnoreCase);
        if (translateMatch.Success)
        {
            return (ParseDouble(translateMatch.Groups[1].Value, 0d), ParseDouble(translateMatch.Groups[2].Value, 0d));
        }

        var translateX = Regex.Match(input, @"translateX\(\s*([-+]?\d*\.?\d+)px\)", RegexOptions.IgnoreCase);
        var translateY = Regex.Match(input, @"translateY\(\s*([-+]?\d*\.?\d+)px\)", RegexOptions.IgnoreCase);
        return
        (
            translateX.Success ? ParseDouble(translateX.Groups[1].Value, 0d) : 0d,
            translateY.Success ? ParseDouble(translateY.Groups[1].Value, 0d) : 0d
        );
    }

    private static (double X, double Y) ExtractCssScale(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (1d, 1d);
        }

        var scaleMatch = Regex.Match(input, @"scale\(\s*([-+]?\d*\.?\d+)(?:\s*,\s*|\s+)?([-+]?\d*\.?\d+)?\)", RegexOptions.IgnoreCase);
        if (scaleMatch.Success)
        {
            var scaleX = ParseDouble(scaleMatch.Groups[1].Value, 1d);
            var scaleY = string.IsNullOrWhiteSpace(scaleMatch.Groups[2].Value) ? scaleX : ParseDouble(scaleMatch.Groups[2].Value, scaleX);
            return (scaleX, scaleY);
        }

        var scaleXMatch = Regex.Match(input, @"scaleX\(\s*([-+]?\d*\.?\d+)\)", RegexOptions.IgnoreCase);
        var scaleYMatch = Regex.Match(input, @"scaleY\(\s*([-+]?\d*\.?\d+)\)", RegexOptions.IgnoreCase);
        return
        (
            scaleXMatch.Success ? ParseDouble(scaleXMatch.Groups[1].Value, 1d) : 1d,
            scaleYMatch.Success ? ParseDouble(scaleYMatch.Groups[1].Value, 1d) : 1d
        );
    }

    private static double ExtractDegrees(string input)
    {
        var match = Regex.Match(input, @"rotate\(([-+]?\d*\.?\d+)deg\)", RegexOptions.IgnoreCase);
        return match.Success ? ParseDouble(match.Groups[1].Value, 0d) : 0d;
    }

    private static string ParseCssColor(IReadOnlyDictionary<string, string> style, string name)
    {
        return style.TryGetValue(name, out var value)
            ? HexColorRegex().Match(value).Value
            : string.Empty;
    }

    private static string? ParseCssBorderColor(IReadOnlyDictionary<string, string> style)
    {
        if (!style.TryGetValue("border", out var border))
        {
            return null;
        }

        var match = HexColorRegex().Match(border);
        return match.Success ? match.Value : null;
    }

    private static double ParseCssSize(IReadOnlyDictionary<string, string> style, string name, double fallback)
    {
        return style.TryGetValue(name, out var value) ? ParseCssPixelValue(value, fallback) : fallback;
    }

    private static double ParseCssDouble(IReadOnlyDictionary<string, string> style, string name, double fallback)
    {
        return style.TryGetValue(name, out var value) ? ParseDouble(value, fallback) : fallback;
    }

    private static double ParseCssPixelValue(string? value, double fallback = 0d)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return ParseDouble(value.Replace("px", string.Empty, StringComparison.OrdinalIgnoreCase), fallback);
    }

    private static double ParseDouble(string? value, double fallback)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static bool ParseBool(string? value, bool fallback)
    {
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static double ParseCue(string? cue)
    {
        if (string.IsNullOrWhiteSpace(cue))
        {
            return 0d;
        }

        cue = cue.Trim();
        if (cue.EndsWith('%'))
        {
            return ParseDouble(cue[..^1], 0d) / 100d;
        }

        return TimelineMath.Clamp(ParseDouble(cue, 0d), 0d, 1d);
    }

    private static double ParseDurationSeconds(string? duration, double fallback)
    {
        if (string.IsNullOrWhiteSpace(duration))
        {
            return fallback;
        }

        duration = duration.Trim();
        if (duration.EndsWith('s') && double.TryParse(duration[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return Math.Max(0.05d, seconds);
        }

        return TimeSpan.TryParse(duration, CultureInfo.InvariantCulture, out var timeSpan)
            ? Math.Max(0.05d, timeSpan.TotalSeconds)
            : fallback;
    }

    private static double ParseSvgLength(string? value, double fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalized = value.Replace("px", string.Empty, StringComparison.OrdinalIgnoreCase);
        return ParseDouble(normalized, fallback);
    }

    private static List<double> ParseNumericList(string? values)
    {
        if (string.IsNullOrWhiteSpace(values))
        {
            return [];
        }

        return
        [
            .. values.Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => ParseDouble(value, double.NaN))
                .Where(value => !double.IsNaN(value))
        ];
    }

    private static List<double[]> ParseTupleList(string? values, int tupleLength)
    {
        if (string.IsNullOrWhiteSpace(values))
        {
            return [];
        }

        var tuples = new List<double[]>();
        foreach (var tuple in values.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = tuple
                .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => ParseDouble(value, double.NaN))
                .Where(value => !double.IsNaN(value))
                .ToArray();
            if (parts.Length >= tupleLength)
            {
                tuples.Add(parts[..tupleLength]);
            }
        }

        return tuples;
    }

    private static string SanitizeFileName(string value)
    {
        var safe = Regex.Replace(string.IsNullOrWhiteSpace(value) ? "animation" : value.Trim(), @"[^\w\-]+", "_");
        return string.IsNullOrWhiteSpace(safe) ? "animation" : safe.ToLowerInvariant();
    }

    private static string GetElementName(Guid id)
    {
        return $"layer_{id:N}";
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static int FindMatchingBrace(string value, int openingBraceIndex)
    {
        var depth = 0;
        for (var index = openingBraceIndex; index < value.Length; index++)
        {
            if (value[index] == '{')
            {
                depth++;
            }
            else if (value[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return index;
                }
            }
        }

        return -1;
    }

    [GeneratedRegex(@"(?<cue>[-+]?\d*\.?\d+)%\s*\{(?<decl>[^}]*)\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex CssFrameRegex();

    [GeneratedRegex(@"[-+]?\d*\.?\d+")]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"#(?:[0-9a-fA-F]{3,8})")]
    private static partial Regex HexColorRegex();

    private sealed record LayerFrameSample(double Time, double Cue, LayerSnapshot Snapshot, bool IsVisible);

    private sealed record AvaloniaAnimationFrame(double Cue, Dictionary<string, double> Setters);

    private sealed record AvaloniaStyleAnimation(double Duration, List<AvaloniaAnimationFrame> Keyframes);

    private sealed record CssAnimationFrame(double Cue, Dictionary<string, string> Declarations);
}
