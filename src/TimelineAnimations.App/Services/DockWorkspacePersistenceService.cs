using System.Collections.ObjectModel;
using Dock.Model;
using Dock.Model.Controls;
using Dock.Serializer;
using TimelineAnimations.App.ViewModels.Dock;

namespace TimelineAnimations.App.Services;

public sealed class DockWorkspacePersistenceService
{
    private readonly DockSerializer _serializer = new(typeof(ObservableCollection<>));

    private static string LayoutFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TimelineAnimations",
            "dock-layout.json");

    public IRootDock? Load(DockWorkspaceFactory factory)
    {
        if (!File.Exists(LayoutFilePath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(LayoutFilePath);
            var layout = _serializer.Load<IRootDock?>(stream);
            if (layout is null)
            {
                return null;
            }

            new DockState().Restore(layout);
            factory.RebindHost(layout);
            factory.InitLayout(layout);
            return layout;
        }
        catch
        {
            return null;
        }
    }

    public void Save(IRootDock? layout)
    {
        if (layout is null)
        {
            return;
        }

        var directory = Path.GetDirectoryName(LayoutFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(LayoutFilePath);
        _serializer.Save(stream, layout);
    }
}

