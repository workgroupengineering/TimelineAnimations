namespace TimelineAnimations.App.ViewModels;

/// <summary>
/// Represents a recent editing operation shown in the History panel.
/// </summary>
public sealed class HistoryEntryViewModel : ViewModelBase
{
    public HistoryEntryViewModel(string message, DateTimeOffset timestamp)
    {
        Message = message;
        Timestamp = timestamp;
    }

    public string Message { get; }

    public DateTimeOffset Timestamp { get; }

    public string TimeLabel => Timestamp.ToLocalTime().ToString("HH:mm:ss");
}
