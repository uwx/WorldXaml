using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("WorldXaml.UI.Yoga")]

namespace WorldXaml.UI.Base;

public enum LogLevel : byte
{
    Info,
    Warning,
    Error,
    Debug
}

public static class Logging
{
    /// <summary>
    /// Set to define a custom log handler for WorldXaml.UI.
    /// </summary>
    public static Action<LogLevel, string> LogMessage { private get; set; } = (level, message) =>
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

    internal static void Info(string message) => LogMessage(LogLevel.Info, message);
    internal static void Warning(string message) => LogMessage(LogLevel.Warning, message);
    internal static void Error(string message) => LogMessage(LogLevel.Error, message);
    internal static void Debug(string message) => LogMessage(LogLevel.Debug, message);
    internal static void Info(object? message) => LogMessage(LogLevel.Info, message?.ToString() ?? "");
    internal static void Warning(object? message) => LogMessage(LogLevel.Warning, message?.ToString() ?? "");
    internal static void Error(object? message) => LogMessage(LogLevel.Error, message?.ToString() ?? "");
    internal static void Debug(object? message) => LogMessage(LogLevel.Debug, message?.ToString() ?? "");
}