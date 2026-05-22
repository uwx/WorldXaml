using Avalonia.Data;

namespace WorldXaml.UI.Base;

/// <summary>
/// Marks a property of a <see cref="PropertyObject"/> as a bindable property that can be used in XAML bindings. Or,
/// attach it to a static property of type <see cref="DirectProperty{TOwner, TValue}"/> or
/// <see cref="Property{TValue}"/> whose name ends with 'FooProperty' to generate a 'Foo' property. 
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PropertyAttribute(object? defaultValue = null, BindingMode defaultMode = BindingMode.OneWay) : Attribute
{
    public object? DefaultValue { get; }
    public BindingMode DefaultMode { get; }
}