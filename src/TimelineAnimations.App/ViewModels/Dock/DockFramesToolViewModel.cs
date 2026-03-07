using Dock.Model.Mvvm.Controls;
using Newtonsoft.Json;

namespace TimelineAnimations.App.ViewModels.Dock;

public class DockFramesToolViewModel : Tool, IMainWindowHostDockable
{
    [JsonIgnore]
    public MainWindowViewModel? Host { get; set; }
}
