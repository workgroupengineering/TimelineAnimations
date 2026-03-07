namespace TimelineAnimations.Core.Models;

public sealed class InteractionBehaviorModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "New Behavior";

    public bool IsEnabled { get; set; } = true;

    public InteractionTriggerKind Trigger { get; set; } = InteractionTriggerKind.PointerClick;

    public string TriggerArgument { get; set; } = string.Empty;

    public InteractionActionKind Action { get; set; } = InteractionActionKind.Play;

    public Guid? TargetSceneId { get; set; }

    public string TargetFrameLabel { get; set; } = string.Empty;

    public Guid? TargetLayerId { get; set; }

    public ButtonVisualState TargetButtonState { get; set; } = ButtonVisualState.Up;

    public bool BoolValue { get; set; } = true;

    public string VariableName { get; set; } = string.Empty;

    public string VariableValue { get; set; } = string.Empty;

    public string TargetVisualStateGroup { get; set; } = string.Empty;

    public string TargetVisualState { get; set; } = string.Empty;

    public string Script { get; set; } = string.Empty;

    public InteractionBehaviorModel Clone()
    {
        return new InteractionBehaviorModel
        {
            Name = Name,
            IsEnabled = IsEnabled,
            Trigger = Trigger,
            TriggerArgument = TriggerArgument,
            Action = Action,
            TargetSceneId = TargetSceneId,
            TargetFrameLabel = TargetFrameLabel,
            TargetLayerId = TargetLayerId,
            TargetButtonState = TargetButtonState,
            BoolValue = BoolValue,
            VariableName = VariableName,
            VariableValue = VariableValue,
            TargetVisualStateGroup = TargetVisualStateGroup,
            TargetVisualState = TargetVisualState,
            Script = Script
        };
    }
}
