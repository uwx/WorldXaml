using System.Runtime.CompilerServices;
using WorldXaml.UI.Base;

namespace WorldXaml.UI.Yoga;

public class YogaInterpolatorProvider : IInterpolatorProvider
{
    public Interpolator<T>? GetInterpolator<T>()
    {
        if (typeof(T) == typeof(Node.MeasurementFlexBasis))
        {
            return (Interpolator<T>)(object)(Interpolator<Node.MeasurementFlexBasis>)((from, to, alpha) =>
            {
                if (from.PointValue is { } fromPoint && to.PointValue is { } toPoint)
                {
                    return Node.MeasurementFlexBasis.Point(fromPoint + (toPoint - fromPoint) * alpha);
                }

                if (from.PercentValue is { } fromPercent && to.PercentValue is { } toPercent)
                {
                    return Node.MeasurementFlexBasis.Percent(fromPercent + (toPercent - fromPercent) * alpha);
                }

                if (alpha < 0.5f) return from;
                return to;
            });
        }

        if (typeof(T) == typeof(Node.MeasurementMarginPosition))
        {
            return (Interpolator<T>)(object)(Interpolator<Node.MeasurementMarginPosition>)((from, to, alpha) =>
            {
                if (from.PointValue is { } fromPoint && to.PointValue is { } toPoint)
                {
                    return Node.MeasurementMarginPosition.Point(fromPoint + (toPoint - fromPoint) * alpha);
                }

                if (from.PercentValue is { } fromPercent && to.PercentValue is { } toPercent)
                {
                    return Node.MeasurementMarginPosition.Percent(fromPercent + (toPercent - fromPercent) * alpha);
                }

                if (alpha < 0.5f) return from;
                return to;
            });
        }

        if (typeof(T) == typeof(Node.MeasurementMultiMargin))
        {
            return (Interpolator<T>)(object)(Interpolator<Node.MeasurementMultiMargin>)((fromAll, toAll, alpha) =>
            {
                InlineArray4<Node.MeasurementMarginPosition> sides = new();

                for (var i = 0; i < 4; i++)
                {
                    var from = fromAll.Sides[i];
                    var to = toAll.Sides[i];
                    if (from.PointValue is { } fromPoint && to.PointValue is { } toPoint)
                    {
                        sides[i] = Node.MeasurementMarginPosition.Point(fromPoint + (toPoint - fromPoint) * alpha);
                    }
                    else if (from.PercentValue is { } fromPercent && to.PercentValue is { } toPercent)
                    {
                        sides[i] = Node.MeasurementMarginPosition.Percent(fromPercent + (toPercent - fromPercent) * alpha);
                    }
                    else
                    {
                        sides[i] = alpha < 0.5f ? from : to;
                    }
                }

                return new Node.MeasurementMultiMargin
                {
                    Top = sides[0],
                    Bottom = sides[1],
                    Left = sides[2],
                    Right = sides[3]
                };
            });
        }

        if (typeof(T) == typeof(Node.MeasurementPadding))
        {
            return (Interpolator<T>)(object)(Interpolator<Node.MeasurementPadding>)((from, to, alpha) =>
            {
                if (from.PointValue is { } fromPoint && to.PointValue is { } toPoint)
                {
                    return Node.MeasurementPadding.Point(fromPoint + (toPoint - fromPoint) * alpha);
                }

                if (from.PercentValue is { } fromPercent && to.PercentValue is { } toPercent)
                {
                    return Node.MeasurementPadding.Percent(fromPercent + (toPercent - fromPercent) * alpha);
                }

                if (alpha < 0.5f) return from;
                return to;
            });
        }

        if (typeof(T) == typeof(Node.MeasurementMultiPadding))
        {
            return (Interpolator<T>)(object)(Interpolator<Node.MeasurementMultiPadding>)((fromAll, toAll, alpha) =>
            {
                InlineArray4<Node.MeasurementPadding> sides = new();

                for (var i = 0; i < 4; i++)
                {
                    var from = fromAll.Sides[i];
                    var to = toAll.Sides[i];
                    if (from.PointValue is { } fromPoint && to.PointValue is { } toPoint)
                    {
                        sides[i] = Node.MeasurementPadding.Point(fromPoint + (toPoint - fromPoint) * alpha);
                    }
                    else if (from.PercentValue is { } fromPercent && to.PercentValue is { } toPercent)
                    {
                        sides[i] = Node.MeasurementPadding.Percent(fromPercent + (toPercent - fromPercent) * alpha);
                    }
                    else
                    {
                        sides[i] = alpha < 0.5f ? from : to;
                    }
                }

                return new Node.MeasurementMultiPadding
                {
                    Top = sides[0],
                    Bottom = sides[1],
                    Left = sides[2],
                    Right = sides[3]
                };
            });
        }

        if (typeof(T) == typeof(Node.MeasurementMultiBorder))
        {
            return (Interpolator<T>)(object)(Interpolator<Node.MeasurementMultiBorder>)((fromAll, toAll, alpha) =>
            {
                InlineArray4<float?> sides = new();

                for (var i = 0; i < 4; i++)
                {
                    var from = fromAll.Sides[i];
                    var to = toAll.Sides[i];
                    sides[i] = from + (to - from) * alpha;
                }

                return new Node.MeasurementMultiBorder
                {
                    Top = sides[0],
                    Bottom = sides[1],
                    Left = sides[2],
                    Right = sides[3]
                };
            });
        }

        if (typeof(T) == typeof(Node.MeasurementGap))
        {
            return (Interpolator<T>)(object)(Interpolator<Node.MeasurementGap>)((from, to, alpha) =>
            {
                if (from.PointValue is { } fromPoint && to.PointValue is { } toPoint)
                {
                    return Node.MeasurementGap.Point(fromPoint + (toPoint - fromPoint) * alpha);
                }

                if (from.PercentValue is { } fromPercent && to.PercentValue is { } toPercent)
                {
                    return Node.MeasurementGap.Percent(fromPercent + (toPercent - fromPercent) * alpha);
                }

                if (alpha < 0.5f) return from;
                return to;
            });
        }

        if (typeof(T) == typeof(Node.MeasurementWidthHeight))
        {
            return (Interpolator<T>)(object)(Interpolator<Node.MeasurementWidthHeight>)((from, to, alpha) =>
            {
                if (from.PointValue is { } fromPoint && to.PointValue is { } toPoint)
                {
                    return Node.MeasurementWidthHeight.Point(fromPoint + (toPoint - fromPoint) * alpha);
                }

                if (from.PercentValue is { } fromPercent && to.PercentValue is { } toPercent)
                {
                    return Node.MeasurementWidthHeight.Percent(fromPercent + (toPercent - fromPercent) * alpha);
                }

                if (alpha < 0.5f) return from;
                return to;
            });
        }

        return null;
    }
}