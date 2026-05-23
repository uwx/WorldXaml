using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace WorldXaml.XamlX;

#if !XAMLX_INTERNAL
public
#endif
class BindingPathTransformer(string CompiledBindFqn, string clrPropertyInfoFqn, string resolvedPathFqn) : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is not XamlAstConstructableObjectNode binding)
            return node;
        if (binding.Type.GetClrType().FullName != CompiledBindFqn)
            return node;

        var pathAssignment = binding.Children
            .OfType<XamlPropertyAssignmentNode>()
            .FirstOrDefault(p => p.Property.Name == "Path");

        if (pathAssignment?.Values[0] is not ParsedBindingPathNode parsed)
            return node;

        // ── Resolve starting DataContext type from nearest x:DataType in scope ──
        var dataContextNode = context.ParentNodes()
            .OfType<DataContextTypeMetadataNode>()
            .FirstOrDefault();

        if (dataContextNode is null)
            throw new XamlTransformException(
                "Cannot use {CompiledBind} without an x:DataType on this element or a parent.",
                node);

        // ── Walk the path segments against the type system ────────────────────
        var resolvedPath = ResolvePath(context, dataContextNode.DataContextType, parsed.Segments, parsed);

        // ── Replace ParsedBindingPathNode with the resolved, emittable node ───
        pathAssignment.Values[0] = resolvedPath;

        return node;
    }

    private ResolvedBindingPathNode ResolvePath(
        AstTransformationContext context,
        IXamlType startType,
        IReadOnlyList<string> segments,
        IXamlLineInfo lineInfo)
    {
        var steps = new List<ResolvedPathStep>();
        var currentType = startType;

        foreach (var seg in segments)
        {
            // Find the CLR property on the current type (walk hierarchy).
            var prop = currentType
                .GetAllProperties()
                .FirstOrDefault(p => p.Name == seg);

            if (prop is null)
                throw new XamlTransformException(
                    $"Property '{seg}' not found on type '{currentType.GetFqn()}'.", lineInfo);

            if (prop.Getter is null)
                throw new XamlTransformException(
                    $"Property '{seg}' on '{currentType.GetFqn()}' has no getter.", lineInfo);

            steps.Add(new ResolvedPathStep(prop, currentType));
            currentType = prop.Getter.ReturnType;
        }

        var ts = context.Configuration.TypeSystem;
        var clrPropertyInfoType = ts.GetType(clrPropertyInfoFqn) ?? throw new XamlTypeSystemException($"Type '{clrPropertyInfoFqn}' not found in type system.");
        var resolvedPathType = ts.GetType(resolvedPathFqn) ?? throw new XamlTypeSystemException($"Type '{resolvedPathFqn}' not found in type system.");

        return new ResolvedBindingPathNode(lineInfo, steps, currentType, clrPropertyInfoType, resolvedPathType);
    }
}

#if !XAMLX_INTERNAL
public
#endif
record ResolvedPathStep(IXamlProperty Property, IXamlType OwnerType);

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

    // ── Emit a private static method: object Get_PropName(object o) ─────────
    private static IXamlMethod EmitGetterMethod(
        XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
        ResolvedPathStep step)
    {
        var objectType = context.Configuration.WellKnownTypes.Object;
        var getter = step.Property.Getter!;

        var name = $"__Get_{step.OwnerType.Name}_{step.Property.Name}";
        var method = context.DeclaringType.DefineMethod(
            objectType, new[] { objectType }, name,
            XamlVisibility.Private, true, false);

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

        var name = $"__Set_{step.OwnerType.Name}_{step.Property.Name}";
        var method = context.DeclaringType.DefineMethod(
            context.Configuration.WellKnownTypes.Void,
            new[] { objectType, objectType }, name,
            XamlVisibility.Private, true, false);

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