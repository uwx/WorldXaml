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
    public static void SetUpCompiler(XamlILCompiler compiler, IKnownTypes knownTypes)
    {
        // Replace TypeExtension markup with efficient XamlTypeExtensionNode (same as x:Type)
        var meIndex = compiler.Transformers.FindIndex(x => x is MarkupExtensionTransformer);
        compiler.Transformers.Insert(meIndex, new TypeExtensionTransformer());

        // Add directive removal transformer
        compiler.Transformers.Add(new RemoveXamlDirectivesTransformer());

        // These must run in this exact order - same as Avalonia's pipeline.
        var insertIndex = compiler.Transformers.FindIndex(t => t is PropertyReferenceResolver);

        compiler.Transformers.Insert(insertIndex, new DataContextTypeTransformer());
        // PropertyObjectTransformer must run AFTER PropertyReferenceResolver (now at insertIndex+1)
        // so that properties are already resolved to XamlAstClrProperty nodes.
        compiler.Transformers.Insert(insertIndex + 2, new PropertyObjectTransformer(knownTypes.PropertyObject, knownTypes.BindableObject, knownTypes.PropertyGeneric, knownTypes.IXamlBinding));

        // BindingAutoCompileTransformer runs AFTER ConstructableObjectTransformer so that
        // {Binding} nodes are already XamlAstConstructableObjectNode with resolved properties.
        // It upgrades {Binding} to CompiledBinding when the DataContext type is known.
        var ctorIndex = compiler.Transformers.FindIndex(t => t is ConstructableObjectTransformer);
        compiler.Transformers.Insert(ctorIndex + 1, new BindingAutoCompileTransformer(
            knownTypes.Binding, knownTypes.CompiledBind,
            knownTypes.ClrPropertyInfo, knownTypes.ResolvedPath));
    }
    
    public static XamlLanguageTypeMappings CreateTypeMappings(IXamlTypeSystem typeSystem, IKnownTypes knownTypes)
    {
        var mappings = new XamlLanguageTypeMappings(typeSystem);

        // Add our custom attributes if they exist
        TryAddType(typeSystem, knownTypes.XmlnsDefinitionAttribute, mappings.XmlnsAttributes);
        TryAddType(typeSystem, knownTypes.ContentAttribute, mappings.ContentAttributes);
        TryAddType(typeSystem, knownTypes.WhitespaceSignificantCollectionAttribute, mappings.WhitespaceSignificantCollectionAttributes);
        TryAddType(typeSystem, knownTypes.TrimSurroundingWhitespaceAttribute, mappings.TrimSurroundingWhitespaceAttributes);
        TryAddType(typeSystem, knownTypes.UsableDuringInitializationAttribute, mappings.UsableDuringInitializationAttributes);
        TryAddType(typeSystem, knownTypes.TemplateContentAttribute, mappings.DeferredContentPropertyAttributes);

        // Set up our runtime interfaces
        var rootObjectProvider = typeSystem.FindType(knownTypes.IRootObjectProvider);
        if (rootObjectProvider != null)
        {
            mappings.RootObjectProvider = rootObjectProvider;
            // Tell XamlX to generate the IntermediateRootObject property getter
            mappings.RootObjectProviderIntermediateRootPropertyName = "IntermediateRootObject";
        }

        var uriContext = typeSystem.FindType(knownTypes.IUriContext);
        if (uriContext != null)
            mappings.UriContextProvider = uriContext;

        var provideValueTarget = typeSystem.FindType(knownTypes.IProvideValueTarget);
        if (provideValueTarget != null)
            mappings.ProvideValueTarget = provideValueTarget;

        var addChild = typeSystem.FindType(knownTypes.IAddChild);
        if (addChild != null)
            mappings.IAddChild = addChild;

        var addChildOfT = typeSystem.FindType(knownTypes.IAddChildGeneric);
        if (addChildOfT != null)
            mappings.IAddChildOfT = addChildOfT;

        // Use XamlX runtime types for parent stack and namespace info
        var parentStackProvider = typeSystem.FindType(knownTypes.IXamlParentStackProviderV1);
        if (parentStackProvider != null)
            mappings.ParentStackProvider = parentStackProvider;

        var xmlNamespaceInfoProvider = typeSystem.FindType(knownTypes.IXamlXmlNamespaceInfoProviderV1);
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