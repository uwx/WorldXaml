using XamlX.TypeSystem;

namespace WorldXaml.XamlX;

#if !XAMLX_INTERNAL
public
#endif
record ResolvedPathStep(IXamlProperty Property, IXamlType OwnerType);