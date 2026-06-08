using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorldXaml.Generator.Common;
using WorldXaml.Generator.NameGenerator;
using Microsoft.CodeAnalysis;
using NFMWorld.XamlX.Core;
using XamlX;
using XamlX.Ast;
using XamlX.CSharp;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;
using WorldXaml.XamlX;

namespace WorldXaml.Generator.Compiler;

/// <summary>
/// Compiles XAML documents to C# source code using the XamlX compiler pipeline
/// with CSharpEmitter/CSharpTypeBuilder instead of IL emission.
/// </summary>
internal sealed class XamlCSharpCompiler
{
    private readonly IXamlTypeSystem _typeSystem;
    private readonly TransformerConfiguration _configuration;
    private readonly XamlILCompiler _compiler;
    private readonly IXamlType _contextType;
    private readonly bool _didNotFindRegisterMethod;


    public XamlCSharpCompiler(
        IXamlTypeSystem typeSystem,
        IKnownTypes knownTypes,
        bool supportHotReloading = false)
    {
        _typeSystem = typeSystem;

        var mappings = XamlHelpers.CreateTypeMappings(typeSystem, knownTypes);
        var diagnosticsHandler = new XamlDiagnosticsHandler();
        var assembly = typeSystem.Assemblies.First();

        _configuration = new TransformerConfiguration(
            typeSystem, assembly, mappings,
            diagnosticsHandler: diagnosticsHandler,
            identifierGenerator: new DeterministicIdentifierGenerator(0));

        var emitMappings = new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>();
        IXamlMethod? registerMethod = null;
        if (supportHotReloading)
        {
            // Find the XamlHotReload.Register method for runtime support
            registerMethod = knownTypes.HotReload is {} hotReloadTypeName
                ? typeSystem.Assemblies
                    .Select(ass => ass.FindType(hotReloadTypeName))
                    .FirstOrDefault(type => type != null)
                    ?.FindMethod(method => method.Name == "Register")
                : null;

            if (registerMethod == null)
            {
                _didNotFindRegisterMethod = true;
            }
        }

        _compiler = new WorldXamlILCompiler(_configuration, emitMappings, true, registerMethod)
        {
            EnableIlVerification = false
        };

        XamlHelpers.SetUpCompiler(
            _compiler,
            knownTypes
        );
        
        // Add emitter for SkipXamlAstNode (used when transforms fail but error is handled)
        // Must be first so it's checked before ValueWithManipulationsEmitter
        _compiler.Emitters.Insert(0, new SkipNodeEmitter());

        // Generate context type eagerly (shared across all views)
        var contextBuilder = new CSharpTypeBuilder(typeSystem, "__XamlRuntime__", "XamlContext",
            typeSystem.GetType("System.Object"), XamlVisibility.Assembly);
        _contextType = _compiler.CreateContextType(contextBuilder);
        ContextSource = contextBuilder.GenerateSource();
    }

    /// <summary>
    /// Gets the generated C# source for the shared XamlContext type.
    /// </summary>
    public string ContextSource { get; }

    /// <summary>
    /// Compiles a XAML document to C# source code, returning the generated member declarations
    /// (Populate, Build methods + helpers) to be embedded in the partial class.
    /// </summary>
    /// <param name="xamlSource">The raw XAML source text.</param>
    /// <param name="filePath">The file path for diagnostics and base URI.</param>
    /// <param name="indentCount">The indentation to use for the generated members.</param>
    /// <returns>Generated C# member declarations, or null if compilation fails.</returns>
    public string CompileView(string xamlSource, string filePath, string xClassName, out IReadOnlyList<DiagnosticDescriptor> diagnostics, byte indentCount = 2)
    {
        var doc = XDocumentXamlParser.Parse(xamlSource, new Dictionary<string, string>
        {
            { XamlNamespaces.Blend2008, XamlNamespaces.Blend2008 }
        });

        // Transform AST (resolves types, properties, etc.)
        _compiler.Transform(doc);

        // Check if root became a skip node due to transform failure
        if (doc.Root is ISkipXamlAstNode)
            throw new Exception($"XAML transform failed for {xClassName}: root type could not be resolved.");

        // Get root type info from the transformed AST
        var rootGrp = (XamlValueWithManipulationNode)doc.Root;
        var rootType = rootGrp.Type.GetClrType();

        // Use the x:Class name for the type builder so self-references
        // (XamlNamespaceInfo, Populate, Build) use the correct class name.
        var lastDot = xClassName.LastIndexOf('.');
        var classNamespace = lastDot >= 0 ? xClassName.Substring(0, lastDot) : "";
        var className = lastDot >= 0 ? xClassName.Substring(lastDot + 1) : xClassName;

        // Create a CSharpTypeBuilder as the container for generated methods.
        // This is a "virtual" type builder - it collects the Populate/Build methods
        // and nested types, then we extract just the members.
        var typeBuilder = new CSharpTypeBuilder(
            _typeSystem,
            classNamespace,
            className,
            null,
            XamlVisibility.Public
        );

        // Compile - this defines Populate, Build, and XamlNamespaceInfo on the typeBuilder
        try
        {
            _compiler.Compile(
                doc,
                typeBuilder,
                _contextType,
                populateMethodName: "Populate",
                createMethodName: "Build",
                namespaceInfoClassName: "XamlNamespaceInfo",
                baseUri: filePath,
                fileSource: new SourceGenFileSource(filePath, xamlSource));
        }
        catch (Exception ex)
        {
            // Dump the transformed AST for debugging
            throw new Exception($"Compile failed for {xClassName}. Root type: {rootType?.FullName ?? "null"}. Root node: {doc.Root?.GetType().Name ?? "null"}. Error: {ex.Message}", ex);
        }

        // Extract just the member declarations
        var sb = new IndentedStringBuilder(indent: indentCount);
        if (_didNotFindRegisterMethod)
        {
            diagnostics = [NameGeneratorDiagnostics.XamlHotReloadNotFound];
            sb.Append($"// Warning: XamlHotReload.Register method not found. Hot reload support will be disabled.\n");
        }
        else
        {
            diagnostics = [];
        }
        
        typeBuilder.GenerateMembers(sb);
        return sb.ToString();
    }

    private static void TryAddType(IXamlTypeSystem typeSystem, string typeName, List<IXamlType> list)
    {
        var type = typeSystem.FindType(typeName);
        if (type != null)
            list.Add(type);
    }

    /// <summary>
    /// Transformer that removes x:Class and other XAML directives before emit.
    /// </summary>
    private class RemoveXamlDirectivesTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlManipulationGroupNode group)
                group.Children.RemoveAll(ShouldRemoveDirective);
            if (node is XamlAstObjectNode objNode)
                objNode.Children.RemoveAll(ShouldRemoveDirective);
            if (node is XamlValueWithManipulationNode { Manipulation: XamlManipulationGroupNode manipGroup })
                manipGroup.Children.RemoveAll(ShouldRemoveDirective);
            return node;
        }

        private static bool ShouldRemoveDirective(IXamlAstNode n)
        {
            if (n is not XamlAstXmlDirective directive)
                return false;
            if (directive.Name is "Class" or "Key" or "Name")
                return true;
            if (directive.Namespace == XamlNamespaces.Blend2008)
                return true;
            return false;
        }
    }
}

internal class DeterministicIdentifierGenerator(int seed) : IXamlIdentifierGenerator
{
#pragma warning disable RS1035
    private readonly Random _random = new Random(seed);
#pragma warning restore RS1035

    public string GenerateIdentifierPart()
    {
        var buffer = new byte[16];
#pragma warning disable RS1035
        _random.NextBytes(buffer);
#pragma warning restore RS1035

        return new Guid(buffer).ToString().Replace("-", "");
    }
}

internal class SourceGenFileSource(string filePath, string content) : IFileSource
{
    public string FilePath { get; } = filePath;
    public byte[] FileContents { get; } = Encoding.UTF8.GetBytes(content);
}

internal class SkipNodeEmitter : IXamlAstNodeEmitter<IXamlILEmitter, XamlILNodeEmitResult>
{
    public XamlILNodeEmitResult? Emit(IXamlAstNode node, XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
    {
        if (node is ISkipXamlAstNode)
        {
            // Pop the item that ManipulationGroup Dup'd for this child.
            // Each MG child is expected to consume exactly 1 item from the stack.
            codeGen.Pop();
            return XamlILNodeEmitResult.Void(1);
        }
        return null;
    }
}
