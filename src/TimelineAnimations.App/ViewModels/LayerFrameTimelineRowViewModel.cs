using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using TimelineAnimations.App.Helpers;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.ViewModels;

public partial class LayerFrameTimelineRowViewModel : ViewModelBase
{
    public LayerFrameTimelineRowViewModel(Guid layerId)
    {
        LayerId = layerId;
    }

    public Guid LayerId { get; }

    public ObservableCollection<FrameSpanViewModel> Spans { get; } = [];

    [ObservableProperty]
    private string layerName = string.Empty;

    [ObservableProperty]
    private ISolidColorBrush fillBrush = ColorHelpers.Brush("#FFFFFF");

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isLocked;

    [ObservableProperty]
    private bool isMuted;

    [ObservableProperty]
    private bool isSolo;

    [ObservableProperty]
    private bool isFolder;

    [ObservableProperty]
    private bool hasChildren;

    [ObservableProperty]
    private bool isExpanded = true;

    [ObservableProperty]
    private int depth;

    [ObservableProperty]
    private int durationFrames;

    [ObservableProperty]
    private bool hasExplicitFrames;

    [ObservableProperty]
    private bool hasMediaClip;

    [ObservableProperty]
    private bool isAudioClip;

    [ObservableProperty]
    private bool isVideoClip;

    [ObservableProperty]
    private int mediaStartFrame;

    [ObservableProperty]
    private int mediaEndFrame;

    [ObservableProperty]
    private string mediaLabel = string.Empty;

    [ObservableProperty]
    private MediaPlaybackMode mediaPlaybackMode = MediaPlaybackMode.Stream;

    public IReadOnlyList<double> MediaWaveformSamples { get; private set; } = [];

    public int MediaFrameCount { get; private set; }

    public string DurationLabel => $"{DurationFrames}f";

    public void LoadFromLayer(
        TimelineLayer layer,
        IReadOnlyDictionary<Guid, MediaAsset> mediaLookup,
        double frameRate,
        int totalFrames,
        Guid? selectedLayerId,
        int selectionStartFrame,
        int selectionEndFrame)
    {
        LayerName = layer.Name;
        FillBrush = ColorHelpers.Brush(layer.Style.Fill, "#FFFFFF");
        IsSelected = selectedLayerId == layer.Id;
        IsLocked = layer.IsLocked;
        IsMuted = layer.IsMuted;
        IsSolo = layer.IsSolo;
        IsFolder = layer.Kind == LayerKind.Folder;
        IsExpanded = layer.IsExpanded;
        DurationFrames = FrameTimelineService.GetLayerDurationFrames(layer, totalFrames);
        HasExplicitFrames = FrameTimelineService.HasExplicitFrameTimeline(layer, totalFrames);
        LoadMediaClip(layer, mediaLookup, frameRate, totalFrames);
        OnPropertyChanged(nameof(DurationLabel));

        var normalizedStart = Math.Min(selectionStartFrame, selectionEndFrame);
        var normalizedEnd = Math.Max(selectionStartFrame, selectionEndFrame);

        Spans.Clear();
        foreach (var span in FrameTimelineService.GetEffectiveSpans(layer, totalFrames))
        {
            Spans.Add(new FrameSpanViewModel(span)
            {
                IsSelected = IsSelected &&
                    normalizedStart >= 0 &&
                    normalizedEnd >= normalizedStart &&
                    span.EndFrame >= normalizedStart &&
                    span.StartFrame <= normalizedEnd
            });
        }
    }

    public void UpdateSelection(Guid? selectedLayerId, int selectionStartFrame, int selectionEndFrame)
    {
        IsSelected = selectedLayerId == LayerId;
        var normalizedStart = Math.Min(selectionStartFrame, selectionEndFrame);
        var normalizedEnd = Math.Max(selectionStartFrame, selectionEndFrame);

        foreach (var span in Spans)
        {
            span.IsSelected = IsSelected &&
                normalizedStart >= 0 &&
                normalizedEnd >= normalizedStart &&
                span.EndFrame >= normalizedStart &&
                span.StartFrame <= normalizedEnd;
        }
    }

    public void ApplyHierarchyState(int depth, bool hasChildren, bool isExpanded)
    {
        Depth = Math.Max(0, depth);
        HasChildren = hasChildren;
        IsExpanded = isExpanded;
    }

    private void LoadMediaClip(TimelineLayer layer, IReadOnlyDictionary<Guid, MediaAsset> mediaLookup, double frameRate, int totalFrames)
    {
        HasMediaClip = false;
        IsAudioClip = false;
        IsVideoClip = false;
        MediaStartFrame = 0;
        MediaEndFrame = 0;
        MediaLabel = string.Empty;
        MediaPlaybackMode = layer.Media.PlaybackMode;
        MediaWaveformSamples = [];
        MediaFrameCount = 0;

        if (layer.Media.SourceMediaAssetId is not Guid mediaAssetId || !mediaLookup.TryGetValue(mediaAssetId, out var mediaAsset))
        {
            return;
        }

        HasMediaClip = true;
        IsAudioClip = mediaAsset.Kind == MediaAssetKind.Audio;
        IsVideoClip = mediaAsset.Kind == MediaAssetKind.Video;
        MediaStartFrame = MediaTimelineService.GetClipStartFrame(layer, frameRate, totalFrames);
        MediaEndFrame = Math.Max(MediaStartFrame, MediaTimelineService.GetClipEndFrame(layer, mediaAsset, frameRate, totalFrames));
        MediaLabel = $"{mediaAsset.Name} • {layer.Media.PlaybackMode}";
        MediaPlaybackMode = layer.Media.PlaybackMode;
        MediaWaveformSamples = mediaAsset.WaveformSamples;
        MediaFrameCount = mediaAsset.Frames.Count;
        OnPropertyChanged(nameof(MediaWaveformSamples));
        OnPropertyChanged(nameof(MediaFrameCount));
    }
}
