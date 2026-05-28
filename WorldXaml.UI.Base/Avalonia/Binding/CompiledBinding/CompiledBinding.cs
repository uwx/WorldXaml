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

    public float TransitionDuration { get; set; } = 0;
    public float TransitionOffset { get; set; } = 0;
    public EasingFunction Easing { get; set; } = EasingFunction.Linear;
    
    public IValueConverter? Converter          { get; set; }
    public object?          ConverterParameter { get; set; }

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

    private IDisposable ApplyOneWay<TValue>(
        IBindingTarget target, StyledProperty<TValue> property, ResolvedPath path)
    {
        var obs = target
            .GetObservable(BindableObject.DataContextProperty)
            .Select(path.Observe)
            .Switch()
            .Select(raw => TypeCoercer.Coerce<TValue>(raw, Converter, ConverterParameter)!);

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

                    return EasingHelpers.GetKeyframeObservable(animationCallback, from ?? default!, to, duration, offset, easing);
                })
                .Switch();
        }
        
        return target.Bind(property, obs);
    }

    private IDisposable ApplyOneTime<TValue>(
        IBindingTarget target, StyledProperty<TValue> property, ResolvedPath path)
    {
        var obs = target
            .GetObservable(BindableObject.DataContextProperty)
            .Where(dc => dc is not null)
            .Take(1)
            .Select(dc => { var (_, v) = path.TryRead(dc); return v; })
            .Select(val => TypeCoercer.Coerce<TValue>(val, Converter, ConverterParameter)!);
        return target.Bind(property, obs);
    }

    private IDisposable ApplyTwoWay<TValue>(
        IBindingTarget target, StyledProperty<TValue> property, ResolvedPath path)
    {
        var forward = ApplyOneWay(target, property, path);
        var skipFirst = true;
        EventHandler<StyledPropertyChangedEventArgs> onTargetChanged = (_, e) =>
        {
            if (e.Property.Id != property.Id) return;
            if (skipFirst) { skipFirst = false; return; }
            var converted = TypeCoercer.CoerceBack(e.NewValue, path.LeafType, Converter, ConverterParameter);
            path.TryWrite(target.DataContext, converted);
        };
        target.StyledPropertyChanged += onTargetChanged;
        return Disposable.Create(() =>
        {
            forward.Dispose();
            target.StyledPropertyChanged -= onTargetChanged;
        });
    }

    private IDisposable ApplyOneWayToSource<TValue>(
        IBindingTarget target, StyledProperty<TValue> property, ResolvedPath path)
    {
        object? currentDc = null;
        void Push()
        {
            var converted = TypeCoercer.CoerceBack(target.GetValue(property), path.LeafType, Converter, ConverterParameter);
            path.TryWrite(currentDc, converted);
        }

        EventHandler<StyledPropertyChangedEventArgs> onTargetChanged = (_, e) =>
        {
            if (e.Property.Id == property.Id) Push();
        };
        target.StyledPropertyChanged += onTargetChanged;

        var dcSub = target
            .GetObservable(BindableObject.DataContextProperty)
            .Subscribe(dc => { currentDc = dc; Push(); });

        return Disposable.Create(() =>
        {
            dcSub.Dispose();
            target.StyledPropertyChanged -= onTargetChanged;
        });
    }
}