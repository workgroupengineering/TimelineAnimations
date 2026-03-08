using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.ViewModels;

public sealed class FrameLabelViewModel
{
    public FrameLabelViewModel(FrameLabelModel model)
    {
        Model = model;
    }

    public FrameLabelModel Model { get; }

    public Guid Id => Model.Id;

    public int Frame => Model.Frame;

    public string Name => Model.Name;

    public string Subtitle => ActionScriptService.GetSummary(Model.Script);
}
