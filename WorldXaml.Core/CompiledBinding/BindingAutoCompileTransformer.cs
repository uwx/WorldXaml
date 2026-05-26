using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace WorldXaml.XamlX;

/// <summary>
/// Runs after ConstructableObjectTransformer. For each {Binding} or {CompiledBind}
/// node, resolves the binding path against the DataContext type (from x:DataType /
/// DataType) and upgrades the node to a CompiledBinding with a ResolvedBindingPathNode.
///
/// - {Binding} with DataContextType available → compiled (path resolved at build time)
/// - {Binding} without DataContextType → left as-is (path resolved at runtime via reflection)
/// - {CompiledBind} always requires DataContextType (throws if missing)
/// </summary>
#if !XAMLX_INTERNAL
public
#endif
class BindingAutoCompileTransformer(
    string bindingFqn,
    string compiledBindFqn,
    string clrPropertyInfoFqn,
    string resolvedPathFqn) : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is not XamlAstConstructableObjectNode binding)
            return node;

        var typeFqn = binding.Type.GetClrType().FullName;
        bool isBinding = typeFqn == bindingFqn;
        bool isCompiledBind = typeFqn == compiledBindFqn;

        if (!isBinding && !isCompiledBind)
            return node;

        // ── Extract the path string ──────────────────────────────────────────
        string? pathStr = null;
        bool pathIsPositionalArg = false;

        // Positional constructor argument: {Binding CenterText}
        if (binding.Arguments.Count > 0 && binding.Arguments[0] is XamlAstTextNode textArg)
        {
            pathStr = textArg.Text;
            pathIsPositionalArg = true;
        }

        // Named property: {Binding Path=CenterText} or {CompiledBind Path=CenterText}
        if (pathStr == null)
        {
            var pathAssignment = binding.Children
                .OfType<XamlPropertyAssignmentNode>()
                .FirstOrDefault(p => p.Property.Name == "Path");

            if (pathAssignment?.Values[0] is XamlAstTextNode pathText)
                pathStr = pathText.Text;
        }

        if (pathStr == null)
            return node; // No path to resolve

        // ── Find the DataContext type from nearest x:DataType ────────────────
        IXamlType? dataContextType = context.ParentNodes()
            .OfType<DataContextTypeMetadataNode>()
            .FirstOrDefault()
            ?.DataContextType;

        // Fall back to root-level DataContext type stored on the transform context.
        if (dataContextType == null && context.TryGetItem<RootDataContextTypeInfo>(out var rootInfo))
            dataContextType = rootInfo.DataContextType;

        if (dataContextType == null)
        {
            if (isCompiledBind)
                context.ReportTransformError(
                    "Cannot use {CompiledBind} without an x:DataType or DataType on this element or a parent.",
                    node);
            return node; // {Binding} without DataContextType → leave for runtime
        }

        // ── Resolve the path segments against the type system ────────────────
        var ts = context.Configuration.TypeSystem;
        var segments = pathStr.Split('.');
        var steps = new List<ResolvedPathStep>();
        var currentType = dataContextType;

        foreach (var seg in segments)
        {
            var prop = currentType.GetAllProperties().FirstOrDefault(p => p.Name == seg);

            if (prop?.Getter == null)
            {
                if (isCompiledBind)
                    context.ReportTransformError(
                        $"Property '{seg}' not found on type '{currentType.GetFqn()}'.", node);
                return node; // {Binding} with unresolvable path → leave for runtime
            }

            steps.Add(new ResolvedPathStep(prop, currentType));
            currentType = prop.Getter.ReturnType;
        }

        // ── Build the resolved path node ─────────────────────────────────────
        var clrPropertyInfoType = ts.GetType(clrPropertyInfoFqn);
        var resolvedPathType = ts.GetType(resolvedPathFqn);
        var resolvedNode = new ResolvedBindingPathNode(
            node, steps, currentType, clrPropertyInfoType, resolvedPathType);

        // ── Build a new CompiledBinding node ─────────────────────────────────
        var compiledBindType = ts.FindType(compiledBindFqn)
            ?? throw new XamlTypeSystemException($"Type '{compiledBindFqn}' not found.");
        var compiledBindCtor = compiledBindType.Constructors
            .FirstOrDefault(c => !c.IsStatic && c.IsPublic && c.Parameters.Count == 0)
            ?? throw new XamlTypeSystemException($"Parameterless constructor not found on '{compiledBindFqn}'.");

        // Create Path property assignment on CompiledBinding
        var pathProp = compiledBindType.GetAllProperties().First(p => p.Name == "Path");
        var pathClrProp = new XamlAstClrProperty(
            node, pathProp.Name, compiledBindType, pathProp.Getter,
            [pathProp.Setter],
            null);

        var pathAssign = new XamlPropertyAssignmentNode(
            node, pathClrProp, pathClrProp.Setters,
            [resolvedNode]);

        var children = new List<IXamlAstNode> { pathAssign };

        // Copy non-Path properties (e.g. Mode) from the original binding
        foreach (var child in binding.Children)
        {
            if (child is XamlPropertyAssignmentNode propAssign && propAssign.Property.Name != "Path")
            {
                // Find the same-named property on CompiledBinding
                var cbProp = compiledBindType.GetAllProperties()
                    .FirstOrDefault(p => p.Name == propAssign.Property.Name);
                if (cbProp != null)
                {
                    var cbClrProp = new XamlAstClrProperty(
                        node, cbProp.Name, compiledBindType, cbProp.Getter,
                        [cbProp.Setter],
                        null);
                    children.Add(new XamlPropertyAssignmentNode(
                        node, cbClrProp, cbClrProp.Setters, propAssign.Values));
                }
            }
        }

        return new XamlAstConstructableObjectNode(
            node,
            new XamlAstClrTypeReference(node, compiledBindType, false),
            compiledBindCtor,
            [],
            children);
    }
}
