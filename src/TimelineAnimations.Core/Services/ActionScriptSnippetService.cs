using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class ActionScriptSnippetService
{
    public static IReadOnlyList<ActionScriptSnippet> BuildSnippets(
        TimelineDocument document,
        SceneModel? selectedScene,
        TimelineLayer? selectedLayer)
    {
        AnimateDocumentProfileService.EnsureSettings(document);

        var firstSceneName = document.Scenes.FirstOrDefault()?.Name ?? "Scene 1";
        var alternateSceneName = document.Scenes.Skip(1).FirstOrDefault()?.Name ?? firstSceneName;
        var firstLabel = selectedScene?.FrameLabels.FirstOrDefault()?.Name
            ?? document.Scenes.SelectMany(scene => scene.FrameLabels).FirstOrDefault()?.Name
            ?? "start";
        var targetLayerName = selectedLayer?.Name ?? "this";
        var targetVisualStateGroup = selectedLayer?.VisualStateGroups.FirstOrDefault()?.Name ?? "CommonStates";
        var targetVisualState = selectedLayer?.VisualStateGroups.FirstOrDefault()?.States.FirstOrDefault()?.Name ?? "Default";

        var snippets = new List<ActionScriptSnippet>
        {
            new()
            {
                Name = "Play Timeline",
                Summary = "Resume playback from the current frame.",
                Category = ActionScriptSnippetCategory.Playback,
                Script = "play"
            },
            new()
            {
                Name = "Stop Timeline",
                Summary = "Stop playback on the current frame.",
                Category = ActionScriptSnippetCategory.Playback,
                Script = "stop"
            },
            new()
            {
                Name = "Jump To Label",
                Summary = $"Navigate to the frame label '{firstLabel}'.",
                Category = ActionScriptSnippetCategory.Navigation,
                Script = $"goto_label \"{firstLabel}\""
            },
            new()
            {
                Name = "Show Layer",
                Summary = $"Reveal the layer '{targetLayerName}'.",
                Category = ActionScriptSnippetCategory.Layers,
                Script = $"show \"{targetLayerName}\""
            },
            new()
            {
                Name = "Hide Layer",
                Summary = $"Hide the layer '{targetLayerName}'.",
                Category = ActionScriptSnippetCategory.Layers,
                Script = $"hide \"{targetLayerName}\""
            },
            new()
            {
                Name = "Set Text",
                Summary = $"Replace text on '{targetLayerName}'.",
                Category = ActionScriptSnippetCategory.TextAndState,
                Script = $"set_text \"{targetLayerName}\" \"HELLO\""
            },
            new()
            {
                Name = "Trace Cue",
                Summary = "Emit a trace message for debugging or prototype monitoring.",
                Category = ActionScriptSnippetCategory.Diagnostics,
                Script = "trace \"Cue fired\""
            }
        };

        if (document.Animate.Type is AnimateDocumentType.ActionScript3 or AnimateDocumentType.AirDesktop or AnimateDocumentType.AirMobile)
        {
            snippets.Insert(
                2,
                new ActionScriptSnippet
                {
                    Name = "Go To Scene",
                    Summary = $"Switch to scene '{alternateSceneName}'.",
                    Category = ActionScriptSnippetCategory.Navigation,
                    Script = $"goto_scene \"{alternateSceneName}\""
                });
        }
        else
        {
            snippets.Insert(
                2,
                new ActionScriptSnippet
                {
                    Name = "Set State Variable",
                    Summary = "Set a runtime variable for HTML5/WebGL state glue.",
                    Category = ActionScriptSnippetCategory.Diagnostics,
                    Script = "set_var state ready"
                });
        }

        if (!string.Equals(targetLayerName, "this", StringComparison.OrdinalIgnoreCase))
        {
            snippets.Add(
                new ActionScriptSnippet
                {
                    Name = "Set Button State",
                    Summary = $"Switch '{targetLayerName}' to the Over button state.",
                    Category = ActionScriptSnippetCategory.TextAndState,
                    Script = $"set_button \"{targetLayerName}\" Over"
                });
        }

        snippets.Add(
            new ActionScriptSnippet
            {
                Name = "Apply Visual State",
                Summary = $"Apply '{targetVisualState}' from '{targetVisualStateGroup}'.",
                Category = ActionScriptSnippetCategory.TextAndState,
                Script = $"set_state \"{targetLayerName}\" \"{targetVisualStateGroup}\" \"{targetVisualState}\""
            });

        return snippets;
    }
}
