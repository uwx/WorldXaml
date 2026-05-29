using System.ComponentModel;
using System.Numerics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace WorldXaml.UI.Base;

public class AnimationTrigger
{
    private bool _hasTriggered;
    private event Action? TriggeredImpl;

    public event Action? Triggered
    {
        add
        {
            TriggeredImpl += value;
            if (_hasTriggered) value?.Invoke();
        }
        remove => TriggeredImpl -= value;
    }

    public void Trigger()
    {
        _hasTriggered = true;
        TriggeredImpl?.Invoke();
    }

    public void Reset()
    {
        _hasTriggered = false;
    }
}

public class Animation : IXamlBinding
{
    public EasingFunction Easing { get; set; }
    public float KeyFrameFrom { get; set; }
    public float KeyFrameTo { get; set; }
    public float KeyFrameDuration { get; set; }
    public float KeyFrameOffset { get; set; }

    // Set by XamlX-generated IL.
    public string? NamedObject { get; set; }
    public ResolvedPath? Path { get; set; }
    
    /// <summary>
    /// Source generation constructor
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Animation()
    {
    }

    /// <summary>
    /// Constructor for IDE integration
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Animation(string path)
    {
        throw new NotImplementedException("Intentionally not implemented, should be replaced by markup transformer");
    }

    /// <summary>
    /// Method for IDE integration
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public object ProvideValue(IServiceProvider serviceProvider)
    {
        throw new NotImplementedException("Intentionally not implemented, should be replaced by markup transformer");
    }

    public IDisposable Apply<TValue>(IBindingTarget target, StyledProperty<TValue> property)
    {
        if (target is not ILogical logical)
        {
            return Disposable.Empty;
        }

        if (target is not IAnimationCallback animationCallback)
        {
            return Disposable.Empty;
        }

        var path = Path ?? throw new InvalidOperationException("CompiledBinding path was not resolved.");

        var easing = EasingHelpers.EasingFunctions[Easing];

        var obs = path.Observe(
                NamedObject != null
                    ? logical
                        .FindNameScope()
                        !.Find(NamedObject)
                    : logical)!
            .Cast<AnimationTrigger>()
            .Select(animation =>
            {
                return Observable.Create<object?>(observer =>
                {
                    animation.Triggered += Triggered;
                    return Disposable.Create(() => animation.Triggered -= Triggered);

                    void Triggered()
                    {
                        observer.OnNext(null);
                    }
                });
            })
            .Switch()
            .Select(_ =>
            {
                var from = KeyFrameFrom;
                var to = KeyFrameTo;
                var duration = TimeSpan.FromMilliseconds(KeyFrameDuration);
                var offset = TimeSpan.FromMilliseconds(KeyFrameOffset);

                return EasingHelpers.GetKeyframeObservable(animationCallback, from, to, duration, offset, easing);
            })
            .Switch()
            .Prepend(KeyFrameFrom)
            .UnsafeCast<float, TValue>();
        
        return target.Bind(property, obs);
    }
}

public enum EasingFunction : byte
{
    Linear,
    EaseInSine,
    EaseOutSine,
    EaseInOutSine,
    EaseInQuad,
    EaseOutQuad,
    EaseInOutQuad,
    EaseInCubic,
    EaseOutCubic,
    EaseInOutCubic,
    EaseInQuart,
    EaseOutQuart,
    EaseInOutQuart,
    EaseInQuint,
    EaseOutQuint,
    EaseInOutQuint,
    EaseInExpo,
    EaseOutExpo,
    EaseInOutExpo,
    EaseInCirc,
    EaseOutCirc,
    EaseInOutCirc,
    EaseInBack,
    EaseOutBack,
    EaseInOutBack,
    EaseInElastic,
    EaseOutElastic,
    EaseInOutElastic,
    EaseInBounce,
    EaseOutBounce,
    EaseInOutBounce,
}