using System.Runtime.CompilerServices;
using WorldXaml.UI.Base;

namespace WorldXaml.UI.Yoga;

internal static class YogaInterpolators
{
    [XamlInterpolator]
    public static Node.MeasurementFlexBasis InterpolateMeasurementFlexBasis(Node.MeasurementFlexBasis from, Node.MeasurementFlexBasis to, float alpha)
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
    }

    [XamlInterpolator]
    public static Node.MeasurementMarginPosition InterpolateMeasurementMarginPosition(Node.MeasurementMarginPosition from, Node.MeasurementMarginPosition to, float alpha)
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
    }

    [XamlInterpolator]
    public static Node.MeasurementMultiMargin InterpolateMeasurementMultiMargin(Node.MeasurementMultiMargin fromAll, Node.MeasurementMultiMargin toAll, float alpha)
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
    }

    [XamlInterpolator]
    public static Node.MeasurementPadding InterpolateMeasurementPadding(Node.MeasurementPadding from, Node.MeasurementPadding to, float alpha)
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
    }
    
    [XamlInterpolator]
    public static Node.MeasurementMultiPadding InterpolateMeasurementMultiPadding(Node.MeasurementMultiPadding fromAll, Node.MeasurementMultiPadding toAll, float alpha)
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
    }

    [XamlInterpolator]
    public static Node.MeasurementMultiBorder InterpolateMeasurementMultiBorder(Node.MeasurementMultiBorder fromAll, Node.MeasurementMultiBorder toAll, float alpha)
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
    }

    [XamlInterpolator]
    public static Node.MeasurementGap InterpolateMeasurementGap(Node.MeasurementGap from, Node.MeasurementGap to, float alpha)
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
    }

    [XamlInterpolator]
    public static Node.MeasurementWidthHeight InterpolateMeasurementWidthHeight(Node.MeasurementWidthHeight from, Node.MeasurementWidthHeight to, float alpha)
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
    }
}