using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace WorldXaml.XamlX;

/// <summary>
/// AOT-safe emittable node. At emit time it generates a chain of
/// ClrPropertyInfo instances with statically typed getter/setter lambdas,
/// then calls CompiledBind.WithResolvedPath(...) to hand them to the runtime.
/// </summary>
#if !XAMLX_INTERNAL
public
#endif
class ResolvedBindingPathNode(IXamlLineInfo lineInfo, List<ResolvedPathStep> steps, IXamlType leafType, IXamlType clrPropertyInfoType, IXamlType resolvedPathType)
    : XamlAstNode(lineInfo), IXamlAstValueNode, IXamlAstLocalsEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
{
    public List<ResolvedPathStep> Steps { get; } = steps;
    public IXamlAstTypeReference Type { get; } = new XamlAstClrTypeReference(lineInfo, leafType, false);

    /// <summary>
    /// Tracks accessor methods already defined on a declaring type,
    /// keyed by the underlying System.Type Id. This avoids calling
    /// TypeBuilder.GetMethods() (which throws NotSupportedException
    /// before CreateType()) on the SRE hot-reload path.
    /// </summary>
    private static readonly ConditionalWeakTable<object, Dictionary<string, IXamlMethod>> s_definedAccessors = new();

    public XamlILNodeEmitResult Emit(
        XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
        IXamlILEmitter codeGen)
    {
        var ts = context.Configuration.TypeSystem;

        // Emit: new ResolvedPath(new[] { step0, step1, ... })
        // Each step = new ClrPropertyInfo(name, getter, setter, type)
        // where getter/setter are lambda method pointers — no reflection.

        var objectType          = context.Configuration.WellKnownTypes.Object;

        // Push array of ClrPropertyInfo onto stack.
        codeGen
            .Ldc_I4(Steps.Count)
            .Newarr(clrPropertyInfoType);

        for (int i = 0; i < Steps.Count; i++)
        {
            var step = Steps[i];
            var getter = step.Property.Getter!;
            var setter = step.Property.Setter;

            // Build the getter delegate: o => ((OwnerType)o).Prop
            var getterHelper = EmitGetterMethod(context, step);
            // Build the setter delegate: (o, v) => ((OwnerType)o).Prop = (PropType)v
            var setterHelper = setter is not null
                ? EmitSetterMethod(context, step)
                : null;

            codeGen
                .Dup()                   // array ref
                .Ldc_I4(i)               // index
                .Ldstr(step.Property.Name)
                // getter delegate
                .Ldnull()
                .Ldftn(getterHelper)
                .Newobj(ts.GetType("System.Func`2")
                    .MakeGenericType(objectType, objectType)
                    .Constructors.First(c => c.Parameters.Count == 2));

            if (setterHelper is not null)
            {
                codeGen
                    .Ldnull()
                    .Ldftn(setterHelper)
                    .Newobj(ts.GetType("System.Action`2")
                        .MakeGenericType(objectType, objectType)
                        .Constructors.First(c => c.Parameters.Count == 2));
            }
            else
            {
                codeGen.Ldnull();
            }

            codeGen
                .Ldtype(getter.ReturnType)
                .Newobj(clrPropertyInfoType.Constructors.First(c => c.Parameters.Count == 4))
                .Stelem_ref();
        }

        codeGen.Newobj(resolvedPathType.Constructors.First(c => c.Parameters.Count == 1));

        return XamlILNodeEmitResult.Type(0, resolvedPathType);
    }

    /// <summary>
    /// Builds a unique method name encoding the owner type's full name + property name.
    /// Same owner type + property → same name → dedup. Different owner types → different names.
    /// </summary>
    private static string BuildAccessorName(string prefix, ResolvedPathStep step)
        => $"{prefix}{step.OwnerType.FullName.Replace('.', '_').Replace('+', '_')}_{step.Property.Name}";

    // ── Emit a private static method: object Get_PropName(object o) ─────────
    private static IXamlMethod EmitGetterMethod(
        XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
        ResolvedPathStep step)
    {
        var objectType = context.Configuration.WellKnownTypes.Object;
        var getter = step.Property.Getter!;

        var name = BuildAccessorName("__Get_", step);

        // Use our own tracking instead of context.DeclaringType.Methods,
        // because on the SRE hot-reload path DeclaringType wraps a TypeBuilder
        // that throws NotSupportedException from GetMethods() before CreateType().
        var typeId = context.DeclaringType.Id;
        var definedMethods = s_definedAccessors.GetOrCreateValue(typeId);
        if (definedMethods.TryGetValue(name, out var existing))
            return existing;

        var method = context.DeclaringType.DefineMethod(
            objectType, new[] { objectType }, name,
            XamlVisibility.Private, true, false);

        definedMethods[name] = method;

        method.Generator
            .Ldarg_0()
            .Castclass(step.OwnerType)
            .EmitCall(getter);

        if (getter.ReturnType.IsValueType)
            method.Generator.Box(getter.ReturnType);

        method.Generator.Ret();
        return method;
    }

    // ── Emit a private static method: void Set_PropName(object o, object v) ─
    private static IXamlMethod EmitSetterMethod(
        XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
        ResolvedPathStep step)
    {
        var objectType = context.Configuration.WellKnownTypes.Object;
        var setter = step.Property.Setter!;
        var valueType = setter.Parameters[0];

        var name = BuildAccessorName("__Set_", step);

        // Same tracking as EmitGetterMethod — avoid TypeBuilder.GetMethods().
        var typeId = context.DeclaringType.Id;
        var definedMethods = s_definedAccessors.GetOrCreateValue(typeId);
        if (definedMethods.TryGetValue(name, out var existing))
            return existing;

        var method = context.DeclaringType.DefineMethod(
            context.Configuration.WellKnownTypes.Void,
            new[] { objectType, objectType }, name,
            XamlVisibility.Private, true, false);

        definedMethods[name] = method;

        method.Generator
            .Ldarg_0()
            .Castclass(step.OwnerType)
            .Ldarg(1);

        if (valueType.IsValueType)
            method.Generator.Unbox_Any(valueType);
        else
            method.Generator.Castclass(valueType);

        method.Generator
            .EmitCall(setter, true)
            .Ret();

        return method;
    }
}