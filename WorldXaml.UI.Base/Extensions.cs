using System.Reactive.Linq;

namespace WorldXaml.UI.Base;

internal static class Extensions
{
    // https://stackoverflow.com/a/16581526
    extension<TSource>(IObservable<TSource> source)
    {
        public IObservable<(TSource? Previous, TSource Current)>
            PairWithPrevious()
        {
            return source.Scan(
                (default(TSource?), default(TSource)!),
                static (acc, current) => (acc.Item2, current)
            );
        }

        public IObservable<TTarget> UnsafeCast<TTarget>()
        {
            return source.Select(x => (TTarget?)(object?)x!)!;
        }
    }
}