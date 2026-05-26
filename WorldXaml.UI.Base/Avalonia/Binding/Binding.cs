using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using WorldXaml.UI.Base;
using PropertyChangedEventArgs = WorldXaml.UI.Base.PropertyChangedEventArgs;

// ReSharper disable once CheckNamespace
namespace Avalonia.Data;

/// <summary>
/// {Binding Path=Foo}  — binds to Foo on the target object's DataContext.
/// </summary>
/// <remarks>
/// The DataContext may change after setup (e.g. when a parent is assigned),
/// so the binding re-evaluates whenever DataContext changes.
/// </remarks>
public sealed class Binding : IXamlBinding
{
    public string?      Path { get; set; }
    public BindingMode  Mode { get; set; } = BindingMode.OneWay;

    public float TransitionDuration { get; set; } = 0;
    public float TransitionOffset { get; set; } = 0;
    public EasingFunction Easing { get; set; } = EasingFunction.Linear;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Binding()
    {
    }

    public Binding(string? path)
    {
        Path = path;
    }

    public IDisposable Apply<TValue>(IBindingTarget target, StyledProperty<TValue> property)
    {
        return Mode switch
        {
            BindingMode.Default => property.DefaultMode switch
            {
                BindingMode.OneWay         => ApplyOneWay(target, property),
                BindingMode.OneTime        => ApplyOneTime(target, property),
                BindingMode.TwoWay         => ApplyTwoWay(target, property),
                BindingMode.OneWayToSource => ApplyOneWayToSource(target, property),
                _                          => throw new ArgumentOutOfRangeException()
            },
            BindingMode.OneWay         => ApplyOneWay(target, property),
            BindingMode.OneTime        => ApplyOneTime(target, property),
            BindingMode.TwoWay         => ApplyTwoWay(target, property),
            BindingMode.OneWayToSource => ApplyOneWayToSource(target, property),
            _                          => throw new ArgumentOutOfRangeException()
        };
    }

    // ── Modes ──────────────────────────────────────────────────────────────

    private IDisposable ApplyOneWay<TValue>(IBindingTarget target, StyledProperty<TValue> property)
    {
        var obs = target
            .GetObservable(BindableObject.DataContextProperty)
            .Select(dc => ObservePath<TValue>(dc, Path))
            .Switch();

        if (TransitionDuration > 0 && target is IAnimationCallback animationCallback)
        {
            var easing = EasingHelpers.EasingFunctions[Easing];

            obs = obs
                .PairWithPrevious()
                .Select(pair =>
                {
                    var (from, to) = pair;
                    
                    var duration = TimeSpan.FromMilliseconds(TransitionDuration);
                    var offset = TimeSpan.FromMilliseconds(TransitionOffset);

                    // TODO transitions for non-float properties
                    return EasingHelpers.GetKeyframeObservable(animationCallback, (float)((object?)from ?? 0f), (float)(object)to!, duration, offset, easing);
                })
                .Switch()
                .Cast<TValue>();
        }

        return target.Bind(property, obs);
    }

    private IDisposable ApplyOneTime<TValue>(IBindingTarget target, StyledProperty<TValue> property)
    {
        var obs = target
            .GetObservable(BindableObject.DataContextProperty)
            .Where(dc => dc is not null)
            .Take(1)
            .Select(dc => ReadLeaf<TValue>(dc, Path));
        return target.Bind(property, obs);
    }

    private IDisposable ApplyTwoWay<TValue>(IBindingTarget target, StyledProperty<TValue> property)
    {
        var forward = ApplyOneWay(target, property);

        var skipFirst = true;
        EventHandler<PropertyChangedEventArgs> onTargetChanged = (_, e) =>
        {
            if (e.Property.Id != property.Id) return;
            if (skipFirst) { skipFirst = false; return; }
            WriteLeaf(target.DataContext, Path, e.NewValue);
        };
        target.PropertyChanged += onTargetChanged;

        return Disposable.Create(() =>
        {
            forward.Dispose();
            target.PropertyChanged -= onTargetChanged;
        });
    }

    private IDisposable ApplyOneWayToSource<TValue>(IBindingTarget target, StyledProperty<TValue> property)
    {
        object? currentDc = null;
        void Push() => WriteLeaf(currentDc, Path, target.GetValue(property));

        EventHandler<PropertyChangedEventArgs> onTargetChanged = (_, e) =>
        {
            if (e.Property.Id == property.Id) Push();
        };
        target.PropertyChanged += onTargetChanged;

        var dcSub = target
            .GetObservable(BindableObject.DataContextProperty)
            .Subscribe(dc => { currentDc = dc; Push(); });

        return Disposable.Create(() =>
        {
            dcSub.Dispose();
            target.PropertyChanged -= onTargetChanged;
        });
    }

    // ── Path helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Walk to the leaf owner and return it alongside the leaf PropertyInfo.
    /// Returns (null, null) if any segment fails to resolve.
    /// </summary>
    private static (object? leafOwner, PropertyInfo? leafProp) WalkToLeaf(object? root, string? path)
    {
        if (root is null || string.IsNullOrEmpty(path))
            return (null, null);

        var segs = path!.Split('.');
        object? current = root;

        for (int i = 0; i < segs.Length - 1; i++)
        {
            if (current is null) return (null, null);
            var prop = current.GetType().GetProperty(segs[i]);
            if (prop is null) return (null, null);
            current = prop.GetValue(current);
        }

        if (current is null) return (null, null);
        var leafProp = current.GetType().GetProperty(segs[^1]);
        return (current, leafProp);
    }

    private static TValue ReadLeaf<TValue>(object? root, string? path)
    {
        var (owner, prop) = WalkToLeaf(root, path);
        if (owner is null || prop is null) return default!;
        return prop.GetValue(owner) is TValue tv ? tv : default!;
    }

    private static void WriteLeaf(object? root, string? path, object? value)
    {
        var (owner, prop) = WalkToLeaf(root, path);
        if (owner is null || prop is null || !prop.CanWrite) return;
        prop.SetValue(owner, value);
    }

    /// <summary>
    /// Returns an observable that emits the leaf value and re-emits whenever
    /// any INPC in the path fires. Re-subscribes to child segments when an
    /// intermediate property changes.
    /// </summary>
    private static IObservable<TValue> ObservePath<TValue>(object? root, string? path)
    {
        if (root is null || string.IsNullOrEmpty(path))
            return Observable.Return(default(TValue)!);

        return Observable.Create<TValue>(observer =>
        {
            var bag = new CompositeDisposable();
            var segs = path!.Split('.');
            SubscribeSegment(root, segs, 0, bag, observer);
            return bag;
        });
    }

    private static void SubscribeSegment<TValue>(
        object? current,
        string[] segs,
        int segIndex,
        CompositeDisposable bag,
        IObserver<TValue> observer)
    {
        if (current is null)
        {
            observer.OnNext(default!);
            return;
        }

        var prop = current.GetType().GetProperty(segs[segIndex]);
        if (prop is null)
        {
            observer.OnNext(default!);
            return;
        }

        if (segIndex == segs.Length - 1)
        {
            // ── Leaf: emit current value and watch for changes ─────────────
            observer.OnNext(prop.GetValue(current) is TValue tv ? tv : default!);

            if (current is INotifyPropertyChanged inpc)
            {
                PropertyChangedEventHandler handler = (_, e) =>
                {
                    if (e.PropertyName is null || e.PropertyName == segs[segIndex])
                        observer.OnNext(prop.GetValue(current) is TValue v ? v : default!);
                };
                inpc.PropertyChanged += handler;
                bag.Add(Disposable.Create(() => inpc.PropertyChanged -= handler));
            }
        }
        else
        {
            // ── Intermediate: recurse then re-subscribe when it changes ────
            var childBag = new CompositeDisposable();
            bag.Add(childBag);

            SubscribeSegment(prop.GetValue(current), segs, segIndex + 1, childBag, observer);

            if (current is INotifyPropertyChanged inpc)
            {
                PropertyChangedEventHandler handler = (_, e) =>
                {
                    if (e.PropertyName is null || e.PropertyName == segs[segIndex])
                    {
                        childBag.Clear();
                        SubscribeSegment(prop.GetValue(current), segs, segIndex + 1, childBag, observer);
                    }
                };
                inpc.PropertyChanged += handler;
                bag.Add(Disposable.Create(() => inpc.PropertyChanged -= handler));
            }
        }
    }
}