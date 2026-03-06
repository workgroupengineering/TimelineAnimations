using System.Diagnostics;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimelineAnimations.App.Services;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.ViewModels;

public partial class PreviewPlayerViewModel : ViewModelBase, IDisposable
{
    private readonly TimelineDocument _document;
    private readonly PublishProfile _profile;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _clock = new();
    private double _playbackOriginTime;

    public PreviewPlayerViewModel(TimelineDocument document, PublishProfile profile)
    {
        _document = DocumentSerializer.Clone(document);
        _profile = DocumentSerializer.Clone(profile);
        Duration = FrameExportService.GetPlaybackDuration(_document, _profile.PlayAllScenes);
        PlaybackFrameRate = FrameExportService.GetPlaybackFrameRate(_document, _profile.FrameRate);
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Background, HandleTick);
        RenderFrame();
    }

    public string Title => $"{_document.Name} • {_profile.Name}";

    public string ProfileSummary => $"{_profile.FormatLabel()} • {_profile.Width}×{_profile.Height} • {PlaybackFrameRate:0.#} fps";

    public string CurrentTimeLabel => $"{CurrentTime:0.00}s / {Duration:0.00}s";

    public string SceneScopeLabel => _profile.PlayAllScenes ? "All scenes" : "Active scene";

    public string PlaybackButtonLabel => IsPlaying ? "Pause" : "Play";

    public double PlaybackFrameRate { get; }

    [ObservableProperty]
    private Bitmap? frameBitmap;

    [ObservableProperty]
    private double currentTime;

    [ObservableProperty]
    private double duration;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private bool loopPlayback = true;

    partial void OnCurrentTimeChanged(double value)
    {
        RenderFrame();
        OnPropertyChanged(nameof(CurrentTimeLabel));
    }

    partial void OnIsPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(PlaybackButtonLabel));
    }

    [RelayCommand]
    private void TogglePlayback()
    {
        if (IsPlaying)
        {
            StopPlayback();
            return;
        }

        _playbackOriginTime = CurrentTime;
        _clock.Restart();
        _timer.Start();
        IsPlaying = true;
    }

    [RelayCommand]
    private void StopPlayback()
    {
        _timer.Stop();
        _clock.Stop();
        IsPlaying = false;
    }

    [RelayCommand]
    private void RestartPlayback()
    {
        StopPlayback();
        CurrentTime = 0d;
    }

    public void Dispose()
    {
        _timer.Stop();
        _clock.Stop();
        FrameBitmap?.Dispose();
        FrameBitmap = null;
    }

    private void HandleTick(object? sender, EventArgs e)
    {
        var nextTime = _playbackOriginTime + _clock.Elapsed.TotalSeconds;
        if (nextTime > Duration)
        {
            if (LoopPlayback)
            {
                _clock.Restart();
                _playbackOriginTime = 0d;
                CurrentTime = 0d;
                return;
            }

            StopPlayback();
            CurrentTime = Duration;
            return;
        }

        CurrentTime = nextTime;
    }

    private void RenderFrame()
    {
        var nextBitmap = FrameExportService.RenderFrameBitmap(
            _document,
            TimelineMath.Clamp(CurrentTime, 0d, Duration),
            _profile.Width,
            _profile.Height,
            PlaybackFrameRate,
            _profile.PlayAllScenes,
            _profile.TransparentBackground);
        var previousBitmap = FrameBitmap;
        FrameBitmap = nextBitmap;
        previousBitmap?.Dispose();
    }
}

internal static class PublishProfileExtensions
{
    public static string FormatLabel(this PublishProfile profile)
    {
        return profile.OutputKind switch
        {
            PublishOutputKind.PngSequence => "Preview",
            PublishOutputKind.SpriteSheet => "Atlas Preview",
            PublishOutputKind.Gif => "GIF Preview",
            PublishOutputKind.Mp4 => "MP4 Preview",
            PublishOutputKind.JsonSceneGraph => "Scene Graph Preview",
            PublishOutputKind.Package => "Package Preview",
            _ => profile.OutputKind.ToString()
        };
    }
}
