using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.Transform;

namespace WorldXaml.XamlX;

/// <summary>
/// Converts the string Path on a {CompiledBind} into a ParsedBindingPathNode
/// that carries the raw segments, ready for type resolution.
/// </summary>
#if !XAMLX_INTERNAL
public
#endif
class BindingPathParser(string CompiledBindFqn) : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is not XamlAstConstructableObjectNode binding)
            return node;

        var bindingClrType = binding.Type.GetClrType();
        if (bindingClrType.FullName != CompiledBindFqn)
            return node;

        // Find Path= assignment.
        var pathAssignment = binding.Children
            .OfType<XamlPropertyAssignmentNode>()
            .FirstOrDefault(p => p.Property.Name == "Path");

        if (pathAssignment?.Values[0] is not XamlAstTextNode pathText)
            return node;

        // Replace the string value with a typed path node.
        var segments = pathText.Text.Split('.').ToList();
        pathAssignment.Values[0] = new ParsedBindingPathNode(pathText, segments);

        return node;
    }
}

/// <summary>Intermediate AST node carrying the raw path segments.</summary>
#if !XAMLX_INTERNAL
public
#endif
class ParsedBindingPathNode(IXamlLineInfo lineInfo, List<string> segments)
    : XamlAstNode(lineInfo), IXamlAstValueNode
{
    public List<string> Segments { get; } = segments;

    // Type is string until the path transformer resolves it to the real type.
    // Will be filled in by BindingPathTransformer.
    public IXamlAstTypeReference Type { get; set; } = new XamlAstClrTypeReference(lineInfo, null!, false);
}