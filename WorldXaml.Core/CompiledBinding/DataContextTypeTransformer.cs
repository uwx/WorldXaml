using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace WorldXaml.XamlX;

/// <summary>
/// Reads x:DataType="vm:MyViewModel" from an object node and wraps it
/// with a metadata node so downstream transformers can find the DataContext type.
/// </summary>
#if !XAMLX_INTERNAL
public
#endif
class DataContextTypeTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        // Already processed — skip.
        if (context.ParentNodes().FirstOrDefault() is DataContextTypeMetadataNode)
            return node;

        if (node is not XamlAstConstructableObjectNode on)
            return node;

        IXamlType? dataContextType = null;

        for (int i = 0; i < on.Children.Count; i++)
        {
            // Look for x:DataType directive.
            if (on.Children[i] is XamlAstXmlDirective { Namespace: XamlNamespaces.Xaml2006, Name: "DataType", Values.Count: 1 } directive)
            {
                on.Children.RemoveAt(i);
                i--;

                dataContextType = directive.Values[0] switch
                {
                    XamlTypeExtensionNode typeNode => typeNode.Value.GetClrType(),
                    XamlAstTextNode text => TypeReferenceResolver.ResolveType(
                        context, text.Text, false, text, true).GetClrType(),
                    _ => null
                };
            }
            // Look for DataType property
            else if (on.Children[i] is XamlAstXamlPropertyValueNode { Property: XamlAstNamePropertyReference { Name: "DataType" }, Values.Count: 1 } propertyValueNode)
            {
                on.Children.RemoveAt(i);
                i--;

                dataContextType = propertyValueNode.Values[0] switch
                {
                    XamlTypeExtensionNode typeNode => typeNode.Value.GetClrType(),
                    XamlAstTextNode text => TypeReferenceResolver.ResolveType(
                        context, text.Text, false, text, true).GetClrType(),
                    _ => null
                };
            }
        }

        if (dataContextType is null)
            return node;

        return new DataContextTypeMetadataNode(on, dataContextType);
    }
}

/// <summary>
/// Wraps an object node and carries the resolved DataContext type for
/// downstream transformers to find via ParentNodes().
/// </summary>
#if !XAMLX_INTERNAL
public
#endif
class DataContextTypeMetadataNode(IXamlAstValueNode value, IXamlType dataContextType)
    : XamlValueWithSideEffectNodeBase(value, value)
{
    public IXamlType DataContextType { get; } = dataContextType;
}