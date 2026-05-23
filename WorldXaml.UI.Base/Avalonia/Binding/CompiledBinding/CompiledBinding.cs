using System.Reactive.Disposables;
using System.Reactive.Linq;
using WorldXaml.UI.Base;

// ReSharper disable once CheckNamespace
namespace Avalonia.Data;

/// <summary>
/// {CompiledBinding} markup extension — path is resolved at compile time by XamlX.
/// No reflection at runtime.
/// </summary>
public sealed class CompiledBinding : IXamlBinding
{
    // Set by XamlX-generated IL via the ResolvedBindingPathNode emitter.
    public ResolvedPath? Path { get; set; }
    public BindingMode Mode { get; set; } = BindingMode.OneWay;

    public IDisposable Apply<TValue>(IBindingTarget target, StyledProperty<TValue> property)
    {
        var path = Path ?? throw new InvalidOperationException("CompiledBinding path was not resolved.");

        return Mode switch
        {
            BindingMode.Default => property.DefaultMode switch
            {
                BindingMode.OneWay         => ApplyOneWay(target, property, path),
                BindingMode.OneTime        => ApplyOneTime(target, property, path),
                BindingMode.TwoWay         => ApplyTwoWay(target, property, path),
                BindingMode.OneWayToSource => ApplyOneWayToSource(target, property, path),
                _                          => throw new ArgumentOutOfRangeException()
            },
            BindingMode.OneWay => ApplyOneWay(target, property, path),
            BindingMode.OneTime => ApplyOneTime(target, property, path),
            BindingMode.TwoWay => ApplyTwoWay(target, property, path),
            BindingMode.OneWayToSource => ApplyOneWayToSource(target, property, path),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static IDisposable ApplyOneWay<TValue>(
        IBindingTarget target, StyledProperty<TValue> property, ResolvedPath path)
    {
        var obs = target
            .GetObservable(BindableObject.DataContextProperty)
            .Select(dc => path.Observe(dc).Select(v => v is TValue tv ? tv : default!))
            .Switch();
        return target.Bind(property, obs);
    }

    private static IDisposable ApplyOneTime<TValue>(
        IBindingTarget target, StyledProperty<TValue> property, ResolvedPath path)
    {
        var obs = target
            .GetObservable(BindableObject.DataContextProperty)
            .Where(dc => dc is not null)
            .Take(1)
            .Select(dc => { var (_, v) = path.TryRead(dc); return v is TValue tv ? tv : default!; });
        return target.Bind(property, obs);
    }

    private static IDisposable ApplyTwoWay<TValue>(
        IBindingTarget target, StyledProperty<TValue> property, ResolvedPath path)
    {
        var forward = ApplyOneWay(target, property, path);
        var skipFirst = true;
        EventHandler<PropertyChangedEventArgs> onTargetChanged = (_, e) =>
        {
            if (e.Property.Id != property.Id) return;
            if (skipFirst) { skipFirst = false; return; }
            path.TryWrite(target.DataContext, e.NewValue);
        };
        target.PropertyChanged += onTargetChanged;
        return Disposable.Create(() =>
        {
            forward.Dispose();
            target.PropertyChanged -= onTargetChanged;
        });
    }

    private static IDisposable ApplyOneWayToSource<TValue>(
        IBindingTarget target, StyledProperty<TValue> property, ResolvedPath path)
    {
        object? currentDc = null;
        void Push() => path.TryWrite(currentDc, target.GetValue(property));

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
}