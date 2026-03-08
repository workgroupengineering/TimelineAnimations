using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class AnimateDocumentProfileService
{
    public static void EnsureSettings(TimelineDocument document)
    {
        document.Animate ??= new AnimateDocumentSettings();
        ApplyMissingDefaults(document.Animate);
    }

    public static void ApplyTargetDefaults(TimelineDocument document, AnimateDocumentType type)
    {
        EnsureSettings(document);
        var settings = document.Animate;
        settings.Type = type;
        settings.Units = AnimateDocumentUnits.Pixels;
        settings.UseAdvancedLayers = true;
        settings.Enable3DTransforms = true;
        settings.PerspectiveAngle = 55d;
        settings.VanishingPointX = 0.5d;
        settings.VanishingPointY = 0.5d;

        switch (type)
        {
            case AnimateDocumentType.Html5Canvas:
                settings.IsResponsive = true;
                settings.UseExternalScriptFile = true;
                settings.UseWebFonts = true;
                settings.TemplateName = "HTML5 Canvas";
                break;
            case AnimateDocumentType.WebGl:
                settings.IsResponsive = false;
                settings.UseExternalScriptFile = true;
                settings.UseWebFonts = false;
                settings.TemplateName = "WebGL";
                break;
            case AnimateDocumentType.ActionScript3:
                settings.IsResponsive = false;
                settings.UseExternalScriptFile = false;
                settings.UseWebFonts = false;
                settings.TemplateName = "ActionScript 3.0";
                break;
            case AnimateDocumentType.AirDesktop:
                settings.IsResponsive = false;
                settings.UseExternalScriptFile = false;
                settings.UseWebFonts = false;
                settings.TemplateName = "AIR Desktop";
                break;
            case AnimateDocumentType.AirMobile:
                settings.IsResponsive = true;
                settings.UseExternalScriptFile = false;
                settings.UseWebFonts = false;
                settings.TemplateName = "AIR Mobile";
                break;
        }
    }

    public static string GetDisplayName(AnimateDocumentType type)
    {
        return type switch
        {
            AnimateDocumentType.Html5Canvas => "HTML5 Canvas",
            AnimateDocumentType.WebGl => "WebGL",
            AnimateDocumentType.ActionScript3 => "ActionScript 3.0",
            AnimateDocumentType.AirDesktop => "AIR Desktop",
            AnimateDocumentType.AirMobile => "AIR Mobile",
            _ => type.ToString()
        };
    }

    public static string GetSummary(AnimateDocumentSettings settings)
    {
        var parts = new List<string>
        {
            GetDisplayName(settings.Type),
            settings.Units.ToString()
        };

        if (settings.IsResponsive)
        {
            parts.Add("responsive");
        }

        if (settings.UseExternalScriptFile)
        {
            parts.Add("external script");
        }

        if (settings.Enable3DTransforms)
        {
            parts.Add($"3D {settings.PerspectiveAngle:0.#}deg");
        }

        if (settings.UseWebFonts)
        {
            parts.Add("web fonts");
        }

        if (!string.IsNullOrWhiteSpace(settings.TemplateName))
        {
            parts.Add(settings.TemplateName);
        }

        return string.Join(" • ", parts);
    }

    public static IReadOnlyList<PublishValidationIssue> ValidateCompatibility(TimelineDocument document)
    {
        EnsureSettings(document);
        var settings = document.Animate;
        var issues = new List<PublishValidationIssue>();
        var allLayers = document.Scenes.SelectMany(scene => scene.Layers).ToList();
        var uses3DTransforms = allLayers.Any(Uses3DTransforms);

        if (settings.IsResponsive &&
            settings.Type is AnimateDocumentType.ActionScript3 or AnimateDocumentType.AirDesktop)
        {
            issues.Add(Warning("Document", $"{GetDisplayName(settings.Type)} does not typically use responsive HTML stage behavior."));
        }

        if (allLayers.Any(layer => layer.Kind == LayerKind.AvaloniaControl))
        {
            issues.Add(Warning(
                "Document",
                $"Avalonia control layers are editor-native surfaces and require manual translation for {GetDisplayName(settings.Type)} publish targets."));
        }

        if (uses3DTransforms && !settings.Enable3DTransforms)
        {
            issues.Add(Warning("Document", "3D-authored layers are present while document 3D transforms are disabled."));
        }

        if (uses3DTransforms)
        {
            issues.Add(Warning("Document", $"{GetDisplayName(settings.Type)} publish targets may flatten 3D-authored layers during export/runtime playback."));
        }

        if (settings.Type is AnimateDocumentType.Html5Canvas or AnimateDocumentType.WebGl)
        {
            if (document.Scenes.SelectMany(scene => scene.Layers)
                .Any(layer => layer.Kind == LayerKind.Text && layer.Style.TextSettings.FieldKind != FlashTextFieldKind.Static))
            {
                issues.Add(Warning("Document", $"{GetDisplayName(settings.Type)} may require custom runtime handling for dynamic or input text fields."));
            }

            if (DocumentUsesScripts(document))
            {
                issues.Add(Warning("Document", $"{GetDisplayName(settings.Type)} exports will need target-side script glue for frame or behavior scripts."));
            }
        }

        if (settings.Type == AnimateDocumentType.WebGl &&
            document.Scenes.SelectMany(scene => scene.Layers).Any(UsesHeavyLayerEffects))
        {
            issues.Add(Warning("Document", "WebGL documents may need effect simplification when layers rely on multiple glow, blur, or shadow effects."));
        }

        return issues;
    }

    private static bool DocumentUsesScripts(TimelineDocument document)
    {
        foreach (var scene in document.Scenes)
        {
            if (scene.FrameLabels.Any(label => !string.IsNullOrWhiteSpace(label.Script)))
            {
                return true;
            }

            if (scene.Layers.Any(layer => layer.Behaviors.Any(behavior => !string.IsNullOrWhiteSpace(behavior.Script))))
            {
                return true;
            }
        }

        return false;
    }

    private static bool UsesHeavyLayerEffects(TimelineLayer layer)
    {
        return layer.Compositing.BlurRadius > 0d ||
               layer.Compositing.GlowOpacity > 0d ||
               layer.Compositing.ShadowOpacity > 0d;
    }

    private static bool Uses3DTransforms(TimelineLayer layer)
    {
        return Math.Abs(layer.Defaults.RotationX) > 0.01d ||
               Math.Abs(layer.Defaults.RotationY) > 0.01d ||
               Math.Abs(layer.Defaults.ZDepth) > 0.01d ||
               layer.Tracks.Any(track =>
                   track.Property is AnimatedProperty.RotationX or AnimatedProperty.RotationY or AnimatedProperty.ZDepth &&
                   track.Keyframes.Any(keyframe => Math.Abs(keyframe.Value) > 0.01d));
    }

    private static void ApplyMissingDefaults(AnimateDocumentSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.TemplateName))
        {
            settings.TemplateName = GetDisplayName(settings.Type);
        }
    }

    private static PublishValidationIssue Warning(string source, string message)
    {
        return new PublishValidationIssue
        {
            Severity = "Warning",
            Source = source,
            Message = message
        };
    }
}
