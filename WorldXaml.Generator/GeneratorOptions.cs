using System;
using System.Collections;
using System.Collections.Generic;
using WorldXaml.Generator.Common;
using WorldXaml.Generator.Common.Domain;
using WorldXaml.Generator.NameGenerator;
using Microsoft.CodeAnalysis.Diagnostics;
using NFMWorld.XamlX.Core;

namespace WorldXaml.Generator;

// When update these enum values, don't forget to update WorldXaml.Generator.props.
internal enum BuildProperties
{
    WorldXamlGeneratorIsEnabled,
    WorldXamlGeneratorBehavior,
    WorldXamlGeneratorDefaultFieldModifier,
    WorldXamlGeneratorFilterByPath,
    WorldXamlGeneratorFilterByNamespace,
    WorldXamlGeneratorViewFileNamingStrategy,
    WorldXamlGeneratorAttachDevTools,
    WorldXamlGeneratorIsHotReloadingEnabled,
    WorldXamlGeneratorHotReloadTypeName,
    WorldXamlGeneratorStyledElementTypeName,
    WorldXamlGeneratorWindowTypeName,
    WorldXamlGeneratorCompiledBindTypeName,
    WorldXamlGeneratorPropertyObjectTypeName,
    WorldXamlGeneratorBindableObjectTypeName,
    WorldXamlGeneratorPropertyGenericTypeName,
    WorldXamlGeneratorIXamlBindingTypeName,
    WorldXamlGeneratorXmlnsDefinitionAttributeTypeName,
    WorldXamlGeneratorContentAttributeTypeName,
    WorldXamlGeneratorWhitespaceSignificantCollectionAttributeTypeName,
    WorldXamlGeneratorTrimSurroundingWhitespaceAttributeTypeName,
    WorldXamlGeneratorUsableDuringInitializationAttributeTypeName,
    WorldXamlGeneratorTemplateContentAttributeTypeName,
    WorldXamlGeneratorIRootObjectProviderTypeName,
    WorldXamlGeneratorIUriContextTypeName,
    WorldXamlGeneratorIProvideValueTargetTypeName,
    WorldXamlGeneratorIAddChildTypeName,
    WorldXamlGeneratorIAddChildGenericTypeName,
    WorldXamlGeneratorIXamlParentStackProviderV1TypeName,
    WorldXamlGeneratorIXamlXmlNamespaceInfoProviderV1TypeName,
    WorldXamlGeneratorPropertyTypeName,
    // TODO add other generators properties here.
}

internal record GeneratorOptions
{
    public GeneratorOptions(AnalyzerConfigOptions options)
    {
        WorldXamlGeneratorIsEnabled = GetBoolProperty(
            options,
            BuildProperties.WorldXamlGeneratorIsEnabled,
            true);
        WorldXamlGeneratorBehavior = GetEnumProperty(
            options,
            BuildProperties.WorldXamlGeneratorBehavior,
            Behavior.WithXamlXCompilation);
        WorldXamlGeneratorClassFieldModifier = GetEnumProperty(
            options,
            BuildProperties.WorldXamlGeneratorDefaultFieldModifier,
            NamedFieldModifier.Internal);
        WorldXamlGeneratorViewFileNamingStrategy = GetEnumProperty(
            options,
            BuildProperties.WorldXamlGeneratorViewFileNamingStrategy,
            ViewFileNamingStrategy.NamespaceAndClassName);
        WorldXamlGeneratorFilterByPath = new GlobPatternGroup(GetStringArrayProperty(
            options,
            BuildProperties.WorldXamlGeneratorFilterByPath,
            "*"));
        WorldXamlGeneratorFilterByNamespace = new GlobPatternGroup(GetStringArrayProperty(
            options,
            BuildProperties.WorldXamlGeneratorFilterByNamespace,
            "*"));
        WorldXamlGeneratorAttachDevTools = GetBoolProperty(
            options,
            BuildProperties.WorldXamlGeneratorAttachDevTools,
            true);
        WorldXamlGeneratorIsHotReloadingEnabled = GetBoolProperty(
            options,
            BuildProperties.WorldXamlGeneratorIsHotReloadingEnabled,
            true);
        KnownTypes = new TheKnownTypes(options);
    }

    internal class TheKnownTypes(AnalyzerConfigOptions options) : IKnownTypes, IEnumerable<KeyValuePair<string, string?>>
    {
        public string StyledElement { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorStyledElementTypeName, "WorldXaml.UI.Yoga.Node");
        public string Window { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorWindowTypeName, "WorldXaml.UI.Yoga.View");

        public string XmlnsDefinitionAttribute { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorXmlnsDefinitionAttributeTypeName, "System.Windows.Markup.XmlnsDefinitionAttribute");
        public string ContentAttribute { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorContentAttributeTypeName, "Avalonia.Metadata.ContentAttribute");
        public string WhitespaceSignificantCollectionAttribute { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorWhitespaceSignificantCollectionAttributeTypeName, "Avalonia.Metadata.WhitespaceSignificantCollectionAttribute");
        public string TrimSurroundingWhitespaceAttribute { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorTrimSurroundingWhitespaceAttributeTypeName, "Avalonia.Metadata.TrimSurroundingWhitespaceAttribute");
        public string UsableDuringInitializationAttribute { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorUsableDuringInitializationAttributeTypeName, "Avalonia.Metadata.UsableDuringInitializationAttribute");
        public string TemplateContentAttribute { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorTemplateContentAttributeTypeName, "Avalonia.Metadata.TemplateContentAttribute");
        public string IRootObjectProvider { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorIRootObjectProviderTypeName, "Avalonia.Markup.Xaml.IRootObjectProvider");
        public string IUriContext { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorIUriContextTypeName, "Avalonia.Markup.Xaml.IUriContext");
        public string IProvideValueTarget { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorIProvideValueTargetTypeName, "Avalonia.Markup.Xaml.IProvideValueTarget");
        public string IAddChild { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorIAddChildTypeName, "Avalonia.Metadata.IAddChild");
        public string IAddChildGeneric { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorIAddChildGenericTypeName, "Avalonia.Metadata.IAddChild`1");
        public string IXamlParentStackProviderV1 { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorIXamlParentStackProviderV1TypeName, "XamlX.Runtime.IXamlParentStackProviderV1");
        public string IXamlXmlNamespaceInfoProviderV1 { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorIXamlXmlNamespaceInfoProviderV1TypeName, "XamlX.Runtime.IXamlXmlNamespaceInfoProviderV1");
    
        public string CompiledBind { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorCompiledBindTypeName, "Avalonia.Data.CompiledBinding");
        public string PropertyObject { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorPropertyObjectTypeName, "WorldXaml.UI.Base.PropertyObject");
        public string BindableObject { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorBindableObjectTypeName, "WorldXaml.UI.Base.BindableObject");
        public string PropertyGeneric { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorPropertyGenericTypeName, "Avalonia.StyledProperty`1");
        public string Property { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorPropertyTypeName, "Avalonia.AvaloniaProperty");
        public string IXamlBinding { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorIXamlBindingTypeName, "WorldXaml.UI.Base.IXamlBinding");
        public string? HotReload { get; } = GetStringProperty(options, BuildProperties.WorldXamlGeneratorHotReloadTypeName, "WorldXaml.UI.Base.Xaml.XamlHotReload");

        public IEnumerator<KeyValuePair<string, string?>> GetEnumerator()
        {
            yield return KeyValuePair.Create<string, string?>(nameof(StyledElement), StyledElement);
            yield return KeyValuePair.Create<string, string?>(nameof(Window), Window);
            yield return KeyValuePair.Create<string, string?>(nameof(XmlnsDefinitionAttribute), XmlnsDefinitionAttribute);
            yield return KeyValuePair.Create<string, string?>(nameof(ContentAttribute), ContentAttribute);
            yield return KeyValuePair.Create<string, string?>(nameof(WhitespaceSignificantCollectionAttribute), WhitespaceSignificantCollectionAttribute);
            yield return KeyValuePair.Create<string, string?>(nameof(TrimSurroundingWhitespaceAttribute), TrimSurroundingWhitespaceAttribute);
            yield return KeyValuePair.Create<string, string?>(nameof(UsableDuringInitializationAttribute), UsableDuringInitializationAttribute);
            yield return KeyValuePair.Create<string, string?>(nameof(TemplateContentAttribute), TemplateContentAttribute);
            yield return KeyValuePair.Create<string, string?>(nameof(IRootObjectProvider), IRootObjectProvider);
            yield return KeyValuePair.Create<string, string?>(nameof(IUriContext), IUriContext);
            yield return KeyValuePair.Create<string, string?>(nameof(IProvideValueTarget), IProvideValueTarget);
            yield return KeyValuePair.Create<string, string?>(nameof(IAddChild), IAddChild);
            yield return KeyValuePair.Create<string, string?>(nameof(IAddChildGeneric), IAddChildGeneric);
            yield return KeyValuePair.Create<string, string?>(nameof(IXamlParentStackProviderV1), IXamlParentStackProviderV1);
            yield return KeyValuePair.Create<string, string?>(nameof(IXamlXmlNamespaceInfoProviderV1), IXamlXmlNamespaceInfoProviderV1);
            yield return KeyValuePair.Create<string, string?>(nameof(CompiledBind), CompiledBind);
            yield return KeyValuePair.Create<string, string?>(nameof(PropertyObject), PropertyObject);
            yield return KeyValuePair.Create<string, string?>(nameof(BindableObject), BindableObject);
            yield return KeyValuePair.Create<string, string?>(nameof(PropertyGeneric), PropertyGeneric);
            yield return KeyValuePair.Create<string, string?>(nameof(Property), Property);
            yield return KeyValuePair.Create<string, string?>(nameof(IXamlBinding), IXamlBinding);
            yield return KeyValuePair.Create<string, string?>(nameof(HotReload), HotReload);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public bool WorldXamlGeneratorIsEnabled { get; }
    public Behavior WorldXamlGeneratorBehavior { get; }
    public NamedFieldModifier WorldXamlGeneratorClassFieldModifier { get; }
    public ViewFileNamingStrategy WorldXamlGeneratorViewFileNamingStrategy { get; }
    public IGlobPattern WorldXamlGeneratorFilterByPath { get; }
    public IGlobPattern WorldXamlGeneratorFilterByNamespace { get; }
    public bool WorldXamlGeneratorAttachDevTools { get; }
    public bool WorldXamlGeneratorIsHotReloadingEnabled { get; }
    public TheKnownTypes KnownTypes { get; }

    private static string[] GetStringArrayProperty(AnalyzerConfigOptions options, BuildProperties name, string defaultValue)
    {
        var key = name.ToString();
        var value = options.GetMsBuildProperty(key, defaultValue);
        return value.Contains(";") ? value.Split(';') : [value];
    }

    private static TEnum GetEnumProperty<TEnum>(AnalyzerConfigOptions options, BuildProperties name, TEnum defaultValue) where TEnum : struct
    {
        var key = name.ToString();
        var value = options.GetMsBuildProperty(key, defaultValue.ToString());
        return Enum.TryParse(value, true, out TEnum behavior) ? behavior : defaultValue;
    }

    private static bool GetBoolProperty(AnalyzerConfigOptions options, BuildProperties name, bool defaultValue)
    {
        var key = name.ToString();
        var value = options.GetMsBuildProperty(key, defaultValue.ToString());
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    private static string GetStringProperty(AnalyzerConfigOptions options, BuildProperties name, string defaultValue)
    {
        var key = name.ToString();
        var value = options.GetMsBuildProperty(key, defaultValue);
        return value;
    }
}
