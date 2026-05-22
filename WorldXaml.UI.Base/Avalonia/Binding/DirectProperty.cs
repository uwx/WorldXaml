using Avalonia.Data;

namespace WorldXaml.UI.Base;

public sealed class DirectProperty<TOwner, TValue> : Property<TValue>
    where TOwner : PropertyObject
{
    public Func<TOwner, TValue> Getter { get; }
    public Action<TOwner, TValue>? Setter { get; }

    private DirectProperty(
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

    public static DirectProperty<TOwner, TValue> Register(
        string name,
        Func<TOwner, TValue> getter,
        Action<TOwner, TValue>? setter = null,
        TValue defaultValue = default!,
        BindingMode? defaultMode = null)
    {
        var prop = new DirectProperty<TOwner, TValue>(name, getter, setter, defaultValue, defaultMode);
        PropertyRegistry.Instance.Register(typeof(TOwner), prop);
        return prop;
    }
}