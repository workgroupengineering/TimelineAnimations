using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using TimelineAnimations.Rendering.Models;

namespace TimelineAnimations.App.Services;

public static class RenderedFrameBitmapFactory
{
    public static WriteableBitmap Create(RenderedFrame frame)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(frame.Width, frame.Height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);
        using var framebuffer = bitmap.Lock();
        Marshal.Copy(frame.Pixels, 0, framebuffer.Address, Math.Min(frame.Pixels.Length, framebuffer.RowBytes * framebuffer.Size.Height));
        return bitmap;
    }
}
