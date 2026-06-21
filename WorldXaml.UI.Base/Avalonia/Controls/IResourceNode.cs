using Avalonia.Controls;

namespace Avalonia.Controls;

/// <summary>
/// An element that owns a <see cref="ResourceDictionary"/>.
/// Resource lookup walks the logical tree through IResourceNode parents.
/// </summary>
public interface IResourceNode
{
    /// <summary>
    /// Local resources for this element. Set by XAML or code.
    /// </summary>
    ResourceDictionary? Resources { get; set; }

    /// <summary>
    /// True when the element has resources (either locally or inherited).
    /// </summary>
    bool HasResources => Resources is { Count: > 0 };
}
