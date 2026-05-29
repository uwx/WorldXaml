using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace WorldXaml.UI.Base
{
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

        public static IObservable<T> GetKeyframeObservable<T>(
            IAnimationCallback animationCallback,
            T from,
            T to,
            TimeSpan duration,
            TimeSpan offset,
            Func<float, float> easing
        )
        {
            var startTime = DateTimeOffset.Now + offset;

            return Observable.Create<T>(observer =>
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
                        observer.OnNext(Interpolate(from, to, easedT));
                    }
                }
            });
        }

        private static class InterpolatorCache<T>
        {
            public static Interpolator<T>? Interpolator { get; } = (Interpolator<T>?)InterpolatorRegistry.Interpolators.GetValueOrDefault(typeof(T));
        }

        public static TValue Interpolate<TValue>(TValue from, TValue to, float alpha)
        {
            var interpolator = InterpolatorCache<TValue>.Interpolator;

            if (interpolator != null)
            {
                return interpolator(from, to, alpha);
            }
        
            ThrowError();
            return default!;

            [DoesNotReturn]
            static void ThrowError()
            {
                throw new ArgumentException($"Cannot interpolate values of type {typeof(TValue)}");
            }
        } 
    }
}