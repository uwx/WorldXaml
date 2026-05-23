using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Maxine.Extensions;
using NFMWorld.XamlX.Core;
using XamlX;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;
using WorldXaml.XamlX;

namespace WorldXaml.UI.Base.Xaml;

#if DEBUG
internal delegate void Debounced<in T>(T argument);

internal static class DebounceExtensions
{
    // Roughly based on https://stackoverflow.com/a/29491927
    public static Debounced<T> Debounce<T>(this Action<T> func, int milliseconds = 300)
    {
        var syncRoot = new object();
        TimerEx? timer = null;
        T? lastArg = default;

        void Debounced()
        {
            try
            {
                func(lastArg!);
            }
            finally
            {
                TimerEx oldTimer;

                lock (syncRoot)
                {
                    oldTimer = timer!.Value;
                    timer = null;
                }

                oldTimer.Dispose();
            }
        }

        return arg =>
        {
            lock (syncRoot) // Lock assuming ??= is not fully atomic
            {
                (timer ??= TimerEx.Once(Debounced, TimeSpan.FromMilliseconds(milliseconds))).Restart();
                lastArg = arg;
            }
        };
    }
}
#endif

public class XamlHotReload
{
#if DEBUG
    private static FileSystemWatcher? _watcher;
    private static ConditionalWeakTable<ILogical, string> _trackedNodes = new();
    
    private static string? _cachedProjectDirectory;
    private static string? TryGetProjectDirectory(string? currentPath = null)
    {
        if (_cachedProjectDirectory != null)
        {
            return _cachedProjectDirectory;
        }
        
        var directory = new DirectoryInfo(currentPath ?? Directory.GetCurrentDirectory());
        while (directory != null && !directory.EnumerateFiles("*.csproj").Any())
        {
            directory = directory.Parent;
        }

        return _cachedProjectDirectory = directory?.ToString();
    }
    
    [RequiresUnreferencedCode("Uses XamlX Sre types which may not be compatible with trimming.")]
    [RequiresDynamicCode("Uses Reflection.Emit which may not be compatible with AOT.")]
    public static void Initialize(string? projectRoot = null)
    {
        _watcher = new FileSystemWatcher(projectRoot ?? TryGetProjectDirectory() ?? ".", "*.xaml");
        _watcher.IncludeSubdirectories = true;
        _watcher.Changed += OnXamlFileChanged;
        _watcher.EnableRaisingEvents = true;
    }
#endif
    
    [Conditional("DEBUG")]
    public static void Register(ILogical node, string xamlPath)
    {
#if DEBUG
        Logging.Debug($"[XamlHotReload] Registered for hot reload: {xamlPath}");
        var fullPath = Path.GetFullPath(Path.Combine(_watcher!.Path, xamlPath));
        _trackedNodes.AddOrUpdate(node, fullPath);
#endif
    }

#if DEBUG
#pragma warning disable IL2026
#pragma warning disable IL3050
    private static readonly Debounced<FileSystemEventArgs> Debounced = ((Action<FileSystemEventArgs>)((e) => Task.Run(() => ReloadXaml(e.FullPath)))).Debounce(1000);
#pragma warning restore IL3050
#pragma warning restore IL2026

    [RequiresUnreferencedCode("Uses XamlX Sre types which may not be compatible with trimming.")]
    [RequiresDynamicCode("Uses Reflection.Emit which may not be compatible with AOT.")]
    private static void OnXamlFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce, compile, and swap
        Debounced(e);
    }

    [RequiresUnreferencedCode("Uses XamlX Sre types which may not be compatible with trimming.")]
    [RequiresDynamicCode("Uses Reflection.Emit which may not be compatible with AOT.")]
    private static async Task? ReloadXaml(string path)
    {
        if (AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType("__XamlKnownTypes"))
                .FirstOrDefault(type => type != null)
                ?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null) is not IKnownTypes knownTypes)
        {
            throw new InvalidOperationException($"Could not find {nameof(IKnownTypes)} instance for hot reload. Ensure that source generation has run.");
        }
        
        var fullPath = Path.GetFullPath(path);
        var nodesToUpdate = _trackedNodes.Where(e => e.Value == fullPath).ToArray();
        if (nodesToUpdate.Length > 0)
        {
            Logging.Debug($"[XamlHotReload] Reloading XAML: {fullPath}");

            // Reload the XAML and re-initialize the view
            try
            {
                var firstNode = nodesToUpdate[0].Key;
                var (create, populate) = CompileXaml(firstNode, path, await File.ReadAllTextAsync(path), knownTypes);
                Logging.Debug($"[XamlHotReload] Successfully compiled XAML: {fullPath}");
                
                foreach (var (node, _) in nodesToUpdate)
                {
                    ClearChildren(node);

                    // Populate the view
                    populate(AvaloniaXamlLoader.CreateDefaultServiceProvider(node), node);
                }
                Logging.Debug($"[XamlHotReload] Successfully reloaded XAML: {fullPath}");
            }
            catch (Exception ex)
            {
                Logging.Warning($"[XamlHotReload] Failed to reload XAML: {fullPath}. Error: {ex}");
            }
        }
    }

    private static void ClearChildren(ILogical node)
    {
        // Find member attributed with [Content]
        var contentAttribute = node
            .GetType()
            .GetMembers()
            .FirstOrDefault(member => member.GetCustomAttribute<ContentAttribute>() != null && member is PropertyInfo or FieldInfo);

        // call member.Clear() on it
        if (contentAttribute is PropertyInfo propInfo)
        {
            if (propInfo.GetMethod != null && propInfo.PropertyType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy) is { } clearMethod)
            {
                var propertyValue  = propInfo.GetValue(node, null);
                if (propertyValue != null)
                {
                    clearMethod.Invoke(propertyValue, null);
                }
            }
        }
        else if (contentAttribute is FieldInfo fieldInfo)
        {
            if (fieldInfo.FieldType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy) is { } clearMethod)
            {
                var fieldValue = fieldInfo.GetValue(node);
                if (fieldValue != null)
                {
                    clearMethod.Invoke(fieldValue, null);
                }
            }
        }
    }

    // Ensure that System.ComponentModel is available
    internal static readonly Assembly _unused = typeof(TypeConverterAttribute).Assembly;

    [RequiresUnreferencedCode("Uses XamlX Sre types which may not be compatible with trimming.")]
    [RequiresDynamicCode("Uses Reflection.Emit which may not be compatible with AOT.")]
    private static (Func<IServiceProvider?, object>? create, Action<IServiceProvider?, object?> populate) CompileXaml(ILogical intoNode, string xamlPath, string text, IKnownTypes knownTypes)
    {
        var typeSystem = new SreTypeSystem();

        var assembly = typeSystem.FindAssembly(Assembly.GetExecutingAssembly().GetName().Name ?? throw new InvalidOperationException("Could not get executing assembly name"));

        // Create XamlX configuration with our type mappings
        var typeMappings = XamlHelpers.CreateTypeMappings(typeSystem, knownTypes);
        var diagnosticsHandler = new XamlDiagnosticsHandler
        {
            HandleDiagnostic = diagnostic =>
            {
                if (diagnostic.Severity == XamlDiagnosticSeverity.Error)
                    Logging.Debug($"XAML: {diagnostic.Code} - {diagnostic.Title}");
                else if (diagnostic.Severity == XamlDiagnosticSeverity.Warning)
                    Logging.Debug($"XAML: {diagnostic.Code} - {diagnostic.Title}");
                else
                    Logging.Debug($"XAML: {diagnostic.Code} - {diagnostic.Title}");
                return diagnostic.Severity;
            }
        };
        var config = new TransformerConfiguration(typeSystem, assembly, typeMappings, diagnosticsHandler: diagnosticsHandler);

        var emitMappings = new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>();
        var compiler = new XamlILCompiler(config, emitMappings, true)
        {
            EnableIlVerification = false // Disable for now, can enable for debugging
        };

        XamlHelpers.SetUpCompiler(compiler, knownTypes);

        var aName = new AssemblyName($"__XamlRuntimeHotReloadAssembly__{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var ab = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);

        var mb = ab.DefineDynamicModule(aName.Name ?? "MainModule");

        var contextBuilder = typeSystem.CreateTypeBuilder(mb.DefineType("__XamlRuntimeHotReloadContext"));
        var contextTypeDef = compiler.CreateContextType(contextBuilder);

        try
        {
            var type = CompileXamlInner(intoNode, xamlPath, text, typeSystem, mb, compiler, contextTypeDef);
            var runtimeType = type.CreateType();
            var (create, populate) = GetCallbacks(runtimeType!);
            return (create, populate);
        }
        catch (Exception ex)
        {
            Logging.Debug($"Error compiling hot-reload Xaml for {intoNode.GetType().FullName}: {ex.Message}");
            throw;
        }
    }

    [RequiresUnreferencedCode("Uses XamlX Sre types which may not be compatible with trimming.")]
    [RequiresDynamicCode("Uses Reflection.Emit which may not be compatible with AOT.")]
    private static TypeBuilder CompileXamlInner(ILogical intoNode, string xamlPath, string xml, SreTypeSystem typeSystem, ModuleBuilder mb, XamlILCompiler compiler, IXamlType contextTypeDef)
    {
        var targetType = mb.DefineType($"__XamlRuntimeHotReloadType__{intoNode.GetType().FullName}__{Guid.NewGuid():N}",
            TypeAttributes.Public | TypeAttributes.Class,
            intoNode.GetType());
        
        var doc = XDocumentXamlParser.Parse(xml);

        // Transform the XAML AST
        compiler.Transform(doc);

        // Create a type builder for the target type
        var typeBuilder = typeSystem.CreateTypeBuilder(targetType);

        // Compile and emit Populate/Build methods
        compiler.Compile(
            doc,
            typeBuilder,
            contextTypeDef,
            populateMethodName: "Populate",
            createMethodName: "Build",
            namespaceInfoClassName: "XamlNamespaceInfo",
            baseUri: xamlPath,
            fileSource: new XamlFileSource(xamlPath, xml));

        return targetType;
    }

    private static (Func<IServiceProvider?, object>? create, Action<IServiceProvider?, object?> populate)
        GetCallbacks([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type created)
    {
        var isp = Expression.Parameter(typeof(IServiceProvider));
        var createCb = created.GetMethod("Build") is { } buildMethod
            ? Expression.Lambda<Func<IServiceProvider?, object>>(
                Expression.Convert(Expression.Call(buildMethod, isp), typeof(object)), isp).Compile()
            : null;

        var epar = Expression.Parameter(typeof(object));
        var populate = created.GetMethod("Populate")!;
        isp = Expression.Parameter(typeof(IServiceProvider));
        var populateCb = Expression.Lambda<Action<IServiceProvider?, object?>>(
            Expression.Call(populate, isp, Expression.Convert(epar, populate.GetParameters()[1].ParameterType)),
            isp, epar).Compile();

        return (createCb, populateCb);
    }
#endif
}