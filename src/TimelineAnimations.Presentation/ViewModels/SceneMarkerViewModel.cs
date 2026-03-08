using Avalonia.Media;
using TimelineAnimations.App.Helpers;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public sealed class SceneMarkerViewModel
{
    public SceneMarkerViewModel(SceneMarkerModel model)
    {
        Model = model;
    }

    public SceneMarkerModel Model { get; }

    public Guid Id => Model.Id;

    public int Frame => Model.Frame;

    public string Name => Model.Name;

    public string Notes => Model.Notes;

    public SceneMarkerKind Kind => Model.Kind;

    public string KindLabel => Kind switch
    {
        SceneMarkerKind.Cue => "Cue",
        SceneMarkerKind.Comment => "Comment",
        SceneMarkerKind.Chapter => "Chapter",
        SceneMarkerKind.Beat => "Beat",
        SceneMarkerKind.PublishNote => "Publish",
        _ => Kind.ToString()
    };

    public string Subtitle => string.IsNullOrWhiteSpace(Model.Notes)
        ? $"{KindLabel} • Frame {Frame + 1}"
        : $"{KindLabel} • Frame {Frame + 1} • {Model.Notes}";

    public IBrush ColorBrush => ColorHelpers.Brush(Model.Color, "#57C9FF");
}
