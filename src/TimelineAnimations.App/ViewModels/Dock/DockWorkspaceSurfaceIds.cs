namespace TimelineAnimations.App.ViewModels.Dock;

public static class DockWorkspaceSurfaceIds
{
    public const string ProjectTool = "ProjectTool";
    public const string InspectorTool = "InspectorTool";
    public const string OutputTool = "OutputTool";
    public const string StageDocument = "StageDocument";
    public const string SourceMonitorTool = "SourceMonitorTool";
    public const string ProgramMonitorTool = "ProgramMonitorTool";
    public const string FramesTimelineTool = "FramesTimelineTool";
    public const string CurvesTimelineTool = "CurvesTimelineTool";

    public static IReadOnlyList<string> All { get; } =
    [
        ProjectTool,
        InspectorTool,
        OutputTool,
        StageDocument,
        SourceMonitorTool,
        ProgramMonitorTool,
        FramesTimelineTool,
        CurvesTimelineTool
    ];

    public static IReadOnlyList<string> OverlayCapable { get; } =
    [
        ProjectTool,
        InspectorTool,
        OutputTool,
        SourceMonitorTool,
        ProgramMonitorTool,
        FramesTimelineTool,
        CurvesTimelineTool
    ];

    public static string GetLabel(string surfaceId)
    {
        return surfaceId switch
        {
            ProjectTool => "Project",
            InspectorTool => "Inspector",
            OutputTool => "Output",
            StageDocument => "Stage",
            SourceMonitorTool => "Source",
            ProgramMonitorTool => "Program",
            FramesTimelineTool => "Frames",
            CurvesTimelineTool => "Curves",
            _ => surfaceId
        };
    }
}
