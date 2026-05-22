using System;
using WorldXaml.Generator.Common;
using WorldXaml.Generator.Common.Domain;
using WorldXaml.Generator.NameGenerator;
using Microsoft.CodeAnalysis.Diagnostics;

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
        WorldXamlGeneratorHotReloadTypeName = GetStringProperty(
            options,
            BuildProperties.WorldXamlGeneratorHotReloadTypeName,
            "WorldXaml.UI.Base.Xaml.XamlHotReload");
        WorldXamlGeneratorStyledElementTypeName = GetStringProperty(
            options,
            BuildProperties.WorldXamlGeneratorStyledElementTypeName,
            "WorldXaml.UI.Yoga.Node");
        WorldXamlGeneratorWindowTypeName = GetStringProperty(
            options,
            BuildProperties.WorldXamlGeneratorWindowTypeName,
            "WorldXaml.UI.Yoga.View");
        WorldXamlGeneratorCompiledBindTypeName = GetStringProperty(
            options,
            BuildProperties.WorldXamlGeneratorCompiledBindTypeName,
            "Avalonia.Data.CompiledBinding");
        WorldXamlGeneratorPropertyObjectTypeName = GetStringProperty(
            options,
            BuildProperties.WorldXamlGeneratorPropertyObjectTypeName,
            "WorldXaml.UI.Base.PropertyObject");
        WorldXamlGeneratorBindableObjectTypeName = GetStringProperty(
            options,
            BuildProperties.WorldXamlGeneratorBindableObjectTypeName,
            "WorldXaml.UI.Base.BindableObject");
        WorldXamlGeneratorPropertyGenericTypeName = GetStringProperty(
            options,
            BuildProperties.WorldXamlGeneratorPropertyGenericTypeName,
            "WorldXaml.UI.Base.Property`1");
        WorldXamlGeneratorIXamlBindingTypeName = GetStringProperty(
            options,
            BuildProperties.WorldXamlGeneratorIXamlBindingTypeName,
            "WorldXaml.UI.Base.IXamlBinding");
    }

    public bool WorldXamlGeneratorIsEnabled { get; }
    public Behavior WorldXamlGeneratorBehavior { get; }
    public NamedFieldModifier WorldXamlGeneratorClassFieldModifier { get; }
    public ViewFileNamingStrategy WorldXamlGeneratorViewFileNamingStrategy { get; }
    public IGlobPattern WorldXamlGeneratorFilterByPath { get; }
    public IGlobPattern WorldXamlGeneratorFilterByNamespace { get; }
    public bool WorldXamlGeneratorAttachDevTools { get; }
    public bool WorldXamlGeneratorIsHotReloadingEnabled { get; }
    public string WorldXamlGeneratorHotReloadTypeName { get; }
    public string WorldXamlGeneratorWindowTypeName { get; }
    public string WorldXamlGeneratorStyledElementTypeName { get; }
    public string WorldXamlGeneratorCompiledBindTypeName { get; }
    public string WorldXamlGeneratorPropertyObjectTypeName { get; }
    public string WorldXamlGeneratorBindableObjectTypeName { get; }
    public string WorldXamlGeneratorPropertyGenericTypeName { get; }
    public string WorldXamlGeneratorIXamlBindingTypeName { get; }

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
