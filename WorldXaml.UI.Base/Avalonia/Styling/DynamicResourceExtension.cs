using System.Reactive.Linq;
using Avalonia.LogicalTree;
using WorldXaml.UI.Base;

namespace Avalonia.Markup.Xaml.Markup;

/// <summary>
/// Markup extension that creates a binding to a resource that updates when the resource changes.
/// Usage: {DynamicResource MyBrush} or {DynamicResource Key=MyBrush}
/// </summary>
public class DynamicResourceExtension
{
    public object Key { get; set; }

    public DynamicResourceExtension() { }

    public DynamicResourceExtension(object key)
    {
        Key = key;
    }

    /// <summary>
    /// Returns an observable that tracks the resource value and re-emits on changes.
    /// Called by the XamlX compiler at load time.
    /// </summary>
    public object? ProvideValue(IServiceProvider serviceProvider)
    {
        var target = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
        var targetElement = target?.TargetObject;

        if (targetElement is BindableObject bindable)
        {
            return CreateResourceObservable(bindable);
        }

        var rootProvider = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
        if (rootProvider?.RootObject is BindableObject root)
        {
            return CreateResourceObservable(root);
        }

        throw new KeyNotFoundException($"DynamicResource '{Key}' not found.");
    }

    private IObservable<object?> CreateResourceObservable(BindableObject element)
    {
        // Resolve initial value
        var initial = element.FindResource(Key);

        // Watch for resource changes in the chain
        var dictionaries = GetResourceChain(element);

        var changes = dictionaries
            .Select(d => Observable.FromEventPattern<
                System.Collections.Specialized.NotifyCollectionChangedEventHandler,
                System.Collections.Specialized.NotifyCollectionChangedEventArgs>(
                h => d.CollectionChanged += h,
                h => d.CollectionChanged -= h))
            .Merge()
            .Select(_ =>
            {
                // Re-resolve after any change in any dictionary in the chain
                return element.FindResource(Key);
            });

        return initial is not null
            ? changes.StartWith(initial)
            : changes;
    }

    private static List<ResourceDictionary> GetResourceChain(BindableObject element)
    {
        var dicts = new List<ResourceDictionary>();
        IResourceNode? node = element;
        while (node is not null)
        {
            if (node.Resources is not null)
                dicts.Add(node.Resources);

            node = node is ILogical logical && logical.LogicalParent is IResourceNode parent
                ? parent
                : null;
        }
        return dicts;
    }
}
