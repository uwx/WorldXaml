using System.Numerics;
using JetBrains.Annotations;

namespace WorldXaml.UI.Base;

public static class XamlConfig
{
    /// <summary>
    /// Set to define a custom log handler for WorldXaml.UI.
    /// </summary>
    public static Action<LogLevel, string> LogMessage { internal get; set; } = (level, message) =>
    {
        if (level == LogLevel.Info)
            Console.WriteLine($"[Info][WorldXaml] {message}");
        else if (level == LogLevel.Warning)
            Console.WriteLine($"[Warning][WorldXaml] {message}");
        else if (level == LogLevel.Error)
            Console.WriteLine($"[Error][WorldXaml] {message}");
        else if (level == LogLevel.Debug)
            Console.WriteLine($"[Debug][WorldXaml] {message}");
        else
            throw new ArgumentOutOfRangeException(nameof(level), level, null);
    };
}

public delegate T Interpolator<T>(T from, T to, float alpha);

public static class InterpolatorRegistry
{
    internal static readonly Dictionary<Type, object> Interpolators = new()
    {
        [typeof(double)] = (Interpolator<double>)((from, to, alpha) => from + (to - from) * alpha),
        [typeof(double)] = (Interpolator<double>)((from, to, alpha) => from + (to - from) * alpha),
        [typeof(float)] = (Interpolator<float>)((from, to, alpha) => from + (to - from) * alpha),
        [typeof(decimal)] = (Interpolator<decimal>)((from, to, alpha) => from + (to - from) * (decimal)alpha),
        [typeof(byte)] = (Interpolator<byte>)((from, to, alpha) => (byte)(from + (to - from) * alpha)),
        [typeof(sbyte)] = (Interpolator<sbyte>)((from, to, alpha) => (sbyte)(from + (to - from) * alpha)),
        [typeof(short)] = (Interpolator<short>)((from, to, alpha) => (short)(from + (to - from) * alpha)),
        [typeof(ushort)] = (Interpolator<ushort>)((from, to, alpha) => (ushort)(from + (to - from) * alpha)),
        [typeof(int)] = (Interpolator<int>)((from, to, alpha) => (int)(from + (to - from) * alpha)),
        [typeof(uint)] = (Interpolator<uint>)((from, to, alpha) => (uint)(from + (to - from) * alpha)),
        [typeof(long)] = (Interpolator<long>)((from, to, alpha) => (long)(from + (to - from) * alpha)),
        [typeof(ulong)] = (Interpolator<ulong>)((from, to, alpha) => (ulong)(from + (to - from) * alpha)),
        [typeof(Int128)] = (Interpolator<Int128>)((from, to, alpha) => (Int128)((float)from + (float)(to - from) * alpha)),
        [typeof(UInt128)] = (Interpolator<UInt128>)((from, to, alpha) => (UInt128)((float)from + (float)(to - from) * alpha)),
        [typeof(Vector2)] = (Interpolator<Vector2>)((from, to, alpha) => new Vector2(from.X + (to.X - from.X) * alpha, from.Y + (to.Y - from.Y) * alpha)),
        [typeof(Vector3)] = (Interpolator<Vector3>)((from, to, alpha) => new Vector3(from.X + (to.X - from.X) * alpha, from.Y + (to.Y - from.Y) * alpha, from.Z + (to.Z - from.Z) * alpha)),
        [typeof(Vector4)] = (Interpolator<Vector4>)((from, to, alpha) => new Vector4(from.X + (to.X - from.X) * alpha, from.Y + (to.Y - from.Y) * alpha, from.Z + (to.Z - from.Z) * alpha, from.W + (to.W - from.W) * alpha)),
        [typeof(double?)] = (Interpolator<double?>)((from, to, alpha) => from + (to - from) * alpha),
        [typeof(float?)] = (Interpolator<float?>)((from, to, alpha) => from + (to - from) * alpha),
        [typeof(decimal?)] = (Interpolator<decimal?>)((from, to, alpha) => from + (to - from) * (decimal)alpha),
        [typeof(byte?)] = (Interpolator<byte?>)((from, to, alpha) => (byte?)(from + (to - from) * alpha)),
        [typeof(sbyte?)] = (Interpolator<sbyte?>)((from, to, alpha) => (sbyte?)(from + (to - from) * alpha)),
        [typeof(short?)] = (Interpolator<short?>)((from, to, alpha) => (short?)(from + (to - from) * alpha)),
        [typeof(ushort?)] = (Interpolator<ushort?>)((from, to, alpha) => (ushort?)(from + (to - from) * alpha)),
        [typeof(int?)] = (Interpolator<int?>)((from, to, alpha) => (int?)(from + (to - from) * alpha)),
        [typeof(uint?)] = (Interpolator<uint?>)((from, to, alpha) => (uint?)(from + (to - from) * alpha)),
        [typeof(long?)] = (Interpolator<long?>)((from, to, alpha) => (long?)(from + (to - from) * alpha)),
        [typeof(ulong?)] = (Interpolator<ulong?>)((from, to, alpha) => (ulong?)(from + (to - from) * alpha)),
        [typeof(Int128?)] = (Interpolator<Int128?>)((from, to, alpha) => (Int128?)((float?)from + (float?)(to - from) * alpha)),
        [typeof(UInt128?)] = (Interpolator<UInt128?>)((from, to, alpha) => (UInt128?)((float?)from + (float?)(to - from) * alpha)),
        [typeof(Vector2?)] = (Interpolator<Vector2?>)((from, to, alpha) =>
            {
                if (from is { } fromValue && to is { } toValue)
                {
                    var fromX = fromValue!.X;
                    var fromY = fromValue!.Y;
                    var toX = toValue!.X;
                    var toY = toValue!.Y;
                    var x = fromX + (toX - fromX) * alpha;
                    var y = fromY + (toY - fromY) * alpha;
                    return new Vector2(x, y);
                }

                if (alpha < 0.5f) return from;
                return to;
            }),
        [typeof(Vector3?)] = (Interpolator<Vector3?>)((from, to, alpha) =>
            {
                if (from is { } fromValue && to is { } toValue)
                {
                    var fromX = fromValue!.X;
                    var fromY = fromValue!.Y;
                    var fromZ = fromValue!.Z;
                    var toX = toValue!.X;
                    var toY = toValue!.Y;
                    var toZ = toValue!.Z;
                    var x = fromX + (toX - fromX) * alpha;
                    var y = fromY + (toY - fromY) * alpha;
                    var z = fromZ + (toZ - fromZ) * alpha;
                    return new Vector3(x, y, z);
                }

                if (alpha < 0.5f) return from;
                return to;
            }),
        [typeof(Vector4?)] = (Interpolator<Vector4?>)((from, to, alpha) =>
            {
                if (from is { } fromValue && to is { } toValue)
                {
                    var fromX = fromValue!.X;
                    var fromY = fromValue!.Y;
                    var fromZ = fromValue!.Z;
                    var fromW = fromValue!.W;
                    var toX = toValue!.X;
                    var toY = toValue!.Y;
                    var toZ = toValue!.Z;
                    var toW = toValue!.W;
                    var x = fromX + (toX - fromX) * alpha;
                    var y = fromY + (toY - fromY) * alpha;
                    var z = fromZ + (toZ - fromZ) * alpha;
                    var w = fromW + (toW - fromW) * alpha;
                    return new Vector4(x, y, z, w);
                }

                if (alpha < 0.5f) return from;
                return to;
            }),
    };

    [UsedImplicitly]
    public static void RegisterInterpolator<T>(Interpolator<T> interpolator)
    {
        Interpolators[typeof(T)] = interpolator;
    }
}