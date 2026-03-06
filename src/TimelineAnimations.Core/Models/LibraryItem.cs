namespace TimelineAnimations.Core.Models;

public sealed class LibraryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "New Symbol";

    public string FolderPath { get; set; } = string.Empty;

    public string LinkageId { get; set; } = string.Empty;

    public string SourceAssetPath { get; set; } = string.Empty;

    public SymbolKind SymbolKind { get; set; } = SymbolKind.Graphic;

    public double Duration { get; set; } = 2;

    public double FrameRate { get; set; } = 24;

    public bool IsComponent { get; set; }

    public string ComponentCategory { get; set; } = string.Empty;

    public string ComponentDescription { get; set; } = string.Empty;

    public TimelineLayer Template { get; set; } = new();

    public List<TimelineLayer> Layers { get; set; } = [];

    public List<SymbolButtonStateModel> ButtonStates { get; set; } = [];

    public List<InteractionBehaviorModel> DefaultBehaviors { get; set; } = [];
}
