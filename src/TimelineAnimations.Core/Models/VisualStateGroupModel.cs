namespace TimelineAnimations.Core.Models;

public sealed class VisualStateGroupModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "CommonStates";

    public string ActiveStateName { get; set; } = string.Empty;

    public List<VisualStateModel> States { get; set; } = [];
}
