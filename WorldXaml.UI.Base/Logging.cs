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
    internal static void Info(string message) => XamlConfig.LogMessage(LogLevel.Info, message);
    internal static void Warning(string message) => XamlConfig.LogMessage(LogLevel.Warning, message);
    internal static void Error(string message) => XamlConfig.LogMessage(LogLevel.Error, message);
    internal static void Debug(string message) => XamlConfig.LogMessage(LogLevel.Debug, message);
    internal static void Info(object? message) => XamlConfig.LogMessage(LogLevel.Info, message?.ToString() ?? "");
    internal static void Warning(object? message) => XamlConfig.LogMessage(LogLevel.Warning, message?.ToString() ?? "");
    internal static void Error(object? message) => XamlConfig.LogMessage(LogLevel.Error, message?.ToString() ?? "");
    internal static void Debug(object? message) => XamlConfig.LogMessage(LogLevel.Debug, message?.ToString() ?? "");
}