using Avalonia;
using Avalonia.Data;

namespace WorldXaml.UI.Base;

public sealed class DirectProperty<TOwner, TValue> : StyledProperty<TValue>
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
}