using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using WorldXaml.Generator.Common;
using XamlX.IL;
using XamlX.IL.CSharp;
using XamlX.TypeSystem;
using SreOpCode = System.Reflection.Emit.OpCode;
using SreOpCodes = System.Reflection.Emit.OpCodes;

namespace XamlX.CSharp;

/// <summary>
/// An IXamlILEmitter implementation that translates IL opcodes to C# source code statements.
/// The emitter maintains a virtual evaluation stack to track types and produce valid C# expressions.
/// </summary>
#if !XAMLX_INTERNAL
public
#endif
class CSharpEmitter : IXamlILEmitter
{
    private readonly CSharpMethodContext _method;
    private readonly List<string> _statements = new();
    private readonly Stack<CSharpExpression> _evalStack = new();
    private readonly List<CSharpLocal> _locals = new();
    private readonly List<CSharpLocal> _tempLocals = new();
    private readonly Dictionary<CSharpLabel, string> _labelNames = new();
    private readonly Dictionary<string, List<CSharpExpression>> _labelStackSnapshots = new();
    private int _labelCounter;
    private int _localCounter;
    private int _tempCounter;
    private readonly CSharpEmitterKnownTypes _knownTypes;

    public IXamlTypeSystem TypeSystem { get; }
    public XamlLocalsPool LocalsPool { get; }

    public CSharpEmitter(CSharpEmitterKnownTypes knownTypes, IXamlTypeSystem typeSystem, CSharpMethodContext method)
    {
        TypeSystem = typeSystem;
        _method = method;
        LocalsPool = new XamlLocalsPool(DefineLocal);
        _knownTypes = knownTypes;
    }

    /// <summary>
    /// Gets the generated C# statements for this method body.
    /// </summary>
    public IReadOnlyList<string> Statements => _statements;

    private void Push(string expr, IXamlType? type = null)
        => _evalStack.Push(new CSharpExpression(expr, type));

    private CSharpExpression Pop()
        => _evalStack.Count > 0 ? _evalStack.Pop() : new CSharpExpression("default", null);

    private string PopExpr() => Pop().Expression;

    /// <summary>
    /// Converts integer literal "0"/"1" to "false"/"true" when the target type is bool.
    /// Returns the original expression unchanged for non-bool targets.
    /// </summary>
    private static string CoerceLiteralToBool(string expr, IXamlType targetType, IXamlType knownBoolean)
    {
        if (!targetType.Equals(knownBoolean))
            return expr;
        if (expr == "0") return "false";
        if (expr == "1") return "true";
        return $"({expr}) != 0";
    }

    /// <summary>
    /// Pops a value expression, casting it to <paramref name="targetType"/> if the stack-tracked
    /// type is not assignable to it (e.g. <c>object</c> → <c>FlexPanel</c>).
    /// </summary>
    private string PopExprCastTo(IXamlType? targetType)
    {
        var val = Pop();
        if (targetType is not null && val.Type is not null && !targetType.IsAssignableFrom(val.Type))
            return $"(({FormatType(targetType)}){val.Expression})";
        return val.Expression;
    }

    private void Emit(string statement) => _statements.Add(statement);

    private string FormatType(IXamlType type) => CSharpFormatting.FormatType(_knownTypes, type);

    #region IXamlILEmitter Implementation

    public IXamlILEmitter Emit(SreOpCode code)
    {
        if (code == SreOpCodes.Nop) { /* skip */ }
        else if (code == SreOpCodes.Ret)
        {
            if (_method.ReturnType != null && !_method.ReturnType.Equals(_knownTypes.SystemVoid))
            {
                var val = PopExpr();
                // Convert int literals to bool when return type is bool
                val = CoerceLiteralToBool(val, _method.ReturnType, _knownTypes.SystemBoolean);
                Emit($"return {val};");
            }
            else
            {
                Emit("return;");
            }
        }
        else if (code == SreOpCodes.Dup)
        {
            var val = Pop();
            var temp = AllocTemp();
            _tempLocals.Add(new CSharpLocal(temp, -1, val.Type ?? _knownTypes.SystemObject));
            Emit($"{temp} = {val.Expression};");
            Push(temp, val.Type);
            Push(temp, val.Type);
        }
        else if (code == SreOpCodes.Pop)
        {
            var val = PopExpr();
            // Only emit if the expression might have side effects
            if (!IsSimpleExpression(val))
                Emit($"_ = {val};");
        }
        else if (code == SreOpCodes.Throw)
        {
            var val = PopExpr();
            Emit($"throw (global::System.Exception){val};");
        }
        else if (code == SreOpCodes.Ldnull) Push("null");
        else if (code == SreOpCodes.Ldc_I4_0) Push("0");
        else if (code == SreOpCodes.Ldc_I4_1) Push("1");
        else if (code == SreOpCodes.Ldc_I4_2) Push("2");
        else if (code == SreOpCodes.Ldc_I4_3) Push("3");
        else if (code == SreOpCodes.Ldc_I4_4) Push("4");
        else if (code == SreOpCodes.Ldc_I4_5) Push("5");
        else if (code == SreOpCodes.Ldc_I4_6) Push("6");
        else if (code == SreOpCodes.Ldc_I4_7) Push("7");
        else if (code == SreOpCodes.Ldc_I4_8) Push("8");
        else if (code == SreOpCodes.Ldc_I4_M1) Push("-1");
        else if (code == SreOpCodes.Ldarg_0) Push(_method.GetArgName(0), _method.GetArgType(0));
        else if (code == SreOpCodes.Ldarg_1) Push(_method.GetArgName(1), _method.GetArgType(1));
        else if (code == SreOpCodes.Ldarg_2) Push(_method.GetArgName(2), _method.GetArgType(2));
        else if (code == SreOpCodes.Ldarg_3) Push(_method.GetArgName(3), _method.GetArgType(3));
        else if (code == SreOpCodes.Ldloc_0) Push(GetLocalName(0), GetLocalType(0));
        else if (code == SreOpCodes.Ldloc_1) Push(GetLocalName(1), GetLocalType(1));
        else if (code == SreOpCodes.Ldloc_2) Push(GetLocalName(2), GetLocalType(2));
        else if (code == SreOpCodes.Ldloc_3) Push(GetLocalName(3), GetLocalType(3));
        else if (code == SreOpCodes.Stloc_0) Emit($"{GetLocalName(0)} = {PopExprCastTo(GetLocalType(0))};");
        else if (code == SreOpCodes.Stloc_1) Emit($"{GetLocalName(1)} = {PopExprCastTo(GetLocalType(1))};");
        else if (code == SreOpCodes.Stloc_2) Emit($"{GetLocalName(2)} = {PopExprCastTo(GetLocalType(2))};");
        else if (code == SreOpCodes.Stloc_3) Emit($"{GetLocalName(3)} = {PopExprCastTo(GetLocalType(3))};");
        else if (code == SreOpCodes.Ldelem_Ref)
        {
            var index = PopExpr();
            var array = PopExpr();
            Push($"{array}[{index}]");
        }
        else if (code == SreOpCodes.Stelem_Ref)
        {
            var value = PopExpr();
            var index = PopExpr();
            var array = PopExpr();
            Emit($"{array}[{index}] = {value};");
        }
        else if (code == SreOpCodes.Ldlen)
        {
            var array = PopExpr();
            Push($"{array}.Length");
        }
        else if (code == SreOpCodes.Add)
        {
            var right = PopExpr();
            var left = PopExpr();
            Push($"({left} + {right})");
        }
        else if (code == SreOpCodes.Sub)
        {
            var right = PopExpr();
            var left = PopExpr();
            Push($"({left} - {right})");
        }
        else
        {
            Emit($"// TODO: Unhandled opcode: {code.Name}");
        }

        return this;
    }

    public IXamlILEmitter Emit(SreOpCode code, IXamlField field)
    {
        var fieldRef = $"{FormatType(field.DeclaringType)}.{field.Name}";

        if (code == SreOpCodes.Ldfld)
        {
            var objPop = Pop();
            var obj = objPop.Expression;
            if (objPop.Type != null && !objPop.Type.Equals(field.DeclaringType))
            {
                bool needsCast;
                try { needsCast = !field.DeclaringType.IsAssignableFrom(objPop.Type); }
                catch { needsCast = true; }
                if (needsCast)
                    obj = $"(({FormatType(field.DeclaringType)}){obj})";
            }
            Push($"{obj}.{field.Name}", field.FieldType);
        }
        else if (code == SreOpCodes.Ldsfld)
        {
            Push(fieldRef, field.FieldType);
        }
        else if (code == SreOpCodes.Stfld)
        {
            var value = PopExpr();
            var objPop = Pop();
            var obj = objPop.Expression;
            if (objPop.Type != null && !objPop.Type.Equals(field.DeclaringType))
            {
                bool needsCast;
                try { needsCast = !field.DeclaringType.IsAssignableFrom(objPop.Type); }
                catch { needsCast = true; }
                if (needsCast)
                    obj = $"(({FormatType(field.DeclaringType)}){obj})";
            }
            if (field.FieldType.IsEnum)
                value = $"(({FormatType(field.FieldType)}){value})";
            else
                value = CoerceLiteralToBool(value, field.FieldType, _knownTypes.SystemBoolean);
            Emit($"{obj}.{field.Name} = {value};");
        }
        else if (code == SreOpCodes.Stsfld)
        {
            var value = PopExpr();
            Emit($"{fieldRef} = {value};");
        }
        else
        {
            Emit($"// TODO: Unhandled field opcode: {code.Name} {fieldRef}");
        }

        return this;
    }

    public IXamlILEmitter Emit(SreOpCode code, IXamlMethod method)
    {
        if (code == SreOpCodes.Call || code == SreOpCodes.Callvirt)
        {
            EmitMethodCall(method, isVirtual: code == SreOpCodes.Callvirt);
        }
        else if (code == SreOpCodes.Ldtoken)
        {
            Push($"typeof({FormatType(method.ReturnType)}).GetMethod(\"{method.Name}\").MethodHandle");
        }
        else if (code == SreOpCodes.Ldftn)
        {
            // Push a method reference - the consumer will typically use it with a delegate constructor
            var obj = method.IsStatic ? FormatType(method.DeclaringType) : PopExpr();
            Push($"{obj}.{method.Name}");
        }
        else
        {
            Emit($"// TODO: Unhandled method opcode: {code.Name} {method.Name}");
        }

        return this;
    }

    public IXamlILEmitter Emit(SreOpCode code, IXamlConstructor ctor)
    {
        if (code == SreOpCodes.Newobj)
        {
            var args = new string[ctor.Parameters.Count];
            for (var i = args.Length - 1; i >= 0; i--)
                args[i] = PopExpr();

            // Cast int literals to enum types when the parameter expects an enum.
            for (var i = 0; i < args.Length; i++)
            {
                if (ctor.Parameters[i].IsEnum)
                    args[i] = $"(({FormatType(ctor.Parameters[i])}){args[i]})";
            }

            // Delegate constructor pattern: new Func<A,B>(null, MethodRef) → new Func<A,B>(MethodRef)
            // IL emits Ldnull + Ldftn + Newobj(delegate..ctor(object, IntPtr))
            if (args is ["null", _] && _knownTypes.SystemDelegate.IsAssignableFrom(ctor.DeclaringType))
            {
                Push($"new {FormatType(ctor.DeclaringType)}({args[1]})", ctor.DeclaringType);
            }
            else
            {
                Push($"new {FormatType(ctor.DeclaringType)}({string.Join(", ", args)})", ctor.DeclaringType);
            }
        }
        else if (code == SreOpCodes.Call)
        {
            // Base constructor call
            var args = new string[ctor.Parameters.Count];
            for (var i = args.Length - 1; i >= 0; i--)
                args[i] = PopExpr();

            var obj = PopExpr(); // 'this'
            Emit($"// base ctor call: {FormatType(ctor.DeclaringType)}({string.Join(", ", args)})");
        }
        else
        {
            Emit($"// TODO: Unhandled ctor opcode: {code.Name}");
        }

        return this;
    }

    public IXamlILEmitter Emit(SreOpCode code, string arg)
    {
        if (code == SreOpCodes.Ldstr)
            Push(CSharpFormatting.FormatStringLiteral(arg));
        else
            Emit($"// TODO: Unhandled string opcode: {code.Name} \"{arg}\"");
        return this;
    }

    public IXamlILEmitter Emit(SreOpCode code, int arg)
    {
        if (code == SreOpCodes.Ldc_I4 || code == SreOpCodes.Ldc_I4_S)
            Push(arg.ToString());
        else if (code == SreOpCodes.Ldarg || code == SreOpCodes.Ldarg_S)
            Push(_method.GetArgName(arg), _method.GetArgType(arg));
        else if (code == SreOpCodes.Ldloc || code == SreOpCodes.Ldloc_S)
            Push(GetLocalName(arg));
        else if (code == SreOpCodes.Stloc || code == SreOpCodes.Stloc_S)
            Emit($"{GetLocalName(arg)} = {PopExprCastTo(GetLocalType(arg))};");
        else
            Emit($"// TODO: Unhandled int opcode: {code.Name} {arg}");
        return this;
    }

    public IXamlILEmitter Emit(SreOpCode code, long arg)
    {
        if (code == SreOpCodes.Ldc_I8)
            Push($"{arg}L");
        else
            Emit($"// TODO: Unhandled long opcode: {code.Name} {arg}");
        return this;
    }

    public IXamlILEmitter Emit(SreOpCode code, sbyte arg)
    {
        if (code == SreOpCodes.Ldc_I4_S)
            Push(((int)arg).ToString());
        else
            Emit($"// TODO: Unhandled sbyte opcode: {code.Name} {arg}");
        return this;
    }

    public IXamlILEmitter Emit(SreOpCode code, byte arg)
    {
        if (code == SreOpCodes.Ldarg_S)
            Push(_method.GetArgName(arg), _method.GetArgType(arg));
        else
            Emit($"// TODO: Unhandled byte opcode: {code.Name} {arg}");
        return this;
    }

    public IXamlILEmitter Emit(SreOpCode code, IXamlType type)
    {
        var typeName = FormatType(type);

        if (code == SreOpCodes.Castclass)
        {
            var val = PopExpr();
            Push($"(({typeName}){val})", type);
        }
        else if (code == SreOpCodes.Isinst)
        {
            var val = PopExpr();
            Push($"({val} as {typeName})", type);
        }
        else if (code == SreOpCodes.Box)
        {
            var val = PopExpr();
            Push($"(object){val}");
        }
        else if (code == SreOpCodes.Unbox_Any)
        {
            var val = PopExpr();
            Push($"(({typeName}){val})", type);
        }
        else if (code == SreOpCodes.Unbox)
        {
            // Unbox leaves a managed pointer; in C# we just cast
            var val = PopExpr();
            Push($"(({typeName}){val})", type);
        }
        else if (code == SreOpCodes.Newarr)
        {
            var length = PopExpr();
            IXamlType? arrayType = null;
            try { arrayType = type.MakeArrayType(1); } catch { }
            Push($"new {typeName}[{length}]", arrayType);
        }
        else if (code == SreOpCodes.Ldtoken)
        {
            Push($"typeof({typeName})");
        }
        else if (code == SreOpCodes.Initobj)
        {
            var addr = PopExpr();
            Emit($"{addr} = default({typeName});");
        }
        else if (code == SreOpCodes.Ldelem)
        {
            var index = PopExpr();
            var array = PopExpr();
            Push($"{array}[{index}]");
        }
        else if (code == SreOpCodes.Stelem)
        {
            var value = PopExpr();
            var index = PopExpr();
            var array = PopExpr();
            Emit($"{array}[{index}] = {value};");
        }
        else
        {
            Emit($"// TODO: Unhandled type opcode: {code.Name} {typeName}");
        }

        return this;
    }

    public IXamlILEmitter Emit(SreOpCode code, float arg)
    {
        if (code == SreOpCodes.Ldc_R4)
            Push($"{arg.ToString(CultureInfo.InvariantCulture)}f");
        else
            Emit($"// TODO: Unhandled float opcode: {code.Name} {arg}");
        return this;
    }

    public IXamlILEmitter Emit(SreOpCode code, double arg)
    {
        if (code == SreOpCodes.Ldc_R8)
            Push($"{arg.ToString(CultureInfo.InvariantCulture)}d");
        else
            Emit($"// TODO: Unhandled double opcode: {code.Name} {arg}");
        return this;
    }

    public IXamlLabel DefineLabel()
    {
        var label = new CSharpLabel($"label_{_labelCounter++}");
        return label;
    }

    public IXamlILEmitter MarkLabel(IXamlLabel label)
    {
        var csl = (CSharpLabel)label;

        // Reconcile the eval stack with the snapshot saved when a branch targeted this label.
        // Emit reconcile assignments BEFORE the label so that goto skips them —
        // only the fall-through path executes these assignments.
        if (_labelStackSnapshots.TryGetValue(csl.Name, out var snapshot))
        {
            var current = new List<CSharpExpression>();
            while (_evalStack.Count > 0)
                current.Add(_evalStack.Pop());
            current.Reverse();

            // Assign fall-through values to the canonical snapshot temps
            for (int i = 0; i < snapshot.Count && i < current.Count; i++)
            {
                if (snapshot[i].Expression != current[i].Expression)
                    Emit($"{snapshot[i].Expression} = {current[i].Expression};");
            }

            // Emit the label after the reconcile assignments
            Emit($"{csl.Name}:;");

            // Restore the snapshot as the canonical eval stack
            _evalStack.Clear();
            foreach (var expr in snapshot)
                _evalStack.Push(expr);
            _labelStackSnapshots.Remove(csl.Name);
        }
        else
        {
            Emit($"{csl.Name}:;");
        }

        return this;
    }

    public IXamlILEmitter Emit(SreOpCode code, IXamlLabel label)
    {
        var csl = (CSharpLabel)label;

        if (code == SreOpCodes.Br || code == SreOpCodes.Br_S)
        {
            FlushStack();
            SaveStackSnapshot(csl.Name);
            Emit($"goto {csl.Name};");
            // Code after an unconditional goto is dead; clear the eval stack
            // so MarkLabel for subsequent labels starts with a clean state.
            _evalStack.Clear();
        }
        else if (code == SreOpCodes.Brfalse || code == SreOpCodes.Brfalse_S)
        {
            var val = Pop();
            FlushStack();
            SaveStackSnapshot(csl.Name);
            Emit($"if ({FormatFalsinessCheck(val)}) goto {csl.Name};");
        }
        else if (code == SreOpCodes.Brtrue || code == SreOpCodes.Brtrue_S)
        {
            var val = Pop();
            FlushStack();
            SaveStackSnapshot(csl.Name);
            Emit($"if ({FormatTruthinessCheck(val)}) goto {csl.Name};");
        }
        else if (code == SreOpCodes.Beq || code == SreOpCodes.Beq_S)
        {
            var right = PopExpr();
            var left = PopExpr();
            Emit($"if ({left} == {right}) goto {csl.Name};");
        }
        else if (code == SreOpCodes.Blt || code == SreOpCodes.Blt_S)
        {
            var right = PopExpr();
            var left = PopExpr();
            Emit($"if ({left} < {right}) goto {csl.Name};");
        }
        else if (code == SreOpCodes.Ble || code == SreOpCodes.Ble_S)
        {
            var right = PopExpr();
            var left = PopExpr();
            Emit($"if ({left} <= {right}) goto {csl.Name};");
        }
        else if (code == SreOpCodes.Bgt || code == SreOpCodes.Bgt_S)
        {
            var right = PopExpr();
            var left = PopExpr();
            Emit($"if ({left} > {right}) goto {csl.Name};");
        }
        else if (code == SreOpCodes.Bge || code == SreOpCodes.Bge_S)
        {
            var right = PopExpr();
            var left = PopExpr();
            Emit($"if ({left} >= {right}) goto {csl.Name};");
        }
        else
        {
            Emit($"// TODO: Unhandled label opcode: {code.Name} {csl.Name}");
        }

        return this;
    }

    public IXamlILEmitter Emit(SreOpCode code, IXamlLocal local)
    {
        var csl = (CSharpLocal)local;

        if (code == SreOpCodes.Ldloc || code == SreOpCodes.Ldloc_S)
            Push(csl.Name, csl.Type);
        else if (code == SreOpCodes.Stloc || code == SreOpCodes.Stloc_S)
            Emit($"{csl.Name} = {PopExprCastTo(csl.Type)};");
        else if (code == SreOpCodes.Ldloca || code == SreOpCodes.Ldloca_S)
            Push($"ref {csl.Name}", csl.Type);
        else
            Emit($"// TODO: Unhandled local opcode: {code.Name} {csl.Name}");

        return this;
    }

    public void InsertSequencePoint(IFileSource file, int line, int position)
    {
        Emit($"// {file.FilePath}:{line}:{position}");
    }

    public IXamlLocal DefineLocal(IXamlType type)
    {
        var local = new CSharpLocal($"__local_{_localCounter}", _localCounter, type);
        _locals.Add(local);
        _localCounter++;
        return local;
    }

    #endregion

    #region Private Helpers

    private string FormatGenericArgs(IXamlMethod method)
    {
        return method is { IsGenericMethod: true, GenericArguments: { Count: > 0 } ga }
            ? $"<{string.Join(", ", ga.Select(FormatType))}>"
            : "";
    }

    private void EmitMethodCall(IXamlMethod method, bool isVirtual)
    {
        var args = new string[method.Parameters.Count];
        for (var i = args.Length - 1; i >= 0; i--)
            args[i] = PopExpr();

        // Cast int literals to enum types when the parameter expects an enum.
        for (var i = 0; i < args.Length; i++)
        {
            if (method.Parameters[i].IsEnum)
                args[i] = $"(({FormatType(method.Parameters[i])}){args[i]})";
        }

        // Convert int literals to bool when the parameter expects a bool.
        for (var i = 0; i < args.Length; i++)
            args[i] = CoerceLiteralToBool(args[i], method.Parameters[i], _knownTypes.SystemBoolean);

        // Special case: Type.GetTypeFromHandle(typeof(X)) → typeof(X)
        if (method is { IsStatic: true, Name: "GetTypeFromHandle" } &&
            method.DeclaringType.Equals(_knownTypes.SystemType) &&
            args.Length == 1 && args[0].StartsWith("typeof("))
        {
            Push(args[0], method.ReturnType);
            return;
        }

        string call;

        if (method.IsStatic)
        {
            var typeName = FormatType(method.DeclaringType);
            // Convert static property accessor calls to C# syntax
            if (method.Name.StartsWith("get_") && args.Length == 0)
            {
                var propName = method.Name.Substring(4);
                call = $"{typeName}.{propName}";
            }
            else if (method.Name.StartsWith("set_") && args.Length == 1)
            {
                var propName = method.Name.Substring(4);
                Emit($"{typeName}.{propName} = {args[0]};");
                return;
            }
            else
            {
                call = $"{typeName}.{method.Name}{FormatGenericArgs(method)}({string.Join(", ", args)})";
            }
        }
        else
        {
            var objPop = Pop();
            var obj = objPop.Expression;
            var objType = objPop.Type;

            // Cast object to method's declaring type if the tracked type doesn't have the method
            if (objType != null && !objType.Equals(method.DeclaringType))
            {
                bool needsCast;
                try { needsCast = !method.DeclaringType.IsAssignableFrom(objType); }
                catch { needsCast = true; }
                if (needsCast)
                    obj = $"(({FormatType(method.DeclaringType)}){obj})";
            }

            // Convert property/indexer accessor calls to C# syntax
            if (method.Name == "get_Item" && args.Length >= 1)
            {
                // Indexer: obj[args]
                call = $"{obj}[{string.Join(", ", args)}]";
            }
            else if (method.Name == "set_Item" && args.Length >= 2)
            {
                // Indexer setter: obj[args[0..n-1]] = args[n]
                var indexArgs = string.Join(", ", args.Take(args.Length - 1));
                Emit($"{obj}[{indexArgs}] = {args[^1]};");
                return;
            }
            else if (method.Name.StartsWith("get_") && args.Length == 0)
            {
                var propName = method.Name[4..];
                call = $"{obj}.{propName}";
            }
            else if (method.Name.StartsWith("set_") && args.Length == 1)
            {
                var propName = method.Name[4..];
                Emit($"{obj}.{propName} = {args[0]};");
                return;
            }
            else
            {
                call = $"{obj}.{method.Name}{FormatGenericArgs(method)}({string.Join(", ", args)})";
            }
        }

        if (method.ReturnType.Equals(_knownTypes.SystemVoid))
        {
            Emit($"{call};");
        }
        else
        {
            Push(call, method.ReturnType);
        }
    }

    private string GetLocalName(int index)
    {
        if (index < _locals.Count)
            return _locals[index].Name;
        return $"__local_{index}";
    }

    private IXamlType? GetLocalType(int index)
    {
        if (index < _locals.Count)
            return _locals[index].Type;
        return null;
    }

    private string AllocTemp() => $"__tmp_{_tempCounter++}";

    private void SaveStackSnapshot(string labelName)
    {
        // Save a copy of the current eval stack so it can be restored/reconciled at MarkLabel.
        // If a snapshot already exists (from a different branch path), reconcile: emit assignments
        // so this path writes its values into the canonical temps from the first snapshot.
        var currentStack = new List<CSharpExpression>(_evalStack.Reverse());

        if (_labelStackSnapshots.TryGetValue(labelName, out var existing))
        {
            // Both paths must converge on the same temp variables.
            // Assign this path's values to the canonical temps from the first snapshot.
            for (int i = 0; i < existing.Count && i < currentStack.Count; i++)
            {
                if (existing[i].Expression != currentStack[i].Expression)
                    Emit($"{existing[i].Expression} = {currentStack[i].Expression};");
            }
            // Keep the existing snapshot as canonical
        }
        else
        {
            _labelStackSnapshots[labelName] = currentStack;
        }
    }

    private void FlushStack()
    {
        // Assign any pending stack values to temps to preserve ordering across gotos.
        // All values are saved to temps to ensure consistent stack state across branches.
        var pending = new List<CSharpExpression>();
        while (_evalStack.Count > 0)
            pending.Add(_evalStack.Pop());

        pending.Reverse();
        foreach (var expr in pending)
        {
            var temp = AllocTemp();
            var tempType = expr.Type ?? _knownTypes.SystemObject;
            _tempLocals.Add(new CSharpLocal(temp, -1, tempType));
            Emit($"{temp} = {expr.Expression};");
            _evalStack.Push(new CSharpExpression(temp, expr.Type));
        }
    }

    private static bool IsSimpleExpression(string expr)
    {
        return expr == "null" || expr == "default" || expr == "this" || (expr.Length <= 20 && !expr.Contains('(') && !expr.Contains('['));
    }

    /// <summary>
    /// Formats a falsiness check for Brfalse: branches when value is null/false/0.
    /// </summary>
    private string FormatFalsinessCheck(CSharpExpression val)
    {
        if (val.Type != null)
        {
            var fn = val.Type;
            if (fn.Equals(_knownTypes.SystemBoolean))
                return $"!{val.Expression}";
            if (val.Type.IsValueType && !fn.Equals(_knownTypes.SystemIntPtr) && !fn.Equals(_knownTypes.SystemUIntPtr))
                return $"{val.Expression} == 0";
        }
        return $"{val.Expression} == null";
    }

    /// <summary>
    /// Formats a truthiness check for Brtrue: branches when value is non-null/true/non-zero.
    /// </summary>
    private string FormatTruthinessCheck(CSharpExpression val)
    {
        if (val.Type != null)
        {
            var fn = val.Type;
            if (fn.Equals(_knownTypes.SystemBoolean))
                return val.Expression;
            if (fn.Equals(_knownTypes.SystemInt32) || fn.Equals(_knownTypes.SystemInt64) || fn.Equals(_knownTypes.SystemByte) || fn.Equals(_knownTypes.SystemInt16))
                return $"{val.Expression} != 0";
            if (val.Type.IsValueType && !fn.Equals(_knownTypes.SystemIntPtr) && !fn.Equals(_knownTypes.SystemUIntPtr))
                return $"{val.Expression} != 0";
        }
        return $"{val.Expression} != null";
    }

    /// <summary>
    /// Generates the local variable declarations for the start of the method body.
    /// </summary>
    public void AppendLocalDeclarations(IndentedStringBuilder sb)
    {
        foreach (var local in _locals)
        {
            sb.AppendLine($"{FormatType(local.Type)} {local.Name} = default;");
        }
        foreach (var temp in _tempLocals)
        {
            sb.AppendLine($"{FormatType(temp.Type)} {temp.Name} = default;");
        }
    }

    #endregion
}

internal struct CSharpExpression(string expression, IXamlType? type)
{
    public string Expression = expression;
    public IXamlType? Type = type;
}

internal class CSharpLocal(string name, int index, IXamlType type) : IXamlILLocal
{
    public string Name { get; } = name;
    public int Index { get; } = index;
    public IXamlType Type { get; } = type;
}

internal class CSharpLabel(string name) : IXamlLabel
{
    public string Name { get; } = name;
}

#if !XAMLX_INTERNAL
public
#endif
class CSharpMethodContext(
    IXamlType? returnType,
    bool isStatic,
    bool isConstructor,
    string[] argNames,
    IXamlType[] argTypes,
    IXamlType? declaringType = null)
{
    public IXamlType? ReturnType { get; } = returnType;
    public bool IsStatic { get; } = isStatic;
    public bool IsConstructor { get; } = isConstructor;

    public string GetArgName(int index)
    {
        if (!IsStatic && index == 0)
            return "this";
        var adjustedIndex = IsStatic ? index : index - 1;
        if (adjustedIndex >= 0 && adjustedIndex < argNames.Length)
            return argNames[adjustedIndex];
        return $"__arg_{index}";
    }

    public IXamlType? GetArgType(int index)
    {
        if (!IsStatic && index == 0)
            return declaringType;
        var adjustedIndex = IsStatic ? index : index - 1;
        if (adjustedIndex >= 0 && adjustedIndex < argTypes.Length)
            return argTypes[adjustedIndex];
        return null;
    }
}