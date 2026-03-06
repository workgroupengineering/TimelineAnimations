using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public sealed class PublishProfileViewModel : ViewModelBase
{
    public PublishProfileViewModel(PublishProfile model)
    {
        Model = model;
    }

    public PublishProfile Model { get; }

    public Guid Id => Model.Id;

    public string Name => Model.Name;

    public PublishOutputKind OutputKind => Model.OutputKind;

    public string FormatLabel => OutputKind switch
    {
        PublishOutputKind.PngSequence => "PNG Sequence",
        PublishOutputKind.SpriteSheet => "Sprite Sheet",
        PublishOutputKind.Gif => "GIF",
        PublishOutputKind.Mp4 => "MP4",
        PublishOutputKind.JsonSceneGraph => "Scene Graph",
        PublishOutputKind.Package => "Package",
        _ => OutputKind.ToString()
    };

    public string Subtitle => $"{Model.Width}×{Model.Height} • {Model.FrameRate:0.#} fps{(Model.PlayAllScenes ? " • all scenes" : " • active scene")}";

    public void RefreshMetadata()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(OutputKind));
        OnPropertyChanged(nameof(FormatLabel));
        OnPropertyChanged(nameof(Subtitle));
    }
}
