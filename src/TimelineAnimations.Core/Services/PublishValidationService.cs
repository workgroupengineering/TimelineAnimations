using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class PublishValidationService
{
    public static IReadOnlyList<PublishValidationIssue> Validate(TimelineDocument document, PublishProfile profile)
    {
        var issues = new List<PublishValidationIssue>();
        SceneEditingService.EnsureScenes(document);
        AnimateDocumentProfileService.EnsureSettings(document);

        if (profile.Width <= 0 || profile.Height <= 0)
        {
            issues.Add(Error("Profile", "Publish size must be greater than zero."));
        }

        if (profile.FrameRate <= 0d)
        {
            issues.Add(Error("Profile", "Publish frame rate must be greater than zero."));
        }

        if (!document.Scenes.Any())
        {
            issues.Add(Error("Document", "The document does not contain any scenes."));
            return issues;
        }

        if (!document.Scenes.Any(scene => scene.Layers.Any()))
        {
            issues.Add(Error("Document", "The document does not contain any renderable layers."));
        }

        foreach (var scene in document.Scenes)
        {
            if (scene.FrameLabels.GroupBy(label => label.Name, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            {
                issues.Add(Warning(scene.Name, "Scene contains duplicate frame labels."));
            }

            foreach (var label in scene.FrameLabels.Where(label => !string.IsNullOrWhiteSpace(label.Script)))
            {
                ValidateActionScript(document, scene, label.Script, $"Frame label '{label.Name}'", issues);
            }

            foreach (var layer in scene.Layers)
            {
                ValidateLayer(document, scene, layer, issues);
            }
        }

        issues.AddRange(AnimateDocumentProfileService.ValidateCompatibility(document));

        return issues;
    }

    private static void ValidateLayer(TimelineDocument document, SceneModel scene, TimelineLayer layer, List<PublishValidationIssue> issues)
    {
        if (layer.Media.SourceMediaAssetId is Guid mediaAssetId &&
            document.MediaAssets.All(item => item.Id != mediaAssetId))
        {
            issues.Add(Error(layer.Name, "Layer references a missing media asset."));
        }

        if (layer.SourceLibraryItemId is Guid libraryItemId &&
            document.LibraryItems.All(item => item.Id != libraryItemId))
        {
            issues.Add(Error(layer.Name, "Layer references a missing library item."));
        }

        foreach (var behavior in layer.Behaviors.Where(static behavior => behavior.IsEnabled))
        {
            if (behavior.Trigger == InteractionTriggerKind.FrameLabelReached &&
                string.IsNullOrWhiteSpace(behavior.TriggerArgument))
            {
                issues.Add(Warning(layer.Name, $"Behavior '{behavior.Name}' listens for a frame label but no label was specified."));
            }

            if (behavior.Action == InteractionActionKind.GoToScene &&
                behavior.TargetSceneId is Guid targetSceneId &&
                document.Scenes.All(item => item.Id != targetSceneId))
            {
                issues.Add(Error(layer.Name, $"Behavior '{behavior.Name}' targets a missing scene."));
            }

            if (behavior.Action == InteractionActionKind.GoToFrameLabel &&
                !string.IsNullOrWhiteSpace(behavior.TargetFrameLabel))
            {
                var targetScene = behavior.TargetSceneId is Guid labelTargetSceneId
                    ? document.Scenes.FirstOrDefault(item => item.Id == labelTargetSceneId)
                    : scene;
                if (targetScene is null || targetScene.FrameLabels.All(label => !string.Equals(label.Name, behavior.TargetFrameLabel, StringComparison.OrdinalIgnoreCase)))
                {
                    issues.Add(Warning(layer.Name, $"Behavior '{behavior.Name}' targets a frame label that does not exist."));
                }
            }

            if (behavior.Action is InteractionActionKind.SetLayerVisibility or InteractionActionKind.SetButtonState &&
                behavior.TargetLayerId is Guid targetLayerId &&
                scene.Layers.All(item => item.Id != targetLayerId))
            {
                issues.Add(Warning(layer.Name, $"Behavior '{behavior.Name}' targets a layer that does not exist in the active scene."));
            }

            if (behavior.Action == InteractionActionKind.ApplyVisualState)
            {
                var targetLayer = behavior.TargetLayerId is Guid targetVisualStateLayerId
                    ? scene.Layers.FirstOrDefault(item => item.Id == targetVisualStateLayerId)
                    : layer;
                if (targetLayer is null)
                {
                    issues.Add(Warning(layer.Name, $"Behavior '{behavior.Name}' targets a layer that does not exist in the active scene."));
                }
                else if (targetLayer.VisualStateGroups.All(group => !string.Equals(group.Name, behavior.TargetVisualStateGroup, StringComparison.OrdinalIgnoreCase)))
                {
                    issues.Add(Warning(layer.Name, $"Behavior '{behavior.Name}' targets a visual state group that does not exist."));
                }
                else
                {
                    var targetGroup = targetLayer.VisualStateGroups.First(group => string.Equals(group.Name, behavior.TargetVisualStateGroup, StringComparison.OrdinalIgnoreCase));
                    if (targetGroup.States.All(state => !string.Equals(state.Name, behavior.TargetVisualState, StringComparison.OrdinalIgnoreCase)))
                    {
                        issues.Add(Warning(layer.Name, $"Behavior '{behavior.Name}' targets a visual state that does not exist."));
                    }
                }
            }

            if (behavior.Action == InteractionActionKind.SetVariable &&
                string.IsNullOrWhiteSpace(behavior.VariableName))
            {
                issues.Add(Warning(layer.Name, $"Behavior '{behavior.Name}' sets a variable but no variable name was specified."));
            }

            if (!string.IsNullOrWhiteSpace(behavior.Script))
            {
                ValidateActionScript(document, scene, behavior.Script, $"Behavior '{behavior.Name}'", issues);
            }
        }
    }

    private static void ValidateActionScript(
        TimelineDocument document,
        SceneModel initialScene,
        string script,
        string source,
        List<PublishValidationIssue> issues)
    {
        var parse = ActionScriptService.Parse(script);
        foreach (var error in parse.Errors)
        {
            issues.Add(Error(source, error));
        }

        var activeScene = initialScene;
        foreach (var command in parse.Commands)
        {
            switch (command.Kind)
            {
                case ActionScriptCommandKind.GoToScene:
                    {
                        var targetSceneName = command.Arguments[0];
                        var targetScene = document.Scenes.FirstOrDefault(item => string.Equals(item.Name, targetSceneName, StringComparison.OrdinalIgnoreCase));
                        if (targetScene is null)
                        {
                            issues.Add(Error(source, $"Script line {command.LineNumber} targets missing scene '{targetSceneName}'."));
                        }
                        else
                        {
                            activeScene = targetScene;
                        }
                    }

                    break;
                case ActionScriptCommandKind.GoToLabel:
                    {
                        var targetLabel = command.Arguments[0];
                        if (activeScene.FrameLabels.All(label => !string.Equals(label.Name, targetLabel, StringComparison.OrdinalIgnoreCase)))
                        {
                            issues.Add(Warning(source, $"Script line {command.LineNumber} targets missing frame label '{targetLabel}' in scene '{activeScene.Name}'."));
                        }
                    }

                    break;
                case ActionScriptCommandKind.GoToFrame:
                    {
                        if (!int.TryParse(command.Arguments[0], out var targetFrame) || targetFrame < 1)
                        {
                            issues.Add(Error(source, $"Script line {command.LineNumber} uses an invalid frame number."));
                            break;
                        }

                        var totalFrames = Math.Max(1, (int)Math.Ceiling(activeScene.Duration * activeScene.FrameRate) + 1);
                        if (targetFrame > totalFrames)
                        {
                            issues.Add(Warning(source, $"Script line {command.LineNumber} targets frame {targetFrame}, but scene '{activeScene.Name}' only has {totalFrames} frames."));
                        }
                    }

                    break;
                case ActionScriptCommandKind.ShowLayer:
                case ActionScriptCommandKind.HideLayer:
                case ActionScriptCommandKind.SetText:
                case ActionScriptCommandKind.SetButtonState:
                case ActionScriptCommandKind.SetVisualState:
                    {
                        var targetLayerName = command.Arguments[0];
                        if (!string.Equals(targetLayerName, "this", StringComparison.OrdinalIgnoreCase) &&
                            activeScene.Layers.All(layer => !string.Equals(layer.Name, targetLayerName, StringComparison.OrdinalIgnoreCase)))
                        {
                            issues.Add(Warning(source, $"Script line {command.LineNumber} targets missing layer '{targetLayerName}' in scene '{activeScene.Name}'."));
                        }
                    }

                    if (command.Kind == ActionScriptCommandKind.SetButtonState &&
                        !Enum.TryParse<ButtonVisualState>(command.Arguments[1], ignoreCase: true, out _))
                    {
                        issues.Add(Error(source, $"Script line {command.LineNumber} uses an invalid button state '{command.Arguments[1]}'."));
                    }

                    if (command.Kind == ActionScriptCommandKind.SetVisualState)
                    {
                        var targetLayer = string.Equals(command.Arguments[0], "this", StringComparison.OrdinalIgnoreCase)
                            ? null
                            : activeScene.Layers.FirstOrDefault(layer => string.Equals(layer.Name, command.Arguments[0], StringComparison.OrdinalIgnoreCase));
                        var targetGroupName = command.Arguments[1];
                        var targetStateName = command.Arguments[2];

                        if (targetLayer is not null)
                        {
                            var targetGroup = targetLayer.VisualStateGroups.FirstOrDefault(group => string.Equals(group.Name, targetGroupName, StringComparison.OrdinalIgnoreCase));
                            if (targetGroup is null)
                            {
                                issues.Add(Warning(source, $"Script line {command.LineNumber} targets missing visual state group '{targetGroupName}' on '{targetLayer.Name}'."));
                            }
                            else if (targetGroup.States.All(state => !string.Equals(state.Name, targetStateName, StringComparison.OrdinalIgnoreCase)))
                            {
                                issues.Add(Warning(source, $"Script line {command.LineNumber} targets missing visual state '{targetStateName}' on '{targetLayer.Name}'."));
                            }
                        }
                    }

                    break;
            }
        }
    }

    private static PublishValidationIssue Error(string source, string message) => new()
    {
        Severity = "Error",
        Source = source,
        Message = message
    };

    private static PublishValidationIssue Warning(string source, string message) => new()
    {
        Severity = "Warning",
        Source = source,
        Message = message
    };
}
