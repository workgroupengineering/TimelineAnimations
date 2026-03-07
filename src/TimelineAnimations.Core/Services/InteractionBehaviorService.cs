using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class InteractionBehaviorService
{
    public static InteractionBehaviorModel CreateBehavior(string name = "New Behavior")
    {
        return new InteractionBehaviorModel
        {
            Name = name
        };
    }

    public static List<InteractionBehaviorModel> CloneBehaviors(IEnumerable<InteractionBehaviorModel> behaviors)
    {
        return [.. behaviors.Select(static behavior => behavior.Clone())];
    }

    public static bool MatchesTrigger(InteractionBehaviorModel behavior, InteractionTriggerKind trigger, string? argument = null)
    {
        if (!behavior.IsEnabled || behavior.Trigger != trigger)
        {
            return false;
        }

        if (trigger != InteractionTriggerKind.FrameLabelReached)
        {
            return true;
        }

        return string.Equals(
            behavior.TriggerArgument?.Trim(),
            argument?.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    public static string GetTriggerLabel(InteractionBehaviorModel behavior)
    {
        return behavior.Trigger switch
        {
            InteractionTriggerKind.PointerClick => "Click",
            InteractionTriggerKind.PointerEnter => "Pointer Enter",
            InteractionTriggerKind.PointerLeave => "Pointer Leave",
            InteractionTriggerKind.PointerDown => "Pointer Down",
            InteractionTriggerKind.PointerUp => "Pointer Up",
            InteractionTriggerKind.FrameLabelReached => string.IsNullOrWhiteSpace(behavior.TriggerArgument)
                ? "Frame Label"
                : $"Label: {behavior.TriggerArgument}",
            _ => behavior.Trigger.ToString()
        };
    }

    public static string GetActionLabel(InteractionBehaviorModel behavior)
    {
        return behavior.Action switch
        {
            InteractionActionKind.Play => "Play Timeline",
            InteractionActionKind.Stop => "Stop Timeline",
            InteractionActionKind.GoToScene => "Go To Scene",
            InteractionActionKind.GoToFrameLabel => string.IsNullOrWhiteSpace(behavior.TargetFrameLabel)
                ? "Go To Label"
                : $"Go To {behavior.TargetFrameLabel}",
            InteractionActionKind.SetLayerVisibility => behavior.BoolValue ? "Show Layer" : "Hide Layer",
            InteractionActionKind.SetButtonState => $"Set Button {behavior.TargetButtonState}",
            InteractionActionKind.ApplyVisualState => string.IsNullOrWhiteSpace(behavior.TargetVisualState)
                ? "Apply Visual State"
                : $"Apply State {behavior.TargetVisualState}",
            InteractionActionKind.SetVariable => string.IsNullOrWhiteSpace(behavior.VariableName)
                ? "Set Variable"
                : $"Set {behavior.VariableName}",
            _ => behavior.Action.ToString()
        };
    }

    public static string GetSummary(InteractionBehaviorModel behavior)
    {
        var summary = $"{GetTriggerLabel(behavior)} -> {GetActionLabel(behavior)}";
        if (!string.IsNullOrWhiteSpace(behavior.Script))
        {
            summary = $"{summary} • {ActionScriptService.GetSummary(behavior.Script)}";
        }

        return summary;
    }
}
