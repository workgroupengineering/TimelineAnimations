using System.Collections.ObjectModel;

namespace TimelineAnimations.App.ViewModels;

/// <summary>
/// Represents one node in the Movie Explorer document tree.
/// </summary>
public sealed class MovieExplorerNodeViewModel : ViewModelBase
{
    public MovieExplorerNodeViewModel(
        string title,
        string kindLabel,
        string subtitle = "",
        string focusToken = "")
    {
        Title = title;
        KindLabel = kindLabel;
        Subtitle = subtitle;
        FocusToken = focusToken;
    }

    public string Title { get; }

    public string KindLabel { get; }

    public string Subtitle { get; }

    public string FocusToken { get; }

    public ObservableCollection<MovieExplorerNodeViewModel> Children { get; } = [];

    public bool HasChildren => Children.Count > 0;

    public bool CanFocus => !string.IsNullOrWhiteSpace(FocusToken);

    public string Summary => string.IsNullOrWhiteSpace(Subtitle)
        ? $"{Title} • {KindLabel}"
        : $"{Title} • {KindLabel} • {Subtitle}";
}
