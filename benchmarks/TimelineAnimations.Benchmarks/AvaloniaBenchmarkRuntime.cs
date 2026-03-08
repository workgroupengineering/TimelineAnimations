using Avalonia;
using Avalonia.Headless;

namespace TimelineAnimations.Benchmarks;

internal sealed class BenchmarkApplication : Application
{
}

internal static class AvaloniaBenchmarkRuntime
{
    private static readonly object Sync = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (Sync)
        {
            if (_initialized)
            {
                return;
            }

            AppBuilder.Configure<BenchmarkApplication>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions
                {
                    UseHeadlessDrawing = true
                })
                .SetupWithoutStarting();

            _initialized = true;
        }
    }
}
