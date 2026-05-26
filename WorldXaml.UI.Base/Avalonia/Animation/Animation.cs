using System.Collections.Frozen;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace WorldXaml.UI.Base;

public class AnimationTrigger
{
    public event Action? Triggered;

    public void Trigger()
    {
        Triggered?.Invoke();
    }
}

internal static class EasingHelpers
{
    public static FrozenDictionary<EasingFunction, Func<float, float>> EasingFunctions { get; } = new Dictionary<EasingFunction, Func<float, float>>
    {
        [EasingFunction.Linear] = static t => t,
        [EasingFunction.EaseInSine] = static t => 1 - MathF.Cos((t * MathF.PI) / 2),
        [EasingFunction.EaseOutSine] = static t => MathF.Sin((t * MathF.PI) / 2),
        [EasingFunction.EaseInOutSine] = static t => -(MathF.Cos(MathF.PI * t) - 1) / 2,
        [EasingFunction.EaseInQuad] = static t => t * t,
        [EasingFunction.EaseOutQuad] = static t => 1 - (1 - t) * (1 - t),
        [EasingFunction.EaseInOutQuad] = static t => t < 0.5 ? 2 * t * t : 1 - MathF.Pow(-2 * t + 2, 2) / 2,
        [EasingFunction.EaseInCubic] = static t => t * t * t,
        [EasingFunction.EaseOutCubic] = static t => 1 - MathF.Pow(1 - t, 3),
        [EasingFunction.EaseInOutCubic] = static t => t < 0.5 ? 4 * t * t * t : 1 - MathF.Pow(-2 * t + 2, 3) / 2,
        [EasingFunction.EaseInQuart] = static t => t * t * t * t,
        [EasingFunction.EaseOutQuart] = static t => 1 - MathF.Pow(1 - t, 4),
        [EasingFunction.EaseInOutQuart] = static t => t < 0.5 ? 8 * t * t * t * t : 1 - MathF.Pow(1 - t, 4) * 8 / 2,
        [EasingFunction.EaseInQuint] = static t => t * t * t * t * t,
        [EasingFunction.EaseOutQuint] = static t => 1 - MathF.Pow(1 - t, 5),
        [EasingFunction.EaseInOutQuint] = static t => t < 0.5 ? 16 * t * t * t * t * t : 1 - MathF.Pow(1 - t, 5) * 16 / 2,
        [EasingFunction.EaseInExpo] = static t => t == 0 ? 0 : MathF.Pow(2, 10 * t - 10),
        [EasingFunction.EaseOutExpo] = static t => t == 1 ? 1 : 1 - MathF.Pow(2, -10 * t),
        [EasingFunction.EaseInOutExpo] = static t => t == 0 ? 0 : t == 1 ? 1 : (t < 0.5 ? MathF.Pow(2, 20 * t - 10) / 2 : (2 - MathF.Pow(2, -20 * t + 10)) / 2),
        [EasingFunction.EaseInCirc] = static t => 1 - MathF.Sqrt(1 - MathF.Pow(t, 2)),
        [EasingFunction.EaseOutCirc] = static t => MathF.Sqrt(1 - MathF.Pow(t - 1, 2)),
        [EasingFunction.EaseInOutCirc] = static t => t < 0.5 ? (1 - MathF.Sqrt(1 - MathF.Pow(2 * t, 2))) / 2 : (MathF.Sqrt(1 - MathF.Pow(-2 * t + 2, 2)) + 1) / 2,
        [EasingFunction.EaseInBack] = static t => 2.70158f * t * t * t - 1.70158f * t * t,
        [EasingFunction.EaseOutBack] = static t => 1 + 2.70158f * MathF.Pow(t - 1, 3) + 1.70158f * MathF.Pow(t - 1, 2),
        [EasingFunction.EaseInOutBack] = static t => t < 0.5 ? (MathF.Pow(2 * t, 2) * ((2.70158f + 1) * 2 * t - 2.70158f)) / 2 : (MathF.Pow(2 * t - 2, 2) * ((2.70158f + 1) * (t * 2 - 2) + 2.70158f) + 2) / 2,
        [EasingFunction.EaseInElastic] = static t => t == 0 ? 0 : t == 1 ? 1 : -MathF.Pow(2, 10 * t - 10) * MathF.Sin((t * 10 - 10.75f) * ((2 * MathF.PI) / 3)),
        [EasingFunction.EaseOutElastic] = static t => t == 0 ? 0 : t == 1 ? 1 : MathF.Pow(2, -10 * t) * MathF.Sin((t * 10 - 0.75f) * ((2 * MathF.PI) / 3)) + 1,
        [EasingFunction.EaseInOutElastic] = static t => t == 0 ? 0 : t == 1 ? 1 : (t < 0.5 ? -(MathF.Pow(2, 20 * t - 10) * MathF.Sin((20 * t - 11.125f) * ((2 * MathF.PI) / 4.5f))) / 2 : (MathF.Pow(2, -20 * t + 10) * MathF.Sin((20 * t - 11.125f) * ((2 * MathF.PI) / 4.5f))) / 2 + 1),
        [EasingFunction.EaseInBounce] = static t =>
        {
            return 1 - EaseOutBounce(1 - t);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static float EaseOutBounce(float t)
            {
                const float n1 = 7.5625f;
                const float d1 = 2.75f;
                if (t < 1 / d1) return n1 * t * t;
                if (t < 2 / d1) return n1 * (t - 1.5f / d1) * (t - 1.5f / d1) + 0.75f;
                if (t < 2.5 / d1) return n1 * (t - 2.25f / d1) * (t - 2.25f / d1) + 0.9375f;
                return n1 * (t - 2.625f / d1) * (t - 2.625f / d1) + 0.984375f;
            }
        },
        [EasingFunction.EaseOutBounce] = static t =>
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1 / d1) return n1 * t * t;
            if (t < 2 / d1) return n1 * (t - 1.5f / d1) * (t - 1.5f / d1) + 0.75f;
            if (t < 2.5 / d1) return n1 * (t - 2.25f / d1) * (t - 2.25f / d1) + 0.9375f;
            return n1 * (t - 2.625f / d1) * (t - 2.625f / d1) + 0.984375f;
        },
        [EasingFunction.EaseInOutBounce] = static t =>
        {
            if (t < 0.5f)
            {
                return (1 - EaseOutBounce(1 - 2 * t)) / 2;
            }

            return (1 + EaseOutBounce(2 * t - 1)) / 2;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static float EaseOutBounce(float t)
            {
                const float n1 = 7.5625f;
                const float d1 = 2.75f;
                if (t < 1 / d1) return n1 * t * t;
                if (t < 2 / d1) return n1 * (t - 1.5f / d1) * (t - 1.5f / d1) + 0.75f;
                if (t < 2.5 / d1) return n1 * (t - 2.25f / d1) * (t - 2.25f / d1) + 0.9375f;
                return n1 * (t - 2.625f / d1) * (t - 2.625f / d1) + 0.984375f;
            }
        }
    }.ToFrozenDictionary();

    public static IObservable<object> GetKeyframeObservable(
        IAnimationCallback animationCallback,
        float from,
        float to,
        TimeSpan duration,
        TimeSpan offset,
        Func<float, float> easing
    )
    {
        var startTime = DateTimeOffset.Now + offset;

        return Observable.Create<object>(observer =>
        {
            animationCallback.AnimationFrameBegan += AnimationFrame;
            return Disposable.Create(() => animationCallback.AnimationFrameBegan -= AnimationFrame);

            void AnimationFrame()
            {
                var now = DateTimeOffset.Now;
                if (now < startTime) return;

                var t = (float)((now - startTime).TotalSeconds / duration.TotalSeconds);
                if (t >= 1f)
                {
                    observer.OnNext(to);
                    observer.OnCompleted();
                    animationCallback.AnimationFrameBegan -= AnimationFrame;
                }
                else
                {
                    var easedT = easing(t);
                    var value = from + (to - from) * easedT;
                    observer.OnNext(value);
                }
            }
        });
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
            .Cast<TValue>();
        
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