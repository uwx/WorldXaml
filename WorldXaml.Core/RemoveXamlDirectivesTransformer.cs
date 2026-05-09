using XamlX.Ast;
using XamlX.Transform;

namespace NFMWorld.XamlX.Core;

/// <summary>
/// Transformer that removes x:Class and other XAML directives that should not be emitted.
/// </summary>
public class RemoveXamlDirectivesTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        // Remove x:Class and other preprocessing directives from the manipulation children
        if (node is XamlManipulationGroupNode group)
        {
            group.Children.RemoveAll(n => n is XamlAstXmlDirective { Name: "Class" or "Key" or "Name" });
        }

        // Also handle object nodes
        if (node is XamlAstObjectNode objNode)
        {
            objNode.Children.RemoveAll(n => n is XamlAstXmlDirective { Name: "Class" or "Key" or "Name" });
        }

        // Handle XamlValueWithManipulationNode which is the root
        if (node is XamlValueWithManipulationNode { Manipulation: XamlManipulationGroupNode manipGroup })
        {
            manipGroup.Children.RemoveAll(n => n is XamlAstXmlDirective { Name: "Class" or "Key" or "Name" });
        }

        return node;
    }
}