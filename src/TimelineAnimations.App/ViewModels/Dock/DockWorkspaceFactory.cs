using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using TimelineAnimations.App.Models;

namespace TimelineAnimations.App.ViewModels.Dock;

public sealed class DockWorkspaceFactory : Factory
{
    private readonly MainWindowViewModel _host;

    public DockWorkspaceFactory(MainWindowViewModel host)
    {
        _host = host;
        DefaultHostWindowLocator = static () => new HostWindow();
    }

    public override IRootDock CreateLayout() => CreateLayout(DockWorkspacePreset.Animate);

    public IRootDock CreateLayout(DockWorkspacePreset preset)
    {
        return preset switch
        {
            DockWorkspacePreset.Editorial => CreateEditorialLayout(),
            DockWorkspacePreset.Review => CreateReviewLayout(),
            DockWorkspacePreset.StageFocus => CreateStageFocusLayout(),
            _ => CreateAnimateLayout()
        };
    }

    private IRootDock CreateAnimateLayout()
    {
        var surfaces = CreateSurfaces();
        var leftDock = CreateToolDock("AnimateLeftTools", Alignment.Left, 0.19d, surfaces.Project, surfaces.Project);
        var rightDock = CreateToolDock("AnimateRightPanels", Alignment.Right, 0.21d, surfaces.Inspector, surfaces.Inspector, surfaces.Output);
        var monitorDock = CreateToolDock("AnimateMonitorTabs", Alignment.Right, 0.26d, surfaces.Source, surfaces.Source, surfaces.Program);
        var timelineDock = CreateToolDock("AnimateTimelineTabs", Alignment.Bottom, 0.34d, surfaces.Frames, surfaces.Frames, surfaces.Curves);
        var stageDock = CreateDocumentDock("AnimateStageDocuments", 0.74d, surfaces.Stage, surfaces.Stage);

        var upperWorkspace = new ProportionalDock
        {
            Id = "AnimateUpperWorkspace",
            Orientation = Orientation.Horizontal,
            Proportion = 0.66d,
            VisibleDockables = CreateList<IDockable>(stageDock, CreateSplitter(), monitorDock)
        };

        var centerDock = new ProportionalDock
        {
            Id = "AnimateCenterWorkspace",
            Orientation = Orientation.Vertical,
            VisibleDockables = CreateList<IDockable>(upperWorkspace, CreateSplitter(), timelineDock)
        };

        return CreateRoot("AnimateWorkspace", leftDock, centerDock, rightDock);
    }

    private IRootDock CreateEditorialLayout()
    {
        var surfaces = CreateSurfaces();
        var leftDock = CreateToolDock("EditorialLeftTools", Alignment.Left, 0.18d, surfaces.Project, surfaces.Project);
        var rightDock = CreateToolDock("EditorialRightPanels", Alignment.Right, 0.20d, surfaces.Inspector, surfaces.Inspector, surfaces.Output);
        var stageDock = CreateDocumentDock("EditorialStageDocuments", 0.60d, surfaces.Stage, surfaces.Stage);
        var sourceDock = CreateToolDock("EditorialSourceDock", Alignment.Right, 0.50d, surfaces.Source, surfaces.Source);
        var programDock = CreateToolDock("EditorialProgramDock", Alignment.Right, 0.50d, surfaces.Program, surfaces.Program);
        var framesDock = CreateToolDock("EditorialFramesDock", Alignment.Bottom, 0.64d, surfaces.Frames, surfaces.Frames);
        var curvesDock = CreateToolDock("EditorialCurvesDock", Alignment.Bottom, 0.36d, surfaces.Curves, surfaces.Curves);

        var monitorStack = new ProportionalDock
        {
            Id = "EditorialMonitorStack",
            Orientation = Orientation.Vertical,
            Proportion = 0.40d,
            VisibleDockables = CreateList<IDockable>(sourceDock, CreateSplitter(), programDock)
        };

        var upperWorkspace = new ProportionalDock
        {
            Id = "EditorialUpperWorkspace",
            Orientation = Orientation.Horizontal,
            Proportion = 0.60d,
            VisibleDockables = CreateList<IDockable>(stageDock, CreateSplitter(), monitorStack)
        };

        var timelineWorkspace = new ProportionalDock
        {
            Id = "EditorialTimelineWorkspace",
            Orientation = Orientation.Horizontal,
            Proportion = 0.40d,
            VisibleDockables = CreateList<IDockable>(framesDock, CreateSplitter(), curvesDock)
        };

        var centerDock = new ProportionalDock
        {
            Id = "EditorialCenterWorkspace",
            Orientation = Orientation.Vertical,
            VisibleDockables = CreateList<IDockable>(upperWorkspace, CreateSplitter(), timelineWorkspace)
        };

        return CreateRoot("EditorialWorkspace", leftDock, centerDock, rightDock);
    }

    private IRootDock CreateReviewLayout()
    {
        var surfaces = CreateSurfaces();
        var leftDock = CreateToolDock("ReviewLeftTools", Alignment.Left, 0.16d, surfaces.Project, surfaces.Project);
        var rightDock = CreateToolDock("ReviewRightPanels", Alignment.Right, 0.18d, surfaces.Output, surfaces.Output, surfaces.Inspector);
        var sourceDock = CreateToolDock("ReviewSourceDock", Alignment.Left, 0.26d, surfaces.Source, surfaces.Source);
        var programDock = CreateToolDock("ReviewProgramDock", Alignment.Right, 0.48d, surfaces.Program, surfaces.Program);
        var stageDock = CreateDocumentDock("ReviewStageDocuments", 0.26d, surfaces.Stage, surfaces.Stage);
        var timelineDock = CreateToolDock("ReviewTimelineTabs", Alignment.Bottom, 0.32d, surfaces.Frames, surfaces.Frames, surfaces.Curves);

        var upperWorkspace = new ProportionalDock
        {
            Id = "ReviewUpperWorkspace",
            Orientation = Orientation.Horizontal,
            Proportion = 0.68d,
            VisibleDockables = CreateList<IDockable>(sourceDock, CreateSplitter(), programDock, CreateSplitter(), stageDock)
        };

        var centerDock = new ProportionalDock
        {
            Id = "ReviewCenterWorkspace",
            Orientation = Orientation.Vertical,
            VisibleDockables = CreateList<IDockable>(upperWorkspace, CreateSplitter(), timelineDock)
        };

        return CreateRoot("ReviewWorkspace", leftDock, centerDock, rightDock);
    }

    private IRootDock CreateStageFocusLayout()
    {
        var surfaces = CreateSurfaces();
        var leftDock = CreateToolDock("StageFocusLeftTools", Alignment.Left, 0.17d, surfaces.Project, surfaces.Project);
        var rightDock = CreateToolDock("StageFocusRightPanels", Alignment.Right, 0.19d, surfaces.Inspector, surfaces.Inspector, surfaces.Output);
        var stageDock = CreateDocumentDock("StageFocusStageDocuments", 0.78d, surfaces.Stage, surfaces.Stage);
        var monitorDock = CreateToolDock("StageFocusMonitorTabs", Alignment.Right, 0.22d, surfaces.Program, surfaces.Program, surfaces.Source);
        var timelineDock = CreateToolDock("StageFocusTimelineTabs", Alignment.Bottom, 0.28d, surfaces.Frames, surfaces.Frames, surfaces.Curves);

        var upperWorkspace = new ProportionalDock
        {
            Id = "StageFocusUpperWorkspace",
            Orientation = Orientation.Horizontal,
            Proportion = 0.72d,
            VisibleDockables = CreateList<IDockable>(stageDock, CreateSplitter(), monitorDock)
        };

        var centerDock = new ProportionalDock
        {
            Id = "StageFocusCenterWorkspace",
            Orientation = Orientation.Vertical,
            VisibleDockables = CreateList<IDockable>(upperWorkspace, CreateSplitter(), timelineDock)
        };

        return CreateRoot("StageFocusWorkspace", leftDock, centerDock, rightDock);
    }

    private SurfaceSet CreateSurfaces()
    {
        return new SurfaceSet(
            new DockProjectToolViewModel
            {
                Id = DockWorkspaceSurfaceIds.ProjectTool,
                Title = "Project",
                Host = _host,
                CanClose = false
            },
            new DockInspectorToolViewModel
            {
                Id = DockWorkspaceSurfaceIds.InspectorTool,
                Title = "Inspector",
                Host = _host,
                CanClose = false
            },
            new DockOutputToolViewModel
            {
                Id = DockWorkspaceSurfaceIds.OutputTool,
                Title = "Output",
                Host = _host,
                CanClose = false
            },
            new DockStageDocumentViewModel
            {
                Id = DockWorkspaceSurfaceIds.StageDocument,
                Title = "Stage",
                Host = _host,
                CanClose = false
            },
            new DockSourceMonitorToolViewModel
            {
                Id = DockWorkspaceSurfaceIds.SourceMonitorTool,
                Title = "Source",
                Host = _host,
                CanClose = false
            },
            new DockProgramMonitorToolViewModel
            {
                Id = DockWorkspaceSurfaceIds.ProgramMonitorTool,
                Title = "Program",
                Host = _host,
                CanClose = false
            },
            new DockFramesToolViewModel
            {
                Id = DockWorkspaceSurfaceIds.FramesTimelineTool,
                Title = "Frames",
                Host = _host,
                CanClose = false
            },
            new DockCurvesToolViewModel
            {
                Id = DockWorkspaceSurfaceIds.CurvesTimelineTool,
                Title = "Curves",
                Host = _host,
                CanClose = false
            });
    }

    private ToolDock CreateToolDock(
        string id,
        Alignment alignment,
        double proportion,
        IDockable activeDockable,
        params IDockable[] dockables)
    {
        return new ToolDock
        {
            Id = id,
            Alignment = alignment,
            Proportion = proportion,
            ActiveDockable = activeDockable,
            VisibleDockables = CreateList<IDockable>(dockables)
        };
    }

    private DocumentDock CreateDocumentDock(
        string id,
        double proportion,
        IDockable activeDockable,
        params IDockable[] dockables)
    {
        return new DocumentDock
        {
            Id = id,
            Proportion = proportion,
            ActiveDockable = activeDockable,
            VisibleDockables = CreateList<IDockable>(dockables),
            CanCreateDocument = false,
            IsCollapsable = false
        };
    }

    private IRootDock CreateRoot(
        string workspaceId,
        IDockable leftDock,
        IDockable centerDock,
        IDockable rightDock)
    {
        var workspace = new ProportionalDock
        {
            Id = workspaceId,
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(leftDock, CreateSplitter(), centerDock, CreateSplitter(), rightDock)
        };

        var root = CreateRootDock();
        root.Id = "Root";
        root.IsCollapsable = false;
        root.ActiveDockable = workspace;
        root.DefaultDockable = workspace;
        root.VisibleDockables = CreateList<IDockable>(workspace);
        root.HiddenDockables = CreateList<IDockable>();
        root.LeftPinnedDockables = CreateList<IDockable>();
        root.RightPinnedDockables = CreateList<IDockable>();
        root.TopPinnedDockables = CreateList<IDockable>();
        root.BottomPinnedDockables = CreateList<IDockable>();
        return root;
    }

    private static ProportionalDockSplitter CreateSplitter()
    {
        return new ProportionalDockSplitter
        {
            CanResize = true,
            ResizePreview = true
        };
    }

    public override void InitLayout(IDockable layout)
    {
        RebindHost(layout);
        base.InitLayout(layout);
    }

    public void RebindHost(IDockable dockable)
    {
        if (dockable is IMainWindowHostDockable hostDockable)
        {
            hostDockable.Host = _host;
        }

        if (dockable is IDock dock && dock.VisibleDockables is { Count: > 0 } visibleDockables)
        {
            foreach (var child in visibleDockables)
            {
                if (child is not null)
                {
                    RebindHost(child);
                }
            }
        }

        if (dockable is IRootDock rootDock)
        {
            RebindDockables(rootDock.HiddenDockables);
            RebindDockables(rootDock.LeftPinnedDockables);
            RebindDockables(rootDock.RightPinnedDockables);
            RebindDockables(rootDock.TopPinnedDockables);
            RebindDockables(rootDock.BottomPinnedDockables);

            if (rootDock.Windows is { Count: > 0 } windows)
            {
                foreach (var window in windows)
                {
                    if (window.Layout is { } layout)
                    {
                        RebindHost(layout);
                    }
                }
            }
        }
    }

    private void RebindDockables(IEnumerable<IDockable>? dockables)
    {
        if (dockables is null)
        {
            return;
        }

        foreach (var dockable in dockables)
        {
            if (dockable is not null)
            {
                RebindHost(dockable);
            }
        }
    }

    private sealed record SurfaceSet(
        DockProjectToolViewModel Project,
        DockInspectorToolViewModel Inspector,
        DockOutputToolViewModel Output,
        DockStageDocumentViewModel Stage,
        DockSourceMonitorToolViewModel Source,
        DockProgramMonitorToolViewModel Program,
        DockFramesToolViewModel Frames,
        DockCurvesToolViewModel Curves);
}
