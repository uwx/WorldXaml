using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace WorldXaml.XamlX;

/// <summary>
/// Reads x:DataType="vm:MyViewModel" or DataType="vm:MyViewModel" from an object
/// node and either wraps it with a <see cref="DataContextTypeMetadataNode"/> (for
/// non-root nodes) or stores the type in <see cref="RootDataContextTypeInfo"/>
/// on the transform context (for root nodes that cannot be wrapped).
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

        List<IXamlAstNode> children;
        if (node is XamlAstObjectNode objNode)
            children = objNode.Children;
        else if (node is XamlAstConstructableObjectNode conNode)
            children = conNode.Children;
        else
            return node;

        IXamlType? dataContextType = null;

        foreach (var child in children)
        {
            // Look for x:DataType directive.
            if (child is XamlAstXmlDirective { Namespace: XamlNamespaces.Xaml2006, Name: "DataType", Values.Count: 1 } directive)
            {
                dataContextType = directive.Values[0] switch
                {
                    XamlTypeExtensionNode typeNode => typeNode.Value.GetClrType(),
                    XamlAstTextNode text => TypeReferenceResolver.ResolveType(
                        context, text.Text, false, text, true).GetClrType(),
                    _ => null
                };
                if (dataContextType != null) break;
            }
            // Look for DataType property
            else if (child is XamlAstXamlPropertyValueNode { Property: XamlAstNamePropertyReference { Name: "DataType" }, Values.Count: 1 } propertyValueNode)
            {
                dataContextType = propertyValueNode.Values[0] switch
                {
                    XamlTypeExtensionNode typeNode => typeNode.Value.GetClrType(),
                    XamlAstTextNode text => TypeReferenceResolver.ResolveType(
                        context, text.Text, false, text, true).GetClrType(),
                    _ => null
                };
                if (dataContextType != null) break;
            }
        }

        if (dataContextType is null)
            return node;

        // Root nodes (no parents) cannot be wrapped because XamlImperativeCompiler
        // hard-casts doc.Root to XamlValueWithManipulationNode. Store this on the
        // transform context instead.
        if (!context.ParentNodes().Any())
        {
            context.SetItem(new RootDataContextTypeInfo(dataContextType));
            return node;
        }

        return new DataContextTypeMetadataNode((IXamlAstValueNode)node, dataContextType);
    }
}

/// <summary>
/// Stored on the <see cref="AstTransformationContext"/> when the root element
/// declares a DataContext type but cannot be wrapped in a metadata node.
/// </summary>
#if !XAMLX_INTERNAL
public
#endif
class RootDataContextTypeInfo(IXamlType dataContextType)
{
    public IXamlType DataContextType { get; } = dataContextType;
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