using System;
using XamlX.TypeSystem;

namespace Avalonia.Generators.Common;

internal static class ResolverExtensions
{
    public static bool IsAvaloniaStyledElement(this IXamlType clrType) =>
        Inherits(clrType, "nfm_world.ui.yoga.Node");
    public static bool IsAvaloniaWindow(this IXamlType clrType) =>
        Inherits(clrType, "nfm_world.ui.yoga.Window");

    private static bool Inherits(IXamlType clrType, string metadataName)
    {
        if (string.Equals(clrType.FullName, metadataName, StringComparison.Ordinal))
            return true;
        return clrType.BaseType is { } baseType && Inherits(baseType, metadataName);
    }
}
