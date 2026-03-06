using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.ViewModels;

public sealed class InteractionBehaviorViewModel : ViewModelBase
{
    public InteractionBehaviorViewModel(InteractionBehaviorModel model)
    {
        Model = model;
    }

    public InteractionBehaviorModel Model { get; }

    public Guid Id => Model.Id;

    public string Name => string.IsNullOrWhiteSpace(Model.Name) ? "Behavior" : Model.Name;

    public string TriggerLabel => InteractionBehaviorService.GetTriggerLabel(Model);

    public string ActionLabel => InteractionBehaviorService.GetActionLabel(Model);

    public string Summary => InteractionBehaviorService.GetSummary(Model);

    public bool IsEnabled => Model.IsEnabled;

    public void RefreshMetadata()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(TriggerLabel));
        OnPropertyChanged(nameof(ActionLabel));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(IsEnabled));
    }
}
