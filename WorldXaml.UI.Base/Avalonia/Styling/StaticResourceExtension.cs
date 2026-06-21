using WorldXaml.UI.Base;

namespace Avalonia.Markup.Xaml.Markup;

/// <summary>
/// Markup extension that resolves a resource key once at load time.
/// Usage: {StaticResource MyBrush} or {StaticResource Key=MyBrush}
/// </summary>
public class StaticResourceExtension
{
    public object Key { get; set; }

    public StaticResourceExtension() { }

    public StaticResourceExtension(object key)
    {
        Key = key;
    }

    /// <summary>
    /// Resolves the resource by walking up the logical tree from the target element.
    /// Called by the XamlX compiler at load time.
    /// </summary>
    public object? ProvideValue(IServiceProvider serviceProvider)
    {
        // Get the target element (the object being populated)
        var target = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
        var targetElement = target?.TargetObject;

        if (targetElement is BindableObject bindable)
        {
            var value = bindable.FindResource(Key);
            if (value is not null)
                return value;
        }

        // Fallback: if target not available, search the root
        var rootProvider = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
        if (rootProvider?.RootObject is BindableObject root)
        {
            var value = root.FindResource(Key);
            if (value is not null)
                return value;
        }

        throw new KeyNotFoundException($"StaticResource '{Key}' not found.");
    }
}
