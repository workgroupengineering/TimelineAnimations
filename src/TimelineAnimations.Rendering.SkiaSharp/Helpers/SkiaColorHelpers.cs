using SkiaSharp;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Rendering.SkiaSharp.Helpers;

public static class SkiaColorHelpers
{
    public static SKColor Parse(string? value, string fallback = "#FFFFFF")
    {
        return TryParse(value, out var color)
            ? color
            : TryParse(fallback, out var fallbackColor) ? fallbackColor : SKColors.White;
    }

    public static SKColor ApplyOpacity(SKColor color, double opacity)
    {
        return color.WithAlpha(ClampChannel(opacity * 255d));
    }

    public static SKColor Blend(SKColor source, SKColor target, double amount)
    {
        var blendAmount = Math.Clamp(amount, 0d, 1d);
        return new SKColor(
            ClampChannel(source.Red + ((target.Red - source.Red) * blendAmount)),
            ClampChannel(source.Green + ((target.Green - source.Green) * blendAmount)),
            ClampChannel(source.Blue + ((target.Blue - source.Blue) * blendAmount)),
            ClampChannel(source.Alpha + ((target.Alpha - source.Alpha) * blendAmount)));
    }

    public static SKColor ApplyCompositeColorEffects(SKColor color, LayerCompositeSettings compositing)
    {
        var adjusted = color;
        if (Math.Abs(compositing.HueShift) > 0.001d)
        {
            adjusted = ShiftHue(adjusted, compositing.HueShift);
        }

        if (Math.Abs(compositing.Saturation - 1d) > 0.001d)
        {
            var luminance = (0.299d * adjusted.Red) + (0.587d * adjusted.Green) + (0.114d * adjusted.Blue);
            adjusted = new SKColor(
                ClampChannel(luminance + ((adjusted.Red - luminance) * compositing.Saturation)),
                ClampChannel(luminance + ((adjusted.Green - luminance) * compositing.Saturation)),
                ClampChannel(luminance + ((adjusted.Blue - luminance) * compositing.Saturation)),
                adjusted.Alpha);
        }

        if (Math.Abs(compositing.Brightness) > 0.001d)
        {
            var brightness = Math.Clamp(compositing.Brightness, -1d, 1d);
            adjusted = brightness >= 0d
                ? Blend(adjusted, SKColors.White, brightness)
                : Blend(adjusted, SKColors.Black, Math.Abs(brightness));
        }

        if (Math.Abs(compositing.Contrast) > 0.001d)
        {
            adjusted = ApplyContrast(adjusted, compositing.Contrast);
        }

        if (compositing.TintStrength > 0.001d)
        {
            adjusted = Blend(adjusted, Parse(compositing.TintColor, "#FFFFFF"), Math.Clamp(compositing.TintStrength, 0d, 1d));
        }

        return ApplyFlashColorEffect(adjusted, compositing);
    }

    public static SKColor ApplyFlashColorEffect(SKColor color, LayerCompositeSettings compositing)
    {
        return compositing.FlashColorEffectMode switch
        {
            FlashColorEffectMode.Alpha => color.WithAlpha(
                ClampChannel(color.Alpha * (Math.Clamp(compositing.FlashAlphaPercent, 0d, 1000d) / 100d))),
            FlashColorEffectMode.Tint => Blend(
                color,
                Parse(compositing.FlashTintColor, "#FFFFFF"),
                Math.Clamp(compositing.FlashTintPercent, 0d, 100d) / 100d),
            FlashColorEffectMode.Brightness => ApplyBrightnessPercent(color, compositing.FlashBrightnessPercent),
            FlashColorEffectMode.Advanced => new SKColor(
                ClampChannel((color.Red * (compositing.FlashRedPercent / 100d)) + compositing.FlashRedOffset),
                ClampChannel((color.Green * (compositing.FlashGreenPercent / 100d)) + compositing.FlashGreenOffset),
                ClampChannel((color.Blue * (compositing.FlashBluePercent / 100d)) + compositing.FlashBlueOffset),
                ClampChannel((color.Alpha * (compositing.FlashAdvancedAlphaPercent / 100d)) + compositing.FlashAlphaOffset)),
            _ => color
        };
    }

    private static bool TryParse(string? value, out SKColor color)
    {
        color = SKColors.Transparent;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        if (normalized.StartsWith('#'))
        {
            normalized = normalized[1..];
        }

        if (normalized.Length == 6 &&
            uint.TryParse(normalized, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var rgb))
        {
            color = new SKColor(
                (byte)((rgb >> 16) & 0xFF),
                (byte)((rgb >> 8) & 0xFF),
                (byte)(rgb & 0xFF),
                0xFF);
            return true;
        }

        if (normalized.Length == 8 &&
            uint.TryParse(normalized, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var argb))
        {
            color = new SKColor(
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF),
                (byte)((argb >> 24) & 0xFF));
            return true;
        }

        return false;
    }

    private static SKColor ApplyBrightnessPercent(SKColor color, double brightnessPercent)
    {
        var amount = Math.Clamp(brightnessPercent, -100d, 100d) / 100d;
        return amount >= 0d
            ? Blend(color, SKColors.White, amount)
            : Blend(color, SKColors.Black, Math.Abs(amount));
    }

    private static SKColor ApplyContrast(SKColor color, double contrast)
    {
        var normalized = Math.Clamp(contrast, -1d, 1d);
        var factor = (259d * ((normalized * 255d) + 255d)) / (255d * (259d - (normalized * 255d)));
        return new SKColor(
            ClampChannel((factor * (color.Red - 128d)) + 128d),
            ClampChannel((factor * (color.Green - 128d)) + 128d),
            ClampChannel((factor * (color.Blue - 128d)) + 128d),
            color.Alpha);
    }

    private static SKColor ShiftHue(SKColor color, double hueShift)
    {
        RgbToHsl(color, out var hue, out var saturation, out var lightness);
        hue = ((hue + hueShift) % 360d + 360d) % 360d;
        return HslToRgb(color.Alpha, hue, saturation, lightness);
    }

    private static void RgbToHsl(SKColor color, out double hue, out double saturation, out double lightness)
    {
        var r = color.Red / 255d;
        var g = color.Green / 255d;
        var b = color.Blue / 255d;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        lightness = (max + min) / 2d;
        if (Math.Abs(max - min) < 0.0001d)
        {
            hue = 0d;
            saturation = 0d;
            return;
        }

        var delta = max - min;
        saturation = lightness > 0.5d
            ? delta / (2d - max - min)
            : delta / (max + min);

        hue = max switch
        {
            var value when NearlyEqual(value, r) => ((g - b) / delta) + (g < b ? 6d : 0d),
            var value when NearlyEqual(value, g) => ((b - r) / delta) + 2d,
            _ => ((r - g) / delta) + 4d
        };
        hue *= 60d;
    }

    private static SKColor HslToRgb(byte alpha, double hue, double saturation, double lightness)
    {
        if (saturation <= 0.0001d)
        {
            var channel = ClampChannel(lightness * 255d);
            return new SKColor(channel, channel, channel, alpha);
        }

        var q = lightness < 0.5d
            ? lightness * (1d + saturation)
            : lightness + saturation - (lightness * saturation);
        var p = 2d * lightness - q;
        var hk = hue / 360d;
        var r = HueToRgb(p, q, hk + (1d / 3d));
        var g = HueToRgb(p, q, hk);
        var b = HueToRgb(p, q, hk - (1d / 3d));
        return new SKColor(ClampChannel(r * 255d), ClampChannel(g * 255d), ClampChannel(b * 255d), alpha);
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0d)
        {
            t += 1d;
        }

        if (t > 1d)
        {
            t -= 1d;
        }

        if (t < (1d / 6d))
        {
            return p + ((q - p) * 6d * t);
        }

        if (t < 0.5d)
        {
            return q;
        }

        if (t < (2d / 3d))
        {
            return p + ((q - p) * ((2d / 3d) - t) * 6d);
        }

        return p;
    }

    private static bool NearlyEqual(double left, double right)
    {
        return Math.Abs(left - right) < 0.0001d;
    }

    private static byte ClampChannel(double value)
    {
        return (byte)Math.Clamp(Math.Round(value), 0d, 255d);
    }
}
