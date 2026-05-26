using System.Reactive.Linq;

namespace WorldXaml.UI.Base;

internal static class Extensions
{
    // https://stackoverflow.com/a/16581526
    public static IObservable<(TSource? Previous, TSource Current)>
        PairWithPrevious<TSource>(this IObservable<TSource> source)
    {
        return source.Scan(
            (default(TSource?), default(TSource)!),
            static (acc, current) => (acc.Item2, current)
        );
    }
}