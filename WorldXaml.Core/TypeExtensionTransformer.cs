using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace WorldXaml.XamlX;

/// <summary>
/// Converts <c>{TypeExtension local:MyType}</c> markup extension nodes into
/// <see cref="XamlTypeExtensionNode"/>, producing the same efficient emit as
/// <c>{x:Type local:MyType}</c>.
/// </summary>
#if !XAMLX_INTERNAL
public
#endif
class TypeExtensionTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is not XamlAstObjectNode objectNode)
            return node;

        if (objectNode.Type is not XamlAstClrTypeReference clrType
            || clrType.Type.FullName != "WorldXaml.UI.Base.TypeExtension")
            return node;

        // Extract the type argument — positional or named "Type" property.
        XamlAstTextNode? textNode = null;

        if (objectNode.Arguments.Count == 1 && objectNode.Children.Count == 0)
        {
            textNode = objectNode.Arguments[0] as XamlAstTextNode;
        }
        else if (objectNode.Arguments.Count == 0 && objectNode.Children.Count == 1
                 && objectNode.Children[0] is XamlAstXamlPropertyValueNode pnode
                 && pnode.Property is XamlAstNamePropertyReference pref
                 && pref.Name == "Type"
                 && pnode.Values.Count == 1)
        {
            textNode = pnode.Values[0] as XamlAstTextNode;
        }

        if (textNode == null)
            return node;

        var resolvedType = TypeReferenceResolver.ResolveType(context, textNode.Text, false, textNode, true);
        var systemType = context.Configuration.TypeSystem.GetType("System.Type");

        return new XamlTypeExtensionNode(node, resolvedType, systemType);
    }
}
