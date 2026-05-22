using System.Collections.Generic;
using WorldXaml.XamlX;
using XamlX.IL;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace NFMWorld.XamlX.Core;

#if !XAMLX_INTERNAL
public
#endif
static class XamlHelpers
{
    public static void SetUpCompiler(XamlILCompiler _compiler, string CompiledBindFqn, string PropertyObjectFqn, string BindableObjectFqn, string PropertyGenericFqn, string IXamlBindingFqn)
    {
        // Replace TypeExtension markup with efficient XamlTypeExtensionNode (same as x:Type)
        var meIndex = _compiler.Transformers.FindIndex(x => x is MarkupExtensionTransformer);
        _compiler.Transformers.Insert(meIndex, new TypeExtensionTransformer());

        // Add directive removal transformer
        _compiler.Transformers.Add(new RemoveXamlDirectivesTransformer());

        // These must run in this exact order - same as Avalonia's pipeline.
        var insertIndex = _compiler.Transformers.FindIndex(t => t is PropertyReferenceResolver);

        _compiler.Transformers.Insert(insertIndex,           new DataContextTypeTransformer());
        _compiler.Transformers.Insert(insertIndex + 1, new BindingPathParser(CompiledBindFqn));
        _compiler.Transformers.Insert(insertIndex + 2, new BindingPathTransformer(CompiledBindFqn));
        _compiler.Transformers.Insert(insertIndex + 3, new PropertyObjectTransformer(PropertyObjectFqn, BindableObjectFqn, PropertyGenericFqn, IXamlBindingFqn));
    }
    
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