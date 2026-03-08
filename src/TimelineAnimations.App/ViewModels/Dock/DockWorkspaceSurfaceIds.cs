namespace TimelineAnimations.App.ViewModels.Dock;

public static class DockWorkspaceSurfaceIds
{
    public const string ProjectTool = "ProjectTool";
    public const string LibraryTool = "LibraryTool";
    public const string ComponentsTool = "ComponentsTool";
    public const string MovieExplorerTool = "MovieExplorerTool";
    public const string InspectorTool = "InspectorTool";
    public const string ColorTool = "ColorTool";
    public const string SwatchesTool = "SwatchesTool";
    public const string AlignTool = "AlignTool";
    public const string TransformTool = "TransformTool";
    public const string InfoTool = "InfoTool";
    public const string HistoryTool = "HistoryTool";
    public const string OutputTool = "OutputTool";
    public const string StageDocument = "StageDocument";
    public const string SourceMonitorTool = "SourceMonitorTool";
    public const string ProgramMonitorTool = "ProgramMonitorTool";
    public const string FramesTimelineTool = "FramesTimelineTool";
    public const string CurvesTimelineTool = "CurvesTimelineTool";
    public const string MotionPresetsTool = "MotionPresetsTool";
    public const string CodeSnippetsTool = "CodeSnippetsTool";

    public static IReadOnlyList<string> All { get; } =
    [
        ProjectTool,
        LibraryTool,
        ComponentsTool,
        MovieExplorerTool,
        InspectorTool,
        ColorTool,
        SwatchesTool,
        AlignTool,
        TransformTool,
        InfoTool,
        HistoryTool,
        OutputTool,
        StageDocument,
        SourceMonitorTool,
        ProgramMonitorTool,
        FramesTimelineTool,
        CurvesTimelineTool,
        MotionPresetsTool,
        CodeSnippetsTool
    ];

    public static IReadOnlyList<string> OverlayCapable { get; } =
    [
        ProjectTool,
        LibraryTool,
        ComponentsTool,
        MovieExplorerTool,
        InspectorTool,
        ColorTool,
        SwatchesTool,
        AlignTool,
        TransformTool,
        InfoTool,
        HistoryTool,
        OutputTool,
        SourceMonitorTool,
        ProgramMonitorTool,
        FramesTimelineTool,
        CurvesTimelineTool,
        MotionPresetsTool,
        CodeSnippetsTool
    ];

    public static string GetLabel(string surfaceId)
    {
        return surfaceId switch
        {
            ProjectTool => "Project",
            LibraryTool => "Library",
            ComponentsTool => "Components",
            MovieExplorerTool => "Movie Explorer",
            InspectorTool => "Inspector",
            ColorTool => "Color",
            SwatchesTool => "Swatches",
            AlignTool => "Align",
            TransformTool => "Transform",
            InfoTool => "Info",
            HistoryTool => "History",
            OutputTool => "Output",
            StageDocument => "Stage",
            SourceMonitorTool => "Source",
            ProgramMonitorTool => "Program",
            FramesTimelineTool => "Frames",
            CurvesTimelineTool => "Curves",
            MotionPresetsTool => "Motion Presets",
            CodeSnippetsTool => "Code Snippets",
            _ => surfaceId
        };
    }
}
