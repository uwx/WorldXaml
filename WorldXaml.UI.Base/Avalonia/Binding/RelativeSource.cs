using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Avalonia.Data;

/// <summary>
/// Defines the mode of a <see cref="RelativeSource"/> object.
/// </summary>
public enum RelativeSourceMode
{
    /// <summary>
    /// The binding will be to the control's data context.
    /// </summary>
    DataContext,

    /// <summary>
    /// The binding will be to the control's templated parent.
    /// </summary>
    TemplatedParent,

    // /// <summary>
    // /// The binding will be to the control itself.
    // /// </summary>
    // Self,
    //
    // /// <summary>
    // /// The binding will be to an ancestor of the control in the visual tree.
    // /// </summary>
    // FindAncestor,
}

/// <summary>
/// Specifies the source of a binding relative to the binding target.
/// Usage: <c>{Binding Foo, RelativeSource={RelativeSource TemplatedParent}}</c>
/// </summary>
public class RelativeSource
{
    public RelativeSourceMode Mode { get; set; }

    public RelativeSource()
    {
    }

    public RelativeSource(RelativeSourceMode mode)
    {
        Mode = mode;
    }

    /// <summary>
    /// Markup extension support — returns itself.
    /// </summary>
    [UsedImplicitly]
    public RelativeSource ProvideValue(IServiceProvider serviceProvider) => this;
}

/// <summary>
/// Equivalent to {RelativeSource TemplatedParent} but doesn't cause a red squiggle in Rider XAML preview.
/// </summary>
public class RelativeSourceTemplatedParent
{
    [UsedImplicitly]
    public RelativeSource ProvideValue(IServiceProvider serviceProvider) => new RelativeSource(RelativeSourceMode.TemplatedParent);
}