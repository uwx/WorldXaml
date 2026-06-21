using Avalonia;
using Avalonia.Controls;
using WorldXaml.UI.Base;

namespace Avalonia.Styling;

public interface IStyle : IResourceNode
{
    /// <summary>
    /// Gets a collection of child styles.
    /// </summary>
    IReadOnlyList<IStyle> Children { get; }
}

public abstract class StyleBase : AvaloniaObject
{
}

/// <summary>
/// Applies a value to a property. Used inside Style or as standalone.
/// </summary>
public class Setter : StyleBase
{
    /// <summary>
    /// The AvaloniaProperty to set.
    /// </summary>
    public AvaloniaProperty? Property { get; set; }

    /// <summary>
    /// The value to assign.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Apply this setter to the given target element.
    /// </summary>
    public void Apply(BindableObject target)
    {
        if (Property is null) return;
        target.SetBoxedValue(Property, Value);
    }
}
