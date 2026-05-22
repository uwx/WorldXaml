using System;
using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace WorldXaml.XamlX;

/// <summary>
/// Detects CLR properties backed by a static Property&lt;T&gt; field
/// (e.g. public static Property&lt;string&gt; TextProperty) and injects
/// XamlX-compatible setters so both plain values and {Bind} work.
/// </summary>
#if !XAMLX_INTERNAL
public
#endif
sealed class PropertyObjectTransformer(string PropertyObjectFqn, string BindableObjectFqn, string PropertyGenericFqn, string IXamlBindingFqn) : IXamlAstTransformer
{
    // // Fully-qualified names of your runtime types. Adjust to your namespace.
    // private const string PropertyObjectFqn  = "MyApp.PropertyObject";
    // private const string PropertyGenericFqn = "MyApp.Property`1";
    // private const string IXamlBindingFqn    = "MyApp.IXamlBinding";

    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is not XamlAstClrProperty prop)
            return node;

        var ts = context.Configuration.TypeSystem;

        // Find the FooProperty static field on the declaring type.
        var fieldName = prop.Name + "Property";
        var field = prop.DeclaringType
            .GetAllFields()
            .FirstOrDefault(f => f.IsStatic && f.IsPublic && f.Name == fieldName);

        if (field == null)
            return node;

        // Confirm it is (or inherits from) Property<T>.
        var valueType = ResolvePropertyValueType(field, ts, PropertyGenericFqn);
        if (valueType == null)
            return node;

        // Resolve the runtime methods we will call.
        var propertyObjectType = ts.FindType(PropertyObjectFqn) ?? throw new XamlTypeSystemException($"Couldn't find type {PropertyObjectFqn} in the type system.");
        var iXamlBindingType   = ts.FindType(IXamlBindingFqn) ?? throw new XamlTypeSystemException($"Couldn't find type {IXamlBindingFqn} in the type system.");
        var bindableObjectType = ts.FindType(BindableObjectFqn) ?? throw new XamlTypeSystemException($"Couldn't find type {BindableObjectFqn} in the type system.");

        // SetValue<TValue>(Property<TValue>, TValue)
        var setValueMethod = propertyObjectType
            .Methods
            .First(m => m.Name == "SetValue" && m is { IsGenericMethod: true, Parameters.Count: 2 });

        // BindFromXaml<TValue>(Property<TValue>, IXamlBinding)
        var bindFromXamlMethod = bindableObjectType
            .Methods
            .First(m => m.Name == "BindFromXaml" && m is { IsGenericMethod: true, Parameters.Count: 2 });

        // Clone the property and prepend our setters (highest-priority first).
        var newProp = new XamlAstClrProperty(prop, prop.Name, prop.DeclaringType, prop.Getter, prop.Setters, []);

        // 1. Binding setter — checked first so {Bind} wins over value setter.
        newProp.Setters.Insert(0, new BindingSetter(
            field, iXamlBindingType, prop.DeclaringType,
            bindFromXamlMethod.MakeGenericMethod([valueType])));

        // 2. Typed value setter — direct assignment.
        newProp.Setters.Insert(1, new ValueSetter(
            field, valueType, prop.DeclaringType,
            setValueMethod.MakeGenericMethod([valueType])));

        return newProp;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IXamlType? ResolvePropertyValueType(
        IXamlField field, IXamlTypeSystem ts, string propertyGenericFqn)
    {
        var genericBase = ts.FindType(propertyGenericFqn) ?? throw new XamlTypeSystemException($"Couldn't find type {propertyGenericFqn} in the type system.");
        for (var t = field.FieldType; t != null; t = t.BaseType)
            if (t.GenericTypeDefinition?.Equals(genericBase) == true)
                return t.GenericArguments[0];
        return null;
    }

    // ── Custom setters ───────────────────────────────────────────────────────

    /// <summary>
    /// Emits: this.SetValue&lt;TValue&gt;(FooProperty, value)
    /// </summary>
    private sealed class ValueSetter(
        IXamlField field,
        IXamlType valueType,
        IXamlType declaringType,
        IXamlMethod setValueMethod) // already closed over TValue
        : IXamlILOptimizedEmitablePropertySetter
    {
        // already closed over TValue

        public IXamlType TargetType { get; } = declaringType;
        public PropertySetterBinderParameters BinderParameters { get; } = new()
        {
            AllowXNull        = !valueType.IsValueType,
            AllowRuntimeNull  = !valueType.IsValueType,
        };

        public IReadOnlyList<IXamlType> Parameters { get; } = [valueType];
        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => [];

        // Called when value is already on the stack: [this | value]
        public void Emit(IXamlILEmitter emitter)
        {
            // Stack: [this, value]  → need [this, FooProperty, value]
            using var valueLoc = emitter.LocalsPool.GetLocal(Parameters[0]);
            emitter
                .Stloc(valueLoc.Local)   // pop value to local
                .Ldsfld(field)           // push FooProperty
                .Ldloc(valueLoc.Local)   // push value
                .EmitCall(setValueMethod, true);
        }

        // Called when XamlX has AST nodes for arguments (preferred fast path).
        public void EmitWithArguments(
            XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter emitter,
            IReadOnlyList<IXamlAstValueNode> arguments)
        {
            emitter.Ldsfld(field);
            context.Emit(arguments[0], emitter, Parameters[0]);
            emitter.EmitCall(setValueMethod, true);
        }
    }

    /// <summary>
    /// Emits: this.BindFromXaml&lt;TValue&gt;(FooProperty, binding)
    /// </summary>
    private sealed class BindingSetter(
        IXamlField field,
        IXamlType iXamlBindingType,
        IXamlType declaringType,
        IXamlMethod bindMethod) // already closed over TValue
        : IXamlILOptimizedEmitablePropertySetter
    {
        public IXamlType TargetType { get; } = declaringType;
        public PropertySetterBinderParameters BinderParameters { get; } = new()
        {
            AllowXNull       = false,
            AllowRuntimeNull = false,
        };

        public IReadOnlyList<IXamlType> Parameters { get; } = [iXamlBindingType];
        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => [];

        public void Emit(IXamlILEmitter emitter)
        {
            using var bindLoc = emitter.LocalsPool.GetLocal(Parameters[0]);
            emitter
                .Stloc(bindLoc.Local)
                .Ldsfld(field)
                .Ldloc(bindLoc.Local)
                .EmitCall(bindMethod, true);
        }

        public void EmitWithArguments(
            XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter emitter,
            IReadOnlyList<IXamlAstValueNode> arguments)
        {
            emitter.Ldsfld(field);
            context.Emit(arguments[0], emitter, Parameters[0]);
            emitter.EmitCall(bindMethod, true);
        }
    }
}