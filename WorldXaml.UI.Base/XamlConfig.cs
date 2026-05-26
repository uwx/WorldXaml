namespace WorldXaml.UI.Base;

public static class XamlConfig
{
    /// <summary>
    /// Set to define a custom log handler for WorldXaml.UI.
    /// </summary>
    public static Action<LogLevel, string> LogMessage { internal get; set; } = (level, message) =>
    {
        if (level == LogLevel.Info)
            Console.WriteLine($"[Info][WorldXaml] {message}");
        else if (level == LogLevel.Warning)
            Console.WriteLine($"[Warning][WorldXaml] {message}");
        else if (level == LogLevel.Error)
            Console.WriteLine($"[Error][WorldXaml] {message}");
        else if (level == LogLevel.Debug)
            Console.WriteLine($"[Debug][WorldXaml] {message}");
        else
            throw new ArgumentOutOfRangeException(nameof(level), level, null);
    };
    
    public static IInterpolatorProvider? InterpolatorProvider { internal get; set; }
}

public sealed class FallbackInterpolatorProvider(params IInterpolatorProvider[] interpolatorProviders) : IInterpolatorProvider
{
    public Interpolator<T>? GetInterpolator<T>()
    {
        foreach (var provider in interpolatorProviders)
        {
            var interpolator = provider.GetInterpolator<T>();
            if (interpolator != null)
                return interpolator;
        }
        return null;
    }
}

public interface IInterpolatorProvider
{
    Interpolator<T>? GetInterpolator<T>();
}

public delegate T Interpolator<T>(T from, T to, float alpha);