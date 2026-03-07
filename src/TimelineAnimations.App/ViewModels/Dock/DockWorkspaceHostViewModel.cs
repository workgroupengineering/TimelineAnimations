using Dock.Model.Controls;
using Dock.Model.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimelineAnimations.App.Models;
using TimelineAnimations.App.Services;

namespace TimelineAnimations.App.ViewModels.Dock;

public partial class DockWorkspaceHostViewModel : ViewModelBase
{
    private readonly DockWorkspaceFactory _factory;
    private readonly DockWorkspacePersistenceService _persistence = new();

    public DockWorkspaceHostViewModel(MainWindowViewModel host)
    {
        Host = host;
        _factory = new DockWorkspaceFactory(host);
        SelectedPreset = DockWorkspacePreset.Animate;
        CurrentPresetLabel = GetPresetLabel(DockWorkspacePreset.Animate);

        var restoredLayout = _persistence.Load(_factory);
        if (IsLayoutCompatible(restoredLayout))
        {
            Layout = restoredLayout;
            CurrentPresetLabel = "Custom";
        }
        else
        {
            Layout = CreateLayout(SelectedPreset);
            SaveLayout();
        }

        RefreshWorkspaceMetadata();
    }

    public MainWindowViewModel Host { get; }

    public IReadOnlyList<DockWorkspacePreset> AvailablePresets { get; } = Enum.GetValues<DockWorkspacePreset>();

    [ObservableProperty]
    private IRootDock? layout;

    [ObservableProperty]
    private DockWorkspacePreset selectedPreset;

    [ObservableProperty]
    private string currentPresetLabel = string.Empty;

    public string WorkspaceOrganizerSummary =>
        $"{CurrentPresetLabel} layout active. Drag any surface tab to split inline, stack tabs, or float monitors and timelines.";

    public string WorkspaceDragHint =>
        "Stage, source, program, frames, and curves are first-class dockables. Overlay uses Dock auto-hide.";

    public bool CanRevealAllSurfaces => DockWorkspaceSurfaceIds.All.Any(IsHiddenOrPinned);

    public string StageSurfaceMenuLabel => BuildSurfaceMenuLabel(DockWorkspaceSurfaceIds.StageDocument);

    public string SourceSurfaceMenuLabel => BuildSurfaceMenuLabel(DockWorkspaceSurfaceIds.SourceMonitorTool);

    public string ProgramSurfaceMenuLabel => BuildSurfaceMenuLabel(DockWorkspaceSurfaceIds.ProgramMonitorTool);

    public string FramesSurfaceMenuLabel => BuildSurfaceMenuLabel(DockWorkspaceSurfaceIds.FramesTimelineTool);

    public string CurvesSurfaceMenuLabel => BuildSurfaceMenuLabel(DockWorkspaceSurfaceIds.CurvesTimelineTool);

    public string ProjectSurfaceMenuLabel => BuildSurfaceMenuLabel(DockWorkspaceSurfaceIds.ProjectTool);

    public string InspectorSurfaceMenuLabel => BuildSurfaceMenuLabel(DockWorkspaceSurfaceIds.InspectorTool);

    public string OutputSurfaceMenuLabel => BuildSurfaceMenuLabel(DockWorkspaceSurfaceIds.OutputTool);

    [RelayCommand]
    private void ApplySelectedPreset()
    {
        ApplyPreset(SelectedPreset);
    }

    [RelayCommand]
    private void ApplyPreset(string? presetName)
    {
        if (Enum.TryParse<DockWorkspacePreset>(presetName, true, out var preset))
        {
            ApplyPreset(preset);
        }
    }

    [RelayCommand]
    private void ResetLayout()
    {
        ApplyPreset(SelectedPreset);
    }

    [RelayCommand]
    private void RestoreAllSurfaces()
    {
        foreach (var surfaceId in DockWorkspaceSurfaceIds.All)
        {
            RestoreSurface(surfaceId, focusSurface: false);
        }

        SaveLayout();
        RefreshWorkspaceMetadata();
        Host.StatusMessage = "All dock surfaces restored.";
    }

    [RelayCommand]
    private void FocusSurface(string? surfaceId)
    {
        if (string.IsNullOrWhiteSpace(surfaceId))
        {
            return;
        }

        var didChangeLayout = IsHiddenOrPinned(surfaceId);
        var dockable = RestoreSurface(surfaceId, focusSurface: true);
        if (dockable is null)
        {
            Host.StatusMessage = $"{DockWorkspaceSurfaceIds.GetLabel(surfaceId)} is unavailable in the current layout.";
            return;
        }

        if (didChangeLayout)
        {
            CurrentPresetLabel = "Custom";
            SaveLayout();
            RefreshWorkspaceMetadata();
        }

        Host.StatusMessage = $"{DockWorkspaceSurfaceIds.GetLabel(surfaceId)} focused.";
    }

    [RelayCommand]
    private void HideSurface(string? surfaceId)
    {
        if (string.IsNullOrWhiteSpace(surfaceId))
        {
            return;
        }

        var dockable = FindSurface(surfaceId);
        if (dockable is null || Layout is null)
        {
            return;
        }

        if (IsHidden(dockable))
        {
            return;
        }

        _factory.HideDockable(dockable);
        CurrentPresetLabel = "Custom";
        SaveLayout();
        RefreshWorkspaceMetadata();
        Host.StatusMessage = $"{DockWorkspaceSurfaceIds.GetLabel(surfaceId)} hidden.";
    }

    [RelayCommand]
    private void OverlaySurface(string? surfaceId)
    {
        if (string.IsNullOrWhiteSpace(surfaceId))
        {
            return;
        }

        if (!DockWorkspaceSurfaceIds.OverlayCapable.Contains(surfaceId, StringComparer.Ordinal))
        {
            Host.StatusMessage = $"{DockWorkspaceSurfaceIds.GetLabel(surfaceId)} remains docked as a primary editing surface.";
            return;
        }

        var dockable = RestoreSurface(surfaceId, focusSurface: false);
        if (dockable is null || Layout is null)
        {
            return;
        }

        if (_factory.IsDockablePinned(dockable, Layout))
        {
            return;
        }

        _factory.PinDockable(dockable);
        CurrentPresetLabel = "Custom";
        SaveLayout();
        RefreshWorkspaceMetadata();
        Host.StatusMessage = $"{DockWorkspaceSurfaceIds.GetLabel(surfaceId)} moved to overlay.";
    }

    public void SaveLayout()
    {
        _persistence.Save(Layout);
    }

    partial void OnLayoutChanged(IRootDock? value)
    {
        RefreshWorkspaceMetadata();
    }

    partial void OnCurrentPresetLabelChanged(string value)
    {
        RefreshWorkspaceMetadata();
    }

    private void ApplyPreset(DockWorkspacePreset preset)
    {
        Layout = CreateLayout(preset);
        SelectedPreset = preset;
        CurrentPresetLabel = GetPresetLabel(preset);
        SaveLayout();
        RefreshWorkspaceMetadata();
        Host.StatusMessage = $"{CurrentPresetLabel} workspace applied.";
    }

    private IRootDock CreateLayout(DockWorkspacePreset preset)
    {
        var root = _factory.CreateLayout(preset);
        _factory.InitLayout(root);
        return root;
    }

    private static string GetPresetLabel(DockWorkspacePreset preset)
    {
        return preset switch
        {
            DockWorkspacePreset.StageFocus => "Stage Focus",
            _ => preset.ToString()
        };
    }

    private bool IsLayoutCompatible(IRootDock? root)
    {
        if (root is null)
        {
            return false;
        }

        foreach (var surfaceId in DockWorkspaceSurfaceIds.All)
        {
            if (FindSurface(root, surfaceId) is null)
            {
                return false;
            }
        }

        return true;
    }

    private IDockable? RestoreSurface(string surfaceId, bool focusSurface)
    {
        if (Layout is null)
        {
            return null;
        }

        var dockable = FindSurface(surfaceId);
        if (dockable is null)
        {
            return null;
        }

        if (IsHidden(dockable))
        {
            _factory.RestoreDockable(dockable);
            dockable = FindSurface(surfaceId);
        }

        if (dockable is null)
        {
            return null;
        }

        if (_factory.IsDockablePinned(dockable, Layout))
        {
            _factory.UnpinDockable(dockable);
            dockable = FindSurface(surfaceId);
        }

        if (dockable is not null && focusSurface)
        {
            ActivateDockableHierarchy(dockable);
        }

        return dockable;
    }

    private void ActivateDockableHierarchy(IDockable dockable)
    {
        IDockable? current = dockable;
        while (current is not null)
        {
            _factory.SetActiveDockable(current);
            current = current.Owner as IDockable;
        }
    }

    private bool IsHiddenOrPinned(string surfaceId)
    {
        var dockable = FindSurface(surfaceId);
        if (dockable is null)
        {
            return false;
        }

        return IsHidden(dockable) || (Layout is not null && _factory.IsDockablePinned(dockable, Layout));
    }

    private bool IsHidden(IDockable dockable)
    {
        return Layout?.HiddenDockables?.Contains(dockable) == true;
    }

    private string BuildSurfaceMenuLabel(string surfaceId)
    {
        return $"{DockWorkspaceSurfaceIds.GetLabel(surfaceId)} • {GetSurfaceStateLabel(surfaceId)}";
    }

    private string GetSurfaceStateLabel(string surfaceId)
    {
        if (Layout is null)
        {
            return "Missing";
        }

        var dockable = FindSurface(surfaceId);
        if (dockable is null)
        {
            return "Missing";
        }

        if (IsHidden(dockable))
        {
            return "Hidden";
        }

        if (_factory.IsDockablePinned(dockable, Layout))
        {
            return "Overlay";
        }

        return "Docked";
    }

    private IDockable? FindSurface(string surfaceId) => FindSurface(Layout, surfaceId);

    private static IDockable? FindSurface(IRootDock? root, string surfaceId)
    {
        return EnumerateDockables(root).FirstOrDefault(dockable => string.Equals(dockable.Id, surfaceId, StringComparison.Ordinal));
    }

    private static IEnumerable<IDockable> EnumerateDockables(IDockable? dockable)
    {
        if (dockable is null)
        {
            yield break;
        }

        yield return dockable;

        if (dockable is IDock dock && dock.VisibleDockables is { Count: > 0 } visibleDockables)
        {
            foreach (var child in visibleDockables)
            {
                foreach (var nested in EnumerateDockables(child))
                {
                    yield return nested;
                }
            }
        }

        if (dockable is IRootDock rootDock)
        {
            foreach (var child in EnumerateDockableCollection(rootDock.HiddenDockables))
            {
                yield return child;
            }

            foreach (var child in EnumerateDockableCollection(rootDock.LeftPinnedDockables))
            {
                yield return child;
            }

            foreach (var child in EnumerateDockableCollection(rootDock.RightPinnedDockables))
            {
                yield return child;
            }

            foreach (var child in EnumerateDockableCollection(rootDock.TopPinnedDockables))
            {
                yield return child;
            }

            foreach (var child in EnumerateDockableCollection(rootDock.BottomPinnedDockables))
            {
                yield return child;
            }

            if (rootDock.Windows is { Count: > 0 } windows)
            {
                foreach (var window in windows)
                {
                    if (window.Layout is { } windowLayout)
                    {
                        foreach (var child in EnumerateDockables(windowLayout))
                        {
                            yield return child;
                        }
                    }
                }
            }
        }
    }

    private static IEnumerable<IDockable> EnumerateDockableCollection(IEnumerable<IDockable>? dockables)
    {
        if (dockables is null)
        {
            yield break;
        }

        foreach (var dockable in dockables)
        {
            foreach (var nested in EnumerateDockables(dockable))
            {
                yield return nested;
            }
        }
    }

    private void RefreshWorkspaceMetadata()
    {
        OnPropertyChanged(nameof(WorkspaceOrganizerSummary));
        OnPropertyChanged(nameof(WorkspaceDragHint));
        OnPropertyChanged(nameof(CanRevealAllSurfaces));
        OnPropertyChanged(nameof(StageSurfaceMenuLabel));
        OnPropertyChanged(nameof(SourceSurfaceMenuLabel));
        OnPropertyChanged(nameof(ProgramSurfaceMenuLabel));
        OnPropertyChanged(nameof(FramesSurfaceMenuLabel));
        OnPropertyChanged(nameof(CurvesSurfaceMenuLabel));
        OnPropertyChanged(nameof(ProjectSurfaceMenuLabel));
        OnPropertyChanged(nameof(InspectorSurfaceMenuLabel));
        OnPropertyChanged(nameof(OutputSurfaceMenuLabel));
    }
}
