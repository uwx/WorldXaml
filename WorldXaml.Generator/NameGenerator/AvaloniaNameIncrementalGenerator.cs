using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using WorldXaml.Generator.Common;
using WorldXaml.Generator.Common.Domain;
using WorldXaml.Generator.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using XamlX;
using XamlX.TypeSystem;

namespace WorldXaml.Generator.NameGenerator;

[Generator(LanguageNames.CSharp)]
public class AvaloniaNameIncrementalGenerator : IIncrementalGenerator
{
    private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";
    private static readonly MiniCompiler s_noopCompiler = MiniCompiler.CreateNoop();

#if AVA_DEBUG
    public static List<string> Logs { get; } = [];
#endif

    [Conditional("AVA_DEBUG")]
    public static void Print(string msg)
#if AVA_DEBUG
        => Logs.Add("//\t" + msg);
#else
    {
    }
#endif

    private readonly record struct PropertyAttributeInfo(
        string DeclaringNamespace,
        (string Type, bool TypeIsRecord, bool TypeIsStruct)[] Hierarchy,
        Accessibility PropertyVisibility,
        string PropertyName,
        string PropertyType,
        bool PropertyIsStatic,
        TypedConstant? DefaultValue,
        string? DefaultValueMember,
        TypedConstant? DefaultMode,
        string? OnChangedMethod,
        bool PropertyHasBackingProperty);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Print("hi from AvaloniaNameIncrementalGenerator");
        
#if AVA_DEBUG
        if (!Debugger.IsAttached) 
        { 
            //Debugger.Launch(); 
        }
#endif
        
        // Find all types annotated with [TypeConverter] attribute
        var typeConverterProperties = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "System.ComponentModel.TypeConverterAttribute",
                static (node, _) => node is TypeDeclarationSyntax,
                static (context, _) =>
                {
                    var syntax = (TypeDeclarationSyntax)context.TargetNode;
                    var type = context.SemanticModel.GetDeclaredSymbol(syntax)!;
                    var attr = context.Attributes.FirstOrDefault()!;

                    return (
                        Name: type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        ConverterName: attr.ConstructorArguments.FirstOrDefault().ToCSharpString()
                    );
                }
            )
            .WithTrackingName(TrackingNames.TypeConverterPropertiesProvider);
        
        // Registers them into the TypeConverterRegistry
        context.RegisterSourceOutput(typeConverterProperties.Collect(), static (context, attrs) =>
        {
            var sb = new IndentedStringBuilder();
            sb.AppendLine("#nullable enable");

            sb.AppendLine("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");;
            sb.AppendLine("internal static class __TypeConverterHook");
            sb.AppendLine("{");
            using (sb.Indent())
            {
                sb.AppendLine("[System.Runtime.CompilerServices.ModuleInitializerAttribute]");
                sb.AppendLine("public static void Init()");
                sb.AppendLine("{");
                using (sb.Indent())
                {
                    foreach (var (name, converterName) in attrs)
                    {
                        sb.AppendLine($"global::WorldXaml.UI.Base.TypeConverterRegistry.RegisterConverter<{name}>({TypeOfToNewInstance(converterName)});");
                    }
                }
                sb.AppendLine("}");
            }
            sb.AppendLine("}");
            
            context.AddSource($"__TypeConverterHook.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            
            static string TypeOfToNewInstance(string typeOf)
            {
                typeOf = typeOf["typeof(".Length..^")".Length];
                return $"new {typeOf}()";
            }
        });
        
        // Find all methods annotated with [XamlInterpolator] attribute
        var interpolatorMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "WorldXaml.UI.Base.XamlInterpolatorAttribute",
                static (node, _) => node is MethodDeclarationSyntax,
                static (context, _) =>
                {
                    var syntax = (MethodDeclarationSyntax)context.TargetNode;
                    var method = context.SemanticModel.GetDeclaredSymbol(syntax)!;

                    return (
                        ContainingType: method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        MethodName: method.Name,
                        InterpolatedType: method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    );
                })
            .WithTrackingName(TrackingNames.XamlInterpolatorMethodsProvider);

        // Registers them into the InterpolatorRegistry
        context.RegisterSourceOutput(interpolatorMethods.Collect(), static (context, attrs) =>
        {
            var sb = new IndentedStringBuilder();
            sb.AppendLine("#nullable enable");

            sb.AppendLine("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");;
            sb.AppendLine("internal static class __InterpolatorHook");
            sb.AppendLine("{");
            using (sb.Indent())
            {
                sb.AppendLine("[System.Runtime.CompilerServices.ModuleInitializerAttribute]");
                sb.AppendLine("public static void Init()");
                sb.AppendLine("{");
                using (sb.Indent())
                {
                    foreach (var (containingType, methodName, interpolatedType) in attrs)
                    {
                        sb.AppendLine($"global::WorldXaml.UI.Base.InterpolatorRegistry.RegisterInterpolator<{interpolatedType}>({containingType}.{methodName});");
                    }
                }
                sb.AppendLine("}");
            }
            sb.AppendLine("}");
            
            context.AddSource($"__InterpolatorHook.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        });

        // Find all properties annotated with [Property] attribute
        var bindableProperties = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "WorldXaml.UI.Base.PropertyAttribute",
                static (node, _) => node is PropertyDeclarationSyntax,
                static (context, _) =>
                {
                    var syntax = (PropertyDeclarationSyntax)context.TargetNode;
                    var prop = context.SemanticModel.GetDeclaredSymbol(syntax)!;
                    var attr = context.Attributes.FirstOrDefault();

                    return new PropertyAttributeInfo(
                        DeclaringNamespace: prop.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Remove(0, "global::".Length),
                        Hierarchy: GetTypeHierarchy(prop.ContainingType).ToArray(),
                        PropertyVisibility: prop.DeclaredAccessibility,
                        PropertyName: prop.Name,
                        PropertyType: prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        PropertyIsStatic: prop.IsStatic,
                        DefaultValue: attr!.NamedArguments.FirstOrDefault(static kv => kv.Key == "DefaultValue").Value,
                        DefaultValueMember: attr.NamedArguments.FirstOrDefault(static kv => kv.Key == "DefaultValueMember").Value.Value as string,
                        DefaultMode: attr.NamedArguments.FirstOrDefault(static kv => kv.Key == "DefaultMode").Value,
                        OnChangedMethod: attr.NamedArguments.FirstOrDefault(static kv => kv.Key == "OnChangedMethod").Value.Value as string,
                        PropertyHasBackingProperty: prop.ContainingType.GetMembers(prop.Name + "Property").Any()
                    );

                    IEnumerable<(string Type, bool TypeIsRecord, bool TypeIsStruct)> GetTypeHierarchy(ITypeSymbol type)
                    {
                        while (true)
                        {
                            yield return (type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), type.IsRecord, type.IsValueType);
                            if (type.ContainingType is { } containingType)
                            {
                                type = containingType;
                                continue;
                            }

                            break;
                        }
                    }
                }
            )
            .WithTrackingName(TrackingNames.PropertyAttributesProvider);
        
        // Map MSBuild properties onto readonly GeneratorOptions.
        var options = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) => new GeneratorOptions(options.GlobalOptions))
            .WithTrackingName(TrackingNames.XamlGeneratorOptionsProvider);

        // Generate implementation Register code from PropertyAttributes
        context.RegisterSourceOutput(bindableProperties.Collect().Combine(options), static (context, pair) =>
        {
            var (bindableProperties, options) = pair;
            foreach (var props in bindableProperties.GroupBy(static prop => prop, PropHierarchyComparer.Instance))
            {
                var declaringTypeNamespace = props.Key.DeclaringNamespace;
                
                var sb = new IndentedStringBuilder();
                sb.AppendLine("#nullable enable");
                
                sb.AppendLine($"namespace {declaringTypeNamespace};");
                sb.AppendLine();

                var iter = props.Key.Hierarchy.Reverse();
                foreach (var type in iter)
                {
                    sb.AppendLine($"partial {(type.TypeIsRecord ? "record" : type.TypeIsStruct ? "struct" : "class")} {type.Type}");
                    sb.AppendLine("{");
                    sb.IncrementIndent();
                }
                
                var containingTypes = $"global::{declaringTypeNamespace}.{string.Join(".", iter.Select(t => t.Type))}";
                foreach (var prop in props)
                {
                    if (!prop.PropertyHasBackingProperty)
                    {
                        sb.AppendLine("/// <summary>");
                        sb.AppendLine($"/// Property field for <see cref=\"{prop.PropertyName}\"/>.");
                        sb.AppendLine("/// </summary>");
                        sb.AppendLine($"{ToCSharp(prop.PropertyVisibility)} static global::{options.KnownTypes.PropertyGeneric.Replace("`1", "")}<{prop.PropertyType}> {prop.PropertyName}Property {{ get; }} = global::{options.KnownTypes.Property}.Register<{containingTypes}, {prop.PropertyType}>(nameof({prop.PropertyName}), defaultValue: {(prop.DefaultValueMember is {} defaultValueMember ? defaultValueMember : prop.DefaultValue is {} defaultValue ? ToCSharpString(defaultValue) : "null")}, defaultMode: {(prop.DefaultMode is {} defaultMode ? ToCSharpString(defaultMode) : "global::Avalonia.Data.BindingMode.OneWay")}, onChanged: {(prop.OnChangedMethod != null ? $"(obj, prop) => obj.{prop.OnChangedMethod}(prop)" : "null")});");
                    }

                    if (prop.DefaultValueMember != null)
                    {
                        sb.AppendLine("/// <summary>");
                        sb.AppendLine($"/// Gets the default value of <see cref=\"{prop.PropertyName}\"/>.");
                        sb.AppendLine("/// </summary>");
                        sb.AppendLine($"private static partial {prop.PropertyType} {prop.DefaultValueMember} {{ get; }}");
                    }

                    if (prop.OnChangedMethod != null)
                    {
                        sb.AppendLine("/// <summary>");
                        sb.AppendLine($"/// Invoked when the value of <see cref=\"{prop.PropertyName}\"/> changes.");
                        sb.AppendLine("/// </summary>");
                        sb.AppendLine($"private partial void {prop.OnChangedMethod}({prop.PropertyType} prop);");
                    }

                    sb.AppendLine($"{ToCSharp(prop.PropertyVisibility)} {(prop.PropertyIsStatic ? "static " : "")}partial {prop.PropertyType} {prop.PropertyName} {{ get => GetValue({prop.PropertyName}Property); set => SetValue({prop.PropertyName}Property, value); }}");
                }

                foreach (var type in iter)
                {
                    sb.DecrementIndent();
                    sb.AppendLine("}");
                }
                
                context.AddSource($"{declaringTypeNamespace.Replace('.', '_')}_{string.Join("_", iter.Select(t => t.Type))}_GeneratedProperties.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
                
                static string ToCSharp(Accessibility accessibility)
                {
                    return accessibility switch {
                        Accessibility.NotApplicable => "",
                        Accessibility.Private => "private",
                        Accessibility.ProtectedAndInternal => "private protected",
                        Accessibility.Protected => "protected",
                        Accessibility.Internal => "internal",
                        Accessibility.ProtectedOrInternal => "protected internal",
                        Accessibility.Public => "public",
                        _ => throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, null)
                    };
                }

                static string ToCSharpString(TypedConstant constant)
                {
                    if (constant.IsNull) return "default";
                    return constant.ToCSharpString();
                }
            }
        });
        
        // Generate __XamlKnownTypes used for hot reload based on KnownTypes.
        context.RegisterSourceOutput(options, static (context, options) =>
        {
            var sb = new IndentedStringBuilder();
            sb.AppendLine("#nullable enable");

            sb.AppendLine("using System.ComponentModel;");
            sb.AppendLine();
            sb.AppendLine("[EditorBrowsable(EditorBrowsableState.Never)]");
            sb.AppendLine("internal class __XamlKnownTypes : global::NFMWorld.XamlX.Core.IKnownTypes");
            sb.AppendLine("{");
            sb.IncrementIndent();
            foreach (var knownType in options.KnownTypes)
            {
                sb.AppendLine($"public string {knownType.Key} => \"{knownType.Value}\";");
            }
            sb.AppendLine("public static global::NFMWorld.XamlX.Core.IKnownTypes Instance { get; } = new __XamlKnownTypes();");
            
            sb.DecrementIndent();
            sb.AppendLine("}");
            
            context.AddSource("__XamlKnownTypes.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        });

        // Filter additional texts, we only need Avalonia XAML files.
        var xamlFiles = context.AdditionalTextsProvider
            .Combine(options.Combine(context.AnalyzerConfigOptionsProvider))
            .Where(static pair =>
            {
                var text = pair.Left;
                var (options, optionsProvider) = pair.Right;
                var filePath = text.Path;
                
                Print($"File path: {filePath}");

                if (!(filePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ||
                      filePath.EndsWith(".paml", StringComparison.OrdinalIgnoreCase) ||
                      filePath.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase)))
                {
                    Print("Not a XAML file.");

                    return false;
                }

                if (!options.WorldXamlGeneratorFilterByPath.Matches(filePath))
                {
                    Print("Filtered out by path.");
                    return false;
                }

                if (!optionsProvider.GetOptions(pair.Left).TryGetValue(SourceItemGroupMetadata, out var itemGroup)
                    || itemGroup != "AvaloniaXaml")
                {
                    Print("Not in AvaloniaXaml item group. Item group: " + itemGroup);
                    return false;
                }

                return true;
            })
            .Select(static (pair, _) => pair.Left)
            .WithTrackingName(TrackingNames.InputXamlFilesProvider);

        // Actual parsing step. We input XAML files one by one, but don't resolve any types.
        // That's why we use NoOp type system here, allowing parsing to run detached from C# compilation.
        // Otherwise we would need to re-parse XAML on any C# file changed.
        var parsedXamlClasses = xamlFiles
            .Select(static (file, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var xaml = file.GetText(cancellationToken)?.ToString();
                if (xaml is null)
                {
                    Print("XAML text is null.");
                    return null;
                }

                ResolvedXmlView? resolvedXmlView;
                DiagnosticFactory? diagnosticFactory = null;
                var location =  new FileLinePositionSpan(file.Path, default);
                try
                {
                    var viewResolver = new XamlXViewResolver(s_noopCompiler);
                    var view = viewResolver.ResolveView(xaml, cancellationToken);
                    if (view is null)
                    {
                        Print("View is null after parsing.");
                        return null;
                    }

                    var xmlNames = EquatableList<ResolvedXmlName>.Empty;
                    var nameResolver = new XamlXNameResolver();
                    xmlNames = nameResolver.ResolveXmlNames(view.Xaml, cancellationToken);
                    
                    Print("Resolved XML names: " + string.Join(", ", xmlNames.Select(n => n.Name)));

                    resolvedXmlView = new ResolvedXmlView(view, xmlNames);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (XmlException ex)
                {
                    Print("Caught XmlException during XAML parsing: " + ex.Message);
                    diagnosticFactory = new(NameGeneratorDiagnostics.ParseFailed, new(file.Path, GetLinePositionSpan(ex)), new([ex.Message]));

                    resolvedXmlView = ex is XamlParseException ? TryExtractTypeFromXml(xaml) : null;
                }
                catch (XamlTypeSystemException ex)
                {
                    Print("Caught XamlTypeSystemException during XAML parsing: " + ex.Message);
                    diagnosticFactory = new(NameGeneratorDiagnostics.ParseFailed, location, new([ex.Message]));
                    resolvedXmlView = TryExtractTypeFromXml(xaml);
                }
                catch (Exception ex)
                {
                    Print("Caught general Exception during XAML parsing: " + ex.ToString());
                    diagnosticFactory = GetInternalErrorDiagnostic(location, ex);
                    resolvedXmlView = null;
                }

                return new XmlClassInfo(file.Path, xaml, resolvedXmlView, diagnosticFactory);
            })
            .Where(request => request is not null)
            .WithTrackingName(TrackingNames.ParsedXamlClasses);
        
        // IMPORTANT: we shouldn't cache CompilationProvider as a whole,
        // But we also should keep in mind that CompilationProvider can frequently re-trigger generator.
        var roslynTypeSystem = context.CompilationProvider
            .Select(static (compilation, _) => new RoslynTypeSystem(compilation))
            .WithTrackingName(TrackingNames.RoslynTypeSystem);

        var compiler = roslynTypeSystem.Combine(options)
            .Select(static (pair, _) =>
            {
                var (roslynTypeSystem, options) = pair;
                return MiniCompiler.CreateRoslyn(roslynTypeSystem, options.KnownTypes.XmlnsDefinitionAttribute);
            })
            .WithTrackingName(TrackingNames.XamlTypeSystem);

        // Create C# XAML compiler for full XAML-to-C# compilation (WithXamlXCompilation behavior).
        var csharpCompiler = roslynTypeSystem
            .Combine(options)
            .Select(static (pair, _) =>
            {
                var (roslynTypeSystem, options) = pair;
                
                try
                {
                    return new XamlCSharpCompiler(
                        roslynTypeSystem,
                        knownTypes: options.KnownTypes,
                        supportHotReloading: true
                    );
                }
                catch (Exception ex)
                {
                    Print($"Failed to create {nameof(XamlCSharpCompiler)}: {ex}");
                    
                    return null;
                }
            })
            .WithTrackingName(TrackingNames.XamlCSharpCompiler);
        
        // Generate the shared XamlContext type (used by all compiled XAML views).
        context.RegisterSourceOutput(csharpCompiler, static (ctx, compiler) =>
        {
            if (compiler != null)
                ctx.AddSource("__XamlContext.g.cs", compiler.ContextSource);
            
            var logContent = string.Join("\n", Logs.Select(l => "// " + l.Replace("\n", "\n// ")));
            ctx.AddSource("__AvaloniaLogs.g.cs", SourceText.From(logContent, Encoding.UTF8));
        });
        
        // Note: this step will be re-executed on any C# file changes.
        // As much as possible heavy tasks should be moved outside of this step, like XAML parsing.
        var resolvedNames = parsedXamlClasses
            .Combine(compiler)
            .Combine(csharpCompiler)
            .Combine(options)
            .Select(static (pair, ct) =>
            {
                var (((classInfo, compiler), csharpCompiler), options) = pair;
                var hasDevToolsReference = compiler.TypeSystem.FindAssembly("Avalonia.Diagnostics") is not null;
                var nameResolver = new XamlXNameResolver();

                var diagnostics = new List<DiagnosticFactory>(2);
                if (classInfo?.Diagnostic != null)
                {
                    diagnostics.Add(classInfo.Diagnostic);
                }

                ResolvedView? view = null;
                string? compiledXamlSource = null;
                if (classInfo?.XmlView is { } xmlView)
                {
                    var type = compiler.TypeSystem.FindType(xmlView.FullName);

                    if (type is null)
                    {
                        diagnostics.Add(new(NameGeneratorDiagnostics.InvalidType, new(classInfo.FilePath, default), new([xmlView.FullName])));
                    }
                    else if (type.IsAvaloniaStyledElement(options.KnownTypes.StyledElement))
                    {
                        var resolvedNames = new List<ResolvedName>();
                        foreach (var xmlName in xmlView.XmlNames)
                        {
                            ct.ThrowIfCancellationRequested();

                            try
                            {
                                var clrType = compiler.ResolveXamlType(xmlName.XmlType);
                                if (!clrType.IsAvaloniaStyledElement(options.KnownTypes.StyledElement))
                                {
                                    Print($"Skipping name resolution for non-StyledElement type: {clrType.GetFqn()}");
                                    continue;
                                }

                                resolvedNames.Add(nameResolver
                                    .ResolveName(clrType, xmlName.Name, xmlName.FieldModifier));
                            }
                            catch (XmlException ex)
                            {
                                Print($"Caught XmlException during name resolution: {ex.Message}");
                                diagnostics.Add(new(NameGeneratorDiagnostics.NamedElementFailed,
                                    new(classInfo.FilePath, GetLinePositionSpan(ex)), new([xmlName.Name, ex.Message])));
                            }
                            catch (Exception ex)
                            {
                                Print($"Caught general Exception during name resolution: {ex}");
                                diagnostics.Add(GetInternalErrorDiagnostic(new(classInfo.FilePath, default), ex));
                            }
                        }

                        view = new ResolvedView(xmlView, type.IsAvaloniaWindow(options.KnownTypes.Window), new(resolvedNames));

                        // Compile XAML to C# for WithXamlXCompilation behavior
                        if (csharpCompiler != null && classInfo.XamlSource != null)
                        {
                            try
                            {
                                compiledXamlSource = csharpCompiler.CompileView(classInfo.XamlSource, classInfo.FilePath, xmlView.FullName, out var xamlDiagnostics);
                                foreach (var diag in xamlDiagnostics)
                                {
                                    diagnostics.Add(new(diag, new(classInfo.FilePath, default), new([xmlView.FullName])));
                                }
                            }
                            catch (Exception ex)
                            {
                                Print($"Caught general Exception during XAML compilation: {ex}");
                                diagnostics.Add(GetInternalXamlErrorDiagnostic(new(classInfo.FilePath, default), ex));
                            }
                        }
                    }
                }

                return new ResolvedClassInfo(view, hasDevToolsReference, new(diagnostics), compiledXamlSource);
            })
            .WithTrackingName(TrackingNames.ResolvedNamesProvider);

        context.RegisterSourceOutput(resolvedNames.Combine(options), static (context, pair) =>
        {
            var (info, options) = pair;

            foreach (var diagnostic in info.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic.Create());
            }

            if (info.View is { } view && options.WorldXamlGeneratorFilterByNamespace.Matches(view.Namespace))
            {
                ICodeGenerator codeGenerator = options.WorldXamlGeneratorBehavior switch
                {
                    Behavior.OnlyProperties => new OnlyPropertiesCodeGenerator(
                        options.WorldXamlGeneratorClassFieldModifier),
                    Behavior.InitializeComponent => new InitializeComponentCodeGenerator(
                        options.WorldXamlGeneratorAttachDevTools && info.CanAttachDevTools && view.IsWindow,
                        options.WorldXamlGeneratorClassFieldModifier),
                    Behavior.WithXamlXCompilation => new XamlXCodeGenerator(
                        options.WorldXamlGeneratorAttachDevTools && info.CanAttachDevTools && view.IsWindow,
                        options.WorldXamlGeneratorClassFieldModifier,
                        info.CompiledXamlSource),
                    _ => throw new ArgumentOutOfRangeException()
                };
                var fileName = options.WorldXamlGeneratorViewFileNamingStrategy switch
                {
                    ViewFileNamingStrategy.ClassName => $"{view.ClassName}.g.cs",
                    ViewFileNamingStrategy.NamespaceAndClassName => $"{view.Namespace}.{view.ClassName}.g.cs",
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(ViewFileNamingStrategy), options.WorldXamlGeneratorViewFileNamingStrategy,
                        "Unknown naming strategy!")
                };

                var generatedPartialClass = codeGenerator.GenerateCode(
                    info.View.ClassName,
                    info.View.Namespace,
                    info.View.Names);

                Print("Generating file: " + fileName);
                context.AddSource(fileName, generatedPartialClass);
            }
        });
        
#if AVA_DEBUG
        context.RegisterPostInitializationOutput(
            static context =>
            {
                var logContent = string.Join("\n", Logs.Select(l => "// " + l.Replace("\n", "\n// ")));
                context.AddSource("logs.g.cs", SourceText.From(logContent, Encoding.UTF8));
            }
        );
#endif
    }

    private static DiagnosticFactory GetInternalErrorDiagnostic(FileLinePositionSpan location, Exception ex) =>
        new(NameGeneratorDiagnostics.InternalError, location, new([ex.ToString().Replace('\n', '*').Replace('\r', '*')]));

    private static DiagnosticFactory GetInternalXamlErrorDiagnostic(FileLinePositionSpan location, Exception ex) =>
        new(NameGeneratorDiagnostics.InternalErrorCompilingXaml, location, new([ex.ToString().Replace('\n', '*').Replace('\r', '*')]));

    /// <summary>
    /// Fallback in case XAML parsing fails. Extracts just the class name and namespace of the root element.
    /// </summary>
    private static ResolvedXmlView? TryExtractTypeFromXml(string xaml)
    {
        try
        {
            var document = XDocument.Parse(xaml);
            var classValue = document.Root.Attribute(XName.Get("Class", XamlNamespaces.Xaml2006))?.Value;
            if (classValue?.LastIndexOf('.') is { } lastDotIndex && lastDotIndex != -1)
            {
                return new(classValue.Substring(lastDotIndex + 1), classValue.Substring(0, lastDotIndex), EquatableList<ResolvedXmlName>.Empty);
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

    private static LinePositionSpan GetLinePositionSpan(XmlException ex)
    {
        var position = new LinePosition(Math.Max(0, ex.LineNumber - 1), Math.Max(0, ex.LinePosition - 1));
        return new(position, position);
    }

    internal record XmlClassInfo(
        string FilePath,
        string? XamlSource,
        ResolvedXmlView? XmlView,
        DiagnosticFactory? Diagnostic);

    internal record ResolvedClassInfo(
        ResolvedView? View,
        bool CanAttachDevTools,
        EquatableList<DiagnosticFactory> Diagnostics,
        string? CompiledXamlSource);

    /// <summary>
    /// Avoid holding references to <see cref="Diagnostic"/> because it can hold references to <see cref="ISymbol"/>, <see cref="SyntaxTree"/>, etc.
    /// </summary>
    internal record DiagnosticFactory(DiagnosticDescriptor Descriptor, FileLinePositionSpan LinePosition, EquatableList<string> FormatArguments)
    {
        public Diagnostic Create() => Diagnostic.Create(Descriptor, 
            Location.Create(LinePosition.Path, default, new(LinePosition.StartLinePosition, LinePosition.EndLinePosition)),
            messageArgs: [.. FormatArguments]);
    }

    private class PropHierarchyComparer : IEqualityComparer<PropertyAttributeInfo>
    {
        public static PropHierarchyComparer Instance { get; } = new();
    
        public bool Equals(PropertyAttributeInfo x, PropertyAttributeInfo y)
        {
            return x.DeclaringNamespace == y.DeclaringNamespace && x.Hierarchy.SequenceEqual(y.Hierarchy);
        }

        public int GetHashCode(PropertyAttributeInfo obj)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + obj.DeclaringNamespace.GetHashCode();
                foreach (var type in obj.Hierarchy)
                {
                    hash = hash * 23 + type.Type.GetHashCode();
                    hash = hash * 23 + type.TypeIsRecord.GetHashCode();
                    hash = hash * 23 + type.TypeIsStruct.GetHashCode();
                }
                return hash;
            }
        }
    }
}