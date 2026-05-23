using XamlX;
using XamlX.Ast;
using XamlX.Transform;

namespace NFMWorld.XamlX.Core;

/// <summary>
/// Transformer that removes x:Class and other XAML directives that should not be emitted.
/// </summary>
#if !XAMLX_INTERNAL
public
#endif
class RemoveXamlDirectivesTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        // Remove x:Class and other preprocessing directives from the manipulation children
        if (node is XamlManipulationGroupNode group)
        {
            group.Children.RemoveAll(ShouldRemoveDirective);
        }

        // Also handle object nodes
        if (node is XamlAstObjectNode objNode)
        {
            objNode.Children.RemoveAll(ShouldRemoveDirective);
        }

        // Handle XamlValueWithManipulationNode which is the root
        if (node is XamlValueWithManipulationNode { Manipulation: XamlManipulationGroupNode manipGroup })
        {
            manipGroup.Children.RemoveAll(ShouldRemoveDirective);
        }

        return node;
    }

    private static bool ShouldRemoveDirective(IXamlAstNode n)
    {
        if (n is not XamlAstXmlDirective directive)
            return false;

        // Remove x:Class, x:Key, x:Name
        if (directive.Name is "Class" or "Key" or "Name")
            return true;

        // Remove all design-time Blend directives (d:DataContext, d:DesignInstance, etc.)
        if (directive.Namespace == XamlNamespaces.Blend2008)
            return true;

        return false;
    }
}