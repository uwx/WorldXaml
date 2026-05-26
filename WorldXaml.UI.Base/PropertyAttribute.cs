using Avalonia.Data;

namespace WorldXaml.UI.Base;

/// <summary>
/// Marks a property of a <see cref="PropertyObject"/> as a bindable property that can be used in XAML bindings.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PropertyAttribute : Attribute
{
    public object? DefaultValue { get; set; }
    public string? DefaultValueMember { get; set; }
    public BindingMode DefaultMode { get; set; } = BindingMode.OneWay;
    public string? OnChangedMethod { get; set; }
}