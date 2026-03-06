using Avalonia.Media;
using TimelineAnimations.App.Helpers;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.ViewModels;

public sealed class MediaAssetViewModel
{
    public MediaAssetViewModel(MediaAsset model)
    {
        Model = model;
        PreviewBrush = ColorHelpers.Brush(model.PreviewFill, "#61E6FF");
    }

    public MediaAsset Model { get; }

    public Guid Id => Model.Id;

    public string Name => Model.Name;

    public MediaAssetKind Kind => Model.Kind;

    public SolidColorBrush PreviewBrush { get; }

    public string KindLabel => Kind == MediaAssetKind.Audio ? "Audio" : "Video";

    public string Subtitle => Kind == MediaAssetKind.Audio
        ? $"{Model.Duration:0.00}s • {Model.SampleRate:0} Hz • {Math.Max(1, Model.ChannelCount)} ch"
        : $"{Model.Width}×{Model.Height} • {Model.Frames.Count} frames • {Model.FrameRate:0.#} fps";

    public string TimelineLabel => Kind == MediaAssetKind.Audio
        ? $"{Model.WaveformSamples.Count} waveform bins"
        : $"{Model.Duration:0.00}s clip";

    public string SourceLabel => string.IsNullOrWhiteSpace(Model.SourceLabel) ? "Embedded media" : Model.SourceLabel;
}
