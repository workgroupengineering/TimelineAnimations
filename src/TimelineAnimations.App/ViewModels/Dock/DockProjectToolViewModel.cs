using Dock.Model.Mvvm.Controls;
using Newtonsoft.Json;

namespace TimelineAnimations.App.ViewModels.Dock;

public class DockProjectToolViewModel : Tool, IMainWindowHostDockable
{
    [JsonIgnore]
    public MainWindowViewModel? Host { get; set; }
}

