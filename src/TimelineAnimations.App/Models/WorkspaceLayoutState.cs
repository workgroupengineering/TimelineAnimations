namespace TimelineAnimations.App.Models;

public sealed class WorkspaceLayoutState
{
    public WorkspaceLayoutPreset SelectedPreset { get; set; } = WorkspaceLayoutPreset.Classic;

    public WorkspacePanelMode LeftPanelMode { get; set; } = WorkspacePanelMode.Docked;

    public WorkspacePanelMode RightPanelMode { get; set; } = WorkspacePanelMode.Docked;

    public WorkspacePanelMode TimelinePanelMode { get; set; } = WorkspacePanelMode.Docked;

    public WorkspacePanelMode LeftRestoreMode { get; set; } = WorkspacePanelMode.Docked;

    public WorkspacePanelMode RightRestoreMode { get; set; } = WorkspacePanelMode.Docked;

    public WorkspacePanelMode TimelineRestoreMode { get; set; } = WorkspacePanelMode.Docked;

    public double LeftDockWidth { get; set; } = 236;

    public double RightDockWidth { get; set; } = 296;

    public double TimelineDockHeight { get; set; } = 304;
}
