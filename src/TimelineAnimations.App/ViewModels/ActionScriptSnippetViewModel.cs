using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public sealed class ActionScriptSnippetViewModel
{
    public ActionScriptSnippetViewModel(ActionScriptSnippet model)
    {
        Model = model;
    }

    public ActionScriptSnippet Model { get; }

    public string Name => Model.Name;

    public string Summary => Model.Summary;

    public string CategoryLabel => Model.Category switch
    {
        ActionScriptSnippetCategory.Playback => "Playback",
        ActionScriptSnippetCategory.Navigation => "Navigation",
        ActionScriptSnippetCategory.Layers => "Layers",
        ActionScriptSnippetCategory.TextAndState => "Text/State",
        ActionScriptSnippetCategory.Diagnostics => "Diagnostics",
        _ => Model.Category.ToString()
    };

    public string Script => Model.Script;
}
