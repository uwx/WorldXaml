using XamlX.TypeSystem;

namespace XamlX.IL.CSharp;

#if !XAMLX_INTERNAL
public
#endif
class CSharpEmitterKnownTypes(IXamlTypeSystem typeSystem)
{
#pragma warning disable IL2122
    public IXamlType SystemVoid { get; } = typeSystem.GetType("System.Void");
    public IXamlType SystemObject { get; } = typeSystem.GetType("System.Object");
    public IXamlType SystemString { get; } = typeSystem.GetType("System.String");
    public IXamlType SystemBoolean { get; } = typeSystem.GetType("System.Boolean");
    public IXamlType SystemInt32 { get; } = typeSystem.GetType("System.Int32");
    public IXamlType SystemInt64 { get; } = typeSystem.GetType("System.Int64");
    public IXamlType SystemSingle { get; } = typeSystem.GetType("System.Single");
    public IXamlType SystemDouble { get; } = typeSystem.GetType("System.Double");
    public IXamlType SystemByte { get; } = typeSystem.GetType("System.Byte");
    public IXamlType SystemSByte { get; } = typeSystem.GetType("System.SByte");
    public IXamlType SystemInt16 { get; } = typeSystem.GetType("System.Int16");
    public IXamlType SystemUInt16 { get; } = typeSystem.GetType("System.UInt16");
    public IXamlType SystemUInt32 { get; } = typeSystem.GetType("System.UInt32");
    public IXamlType SystemUInt64 { get; } = typeSystem.GetType("System.UInt64");
    public IXamlType SystemChar { get; } = typeSystem.GetType("System.Char");
    public IXamlType SystemDecimal { get; } = typeSystem.GetType("System.Decimal");
    public IXamlType SystemIntPtr { get; } = typeSystem.GetType("System.IntPtr");
    public IXamlType SystemUIntPtr { get; } = typeSystem.GetType("System.UIntPtr");
    public IXamlType SystemDelegate { get; } = typeSystem.GetType("System.Delegate");
    public IXamlType SystemType { get; } = typeSystem.GetType("System.Type");
#pragma warning restore IL2122
}