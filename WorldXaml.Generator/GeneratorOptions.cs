using System;
using WorldXaml.Generator.Common;
using WorldXaml.Generator.Common.Domain;
using WorldXaml.Generator.NameGenerator;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WorldXaml.Generator;

// When update these enum values, don't forget to update WorldXaml.Generator.props.
internal enum BuildProperties
{
    WorldXamlGeneratorIsEnabled = 0,
    WorldXamlGeneratorBehavior = 1,
    WorldXamlGeneratorDefaultFieldModifier = 2,
    WorldXamlGeneratorFilterByPath = 3,
    WorldXamlGeneratorFilterByNamespace = 4,
    WorldXamlGeneratorViewFileNamingStrategy = 5,
    WorldXamlGeneratorAttachDevTools = 6,
    WorldXamlGeneratorIsHotReloadingEnabled = 7,
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
    }

    public bool WorldXamlGeneratorIsEnabled { get; }
    
    public Behavior WorldXamlGeneratorBehavior { get; }

    public NamedFieldModifier WorldXamlGeneratorClassFieldModifier { get; }

    public ViewFileNamingStrategy WorldXamlGeneratorViewFileNamingStrategy { get; }

    public IGlobPattern WorldXamlGeneratorFilterByPath { get; }

    public IGlobPattern WorldXamlGeneratorFilterByNamespace { get; }

    public bool WorldXamlGeneratorAttachDevTools { get; }

    public bool WorldXamlGeneratorIsHotReloadingEnabled { get; }

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
}
