using XamlX.Transform;
using XamlX.TypeSystem;

namespace NFMWorld.XamlX.Core
{
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
}