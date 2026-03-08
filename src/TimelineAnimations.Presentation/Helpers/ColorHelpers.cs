using Avalonia.Media;
using Avalonia.Media.Immutable;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.App.Helpers;

public static class ColorHelpers
{
    public static Color Parse(string? value, string fallback = "#FFFFFF")
    {
        if (!string.IsNullOrWhiteSpace(value) && Color.TryParse(value, out var color))
        {
            return color;
        }

        return Color.TryParse(fallback, out var fallbackColor) ? fallbackColor : Colors.White;
    }

    public static ISolidColorBrush Brush(string? value, string fallback = "#FFFFFF")
    {
        return new ImmutableSolidColorBrush(Parse(value, fallback));
    }

    public static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public static Color ApplyCompositeColorEffects(Color color, LayerCompositeSettings compositing)
    {
        var adjusted = color;
        if (Math.Abs(compositing.HueShift) > 0.001d)
        {
            adjusted = ShiftHue(adjusted, compositing.HueShift);
        }

        if (Math.Abs(compositing.Saturation - 1d) > 0.001d)
        {
            var luminance = (0.299d * adjusted.R) + (0.587d * adjusted.G) + (0.114d * adjusted.B);
            adjusted = Color.FromArgb(
                adjusted.A,
                ClampChannel(luminance + ((adjusted.R - luminance) * compositing.Saturation)),
                ClampChannel(luminance + ((adjusted.G - luminance) * compositing.Saturation)),
                ClampChannel(luminance + ((adjusted.B - luminance) * compositing.Saturation)));
        }

        if (Math.Abs(compositing.Brightness) > 0.001d)
        {
            var brightness = Math.Clamp(compositing.Brightness, -1d, 1d);
                adjusted = brightness >= 0d
                ? Blend(adjusted, Colors.White, brightness)
                : Blend(adjusted, Colors.Black, Math.Abs(brightness));
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

    public static Color ApplyFlashColorEffect(Color color, LayerCompositeSettings compositing)
    {
        return compositing.FlashColorEffectMode switch
        {
            FlashColorEffectMode.Alpha => Color.FromArgb(
                ClampChannel(color.A * (Math.Clamp(compositing.FlashAlphaPercent, 0d, 1000d) / 100d)),
                color.R,
                color.G,
                color.B),
            FlashColorEffectMode.Tint => Blend(color, Parse(compositing.FlashTintColor, "#FFFFFF"), Math.Clamp(compositing.FlashTintPercent, 0d, 100d) / 100d),
            FlashColorEffectMode.Brightness => ApplyBrightnessPercent(color, compositing.FlashBrightnessPercent),
            FlashColorEffectMode.Advanced => Color.FromArgb(
                ClampChannel((color.A * (compositing.FlashAdvancedAlphaPercent / 100d)) + compositing.FlashAlphaOffset),
                ClampChannel((color.R * (compositing.FlashRedPercent / 100d)) + compositing.FlashRedOffset),
                ClampChannel((color.G * (compositing.FlashGreenPercent / 100d)) + compositing.FlashGreenOffset),
                ClampChannel((color.B * (compositing.FlashBluePercent / 100d)) + compositing.FlashBlueOffset)),
            _ => color
        };
    }

    public static Color Blend(Color source, Color target, double amount)
    {
        var blendAmount = Math.Clamp(amount, 0d, 1d);
        return Color.FromArgb(
            ClampChannel(source.A + ((target.A - source.A) * blendAmount)),
            ClampChannel(source.R + ((target.R - source.R) * blendAmount)),
            ClampChannel(source.G + ((target.G - source.G) * blendAmount)),
            ClampChannel(source.B + ((target.B - source.B) * blendAmount)));
    }

    private static Color ApplyBrightnessPercent(Color color, double brightnessPercent)
    {
        var amount = Math.Clamp(brightnessPercent, -100d, 100d) / 100d;
        return amount >= 0d
            ? Blend(color, Colors.White, amount)
            : Blend(color, Colors.Black, Math.Abs(amount));
    }

    private static Color ApplyContrast(Color color, double contrast)
    {
        var normalized = Math.Clamp(contrast, -1d, 1d);
        var factor = (259d * ((normalized * 255d) + 255d)) / (255d * (259d - (normalized * 255d)));
        return Color.FromArgb(
            color.A,
            ClampChannel((factor * (color.R - 128d)) + 128d),
            ClampChannel((factor * (color.G - 128d)) + 128d),
            ClampChannel((factor * (color.B - 128d)) + 128d));
    }

    private static Color ShiftHue(Color color, double hueShift)
    {
        RgbToHsl(color, out var h, out var s, out var l);
        h = ((h + hueShift) % 360d + 360d) % 360d;
        return HslToRgb(color.A, h, s, l);
    }

    private static void RgbToHsl(Color color, out double hue, out double saturation, out double lightness)
    {
        var r = color.R / 255d;
        var g = color.G / 255d;
        var b = color.B / 255d;
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

    private static Color HslToRgb(byte alpha, double hue, double saturation, double lightness)
    {
        if (saturation <= 0.0001d)
        {
            var channel = ClampChannel(lightness * 255d);
            return Color.FromArgb(alpha, channel, channel, channel);
        }

        var q = lightness < 0.5d
            ? lightness * (1d + saturation)
            : lightness + saturation - (lightness * saturation);
        var p = 2d * lightness - q;
        var hk = hue / 360d;
        var r = HueToRgb(p, q, hk + (1d / 3d));
        var g = HueToRgb(p, q, hk);
        var b = HueToRgb(p, q, hk - (1d / 3d));
        return Color.FromArgb(alpha, ClampChannel(r * 255d), ClampChannel(g * 255d), ClampChannel(b * 255d));
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
