using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Rendering.Services;

public static class RenderPixelBufferComposer
{
    private static readonly byte[] MultiplyBlendTable = CreateBlendTable(static (backdrop, source) => ((backdrop * source) + 127) / 255);
    private static readonly byte[] ScreenBlendTable = CreateBlendTable(static (backdrop, source) => 255 - ((((255 - backdrop) * (255 - source)) + 127) / 255));
    private static readonly byte[] OverlayBlendTable = CreateBlendTable(static (backdrop, source) => backdrop <= 127
        ? ((2 * backdrop * source) + 127) / 255
        : 255 - ((((2 * (255 - backdrop) * (255 - source)) + 127) / 255)));
    private static readonly byte[] HardLightBlendTable = CreateBlendTable(static (backdrop, source) => source <= 127
        ? ((2 * backdrop * source) + 127) / 255
        : 255 - ((((2 * (255 - backdrop) * (255 - source)) + 127) / 255)));

    public static void ApplyMask(byte[] contentPixels, byte[] maskPixels)
    {
        ApplyMask(contentPixels, maskPixels, Math.Min(contentPixels.Length, maskPixels.Length));
    }

    public static void ApplyMask(byte[] contentPixels, byte[] maskPixels, int length)
    {
        length = Math.Min(length, Math.Min(contentPixels.Length, maskPixels.Length));
        for (var index = 0; index < length; index += 4)
        {
            var maskAlphaByte = maskPixels[index + 3];
            if (maskAlphaByte == byte.MaxValue)
            {
                continue;
            }

            if (maskAlphaByte == 0)
            {
                contentPixels[index + 3] = 0;
                continue;
            }

            var maskAlpha = maskAlphaByte / 255d;
            contentPixels[index + 3] = ClampChannel(contentPixels[index + 3] * maskAlpha);
        }
    }

    public static void BlendPixels(byte[] destination, byte[] source, LayerBlendMode blendMode)
    {
        BlendPixels(destination, source, blendMode, Math.Min(destination.Length, source.Length));
    }

    public static void BlendPixels(byte[] destination, byte[] source, LayerBlendMode blendMode, int length)
    {
        length = Math.Min(length, Math.Min(destination.Length, source.Length));
        var directSourceBlend = blendMode is LayerBlendMode.Normal or LayerBlendMode.Layer;
        for (var index = 0; index < length; index += 4)
        {
            var sourceAlphaByte = source[index + 3];
            if (sourceAlphaByte == 0)
            {
                continue;
            }

            if (directSourceBlend)
            {
                BlendNormalPixel(destination, source, index, sourceAlphaByte);
                continue;
            }

            var destinationAlphaByte = destination[index + 3];

            if (blendMode == LayerBlendMode.Alpha)
            {
                destination[index + 3] = (byte)(((destinationAlphaByte * sourceAlphaByte) + 127) / 255);
                continue;
            }

            if (blendMode == LayerBlendMode.Erase)
            {
                var erasedAlpha = (destinationAlphaByte * (255 - sourceAlphaByte) + 127) / 255;
                destination[index + 3] = (byte)erasedAlpha;
                if (erasedAlpha <= 0)
                {
                    destination[index] = 0;
                    destination[index + 1] = 0;
                    destination[index + 2] = 0;
                }

                continue;
            }

            var outAlpha = sourceAlphaByte + ((destinationAlphaByte * (255 - sourceAlphaByte) + 127) / 255);
            if (outAlpha <= 0)
            {
                destination[index] = 0;
                destination[index + 1] = 0;
                destination[index + 2] = 0;
                destination[index + 3] = 0;
                continue;
            }

            if (sourceAlphaByte == byte.MaxValue && destinationAlphaByte == byte.MaxValue)
            {
                destination[index] = BlendChannel(destination[index], source[index], blendMode);
                destination[index + 1] = BlendChannel(destination[index + 1], source[index + 1], blendMode);
                destination[index + 2] = BlendChannel(destination[index + 2], source[index + 2], blendMode);
                destination[index + 3] = byte.MaxValue;
                continue;
            }

            destination[index] = BlendChannel(destination[index], source[index], destinationAlphaByte, sourceAlphaByte, outAlpha, blendMode);
            destination[index + 1] = BlendChannel(destination[index + 1], source[index + 1], destinationAlphaByte, sourceAlphaByte, outAlpha, blendMode);
            destination[index + 2] = BlendChannel(destination[index + 2], source[index + 2], destinationAlphaByte, sourceAlphaByte, outAlpha, blendMode);
            destination[index + 3] = (byte)outAlpha;
        }
    }

    private static byte BlendChannel(
        byte backdrop,
        byte source,
        int destinationAlpha,
        int sourceAlpha,
        int outAlpha,
        LayerBlendMode blendMode)
    {
        var blended = BlendChannel(backdrop, source, blendMode);
        var numerator =
            (source * sourceAlpha * (255 - destinationAlpha)) +
            (blended * sourceAlpha * destinationAlpha) +
            (backdrop * destinationAlpha * (255 - sourceAlpha));
        var denominator = outAlpha * 255;
        return denominator <= 0
            ? (byte)0
            : (byte)((numerator + (denominator / 2)) / denominator);
    }

    private static byte BlendChannel(byte backdrop, byte source, LayerBlendMode blendMode)
    {
        return blendMode switch
        {
            LayerBlendMode.Layer => source,
            LayerBlendMode.Add => (byte)Math.Min(255, backdrop + source),
            LayerBlendMode.Screen => ScreenBlendTable[GetBlendIndex(backdrop, source)],
            LayerBlendMode.Multiply => MultiplyBlendTable[GetBlendIndex(backdrop, source)],
            LayerBlendMode.Overlay => OverlayBlendTable[GetBlendIndex(backdrop, source)],
            LayerBlendMode.Darken => backdrop <= source ? backdrop : source,
            LayerBlendMode.Lighten => backdrop >= source ? backdrop : source,
            LayerBlendMode.Difference => (byte)Math.Abs(backdrop - source),
            LayerBlendMode.HardLight => HardLightBlendTable[GetBlendIndex(backdrop, source)],
            LayerBlendMode.Subtract => backdrop > source ? (byte)(backdrop - source) : (byte)0,
            LayerBlendMode.Invert => (byte)(255 - backdrop),
            _ => source
        };
    }

    private static void BlendNormalPixel(byte[] destination, byte[] source, int index, byte sourceAlphaByte)
    {
        var destinationAlphaByte = destination[index + 3];
        if (sourceAlphaByte == byte.MaxValue || destinationAlphaByte == 0)
        {
            destination[index] = source[index];
            destination[index + 1] = source[index + 1];
            destination[index + 2] = source[index + 2];
            destination[index + 3] = sourceAlphaByte;
            return;
        }

        var sourceAlpha = sourceAlphaByte;
        var destinationAlpha = destinationAlphaByte;
        var inverseSourceAlpha = 255 - sourceAlpha;
        var outAlpha = sourceAlpha + ((destinationAlpha * inverseSourceAlpha + 127) / 255);
        if (outAlpha <= 0)
        {
            destination[index] = 0;
            destination[index + 1] = 0;
            destination[index + 2] = 0;
            destination[index + 3] = 0;
            return;
        }

        destination[index] = BlendNormalChannel(destination[index], source[index], destinationAlpha, sourceAlpha, inverseSourceAlpha, outAlpha);
        destination[index + 1] = BlendNormalChannel(destination[index + 1], source[index + 1], destinationAlpha, sourceAlpha, inverseSourceAlpha, outAlpha);
        destination[index + 2] = BlendNormalChannel(destination[index + 2], source[index + 2], destinationAlpha, sourceAlpha, inverseSourceAlpha, outAlpha);
        destination[index + 3] = (byte)outAlpha;
    }

    private static byte BlendNormalChannel(
        byte destinationChannel,
        byte sourceChannel,
        int destinationAlpha,
        int sourceAlpha,
        int inverseSourceAlpha,
        int outAlpha)
    {
        var premultipliedSource = sourceChannel * sourceAlpha;
        var premultipliedDestination = destinationChannel * destinationAlpha;
        var outPremultiplied = premultipliedSource + ((premultipliedDestination * inverseSourceAlpha + 127) / 255);
        return (byte)((outPremultiplied + (outAlpha / 2)) / outAlpha);
    }

    public static byte ClampChannel(double value)
    {
        return (byte)Math.Clamp(Math.Round(value), 0d, 255d);
    }

    private static byte[] CreateBlendTable(Func<int, int, int> blend)
    {
        var table = new byte[256 * 256];
        for (var backdrop = 0; backdrop < 256; backdrop++)
        {
            for (var source = 0; source < 256; source++)
            {
                table[GetBlendIndex(backdrop, source)] = (byte)blend(backdrop, source);
            }
        }

        return table;
    }

    private static int GetBlendIndex(int backdrop, int source)
    {
        return (backdrop << 8) | source;
    }
}
