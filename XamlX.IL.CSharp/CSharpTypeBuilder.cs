using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorldXaml.Generator.Common;
using XamlX.IL;
using XamlX.IL.CSharp;
using XamlX.TypeSystem;

namespace XamlX.CSharp;

/// <summary>
/// IXamlTypeBuilder that generates C# source code instead of IL/Cecil types.
/// </summary>
#if !XAMLX_INTERNAL
public
#endif
class CSharpTypeBuilder : IXamlTypeBuilder<IXamlILEmitter>
{
    private readonly IXamlTypeSystem _typeSystem;
    private readonly string _namespace;
    private readonly string _name;
    private readonly IXamlType? _baseType;
    private readonly XamlVisibility _visibility;
    private readonly List<CSharpFieldInfo> _fields = [];
    private readonly List<CSharpMethodBuilder> _methods = [];
    private readonly List<CSharpConstructorBuilder> _constructors = [];
    private readonly List<CSharpPropertyInfo> _properties = [];
    private readonly List<IXamlType> _interfaces = [];
    private readonly List<CSharpTypeBuilder> _nestedTypes = [];
    private readonly List<KeyValuePair<string, XamlGenericParameterConstraint>> _genericParams = [];
    private readonly List<CSharpGenericParameterType> _genericParamTypes = [];
    private readonly CSharpTypeBuilder? _parent;
    private readonly CSharpEmitterKnownTypes _knownTypes;

    internal CSharpTypeBuilder(IXamlTypeSystem typeSystem, string namespaceName, string name,
        IXamlType? baseType, XamlVisibility visibility, CSharpTypeBuilder? parent = null)
    {
        _typeSystem = typeSystem;
        _namespace = namespaceName;
        _name = name;
        _baseType = baseType;
        _visibility = visibility;
        _parent = parent;
        _knownTypes = new CSharpEmitterKnownTypes(typeSystem);
    }

    public string FullName
    {
        get
        {
            if (_parent != null)
                return $"{_parent.FullName}.{_name}";
            return string.IsNullOrEmpty(_namespace) ? _name : $"{_namespace}.{_name}";
        }
    }

    #region IXamlType Implementation (read-only reflection for the type being built)

    public object Id { get; } = Guid.NewGuid();
    public string Name => _name;
    public string? Namespace => _namespace;
    string IXamlType.FullName => FullName;
    public bool IsPublic => _visibility == XamlVisibility.Public;
    public bool IsNestedPrivate => _visibility == XamlVisibility.Private && _parent != null;
    public IXamlAssembly? Assembly => null;
    public IReadOnlyList<IXamlProperty> Properties => _properties;
    public IReadOnlyList<IXamlEventInfo> Events => [];
    public IReadOnlyList<IXamlField> Fields => _fields;
    public IReadOnlyList<IXamlMethod> Methods => _methods;
    public IReadOnlyList<IXamlConstructor> Constructors => _constructors;
    public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => [];
    public IReadOnlyList<IXamlType> GenericArguments => [];
    public IXamlType? GenericTypeDefinition => null;
    public bool IsArray => false;
    public IXamlType? ArrayElementType => null;
    public IXamlType? BaseType => _baseType;
    public IXamlType? DeclaringType => _parent;
    public bool IsValueType => false;
    public bool IsEnum => false;
    public IReadOnlyList<IXamlType> Interfaces => _interfaces;
    public bool IsInterface => false;
    public IReadOnlyList<IXamlType> GenericParameters => _genericParamTypes;
    public bool IsFunctionPointer => false;

    public bool IsAssignableFrom(IXamlType type) => type.Equals(this)
        || type.GetAllInterfaces().Any(i => i.Equals(this))
        || (type is ConstructedCSharpType ct && ct.GenericTypeDefinition == this);
    public IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments) => new ConstructedCSharpType(this, typeArguments);
    public IXamlType MakeArrayType(int dimensions) => throw new NotSupportedException();
    public IXamlType GetEnumUnderlyingType() => throw new NotSupportedException();
    public bool Equals(IXamlType? other) => ReferenceEquals(this, other);

    #endregion

    #region IXamlTypeBuilder Implementation

    public IXamlField DefineField(IXamlType type, string name, XamlVisibility visibility, bool isStatic)
    {
        var field = new CSharpFieldInfo(this, type, name, visibility, isStatic);
        _fields.Add(field);
        return field;
    }

    public void AddInterfaceImplementation(IXamlType type)
    {
        _interfaces.Add(type);
    }

    public IXamlMethodBuilder<IXamlILEmitter> DefineMethod(IXamlType returnType, IEnumerable<IXamlType> args,
        string name, XamlVisibility visibility, bool isStatic, bool isInterfaceImpl,
        IXamlMethod? overrideMethod = null)
    {
        var argsList = args.ToList();
        var argNames = new string[argsList.Count];
        for (var i = 0; i < argsList.Count; i++)
            argNames[i] = $"arg{i}";

        var methodCtx = new CSharpMethodContext(returnType, isStatic, false, argNames, argsList.ToArray(), this);
        var emitter = new CSharpEmitter(_knownTypes, _typeSystem, methodCtx);
        var method = new CSharpMethodBuilder(this, returnType, argsList, name, visibility, isStatic, isInterfaceImpl, argNames, emitter, overrideMethod);
        _methods.Add(method);
        return method;
    }

    public IXamlProperty DefineProperty(IXamlType propertyType, string name, IXamlMethod? setter, IXamlMethod? getter)
    {
        var prop = new CSharpPropertyInfo(this, propertyType, name, setter, getter);
        _properties.Add(prop);
        return prop;
    }

    public IXamlConstructorBuilder<IXamlILEmitter> DefineConstructor(bool isStatic, params IXamlType[] args)
    {
        var argNames = new string[args.Length];
        for (var i = 0; i < args.Length; i++)
            argNames[i] = $"arg{i}";

        var methodCtx = new CSharpMethodContext(null, isStatic, true, argNames, args, this);
        var emitter = new CSharpEmitter(_knownTypes, _typeSystem, methodCtx);
        var ctor = new CSharpConstructorBuilder(this, isStatic, args, argNames, emitter);
        _constructors.Add(ctor);
        return ctor;
    }

    public IXamlType CreateType() => this;

    public IXamlTypeBuilder<IXamlILEmitter> DefineSubType(IXamlType baseType, string name, XamlVisibility visibility)
    {
        var nested = new CSharpTypeBuilder(_typeSystem, "", name, baseType, visibility, this);
        _nestedTypes.Add(nested);
        return nested;
    }

    public IXamlTypeBuilder<IXamlILEmitter> DefineDelegateSubType(string name, XamlVisibility visibility,
        IXamlType returnType, IEnumerable<IXamlType> parameterTypes)
    {
        // For C# source, we output a delegate declaration
        var nested = new CSharpTypeBuilder(_typeSystem, "", name, null, visibility, this);
        _nestedTypes.Add(nested);
        return nested;
    }

    public void DefineGenericParameters(IReadOnlyList<KeyValuePair<string, XamlGenericParameterConstraint>> names)
    {
        _genericParams.AddRange(names);
        foreach (var kvp in names)
            _genericParamTypes.Add(new CSharpGenericParameterType(kvp.Key, this));
    }

    #endregion

    #region C# Source Generation

    /// <summary>
    /// Generates just the member declarations (fields, constructors, properties, methods, nested types)
    /// without the enclosing class/namespace wrapper. Used for embedding into existing partial classes.
    /// </summary>
    public void GenerateMembers(IndentedStringBuilder sb)
    {
        GenerateMembersBody(sb);
    }

    private void GenerateMembersBody(IndentedStringBuilder sb)
    {
        // Fields
        foreach (var field in _fields)
        {
            var fieldVis = field.Visibility switch
            {
                XamlVisibility.Public => "public",
                XamlVisibility.Assembly => "internal",
                XamlVisibility.Private => "private",
                _ => "private"
            };
            var staticMod = field.IsStatic ? "static " : "";
            sb.AppendLine($"{fieldVis} {staticMod}{CSharpFormatting.FormatType(_knownTypes, field.FieldType)} {field.Name};");
        }
        if (_fields.Count > 0) sb.AppendLine();

        // Constructors
        foreach (var ctor in _constructors)
        {
            GenerateConstructor(sb, ctor);
            sb.AppendLine();
        }

        // Properties
        foreach (var prop in _properties)
        {
            GenerateProperty(sb, prop);
            sb.AppendLine();
        }

        // Methods - skip methods that are already emitted as property getters/setters
        var propMethods = new HashSet<object>();
        foreach (var prop in _properties)
        {
            if (prop.Getter is CSharpMethodBuilder gm2) propMethods.Add(gm2);
            if (prop.Setter is CSharpMethodBuilder sm2) propMethods.Add(sm2);
        }
        foreach (var method in _methods)
        {
            if (propMethods.Contains(method))
                continue;
            GenerateMethod(sb, method);
            sb.AppendLine();
        }

        // Nested types
        foreach (var nested in _nestedTypes)
        {
            nested.GenerateTypeBody(sb);
            sb.AppendLine();
        }
    }

    public string GenerateSource()
    {
        var sb = new IndentedStringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable disable");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(_namespace))
        {
            sb.AppendLine($"namespace {_namespace}");
            sb.AppendLine("{");
            using (sb.Indent())
            {
                GenerateTypeBody(sb);
            }

            sb.AppendLine("}");
        }
        else
        {
            GenerateTypeBody(sb);
        }

        return sb.ToString();
    }

    private void GenerateTypeBody(IndentedStringBuilder sb)
    {
        var vis = _visibility switch
        {
            XamlVisibility.Public => "public",
            XamlVisibility.Assembly => "internal",
            XamlVisibility.Private => "private",
            _ => "internal"
        };

        var genericSuffix = "";
        if (_genericParams.Count > 0)
            genericSuffix = "<" + string.Join(", ", _genericParams.Select(p => p.Key)) + ">";

        var baseClause = "";
        var bases = new List<string>();
        if (_baseType != null)
            bases.Add(CSharpFormatting.FormatType(_knownTypes, _baseType));
        bases.AddRange(_interfaces.Select(t => CSharpFormatting.FormatType(_knownTypes, t)));
        if (bases.Count > 0)
            baseClause = " : " + string.Join(", ", bases);

        sb.AppendLine($"{vis} partial class {_name}{genericSuffix}{baseClause}");
        sb.AppendLine($"{{");

        // Fields
        using (sb.Indent())
        {
            foreach (var field in _fields)
            {
                var fieldVis = field.Visibility switch
                {
                    XamlVisibility.Public => "public",
                    XamlVisibility.Assembly => "internal",
                    XamlVisibility.Private => "private",
                    _ => "private"
                };
                var staticMod = field.IsStatic ? "static " : "";
                sb.AppendLine($"{fieldVis} {staticMod}{CSharpFormatting.FormatType(_knownTypes, field.FieldType)} {field.Name};");
            }

            if (_fields.Count > 0) sb.AppendLine();

            // Constructors
            foreach (var ctor in _constructors)
            {
                GenerateConstructor(sb, ctor);
                sb.AppendLine();
            }

            // Properties
            foreach (var prop in _properties)
            {
                GenerateProperty(sb, prop);
                sb.AppendLine();
            }

            // Methods - skip methods that are already emitted as property getters/setters
            var propertyMethods = new HashSet<object>();
            foreach (var prop in _properties)
            {
                if (prop.Getter is CSharpMethodBuilder gm) propertyMethods.Add(gm);
                if (prop.Setter is CSharpMethodBuilder sm) propertyMethods.Add(sm);
            }
            foreach (var method in _methods)
            {
                if (propertyMethods.Contains(method))
                    continue;
                GenerateMethod(sb, method);
                sb.AppendLine();
            }

            // Nested types
            foreach (var nested in _nestedTypes)
            {
                nested.GenerateTypeBody(sb);
                sb.AppendLine();
            }

            // Generic constraints
            foreach (var gp in _genericParams)
            {
                if (gp.Value.IsClass)
                    sb.AppendLine($"// where {gp.Key} : class");
            }
        }

        sb.AppendLine($"}}");
    }

    private void GenerateConstructor(IndentedStringBuilder sb, CSharpConstructorBuilder ctor)
    {
        var staticMod = ctor.IsStatic ? "static " : "public ";
        var args = string.Join(", ", ctor.Parameters.Select((p, i) => $"{CSharpFormatting.FormatType(_knownTypes, p)} {ctor.ArgNames[i]}"));

        sb.AppendLine($"{staticMod}{_name}({args})");
        sb.AppendLine($"{{");

        using (sb.Indent())
        {
            ctor.Emitter.AppendLocalDeclarations(sb);
            foreach (var stmt in ctor.Emitter.Statements)
                sb.AppendLine(stmt);
        }

        sb.AppendLine($"}}");
    }

    private void GenerateMethod(IndentedStringBuilder sb, CSharpMethodBuilder method)
    {
        var isExplicitImpl = method.Name.Contains('.');
        var vis = isExplicitImpl ? "" : method.MethodVisibility switch
        {
            XamlVisibility.Public => "public ",
            XamlVisibility.Assembly => "internal ",
            XamlVisibility.Private => "private ",
            _ => "private "
        };
        var staticMod = method.IsStatic ? "static " : "";
        var retType = CSharpFormatting.FormatType(_knownTypes, method.ReturnType);
        var args = string.Join(", ", method.Parameters.Select((p, i) => $"{CSharpFormatting.FormatType(_knownTypes, p)} {method.ArgNames[i]}"));

        // For explicit interface implementations, format the interface part properly
        // to handle generic interfaces (replace backtick metadata names with C# generic syntax)
        var methodName = FormatMethodName(method);

        sb.AppendLine($"{vis}{staticMod}{retType} {methodName}({args})");
        sb.AppendLine($"{{");

        using (sb.Indent())
        {
            method.Emitter.AppendLocalDeclarations(sb);
            foreach (var stmt in method.Emitter.Statements)
                sb.AppendLine($"{stmt}");
        }

        sb.AppendLine($"}}");
    }

    /// <summary>
    /// Formats a method name, converting explicit interface implementation names
    /// from metadata format (using backtick) to C# generic syntax.
    /// </summary>
    private string FormatMethodName(CSharpMethodBuilder method)
    {
        if (!method.Name.Contains('.'))
            return method.Name;

        // Explicit interface implementation: use the override method's declaring type to get proper formatting
        if (method.OverrideMethod?.DeclaringType is { } interfaceType)
        {
            var lastDot = method.Name.LastIndexOf('.');
            var simpleName = method.Name[(lastDot + 1)..];
            var formattedInterface = CSharpFormatting.FormatType(_knownTypes, interfaceType);
            // Remove "global::" prefix since explicit implementations don't use it
            if (formattedInterface.StartsWith("global::"))
                formattedInterface = formattedInterface["global::".Length..];
            return $"{formattedInterface}.{simpleName}";
        }

        // Fallback: strip backtick arity from the name (loses generic args but at least compiles)
        return StripBacktickArity(method.Name);
    }

    /// <summary>
    /// Formats a property name, handling explicit interface implementation names.
    /// </summary>
    private string FormatPropertyName(CSharpPropertyInfo prop)
    {
        if (!prop.Name.Contains('.'))
            return prop.Name;

        // Try to get the interface type from the getter or setter's override method
        var overrideMethod = (prop.Getter as CSharpMethodBuilder)?.OverrideMethod
                          ?? (prop.Setter as CSharpMethodBuilder)?.OverrideMethod;
        if (overrideMethod?.DeclaringType is { } interfaceType)
        {
            var lastDot = prop.Name.LastIndexOf('.');
            var simpleName = prop.Name[(lastDot + 1)..];
            var formattedInterface = CSharpFormatting.FormatType(_knownTypes, interfaceType);
            if (formattedInterface.StartsWith("global::"))
                formattedInterface = formattedInterface["global::".Length..];
            return $"{formattedInterface}.{simpleName}";
        }

        return StripBacktickArity(prop.Name);
    }

    private static string StripBacktickArity(string name)
    {
        // Remove backtick+digits patterns like `1, `2 etc.
        var result = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            if (name[i] == '`')
            {
                // Skip backtick and following digits
                i++;
                while (i < name.Length && char.IsDigit(name[i]))
                    i++;
                i--; // Will be incremented by for loop
            }
            else
            {
                result.Append(name[i]);
            }
        }
        return result.ToString();
    }

    private void GenerateProperty(IndentedStringBuilder sb, CSharpPropertyInfo prop)
    {
        var typeName = CSharpFormatting.FormatType(_knownTypes, prop.PropertyType);
        var isExplicitImpl = prop.Name.Contains('.');
        var vis = isExplicitImpl ? "" : "public ";
        var propName = FormatPropertyName(prop);

        // Check if getter/setter have method bodies (CSharpMethodBuilder with statements)
        var getterMethod = prop.Getter as CSharpMethodBuilder;
        var setterMethod = prop.Setter as CSharpMethodBuilder;
        var hasGetterBody = getterMethod?.Emitter.Statements.Count > 0;
        var hasSetterBody = setterMethod?.Emitter.Statements.Count > 0;

        if (hasGetterBody || hasSetterBody)
        {
            sb.AppendLine($"{vis}{typeName} {propName}");
            sb.AppendLine($"{{");
            using (sb.Indent())
            {
                if (hasGetterBody)
                {
                    sb.AppendLine($"get");
                    sb.AppendLine($"{{");
                    getterMethod!.Emitter.AppendLocalDeclarations(sb);
                    using (sb.Indent())
                    {
                        foreach (var stmt in getterMethod.Emitter.Statements)
                            sb.AppendLine($"{stmt}");
                    }
                    sb.AppendLine($"}}");
                }
                else if (prop.Getter != null)
                {
                    sb.AppendLine($"get;");
                }

                if (hasSetterBody)
                {
                    sb.AppendLine($"set");
                    sb.AppendLine($"{{");
                    setterMethod!.Emitter.AppendLocalDeclarations(sb);
                    // In property setters, replace arg0 references with 'value'
                    using (sb.Indent())
                    {
                        foreach (var stmt in setterMethod.Emitter.Statements)
                            sb.AppendLine($"{ReplaceSetterArg(stmt)}");
                    }
                    sb.AppendLine($"}}");
                }
                else if (prop.Setter != null)
                {
                    sb.AppendLine($"set;");
                }
            }

            sb.AppendLine($"}}");
        }
        else
        {
            sb.Append($"{vis}{typeName} {propName} {{ ");
            if (prop.Getter != null) sb.Append("get; ");
            if (prop.Setter != null) sb.Append("set; ");
            sb.AppendLine("}");
        }
    }

    /// <summary>
    /// In a property setter body emitted from IL, the first parameter (arg0 for an instance method's
    /// second arg) maps to the implicit 'value' parameter in C# property setters.
    /// </summary>
    private static string ReplaceSetterArg(string statement)
    {
        // TODO figure out a more robust solution for this
        
        // The setter's CSharpMethodContext has argNames = ["arg0"], and for a non-static method,
        // GetArgName(1) returns argNames[0] = "arg0". In C# property setters, this is 'value'.
        return statement.Replace("arg0", "value");
    }

    #endregion
}

#region Supporting Types

internal class CSharpFieldInfo(
    CSharpTypeBuilder declaringType,
    IXamlType fieldType,
    string name,
    XamlVisibility visibility,
    bool isStatic)
    : IXamlField
{
    public string Name { get; } = name;
    public IXamlType DeclaringType => declaringType;
    public IXamlType FieldType { get; } = fieldType;
    public bool IsPublic => Visibility == XamlVisibility.Public;
    public bool IsStatic { get; } = isStatic;
    public bool IsLiteral => false;
    public XamlVisibility Visibility { get; } = visibility;
    public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => [];

    public object GetLiteralValue() => throw new NotSupportedException("Field has no literal value");
    public bool Equals(IXamlField? other) => ReferenceEquals(this, other);
}

internal class CSharpMethodBuilder(
    CSharpTypeBuilder declaringType,
    IXamlType returnType,
    List<IXamlType> parameters,
    string name,
    XamlVisibility visibility,
    bool isStatic,
    bool isInterfaceImpl,
    string[] argNames,
    CSharpEmitter emitter,
    IXamlMethod? overrideMethod = null)
    : IXamlMethodBuilder<IXamlILEmitter>
{
    public string Name { get; } = name;
    public IXamlType DeclaringType => declaringType;
    public IXamlType ReturnType { get; } = returnType;
    public IReadOnlyList<IXamlType> Parameters { get; } = parameters;
    public XamlVisibility MethodVisibility { get; } = visibility;
    public bool IsPublic => MethodVisibility == XamlVisibility.Public;
    public bool IsPrivate => MethodVisibility == XamlVisibility.Private;
    public bool IsFamily => false;
    public bool IsStatic { get; } = isStatic;
    public bool IsInterfaceImpl { get; } = isInterfaceImpl;
    public IXamlMethod? OverrideMethod { get; } = overrideMethod;
    public bool ContainsGenericParameters => false;
    public bool IsGenericMethod => false;
    public bool IsGenericMethodDefinition => false;
    public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => [];
    public IReadOnlyList<IXamlType> GenericParameters => [];
    public IReadOnlyList<IXamlType> GenericArguments => [];
    public string[] ArgNames { get; } = argNames;
    public CSharpEmitter Emitter { get; } = emitter;

    IXamlILEmitter IXamlMethodBuilder<IXamlILEmitter>.Generator => Emitter;

    public IXamlMethod MakeGenericMethod(IReadOnlyList<IXamlType> typeArguments) => throw new NotSupportedException();
    public IXamlParameterInfo GetParameterInfo(int index)
        => new AnonymousParameterInfo(Parameters[index], ArgNames[index]);
    public bool Equals(IXamlMethod? other) => ReferenceEquals(this, other);
}

internal class CSharpConstructorBuilder(
    CSharpTypeBuilder declaringType,
    bool isStatic,
    IXamlType[] parameters,
    string[] argNames,
    CSharpEmitter emitter)
    : IXamlConstructorBuilder<IXamlILEmitter>
{
    public string Name => IsStatic ? ".cctor" : ".ctor";
    public IXamlType DeclaringType => declaringType;
    public bool IsPublic => !IsStatic;
    public bool IsStatic { get; } = isStatic;
    public IReadOnlyList<IXamlType> Parameters { get; } = parameters;
    public string[] ArgNames { get; } = argNames;
    public CSharpEmitter Emitter { get; } = emitter;

    IXamlILEmitter IXamlConstructorBuilder<IXamlILEmitter>.Generator => Emitter;

    public IXamlParameterInfo GetParameterInfo(int index)
        => new AnonymousParameterInfo(Parameters[index], ArgNames[index]);
    public bool Equals(IXamlConstructor? other) => ReferenceEquals(this, other);
}

internal class CSharpPropertyInfo(
    CSharpTypeBuilder declaringType,
    IXamlType propertyType,
    string name,
    IXamlMethod? setter,
    IXamlMethod? getter)
    : IXamlProperty
{
    public string Name { get; } = name;
    public IXamlType DeclaringType => declaringType;
    public IXamlType PropertyType { get; } = propertyType;
    public IXamlMethod? Setter { get; } = setter;
    public IXamlMethod? Getter { get; } = getter;
    public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => [];
    public IReadOnlyList<IXamlType> IndexerParameters => [];
    public bool Equals(IXamlProperty? other) => ReferenceEquals(this, other);
}

internal class CSharpGenericParameterType(string name, CSharpTypeBuilder declaringType) : IXamlType
{
    public object Id { get; } = Guid.NewGuid();
    public string Name { get; } = name;
    public string? Namespace => null;
    public string FullName => Name;
    public bool IsPublic => true;
    public bool IsNestedPrivate => false;
    public IXamlAssembly? Assembly => null;
    public IReadOnlyList<IXamlProperty> Properties => [];
    public IReadOnlyList<IXamlEventInfo> Events => [];
    public IReadOnlyList<IXamlField> Fields => [];
    public IReadOnlyList<IXamlMethod> Methods => [];
    public IReadOnlyList<IXamlConstructor> Constructors => [];
    public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => [];
    public IReadOnlyList<IXamlType> GenericArguments => [];
    public IXamlType? GenericTypeDefinition => null;
    public bool IsArray => false;
    public IXamlType? ArrayElementType => null;
    public IXamlType? BaseType => null;
    public IXamlType? DeclaringType => declaringType;
    public bool IsValueType => false;
    public bool IsEnum => false;
    public IReadOnlyList<IXamlType> Interfaces => [];
    public bool IsInterface => false;
    public IReadOnlyList<IXamlType> GenericParameters => [];
    public bool IsFunctionPointer => false;

    public bool IsAssignableFrom(IXamlType type) => Equals(type);
    public IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments) => throw new NotSupportedException();
    public IXamlType MakeArrayType(int dimensions) => throw new NotSupportedException();
    public IXamlType GetEnumUnderlyingType() => throw new NotSupportedException();
    public bool Equals(IXamlType? other) => ReferenceEquals(this, other);
}

/// <summary>
/// Represents a constructed generic type (e.g. Context&lt;SomeNode&gt;) created from a CSharpTypeBuilder
/// generic definition. Forwards Fields/Methods/Constructors from the definition.
/// </summary>
internal class ConstructedCSharpType : IXamlType
{
    private readonly CSharpTypeBuilder _definition;
    private readonly IReadOnlyList<IXamlType> _typeArguments;

    public IReadOnlyList<IXamlConstructor> Constructors { get; }

    public ConstructedCSharpType(CSharpTypeBuilder definition, IReadOnlyList<IXamlType> typeArguments)
    {
        _definition = definition;
        _typeArguments = typeArguments;

        // Wrap constructors so they report this constructed type as DeclaringType
        Constructors = definition.Constructors
            .Select(c => new ConstructedCtorWrapper(this, c))
            .ToArray();
    }

    public object Id { get; } = Guid.NewGuid();
    public string Name => _definition.Name;
    public string? Namespace => _definition.Namespace;

    public string FullName
    {
        get
        {
            var baseName = _definition.FullName;
            return $"{baseName}<{string.Join(", ", _typeArguments.Select(t => t.FullName))}>";
        }
    }

    public bool IsPublic => _definition.IsPublic;
    public bool IsNestedPrivate => _definition.IsNestedPrivate;
    public IXamlAssembly? Assembly => _definition.Assembly;
    public IReadOnlyList<IXamlProperty> Properties => _definition.Properties;
    public IReadOnlyList<IXamlEventInfo> Events => _definition.Events;
    public IReadOnlyList<IXamlField> Fields => _definition.Fields;
    public IReadOnlyList<IXamlMethod> Methods => _definition.Methods;
    public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => _definition.CustomAttributes;
    public IReadOnlyList<IXamlType> GenericArguments => _typeArguments;
    public IXamlType? GenericTypeDefinition => _definition;
    public bool IsArray => false;
    public IXamlType? ArrayElementType => null;
    public IXamlType? BaseType => _definition.BaseType;
    public IXamlType? DeclaringType => _definition.DeclaringType;
    public bool IsValueType => false;
    public bool IsEnum => false;
    public IReadOnlyList<IXamlType> Interfaces => _definition.Interfaces;
    public bool IsInterface => false;
    public IReadOnlyList<IXamlType> GenericParameters => [];
    public bool IsFunctionPointer => false;

    public bool IsAssignableFrom(IXamlType type) => type.Equals(this) || _definition.IsAssignableFrom(type);
    public IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments) => throw new NotSupportedException();
    public IXamlType MakeArrayType(int dimensions) => throw new NotSupportedException();
    public IXamlType GetEnumUnderlyingType() => throw new NotSupportedException();
    public bool Equals(IXamlType? other)
    {
        if (other is ConstructedCSharpType c)
            return ReferenceEquals(_definition, c._definition) &&
                   _typeArguments.Count == c._typeArguments.Count &&
                   _typeArguments.Zip(c._typeArguments, (a, b) => a.Equals(b)).All(x => x);
        return false;
    }
}

internal class ConstructedCtorWrapper(IXamlType declaringType, IXamlConstructor inner) : IXamlConstructor
{
    private readonly IXamlConstructor _inner = inner;

    public IXamlType DeclaringType { get; } = declaringType;
    public string Name => _inner.Name;
    public bool IsPublic => _inner.IsPublic;
    public bool IsStatic => _inner.IsStatic;
    public IReadOnlyList<IXamlType> Parameters => _inner.Parameters;
    public IXamlParameterInfo GetParameterInfo(int index) => _inner.GetParameterInfo(index);
    public bool Equals(IXamlConstructor? other) => _inner.Equals(other is ConstructedCtorWrapper w ? w._inner : other);
}

#endregion