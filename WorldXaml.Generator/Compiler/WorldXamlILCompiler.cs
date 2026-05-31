using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace WorldXaml.Generator.Compiler;

/// <summary>
/// Extends XamlILCompiler to inject XamlHotReload.Register(element, baseUri) at the start
/// of Populate methods. This avoids using ContextFactoryCallback which would also fire
/// inside deferred content closures (ControlTemplate bodies) where arg1 doesn't exist.
/// </summary>
internal sealed class WorldXamlILCompiler : XamlILCompiler
{
    private readonly IXamlMethod? _registerMethod;

    public WorldXamlILCompiler(
        TransformerConfiguration configuration,
        XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings,
        bool fillWithDefaults,
        IXamlMethod? registerMethod)
        : base(configuration, emitMappings, fillWithDefaults)
    {
        _registerMethod = registerMethod;
    }

    protected override void CompilePopulate(
        IFileSource? fileSource,
        IXamlAstManipulationNode manipulation,
        IXamlTypeBuilder<IXamlILEmitter> declaringType,
        IXamlILEmitter codeGen,
        XamlRuntimeContext<IXamlILEmitter, XamlILNodeEmitResult> context)
    {
        var emitContext = InitCodeGen(fileSource, declaringType, codeGen, context, true);

        // Emit XamlHotReload.Register(arg1, baseUri) — only in Populate, not in deferred content
        if (_registerMethod != null)
        {
            codeGen
                .Ldarg(1)
                .Ldstr(context.BaseUrl)
                .EmitCall(_registerMethod);
        }

        codeGen
            .Ldloc(emitContext.ContextLocal)
            .Emit(OpCodes.Ldarg_1)
            .Emit(OpCodes.Stfld, context.RootObjectField!)
            .Ldloc(emitContext.ContextLocal)
            .Emit(OpCodes.Ldarg_1)
            .Emit(OpCodes.Stfld, context.IntermediateRootObjectField!)
            .Emit(OpCodes.Ldarg_1);
        emitContext.Emit(manipulation, codeGen, null);
        codeGen.Emit(OpCodes.Ret);

        emitContext.ExecuteAfterEmitCallbacks();
    }
}
