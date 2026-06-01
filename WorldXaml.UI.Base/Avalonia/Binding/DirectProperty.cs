using Avalonia;
using Avalonia.Data;

namespace WorldXaml.UI.Base;

public abstract class BaseDirectProperty<TValue>(
    string name,
    Type ownerType,
    TValue defaultValue,
    BindingMode defaultMode = BindingMode.OneWay,
    Action<PropertyObject, TValue>? onChanged = null)
    : StyledProperty<TValue>(name, ownerType, defaultValue, defaultMode, onChanged)
{
    /// <summary>Read the backing value via the DirectProperty getter.</summary>
    internal abstract TValue? GetDirectValue(PropertyObject target);
    
    /// <summary>Write the backing value via the DirectProperty setter.</summary>
    internal abstract void SetDirectValue(PropertyObject target, TValue? value);
}

public sealed class DirectProperty<TOwner, TValue> : BaseDirectProperty<TValue>
    where TOwner : PropertyObject
{
    public Func<TOwner, TValue> Getter { get; }
    public Action<TOwner, TValue>? Setter { get; }

    internal DirectProperty(
        string name,
        Func<TOwner, TValue> getter,
        Action<TOwner, TValue>? setter,
        TValue defaultValue,
        BindingMode? defaultMode = null)
        : base(name, typeof(TOwner), defaultValue, defaultMode: defaultMode ?? (setter != null ? BindingMode.OneWay : BindingMode.OneWayToSource))
    {
        Getter = getter;
        Setter = setter;
    }

    internal override TValue? GetDirectValue(PropertyObject target) => Getter((TOwner)target);

    internal override void SetDirectValue(PropertyObject target, TValue? value) =>
        (Setter ?? throw new InvalidOperationException($"Property '{Name}' is read-only."))((TOwner)target, (TValue)value!);
}