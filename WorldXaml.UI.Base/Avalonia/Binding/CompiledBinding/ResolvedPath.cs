using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace WorldXaml.UI.Base;

/// <summary>
/// A fully resolved, AOT-safe binding path produced at XAML compile time.
/// </summary>
public sealed class ResolvedPath(CompiledClrPropertyInfo[] steps)
{
    public Type LeafType => steps[^1].PropertyType;

    /// <summary>Read the leaf value, walking the chain.</summary>
    public (bool Found, object? Value) TryRead(object? root)
    {
        object? current = root;
        foreach (var step in steps)
        {
            if (current is null) return (false, null);
            current = step.Get(current);
        }
        return (true, current);
    }

    /// <summary>Write to the leaf, walking to the parent first.</summary>
    public bool TryWrite(object? root, object? value)
    {
        object? current = root;
        for (int i = 0; i < steps.Length - 1; i++)
        {
            if (current is null) return false;
            current = steps[i].Get(current);
        }
        if (current is null) return false;
        var leaf = steps[^1];
        if (!leaf.CanSet) return false;
        leaf.Set(current, value);
        return true;
    }

    /// <summary>
    /// Returns an observable that emits whenever any INPC in the path fires.
    /// Fully AOT-safe — no reflection after construction.
    /// </summary>
    public IObservable<object?> Observe(object? root)
    {
        if (root is null) return Observable.Return<object?>(null);

        return Observable.Create<object?>(observer =>
        {
            var bag = new CompositeDisposable();
            Subscribe(root, 0, bag, observer);
            return bag;
        });
    }

    private void Subscribe(
        object? current, int stepIndex,
        CompositeDisposable bag,
        IObserver<object?> observer)
    {
        if (current is null) { observer.OnNext(null); return; }

        if (stepIndex == steps.Length - 1)
        {
            // Leaf — emit and watch.
            observer.OnNext(steps[stepIndex].Get(current));

            if (current is INotifyPropertyChanged inpc)
            {
                var name = steps[stepIndex].Name;
                PropertyChangedEventHandler handler = (_, e) =>
                {
                    if (e.PropertyName is null || e.PropertyName == name)
                        observer.OnNext(steps[stepIndex].Get(current));
                };
                inpc.PropertyChanged += handler;
                bag.Add(Disposable.Create(() => inpc.PropertyChanged -= handler));
            }
        }
        else
        {
            // Intermediate — recurse and re-subscribe when it changes.
            var next = steps[stepIndex].Get(current);
            var childBag = new CompositeDisposable();
            bag.Add(childBag);
            Subscribe(next, stepIndex + 1, childBag, observer);

            if (current is INotifyPropertyChanged inpc)
            {
                var name = steps[stepIndex].Name;
                PropertyChangedEventHandler handler = (_, e) =>
                {
                    if (e.PropertyName is null || e.PropertyName == name)
                    {
                        childBag.Clear();
                        Subscribe(steps[stepIndex].Get(current), stepIndex + 1, childBag, observer);
                    }
                };
                inpc.PropertyChanged += handler;
                bag.Add(Disposable.Create(() => inpc.PropertyChanged -= handler));
            }
        }
    }
}
