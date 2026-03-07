namespace TimelineAnimations.Core.Models;

public sealed class SceneTransitionModel
{
    public SceneTransitionKind Kind { get; set; } = SceneTransitionKind.None;

    public double Duration { get; set; } = 0.35d;

    public string AccentColor { get; set; } = "#000000";

    public SceneTransitionModel Clone()
    {
        return new SceneTransitionModel
        {
            Kind = Kind,
            Duration = Duration,
            AccentColor = AccentColor
        };
    }
}
