namespace WorldXaml.UI.Base;

/// <summary>
/// AOT-safe property accessor for one segment of a binding path.
/// The getter/setter are compiled delegates, not PropertyInfo.
/// </summary>
public sealed class CompiledClrPropertyInfo(
    string name,
    Func<object, object?> getter,
    Action<object, object?>? setter,
    Type propertyType
)
{
    public string Name { get; } = name;
    public Type PropertyType { get; } = propertyType;
    public bool CanSet => setter is not null;

    public object? Get(object owner) => getter(owner);

    public void Set(object owner, object? value)
    {
        if (setter is null) throw new InvalidOperationException($"Property {Name} is read-only.");
        setter(owner, value);
    }
}