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
        TValue defaultValue)
        : base(name, typeof(TOwner), defaultValue)
    {
        Getter = getter;
        Setter = setter;
    }

    public static DirectProperty<TOwner, TValue> Register(
        string name,
        Func<TOwner, TValue> getter,
        Action<TOwner, TValue>? setter = null,
        TValue defaultValue = default!)
    {
        var prop = new DirectProperty<TOwner, TValue>(name, getter, setter, defaultValue);
        PropertyRegistry.Instance.Register(typeof(TOwner), prop);
        return prop;
    }
}