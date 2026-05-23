using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using XamlX.IL.CSharp;
using XamlX.TypeSystem;

namespace XamlX.CSharp;

/// <summary>
/// Utilities for formatting C# type names and string literals.
/// </summary>
internal static class CSharpFormatting
{
    /// <summary>
    /// Formats an IXamlType as a fully-qualified C# type name using global:: prefix.
    /// </summary>
    public static string FormatType(CSharpEmitterKnownTypes knownTypes, IXamlType type)
    {
        if (type.Equals(knownTypes.SystemVoid)) return "void";
        if (type.Equals(knownTypes.SystemObject)) return "object";
        if (type.Equals(knownTypes.SystemString)) return "string";
        if (type.Equals(knownTypes.SystemBoolean)) return "bool";
        if (type.Equals(knownTypes.SystemInt32)) return "int";
        if (type.Equals(knownTypes.SystemInt64)) return "long";
        if (type.Equals(knownTypes.SystemSingle)) return "float";
        if (type.Equals(knownTypes.SystemDouble)) return "double";
        if (type.Equals(knownTypes.SystemByte)) return "byte";
        if (type.Equals(knownTypes.SystemSByte)) return "sbyte";
        if (type.Equals(knownTypes.SystemInt16)) return "short";
        if (type.Equals(knownTypes.SystemUInt16)) return "ushort";
        if (type.Equals(knownTypes.SystemUInt32)) return "uint";
        if (type.Equals(knownTypes.SystemUInt64)) return "ulong";
        if (type.Equals(knownTypes.SystemChar)) return "char";
        if (type.Equals(knownTypes.SystemDecimal)) return "decimal";
        if (type.Equals(knownTypes.SystemIntPtr)) return "nint";
        if (type.Equals(knownTypes.SystemUIntPtr)) return "nuint";

        if (type is { IsArray: true, ArrayElementType: not null })
            return FormatType(knownTypes, type.ArrayElementType) + "[]";

        if (type is CSharpTypeBuilder csBuilder)
        {
            if (csBuilder.GenericParameters.Count > 0)
                return $"{csBuilder.FullName}<{string.Join(", ", csBuilder.GenericParameters.Select(p => p.Name))}>";
            return csBuilder.FullName;
        }

        // Constructed generic CSharpTypeBuilder (e.g. Context<SomeNode>)
        if (type is ConstructedCSharpType constructed)
        {
            var defName = constructed.GenericTypeDefinition!.FullName;
            var genericArgs = string.Join(", ", constructed.GenericArguments.Select(t => FormatType(knownTypes, t)));
            return $"{defName}<{genericArgs}>";
        }

        // Generic parameter types (like TTarget) - no global:: prefix
        if (type is CSharpGenericParameterType)
            return type.Name;

        var name = type.FullName;

        // Handle generic types
        if (type.GenericArguments.Count > 0)
        {
            var baseName = name;
            var backtick = baseName.IndexOf('`');
            if (backtick >= 0)
                baseName = baseName[..backtick];

            var genericArgs = string.Join(", ", type.GenericArguments.Select(t => FormatType(knownTypes, t)));
            return $"global::{baseName.Replace('+', '.')}<{genericArgs}>";
        }

        // Handle nested types
        name = name.Replace('+', '.');

        return $"global::{name}";
    }

    /// <summary>
    /// Formats a string as a C# string literal with proper escaping.
    /// </summary>
    public static string FormatStringLiteral(string value)
    {
        var sb = new DefaultInterpolatedStringHandler(-1, -1, null, stackalloc char[Math.Min(value.Length + 2, 128)]);
        sb.AppendLiteral("\"");
        foreach (var ch in value)
        {
            sb.AppendLiteral(ch switch
            {
                '\\' => @"\\",
                '"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                '\0' => "\\0",
                '\a' => "\\a",
                '\b' => "\\b",
                '\f' => "\\f",
                '\v' => "\\v",
                _ when char.IsControl(ch) => $"\\u{(int)ch:X4}",
                _ => ch.ToString()
            });
        }
        sb.AppendLiteral("\"");
        return sb.ToStringAndClear();
    }
}