using System.Text;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

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

public static class XamlHelpers
{
    public static XamlLanguageTypeMappings CreateTypeMappings(IXamlTypeSystem typeSystem)
    {
        var mappings = new XamlLanguageTypeMappings(typeSystem);

        // Add our custom attributes if they exist
        TryAddType(typeSystem, "Avalonia.Metadata.XmlnsDefinitionAttribute", mappings.XmlnsAttributes);
        TryAddType(typeSystem, "Avalonia.Metadata.ContentAttribute", mappings.ContentAttributes);
        TryAddType(typeSystem, "Avalonia.Metadata.WhitespaceSignificantCollectionAttribute", mappings.WhitespaceSignificantCollectionAttributes);
        TryAddType(typeSystem, "Avalonia.Metadata.TrimSurroundingWhitespaceAttribute", mappings.TrimSurroundingWhitespaceAttributes);
        TryAddType(typeSystem, "Avalonia.Metadata.UsableDuringInitializationAttribute", mappings.UsableDuringInitializationAttributes);
        TryAddType(typeSystem, "Avalonia.Metadata.TemplateContentAttribute", mappings.DeferredContentPropertyAttributes);

        // Set up our runtime interfaces
        var rootObjectProvider = typeSystem.FindType("Avalonia.Markup.Xaml.IRootObjectProvider");
        if (rootObjectProvider != null)
        {
            mappings.RootObjectProvider = rootObjectProvider;
            // Tell XamlX to generate the IntermediateRootObject property getter
            mappings.RootObjectProviderIntermediateRootPropertyName = "IntermediateRootObject";
        }

        var uriContext = typeSystem.FindType("Avalonia.Markup.Xaml.IUriContext");
        if (uriContext != null)
            mappings.UriContextProvider = uriContext;

        var provideValueTarget = typeSystem.FindType("Avalonia.Markup.Xaml.IProvideValueTarget");
        if (provideValueTarget != null)
            mappings.ProvideValueTarget = provideValueTarget;

        var addChild = typeSystem.FindType("Avalonia.Metadata.IAddChild");
        if (addChild != null)
            mappings.IAddChild = addChild;

        var addChildOfT = typeSystem.FindType("Avalonia.Metadata.IAddChild`1");
        if (addChildOfT != null)
            mappings.IAddChildOfT = addChildOfT;

        // Use XamlX runtime types for parent stack and namespace info
        var parentStackProvider = typeSystem.FindType("XamlX.Runtime.IXamlParentStackProviderV1");
        if (parentStackProvider != null)
            mappings.ParentStackProvider = parentStackProvider;

        var xmlNamespaceInfoProvider = typeSystem.FindType("XamlX.Runtime.IXamlXmlNamespaceInfoProviderV1");
        if (xmlNamespaceInfoProvider != null)
            mappings.XmlNamespaceInfoProvider = xmlNamespaceInfoProvider;

        return mappings;
    }
    
    private static void TryAddType(IXamlTypeSystem typeSystem, string typeName, List<IXamlType> list)
    {
        var type = typeSystem.FindType(typeName);
        if (type != null)
            list.Add(type);
    }
}

public class XamlFileSource(string filePath, string xml) : IFileSource
{
    public string FilePath { get; } = filePath;
    public byte[] FileContents { get; } = Encoding.UTF8.GetBytes(xml);
}