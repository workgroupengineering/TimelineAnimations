namespace TimelineAnimations.Core.Models;

public sealed class ComponentParameterDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "parameter";

    public ComponentParameterKind Kind { get; set; } = ComponentParameterKind.String;

    public ComponentParameterBindingTarget BindingTarget { get; set; } = ComponentParameterBindingTarget.Text;

    public string TargetLayerName { get; set; } = string.Empty;

    public string DefaultValue { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
