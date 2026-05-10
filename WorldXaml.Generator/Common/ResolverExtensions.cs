using System;
using XamlX.TypeSystem;

namespace WorldXaml.Generator.Common;

internal static class ResolverExtensions
{
    extension(IXamlType clrType)
    {
        public bool IsAvaloniaStyledElement(string styledElementTypeName) =>
            Inherits(clrType, styledElementTypeName);

        public bool IsAvaloniaWindow(string windowTypeName) =>
            Inherits(clrType, windowTypeName);
    }

    private static bool Inherits(IXamlType clrType, string metadataName)
    {
        if (string.Equals(clrType.FullName, metadataName, StringComparison.Ordinal))
            return true;
        return clrType.BaseType is { } baseType && Inherits(baseType, metadataName);
    }
}
