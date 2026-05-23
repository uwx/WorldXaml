using System.Collections;
using System.Collections.Generic;

namespace NFMWorld.XamlX.Core;

public interface IKnownTypes
{
    public string StyledElement { get; } // WorldXaml.UI.Yoga.Node
    public string Window { get; } // WorldXaml.UI.Yoga.View
    
    public string XmlnsDefinitionAttribute { get; } // Avalonia.Metadata.XmlnsDefinitionAttribute
    public string ContentAttribute { get; } // Avalonia.Metadata.ContentAttribute
    public string WhitespaceSignificantCollectionAttribute { get; } // Avalonia.Metadata.WhitespaceSignificantCollectionAttribute
    public string TrimSurroundingWhitespaceAttribute { get; } // Avalonia.Metadata.TrimSurroundingWhitespaceAttribute
    public string UsableDuringInitializationAttribute { get; } // Avalonia.Metadata.UsableDuringInitializationAttribute
    public string TemplateContentAttribute { get; } // Avalonia.Metadata.TemplateContentAttribute
    public string IRootObjectProvider { get; } // Avalonia.Markup.Xaml.IRootObjectProvider
    public string IUriContext { get; } // Avalonia.Markup.Xaml.IUriContext
    public string IProvideValueTarget { get; } // Avalonia.Markup.Xaml.IProvideValueTarget
    public string IAddChild { get; } // Avalonia.Metadata.IAddChild
    public string IAddChildGeneric { get; } // Avalonia.Metadata.IAddChild`1
    public string IXamlParentStackProviderV1 { get; } // XamlX.Runtime.IXamlParentStackProviderV1
    public string IXamlXmlNamespaceInfoProviderV1 { get; } // XamlX.Runtime.IXamlXmlNamespaceInfoProviderV1
    
    public string CompiledBind { get; } // Avalonia.Data.CompiledBinding
    public string PropertyObject { get; } // WorldXaml.UI.Base.PropertyObject
    public string BindableObject { get; } // WorldXaml.UI.Base.BindableObject
    public string PropertyGeneric { get; } // WorldXaml.UI.Base.Property`1
    public string Property { get; } // WorldXaml.UI.Base.Property
    public string IXamlBinding { get; } // WorldXaml.UI.Base.IXamlBinding
    public string? HotReload { get; } // WorldXaml.UI.Base.Xaml.XamlHotReload
    
    public string ClrPropertyInfo  { get; } // WorldXaml.UI.Base.CompiledClrPropertyInfo
    public string ResolvedPath { get; } // WorldXaml.UI.Base.ResolvedPath
}
