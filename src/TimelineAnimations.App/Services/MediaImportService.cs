using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using TimelineAnimations.Core.Models;
using TimelineAnimations.Core.Services;

namespace TimelineAnimations.App.Services;

public readonly record struct ImportedMediaFrame(string Name, byte[] Bytes);

public static class MediaImportService
{
    private const int WaveformResolution = 96;

    public static async Task<MediaAsset> ImportAudioAsync(string name, Stream stream, CancellationToken cancellationToken = default)
    {
        await using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        return ImportAudio(name, memory.ToArray());
    }

    public static MediaAsset ImportAudio(string name, byte[] bytes)
    {
        if (bytes.Length < 44)
        {
            throw new InvalidOperationException("Audio import expects a WAV file.");
        }

        var span = bytes.AsSpan();
        if (!span[..4].SequenceEqual("RIFF"u8) || !span[8..12].SequenceEqual("WAVE"u8))
        {
            throw new InvalidOperationException("Only RIFF/WAVE audio files are supported.");
        }

        short formatCode = 0;
        short channels = 1;
        int sampleRate = 0;
        short bitsPerSample = 16;
        short blockAlign = 0;
        ReadOnlySpan<byte> audioData = default;

        var offset = 12;
        while (offset + 8 <= span.Length)
        {
            var chunkId = span.Slice(offset, 4);
            var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset + 4, 4));
            offset += 8;

            if (offset + chunkSize > span.Length)
            {
                break;
            }

            if (chunkId.SequenceEqual("fmt "u8))
            {
                formatCode = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(offset, 2));
                channels = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(offset + 2, 2));
                sampleRate = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset + 4, 4));
                blockAlign = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(offset + 12, 2));
                bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(offset + 14, 2));
            }
            else if (chunkId.SequenceEqual("data"u8))
            {
                audioData = span.Slice(offset, chunkSize);
            }

            offset += chunkSize + (chunkSize % 2);
        }

        if (sampleRate <= 0 || blockAlign <= 0 || audioData.IsEmpty)
        {
            throw new InvalidOperationException("The WAV file does not contain a readable PCM stream.");
        }

        var frameCount = audioData.Length / blockAlign;
        if (frameCount <= 0)
        {
            throw new InvalidOperationException("The WAV file does not contain audio samples.");
        }

        var waveform = BuildWaveform(audioData, frameCount, channels, bitsPerSample, formatCode, blockAlign);
        var duration = frameCount / (double)sampleRate;
        return new MediaAsset
        {
            Name = Path.GetFileNameWithoutExtension(name),
            Kind = MediaAssetKind.Audio,
            SourceLabel = name,
            Duration = Math.Max(0.05d, duration),
            SampleRate = sampleRate,
            ChannelCount = Math.Max(1, (int)channels),
            PreviewFill = "#61E6FF",
            PreviewAccent = "#FF8A4C",
            AudioBase64 = Convert.ToBase64String(bytes),
            WaveformSamples = waveform
        };
    }

    public static MediaAsset ImportVideoSequence(string name, IReadOnlyList<ImportedMediaFrame> frames, double frameRate)
    {
        if (frames.Count == 0)
        {
            throw new InvalidOperationException("Video import expects one or more image frames.");
        }

        var normalizedFrameRate = Math.Max(1d, frameRate);
        var firstFrame = frames[0];
        var (width, height, fill, accent) = AnalyzeImage(firstFrame.Bytes);
        var mediaFrames = new List<MediaFrameModel>(frames.Count);

        for (var index = 0; index < frames.Count; index++)
        {
            mediaFrames.Add(new MediaFrameModel
            {
                Duration = 1d / normalizedFrameRate,
                ImageBase64 = Convert.ToBase64String(frames[index].Bytes),
                Fill = fill,
                Accent = accent,
                Label = $"F{index + 1:00}"
            });
        }

        return new MediaAsset
        {
            Name = Path.GetFileNameWithoutExtension(name),
            Kind = MediaAssetKind.Video,
            SourceLabel = $"{frames.Count} frame{(frames.Count == 1 ? string.Empty : "s")}",
            Duration = Math.Max(0.05d, frames.Count / normalizedFrameRate),
            FrameRate = normalizedFrameRate,
            Width = width,
            Height = height,
            PreviewFill = fill,
            PreviewAccent = accent,
            Frames = mediaFrames
        };
    }

    private static List<double> BuildWaveform(
        ReadOnlySpan<byte> audioData,
        int frameCount,
        int channels,
        int bitsPerSample,
        int formatCode,
        int blockAlign)
    {
        var waveform = new List<double>(WaveformResolution);
        for (var bucket = 0; bucket < WaveformResolution; bucket++)
        {
            var startFrame = (int)((bucket / (double)WaveformResolution) * frameCount);
            var endFrame = (int)(((bucket + 1) / (double)WaveformResolution) * frameCount);
            if (endFrame <= startFrame)
            {
                endFrame = Math.Min(frameCount, startFrame + 1);
            }

            var peak = 0d;
            for (var frame = startFrame; frame < endFrame; frame++)
            {
                var frameOffset = frame * blockAlign;
                var amplitude = 0d;
                for (var channel = 0; channel < channels; channel++)
                {
                    amplitude += Math.Abs(ReadSample(audioData, frameOffset, channel, bitsPerSample, formatCode));
                }

                peak = Math.Max(peak, amplitude / Math.Max(1, channels));
            }

            waveform.Add(TimelineMath.Clamp(peak, 0d, 1d));
        }

        return waveform;
    }

    private static double ReadSample(ReadOnlySpan<byte> audioData, int frameOffset, int channel, int bitsPerSample, int formatCode)
    {
        var bytesPerSample = Math.Max(1, bitsPerSample / 8);
        var offset = frameOffset + (channel * bytesPerSample);
        if (offset + bytesPerSample > audioData.Length)
        {
            return 0d;
        }

        return (formatCode, bitsPerSample) switch
        {
            (3, 32) => Math.Clamp(BitConverter.ToSingle(audioData.Slice(offset, 4)), -1f, 1f),
            (1, 8) => ((audioData[offset] - 128) / 128d),
            (1, 16) => BinaryPrimitives.ReadInt16LittleEndian(audioData.Slice(offset, 2)) / 32768d,
            (1, 24) => ReadInt24(audioData.Slice(offset, 3)) / 8388608d,
            (1, 32) => BinaryPrimitives.ReadInt32LittleEndian(audioData.Slice(offset, 4)) / 2147483648d,
            _ => throw new InvalidOperationException("Only PCM or IEEE float WAV audio is supported.")
        };
    }

    private static int ReadInt24(ReadOnlySpan<byte> bytes)
    {
        var value = bytes[0] | (bytes[1] << 8) | (bytes[2] << 16);
        if ((value & 0x800000) != 0)
        {
            value |= unchecked((int)0xFF000000);
        }

        return value;
    }

    private static (int Width, int Height, string Fill, string Accent) AnalyzeImage(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var bitmap = new Bitmap(stream);
        var width = bitmap.PixelSize.Width;
        var height = bitmap.PixelSize.Height;
        using var staging = new WriteableBitmap(
            bitmap.PixelSize,
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);
        using var framebuffer = staging.Lock();
        bitmap.CopyPixels(framebuffer, AlphaFormat.Unpremul);

        var pixelBytes = new byte[framebuffer.RowBytes * framebuffer.Size.Height];
        Marshal.Copy(framebuffer.Address, pixelBytes, 0, pixelBytes.Length);

        long red = 0;
        long green = 0;
        long blue = 0;
        long count = 0;
        for (var index = 0; index < pixelBytes.Length; index += 32)
        {
            blue += pixelBytes[index];
            green += pixelBytes[index + 1];
            red += pixelBytes[index + 2];
            count++;
        }

        var fill = Color.FromRgb(
            (byte)(red / Math.Max(1, count)),
            (byte)(green / Math.Max(1, count)),
            (byte)(blue / Math.Max(1, count)));
        var accent = Blend(fill, Colors.White, 0.42d);
        return (width, height, $"#{fill.ToUInt32():X8}"[2..], $"#{accent.ToUInt32():X8}"[2..]);
    }

    private static Color Blend(Color baseColor, Color tint, double amount)
    {
        var normalized = Math.Clamp(amount, 0d, 1d);
        return Color.FromArgb(
            255,
            (byte)(baseColor.R + ((tint.R - baseColor.R) * normalized)),
            (byte)(baseColor.G + ((tint.G - baseColor.G) * normalized)),
            (byte)(baseColor.B + ((tint.B - baseColor.B) * normalized)));
    }
}
