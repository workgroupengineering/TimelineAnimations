namespace TimelineAnimations.Rendering.Models;

public sealed class RenderedFrame
{
    public RenderedFrame(int width, int height, int stride, byte[] pixels)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        Stride = Math.Max(Width * 4, stride);
        Pixels = pixels;
    }

    public int Width { get; }

    public int Height { get; }

    public int Stride { get; }

    public byte[] Pixels { get; }
}
