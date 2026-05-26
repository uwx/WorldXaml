using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Data;
using Avalonia.LogicalTree;
using WorldXaml.UI.Base;
using Yoga;

namespace WorldXaml.UI.Yoga;

// ReSharper disable InconsistentNaming

/// <summary>
/// Represents a single node in the Yoga layout system.
/// </summary>
[DebuggerDisplay("{DebugToString()}")]
public partial class Node : BindableObject, IDisposable, INamed, ILogical, IAnimationCallback
{
    internal static readonly YGConfigPtr Config;

    internal YGNodePtr NodeInternal = new(Config);

    internal readonly string __INTERNAL_CtorCallerFilePath = "";
    internal readonly int __INTERNAL_CtorCallerLineNumber = 0;
    internal readonly string __INTERNAL_CtorCallerMemberName = "";

    internal static readonly List<Node> __INTERNAL_YogaRootsThisFrame = new();

    public override IReadOnlyList<ILogical> LogicalChildren => [];

#if DEBUG
    [MethodImpl(MethodImplOptions.NoInlining)]
#endif
    public Node()
    {
#if DEBUG
        var stackTrace = new StackTrace(1, true);
        // skip inherited constructors
        var stackFrame = stackTrace.GetFrames()
            .FirstOrDefault(e => e.GetMethod()?.DeclaringType?.IsAssignableTo(typeof(Node)) != true);
        __INTERNAL_CtorCallerFilePath = stackFrame?.GetFileName() ?? "";
        __INTERNAL_CtorCallerLineNumber = stackFrame?.GetFileLineNumber() ?? 0;
        __INTERNAL_CtorCallerMemberName = stackFrame?.GetMethod()?.Name ?? "";
#endif
    }
    
    public event Action? AnimationFrameBegan;

    [Property]
    public partial string? Name { get; set; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public string DebugToString()
    {
        return $"Node(Name={Name}, LayoutX={LayoutX}, LayoutY={LayoutY}, LayoutWidth={LayoutWidth}, LayoutHeight={LayoutHeight})";
    }

    #region Layout

    // https://www.w3schools.com/css/css_boxmodel.asp
    private protected Vector2 _root;
    
    /// <summary>
    /// Property field for <see cref="LayoutMarginPosition"/>.
    /// </summary>
    public static DirectProperty<Node, Vector2> LayoutMarginPositionProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, Vector2>(
            name:   nameof(LayoutMarginPosition),
            getter: node => node.LayoutMarginPosition);
    
    /// <summary>
    /// Property field for <see cref="LayoutMarginSize"/>.
    /// </summary>
    public static DirectProperty<Node, Vector2> LayoutMarginSizeProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, Vector2>(
            name:   nameof(LayoutMarginSize),
            getter: node => node.LayoutMarginSize);
    
    /// <summary>
    /// Property field for <see cref="LayoutBorderPosition"/>.
    /// </summary>
    public static DirectProperty<Node, Vector2> LayoutBorderPositionProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, Vector2>(
            name:   nameof(LayoutBorderPosition),
            getter: node => node.LayoutBorderPosition);
    
    /// <summary>
    /// Property field for <see cref="LayoutBorderSize"/>.
    /// </summary>
    public static DirectProperty<Node, Vector2> LayoutBorderSizeProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, Vector2>(
            name:   nameof(LayoutBorderSize),
            getter: node => node.LayoutBorderSize);
    
    /// <summary>
    /// Property field for <see cref="LayoutPaddingPosition"/>.
    /// </summary>
    public static DirectProperty<Node, Vector2> LayoutPaddingPositionProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, Vector2>(
            name:   nameof(LayoutPaddingPosition),
            getter: node => node.LayoutPaddingPosition);
    
    /// <summary>
    /// Property field for <see cref="LayoutPaddingSize"/>.
    /// </summary>
    public static DirectProperty<Node, Vector2> LayoutPaddingSizeProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, Vector2>(
            name:   nameof(LayoutPaddingSize),
            getter: node => node.LayoutPaddingSize);
    
    /// <summary>
    /// Property field for <see cref="LayoutContentPosition"/>.
    /// </summary>
    public static DirectProperty<Node, Vector2> LayoutContentPositionProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, Vector2>(
            name:   nameof(LayoutContentPosition),
            getter: node => node.LayoutContentPosition);
    
    /// <summary>
    /// Property field for <see cref="LayoutContentSize"/>.
    /// </summary>
    public static DirectProperty<Node, Vector2> LayoutContentSizeProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, Vector2>(
            name:   nameof(LayoutContentSize),
            getter: node => node.LayoutContentSize);
    
    /// <summary>
    /// Property field for <see cref="LayoutMargin"/>.
    /// </summary>
    public static DirectProperty<Node, Vector2> LayoutMarginProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, Vector2>(
            name:   nameof(LayoutMargin),
            getter: node => node.LayoutMargin);
    
    /// <summary>
    /// Property field for <see cref="LayoutPadding"/>.
    /// </summary>
    public static DirectProperty<Node, Vector2> LayoutPaddingProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, Vector2>(
            name:   nameof(LayoutPadding),
            getter: node => node.LayoutPadding);
    
    /// <summary>
    /// Property field for <see cref="LayoutBorder"/>.
    /// </summary>
    public static DirectProperty<Node, Vector2> LayoutBorderProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, Vector2>(
            name:   nameof(LayoutBorder),
            getter: node => node.LayoutBorder);
    
    /// <summary>
    /// Property field for <see cref="LayoutWidth"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutWidthProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutWidth),
            getter: node => node.LayoutWidth);
    
    /// <summary>
    /// Property field for <see cref="LayoutHeight"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutHeightProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutHeight),
            getter: node => node.LayoutHeight);
    
    /// <summary>
    /// Property field for <see cref="LayoutX"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutXProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutX),
            getter: node => node.LayoutX);
    
    /// <summary>
    /// Property field for <see cref="LayoutY"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutYProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutY),
            getter: node => node.LayoutY);
    
    /// <summary>
    /// Property field for <see cref="LayoutDirection"/>.
    /// </summary>
    public static DirectProperty<Node, YgDirection> LayoutDirectionProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, YgDirection>(
            name:   nameof(LayoutDirection),
            getter: node => node.LayoutDirection);
    
    /// <summary>
    /// Property field for <see cref="HadOverflow"/>.
    /// </summary>
    public static DirectProperty<Node, bool> HadOverflowProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, bool>(
            name:   nameof(HadOverflow),
            getter: node => node.HadOverflow);
    
    /// <summary>
    /// Property field for <see cref="LayoutMarginTop"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutMarginTopProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutMarginTop),
            getter: node => node.LayoutMarginTop);
    
    /// <summary>
    /// Property field for <see cref="LayoutMarginBottom"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutMarginBottomProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutMarginBottom),
            getter: node => node.LayoutMarginBottom);
    
    /// <summary>
    /// Property field for <see cref="LayoutMarginLeft"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutMarginLeftProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutMarginLeft),
            getter: node => node.LayoutMarginLeft);
    
    /// <summary>
    /// Property field for <see cref="LayoutMarginRight"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutMarginRightProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutMarginRight),
            getter: node => node.LayoutMarginRight);
    
    /// <summary>
    /// Property field for <see cref="LayoutPaddingTop"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutPaddingTopProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutPaddingTop),
            getter: node => node.LayoutPaddingTop);
    
    /// <summary>
    /// Property field for <see cref="LayoutPaddingBottom"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutPaddingBottomProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutPaddingBottom),
            getter: node => node.LayoutPaddingBottom);
    
    /// <summary>
    /// Property field for <see cref="LayoutPaddingLeft"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutPaddingLeftProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutPaddingLeft),
            getter: node => node.LayoutPaddingLeft);
    
    /// <summary>
    /// Property field for <see cref="LayoutPaddingRight"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutPaddingRightProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutPaddingRight),
            getter: node => node.LayoutPaddingRight);
    
    /// <summary>
    /// Property field for <see cref="LayoutBorderTop"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutBorderTopProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutBorderTop),
            getter: node => node.LayoutBorderTop);
    
    /// <summary>
    /// Property field for <see cref="LayoutBorderBottom"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutBorderBottomProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutBorderBottom),
            getter: node => node.LayoutBorderBottom);
    
    /// <summary>
    /// Property field for <see cref="LayoutBorderLeft"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutBorderLeftProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutBorderLeft),
            getter: node => node.LayoutBorderLeft);
    
    /// <summary>
    /// Property field for <see cref="LayoutBorderRight"/>.
    /// </summary>
    public static DirectProperty<Node, float> LayoutBorderRightProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float>(
            name:   nameof(LayoutBorderRight),
            getter: node => node.LayoutBorderRight);
    
    public Vector2 LayoutMarginPosition => _root + new Vector2(LayoutX, LayoutY);
    public Vector2 LayoutMarginSize => new(LayoutWidth, LayoutHeight);
    public Vector2 LayoutBorderPosition => _root + new Vector2(LayoutX + LayoutMarginLeft, LayoutY + LayoutMarginTop);
    public Vector2 LayoutBorderSize => new(LayoutWidth - (LayoutMarginLeft + LayoutMarginRight), LayoutHeight - (LayoutMarginTop + LayoutMarginBottom));
    public Vector2 LayoutPaddingPosition => _root + new Vector2(LayoutX + LayoutMarginLeft + LayoutBorderLeft, LayoutY + LayoutMarginTop + LayoutBorderTop);
    public Vector2 LayoutPaddingSize => new(LayoutWidth - (LayoutMarginLeft + LayoutMarginRight + LayoutBorderLeft + LayoutBorderRight), LayoutHeight - (LayoutMarginTop + LayoutMarginBottom + LayoutBorderTop + LayoutBorderBottom));
    public Vector2 LayoutContentPosition => _root + new Vector2(LayoutX + LayoutMarginLeft + LayoutBorderLeft + LayoutPaddingLeft, LayoutY + LayoutMarginTop + LayoutBorderTop + LayoutPaddingTop);
    public Vector2 LayoutContentSize => new(LayoutWidth - (LayoutMarginLeft + LayoutMarginRight + LayoutBorderLeft + LayoutBorderRight + LayoutPaddingLeft + LayoutPaddingRight), LayoutHeight - (LayoutMarginTop + LayoutMarginBottom + LayoutBorderTop + LayoutBorderBottom + LayoutPaddingTop + LayoutPaddingBottom));

    public Vector2 LayoutMargin => new(LayoutMarginLeft + LayoutMarginRight, LayoutMarginTop + LayoutMarginBottom);
    public Vector2 LayoutPadding => new(LayoutPaddingLeft + LayoutPaddingRight, LayoutPaddingTop + LayoutPaddingBottom);
    public Vector2 LayoutBorder => new(LayoutBorderLeft + LayoutBorderRight, LayoutBorderTop + LayoutBorderBottom);

    public float LayoutWidth => NodeInternal.LayoutWidth;
    public float LayoutHeight => NodeInternal.LayoutHeight;
    public float LayoutX => NodeInternal.LayoutX;
    public float LayoutY => NodeInternal.LayoutY;
    public YgDirection LayoutDirection => NodeInternal.LayoutDirection.ToNfmDirection();
    public bool HadOverflow => NodeInternal.HadOverflow;
    public float LayoutMarginTop => NodeInternal.LayoutMarginTop;
    public float LayoutMarginBottom => NodeInternal.LayoutMarginBottom;
    public float LayoutMarginLeft => NodeInternal.LayoutMarginLeft;
    public float LayoutMarginRight => NodeInternal.LayoutMarginRight;
    public float LayoutPaddingTop => NodeInternal.LayoutPaddingTop;
    public float LayoutPaddingBottom => NodeInternal.LayoutPaddingBottom;
    public float LayoutPaddingLeft => NodeInternal.LayoutPaddingLeft;
    public float LayoutPaddingRight => NodeInternal.LayoutPaddingRight;
    public float LayoutBorderTop => NodeInternal.LayoutBorderTop;
    public float LayoutBorderBottom => NodeInternal.LayoutBorderBottom;
    public float LayoutBorderLeft => NodeInternal.LayoutBorderLeft;
    public float LayoutBorderRight => NodeInternal.LayoutBorderRight;

    public bool HasNewLayout
    {
        get => NodeInternal.HasNewLayout;
        set => NodeInternal.HasNewLayout = value;
    }

    public bool IsDirty
    {
        get => NodeInternal.IsDirty;
        set => NodeInternal.IsDirty = value;
    }

    public bool IsReferenceBaseline
    {
        set => NodeInternal.IsReferenceBaseline = value;
        get => NodeInternal.IsReferenceBaseline;
    }

    public YgNodeType NodeType
    {
        get => NodeInternal.NodeType.ToNfmNodeType();
        set => NodeInternal.NodeType = value.ToYogaNodeType();
    }

    public bool AlwaysFormsContainingBlock
    {
        get => NodeInternal.AlwaysFormsContainingBlock;
        set => NodeInternal.AlwaysFormsContainingBlock = value;
    }

    #endregion

    #region Style

    /// <summary>
    /// CSS: visibility - Controls whether the element is visible (visible/hidden/collapsed)
    /// </summary>
    [Property(defaultValue: Visibility.Visible)]
    public partial Visibility Visibility { get; set; }

    /// <summary>
    /// CSS: opacity - Sets the transparency level (0.0 = fully transparent, 1.0 = fully opaque)
    /// </summary>
    [Property(defaultValue: 1.0f)]
    public partial float Opacity { get; set; }

    // https://css-tricks.com/snippets/css/a-guide-to-flexbox/
    
    /// <summary>
    /// Property field for <see cref="Direction"/>.
    /// </summary>
    public static DirectProperty<Node, YgDirection> DirectionProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, YgDirection>(
            name:         nameof(Direction),
            getter:       node => node.NodeInternal.Direction.ToNfmDirection(),
            setter:       (node, value) => node.NodeInternal.Direction = value.ToYogaDirection(),
            defaultValue: YgDirection.Inherit);

    /// <summary>
    /// CSS: direction - Establishes the main-axis (ltr/rtl/inherit)
    /// </summary>
    [Property]
    public partial YgDirection Direction { get; set; }

    /// <summary>
    /// Property field for <see cref="FlexDirection"/>.
    /// </summary>
    public static DirectProperty<Node, YgFlexDirection> FlexDirectionProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, YgFlexDirection>(
            name:         nameof(FlexDirection),
            getter:       node => node.NodeInternal.FlexDirection.ToNfmFlexDirection(),
            setter:       (node, value) => node.NodeInternal.FlexDirection = value.ToYogaFlexDirection(),
            defaultValue: YgFlexDirection.Row);

    /// <summary>
    /// CSS: flex-direction - Establishes the main-axis (row/column/row-reverse/column-reverse)
    /// </summary>
    [Property]
    public partial YgFlexDirection FlexDirection { get; set; }

    /// <summary>
    /// Property field for <see cref="JustifyContent"/>.
    /// </summary>
    public static DirectProperty<Node, YgJustify> JustifyContentProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, YgJustify>(
            name:         nameof(JustifyContent),
            getter:       node => node.NodeInternal.JustifyContent.ToNfmJustify(),
            setter:       (node, value) => node.NodeInternal.JustifyContent = value.ToYogaJustify(),
            defaultValue: YgJustify.FlexStart);

    /// <summary>
    /// CSS: justify-content - Defines alignment along the main axis
    /// </summary>
    [Property]
    public partial YgJustify JustifyContent { get; set; }

    /// <summary>
    /// Property field for <see cref="AlignItems"/>.
    /// </summary>
    public static DirectProperty<Node, YgAlign> AlignItemsProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, YgAlign>(
            name:         nameof(AlignItems),
            getter:       node => node.NodeInternal.AlignItems.ToNfmAlign(),
            setter:       (node, value) => node.NodeInternal.AlignItems = value.ToYogaAlign(),
            defaultValue: YgAlign.Stretch);

    /// <summary>
    /// CSS: align-items - Defines default alignment for all children along the cross axis
    /// </summary>
    [Property]
    public partial YgAlign AlignItems { get; set; }

    /// <summary>
    /// Property field for <see cref="AlignSelf"/>.
    /// </summary>
    public static DirectProperty<Node, YgAlign> AlignSelfProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, YgAlign>(
            name:         nameof(AlignSelf),
            getter:       node => node.NodeInternal.AlignSelf.ToNfmAlign(),
            setter:       (node, value) => node.NodeInternal.AlignSelf = value.ToYogaAlign(),
            defaultValue: YgAlign.Auto);

    /// <summary>
    /// CSS: align-self - Allows a child to override the default cross-axis alignment
    /// </summary>
    [Property]
    public partial YgAlign AlignSelf { get; set; }

    /// <summary>
    /// Property field for <see cref="AlignContent"/>.
    /// </summary>
    public static DirectProperty<Node, YgAlign> AlignContentProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, YgAlign>(
            name:         nameof(AlignContent),
            getter:       node => node.NodeInternal.AlignContent.ToNfmAlign(),
            setter:       (node, value) => node.NodeInternal.AlignContent = value.ToYogaAlign(),
            defaultValue: YgAlign.FlexStart);

    /// <summary>
    /// CSS: align-content - Aligns flex container's lines when there is extra space in the cross-axis
    /// </summary>
    [Property]
    public partial YgAlign AlignContent { get; set; }

    /// <summary>
    /// Property field for <see cref="Position"/>.
    /// </summary>
    public static DirectProperty<Node, YgPositionType> PositionProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, YgPositionType>(
            name:         nameof(Position),
            getter:       node => node.NodeInternal.PositionType.ToNfmPositionType(),
            setter:       (node, value) => node.NodeInternal.PositionType = value.ToYogaPositionType(),
            defaultValue: YgPositionType.Static);

    /// <summary>
    /// CSS: position - Sets how an element is positioned (static/relative/absolute/fixed)
    /// </summary>
    [Property]
    public partial YgPositionType Position { get; set; }

    /// <summary>
    /// Property field for <see cref="FlexWrap"/>.
    /// </summary>
    public static DirectProperty<Node, YgWrap> FlexWrapProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, YgWrap>(
            name:         nameof(FlexWrap),
            getter:       node => node.NodeInternal.FlexWrap.ToNfmWrap(),
            setter:       (node, value) => node.NodeInternal.FlexWrap = value.ToYogaWrap(),
            defaultValue: YgWrap.NoWrap);

    /// <summary>
    /// CSS: flex-wrap - Controls whether flex items wrap onto multiple lines (nowrap/wrap/wrap-reverse)
    /// </summary>
    [Property]
    public partial YgWrap FlexWrap { get; set; }

    /// <summary>
    /// Property field for <see cref="Overflow"/>.
    /// </summary>
    public static DirectProperty<Node, YgOverflow> OverflowProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, YgOverflow>(
            name:         nameof(Overflow),
            getter:       node => node.NodeInternal.Overflow.ToNfmOverflow(),
            setter:       (node, value) => node.NodeInternal.Overflow = value.ToYogaOverflow(),
            defaultValue: YgOverflow.Visible);

    /// <summary>
    /// CSS: overflow - Controls what happens to content that is too big to fit (visible/hidden/scroll)
    /// </summary>
    [Property]
    public partial YgOverflow Overflow { get; set; }

    /// <summary>
    /// Property field for <see cref="Display"/>.
    /// </summary>
    public static DirectProperty<Node, YgDisplay> DisplayProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, YgDisplay>(
            name:         nameof(Display),
            getter:       node => node.NodeInternal.Display.ToNfmDisplay(),
            setter:       (node, value) => node.NodeInternal.Display = value.ToYogaDisplay(),
            defaultValue: YgDisplay.Flex);

    /// <summary>
    /// CSS: display - Defines the display type of the element (flex/none/block)
    /// </summary>
    [Property]
    public partial YgDisplay Display { get; set; }

    public sealed class PixelsConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string str)
            {
                var trimmed = str.AsSpan().Trim();
                if (trimmed.EndsWith("px"))
                {
                    if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                    {
                        return pointValue;
                    }
                }
                else
                {
                    if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                    {
                        return pointValue;
                    }
                }

                throw new FormatException($"Cannot convert '{str}' to pixels.");
            }
            return base.ConvertFrom(context, culture, value);
        }
    }

    public class PixelsOrUndefinedConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string str)
            {
                var trimmed = str.AsSpan().Trim();
                if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                if (trimmed.Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    return (float?)0;
                }
                if (trimmed.EndsWith("px"))
                {
                    if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                    {
                        return (float?)pointValue;
                    }
                }
                else
                {
                    if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                    {
                        return (float?)pointValue;
                    }
                }

                throw new FormatException($"Cannot convert '{str}' to pixels or undefined.");
            }
            return base.ConvertFrom(context, culture, value);
        }
    }

    /// <summary>
    /// Property field for <see cref="Flex"/>.
    /// </summary>
    public static DirectProperty<Node, float?> FlexProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float?>(
            name:         nameof(Flex),
            getter:       node => node.NodeInternal.Flex is var v && !float.IsNaN(v) ? v : null,
            setter:       (node, value) => node.NodeInternal.Flex = value ?? float.NaN);

    /// <summary>
    /// CSS: flex - Shorthand for flex-grow, flex-shrink, and flex-basis combined
    /// </summary>
    [Property]
    public partial float? Flex { get; set; }

    /// <summary>
    /// Property field for <see cref="FlexGrow"/>.
    /// </summary>
    public static DirectProperty<Node, float?> FlexGrowProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float?>(
            name:         nameof(FlexGrow),
            getter:       node => node.NodeInternal.FlexGrow is var v && !float.IsNaN(v) ? v : null,
            setter:       (node, value) => node.NodeInternal.FlexGrow = value ?? float.NaN);

    /// <summary>
    /// CSS: flex-grow - Defines the ability for a flex item to grow if necessary
    /// </summary>
    [Property]
    public partial float? FlexGrow { get; set; }

    /// <summary>
    /// Property field for <see cref="FlexShrink"/>.
    /// </summary>
    public static DirectProperty<Node, float?> FlexShrinkProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float?>(
            name:         nameof(FlexShrink),
            getter:       node => node.NodeInternal.FlexShrink is var v && !float.IsNaN(v) ? v : null,
            setter:       (node, value) => node.NodeInternal.FlexShrink = value ?? float.NaN);

    /// <summary>
    /// CSS: flex-shrink - Defines the ability for a flex item to shrink if necessary
    /// </summary>
    [Property]
    public partial float? FlexShrink { get; set; }

    [TypeConverter(typeof(MeasurementFlexBasisTypeConverter))]
    public struct MeasurementFlexBasis
    {
        public sealed class MeasurementFlexBasisTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                if (value is string str)
                {
                    var trimmed = str.AsSpan().Trim();
                    if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                    {
                        return Undefined;
                    }
                    if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
                    {
                        return Auto;
                    }
                    if (trimmed.Equals("max-content", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Equals("maxcontent", StringComparison.OrdinalIgnoreCase))
                    {
                        return MaxContent;
                    }
                    if (trimmed.Equals("stretch", StringComparison.OrdinalIgnoreCase))
                    {
                        return Stretch;
                    }
                    if (trimmed.EndsWith("%", StringComparison.OrdinalIgnoreCase))
                    {
                        if (float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentValue))
                        {
                            return Percent(percentValue);
                        }
                    }
                    else if (trimmed.EndsWith("px"))
                    {
                        if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }
                    else
                    {
                        if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }

                    throw new FormatException($"Cannot convert '{str}' to MeasurementFlexBasis. Expected 'auto', 'max-content', 'stretch', '<number>px', '<number>%', or '<number>'.");
                }
                return base.ConvertFrom(context, culture, value);
            }
        }

        internal YGValue InternalValue;
        public YgUnit Unit => InternalValue.unit.ToNfmUnit();
        public float Value => InternalValue.value;
        public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
        public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

        public static implicit operator MeasurementFlexBasis(float value)
        {
            return new MeasurementFlexBasis
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }
        public static implicit operator MeasurementFlexBasis(YGValue value)
        {
            return new MeasurementFlexBasis
            {
                InternalValue = value
            };
        }
        public static implicit operator YGValue(MeasurementFlexBasis value)
        {
            return value.InternalValue;
        }

        public static MeasurementFlexBasis Undefined = new()
        {
            InternalValue = new YGValue
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementFlexBasis Auto =>
            new()
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitFitContent
                }
            };

        public static MeasurementFlexBasis MaxContent =>
            new()
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitMaxContent
                }
            };

        public static MeasurementFlexBasis Stretch =>
            new()
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitStretch
                }
            };

        public static MeasurementFlexBasis Percent(float value)
        {
            return new MeasurementFlexBasis
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPercent,
                    value = value
                }
            };
        }

        public static MeasurementFlexBasis Point(float value)
        {
            return new MeasurementFlexBasis
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }

        public static MeasurementFlexBasis FitContent =>
            new()
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitFitContent
                }
            };

        public MeasurementFlexBasis Scale(float scale)
        {
            if (InternalValue.unit == YGUnit.YGUnitPoint)
            {
                return Point(InternalValue.value * scale);
            }

            return this;
        }
    }
    
    /// <summary>
    /// Property field for <see cref="FlexBasis"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementFlexBasis> FlexBasisProperty =
        AvaloniaProperty.Register<Node, MeasurementFlexBasis>(
            name:         nameof(FlexBasis),
            defaultValue: MeasurementFlexBasis.Undefined,
            onChanged:    (node, value) => node.NodeInternal.FlexBasis = value.Scale(XamlG.Scale));
    
    /// <summary>
    /// CSS: flex-basis - Defines the default size of an element before remaining space is distributed
    /// </summary>
    [Property]
    public partial MeasurementFlexBasis FlexBasis { get; set; }

    [TypeConverter(typeof(MeasurementMarginPositionTypeConverter))]
    public struct MeasurementMarginPosition
    {
        public sealed class MeasurementMarginPositionTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                if (value is string str)
                {
                    var trimmed = str.AsSpan().Trim();
                    if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                    {
                        return Undefined;
                    }
                    if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
                    {
                        return Auto;
                    }
                    if (trimmed.EndsWith("%", StringComparison.OrdinalIgnoreCase))
                    {
                        if (float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentValue))
                        {
                            return Percent(percentValue);
                        }
                    }
                    else if (trimmed.EndsWith("px"))
                    {
                        if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }
                    else
                    {
                        if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }

                    throw new FormatException($"Cannot convert '{str}' to MeasurementMarginPosition. Expected 'auto', '<number>px', '<number>%', or '<number>'.");
                }
                return base.ConvertFrom(context, culture, value);
            }
        }

        internal YGValue InternalValue;
        public YgUnit Unit => InternalValue.unit.ToNfmUnit();
        public float Value => InternalValue.value;
        public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
        public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

        public static implicit operator MeasurementMarginPosition(float value)
        {
            return new MeasurementMarginPosition
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }
        public static implicit operator MeasurementMarginPosition(YGValue value)
        {
            return new MeasurementMarginPosition
            {
                InternalValue = value
            };
        }
        public static implicit operator YGValue(MeasurementMarginPosition value)
        {
            return value.InternalValue;
        }

        public static MeasurementMarginPosition Auto =>
            new()
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitAuto
                }
            };

        public static MeasurementMarginPosition Undefined => new()
        {
            InternalValue = new YGValue
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementMarginPosition Percent(float value)
        {
            return new MeasurementMarginPosition
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPercent,
                    value = value
                }
            };
        }
        public static MeasurementMarginPosition Point(float value)
        {
            return new MeasurementMarginPosition
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }

        public MeasurementMarginPosition Scale(float scale)
        {
            if (InternalValue.unit == YGUnit.YGUnitPoint)
            {
                return Point(InternalValue.value * scale);
            }

            return this;
        }
    }

    /// <summary>
    /// Property field for <see cref="Left"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementMarginPosition> LeftProperty =
        AvaloniaProperty.Register<Node, MeasurementMarginPosition>(
            name:         nameof(Left),
            defaultValue: MeasurementMarginPosition.Undefined,
            onChanged:    (node, value) => node.NodeInternal.Left = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: left - Specifies the left position of a positioned element
    /// </summary>
    [Property]
    public partial MeasurementMarginPosition Left { get; set; }

    /// <summary>
    /// Property field for <see cref="Top"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementMarginPosition> TopProperty =
        AvaloniaProperty.Register<Node, MeasurementMarginPosition>(
            name:         nameof(Top),
            defaultValue: MeasurementMarginPosition.Undefined,
            onChanged:    (node, value) => node.NodeInternal.Top = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: top - Specifies the top position of a positioned element
    /// </summary>
    [Property]
    public partial MeasurementMarginPosition Top { get; set; }

    /// <summary>
    /// Property field for <see cref="Right"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementMarginPosition> RightProperty =
        AvaloniaProperty.Register<Node, MeasurementMarginPosition>(
            name:         nameof(Right),
            defaultValue: MeasurementMarginPosition.Undefined,
            onChanged:    (node, value) => node.NodeInternal.Right = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: right - Specifies the right position of a positioned element
    /// </summary>
    [Property]
    public partial MeasurementMarginPosition Right { get; set; }

    /// <summary>
    /// Property field for <see cref="Bottom"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementMarginPosition> BottomProperty =
        AvaloniaProperty.Register<Node, MeasurementMarginPosition>(
            name:         nameof(Bottom),
            defaultValue: MeasurementMarginPosition.Undefined,
            onChanged:    (node, value) => node.NodeInternal.Bottom = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: bottom - Specifies the bottom position of a positioned element
    /// </summary>
    [Property]
    public partial MeasurementMarginPosition Bottom { get; set; }

    [TypeConverter(typeof(MeasurementMultiMarginTypeConverter))]
    public struct MeasurementMultiMargin
    {
        public class MeasurementMultiMarginTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                if (value is string str)
                {
                    var trimmed = str.AsSpan().Trim();
                    if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                    {
                        return All(MeasurementMarginPosition.Undefined);
                    }
                    if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
                    {
                        return All(MeasurementMarginPosition.Auto);
                    }

                    var idx = 0;
                    var sides = new InlineArray4<MeasurementMarginPosition>();
                    foreach (var elementRange in trimmed.SplitAny(',', ' '))
                    {
                        var element = trimmed[elementRange];

                        if (element.EndsWith("%", StringComparison.OrdinalIgnoreCase))
                        {
                            if (float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentValue))
                            {
                                sides[idx] = MeasurementMarginPosition.Percent(percentValue);
                            }
                        }
                        else if (element.EndsWith("px"))
                        {
                            if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                            {
                                sides[idx] = MeasurementMarginPosition.Point(pointValue);
                            }
                        }
                        else
                        {
                            if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                            {
                                sides[idx] = MeasurementMarginPosition.Point(pointValue);
                            }
                        }

                        idx++;
                    }

                    if (idx == 1)
                    {
                        return All(sides[0]);
                    }

                    if (idx == 2)
                    {
                        return new MeasurementMultiMargin
                        {
                            Top = sides[0],
                            Bottom = sides[0],
                            Left = sides[1],
                            Right = sides[1]
                        };
                    }

                    if (idx == 4)
                    {
                        return new MeasurementMultiMargin
                        {
                            Top = sides[0],
                            Right = sides[1],
                            Bottom = sides[2],
                            Left = sides[3]
                        };
                    }

                    throw new FormatException($"Cannot convert '{str}' to MeasurementMultiMargin. Expected 'auto', '<number>px', '<number>%', or '<number>', as 1, 2 or 4 elements, in order top-right-bottom-left, separated by comma or space.");
                }
                return base.ConvertFrom(context, culture, value);
            }
        }

        public InlineArray4<MeasurementMarginPosition> Sides;
        public MeasurementMarginPosition Top
        {
            get => Sides[0];
            set => Sides[0] = value;
        }
        public MeasurementMarginPosition Bottom
        {
            get => Sides[1];
            set => Sides[1] = value;
        }
        public MeasurementMarginPosition Left
        {
            get => Sides[2];
            set => Sides[2] = value;
        }
        public MeasurementMarginPosition Right
        {
            get => Sides[3];
            set => Sides[3] = value;
        }

        public static MeasurementMultiMargin Auto => MeasurementMarginPosition.Auto;

        public static MeasurementMultiMargin Undefined => MeasurementMarginPosition.Undefined;

        public static MeasurementMultiMargin All(MeasurementMarginPosition value)
        {
            return new MeasurementMultiMargin
            {
                Top = value,
                Bottom = value,
                Left = value,
                Right = value
            };
        }

        public static implicit operator MeasurementMultiMargin(MeasurementMarginPosition value) => All(value);
    }

    /// <summary>
    /// CSS: margin - Shorthand for setting all margin values (top, right, bottom, left)
    /// </summary>
    public MeasurementMultiMargin Margin
    {
        set
        {
            MarginLeft = value.Left;
            MarginRight = value.Right;
            MarginTop = value.Top;
            MarginBottom = value.Bottom;
        }
    }

    /// <summary>
    /// Property field for <see cref="MarginTop"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementMarginPosition> MarginTopProperty =
        AvaloniaProperty.Register<Node, MeasurementMarginPosition>(
            name:         nameof(MarginTop),
            defaultValue: MeasurementMarginPosition.Undefined,
            onChanged:    (node, value) => node.NodeInternal.MarginTop = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: margin-top - Sets the top margin space outside the element
    /// </summary>
    [Property]
    public partial MeasurementMarginPosition MarginTop { get; set; }

    /// <summary>
    /// Property field for <see cref="MarginBottom"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementMarginPosition> MarginBottomProperty =
        AvaloniaProperty.Register<Node, MeasurementMarginPosition>(
            name:         nameof(MarginBottom),
            defaultValue: MeasurementMarginPosition.Undefined,
            onChanged:    (node, value) => node.NodeInternal.MarginBottom = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: margin-bottom - Sets the bottom margin space outside the element
    /// </summary>
    [Property]
    public partial MeasurementMarginPosition MarginBottom { get; set; }

    /// <summary>
    /// Property field for <see cref="MarginLeft"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementMarginPosition> MarginLeftProperty =
        AvaloniaProperty.Register<Node, MeasurementMarginPosition>(
            name:         nameof(MarginLeft),
            defaultValue: MeasurementMarginPosition.Undefined,
            onChanged:    (node, value) => node.NodeInternal.MarginLeft = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: margin-left - Sets the left margin space outside the element
    /// </summary>
    [Property]
    public partial MeasurementMarginPosition MarginLeft { get; set; }

    /// <summary>
    /// Property field for <see cref="MarginRight"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementMarginPosition> MarginRightProperty =
        AvaloniaProperty.Register<Node, MeasurementMarginPosition>(
            name:         nameof(MarginRight),
            defaultValue: MeasurementMarginPosition.Undefined,
            onChanged:    (node, value) => node.NodeInternal.MarginRight = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: margin-right - Sets the right margin space outside the element
    /// </summary>
    [Property]
    public partial MeasurementMarginPosition MarginRight { get; set; }

    [TypeConverter(typeof(MeasurementPaddingTypeConverter))]
    public struct MeasurementPadding
    {
        public sealed class MeasurementPaddingTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                if (value is string str)
                {
                    var trimmed = str.AsSpan().Trim();
                    if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                    {
                        return Undefined;
                    }
                    if (trimmed.EndsWith("%", StringComparison.OrdinalIgnoreCase))
                    {
                        if (float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentValue))
                        {
                            return Percent(percentValue);
                        }
                    }
                    else if (trimmed.EndsWith("px"))
                    {
                        if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }
                    else
                    {
                        if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }

                    throw new FormatException($"Cannot convert '{str}' to MeasurementPadding. Expected '<number>px', '<number>%', or '<number>'.");
                }
                return base.ConvertFrom(context, culture, value);
            }
        }

        internal YGValue InternalValue;
        public YgUnit Unit => InternalValue.unit.ToNfmUnit();
        public float Value => InternalValue.value;
        public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
        public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

        public static implicit operator MeasurementPadding(float value)
        {
            return new MeasurementPadding
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }
        public static implicit operator MeasurementPadding(YGValue value)
        {
            return new MeasurementPadding
            {
                InternalValue = value
            };
        }
        public static implicit operator YGValue(MeasurementPadding value)
        {
            return value.InternalValue;
        }

        public static MeasurementPadding Undefined => new()
        {
            InternalValue = new YGValue
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementPadding Percent(float value)
        {
            return new MeasurementPadding
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPercent,
                    value = value
                }
            };
        }
        public static MeasurementPadding Point(float value)
        {
            return new MeasurementPadding
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }

        public MeasurementPadding Scale(float scale)
        {
            if (InternalValue.unit == YGUnit.YGUnitPoint)
            {
                return Point(InternalValue.value * scale);
            }

            return this;
        }
    }

    [TypeConverter(typeof(MeasurementMultiPaddingTypeConverter))]
    public struct MeasurementMultiPadding
    {
        public class MeasurementMultiPaddingTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                if (value is string str)
                {
                    var trimmed = str.AsSpan().Trim();
                    if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                    {
                        return All(MeasurementPadding.Undefined);
                    }

                    var idx = 0;
                    var sides = new InlineArray4<MeasurementPadding>();
                    foreach (var elementRange in trimmed.SplitAny(',', ' '))
                    {
                        var element = trimmed[elementRange];

                        if (element.EndsWith("%", StringComparison.OrdinalIgnoreCase))
                        {
                            if (float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentValue))
                            {
                                sides[idx] = MeasurementPadding.Percent(percentValue);
                            }
                        }
                        else if (element.EndsWith("px"))
                        {
                            if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                            {
                                sides[idx] = MeasurementPadding.Point(pointValue);
                            }
                        }
                        else
                        {
                            if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                            {
                                sides[idx] = MeasurementPadding.Point(pointValue);
                            }
                        }

                        idx++;
                    }

                    if (idx == 1)
                    {
                        return All(sides[0]);
                    }

                    if (idx == 2)
                    {
                        return new MeasurementMultiPadding
                        {
                            Top = sides[0],
                            Bottom = sides[0],
                            Left = sides[1],
                            Right = sides[1]
                        };
                    }

                    if (idx == 4)
                    {
                        return new MeasurementMultiPadding
                        {
                            Top = sides[0],
                            Right = sides[1],
                            Bottom = sides[2],
                            Left = sides[3]
                        };
                    }

                    throw new FormatException($"Cannot convert '{str}' to MeasurementMultiMargin. Expected '<number>px', '<number>%', or '<number>', as 1, 2 or 4 elements, in order top-right-bottom-left, separated by comma or space.");
                }
                return base.ConvertFrom(context, culture, value);
            }
        }

        public InlineArray4<MeasurementPadding> Sides;
        public MeasurementPadding Top
        {
            get => Sides[0];
            set => Sides[0] = value;
        }
        public MeasurementPadding Bottom
        {
            get => Sides[1];
            set => Sides[1] = value;
        }
        public MeasurementPadding Left
        {
            get => Sides[2];
            set => Sides[2] = value;
        }
        public MeasurementPadding Right
        {
            get => Sides[3];
            set => Sides[3] = value;
        }

        public static MeasurementMultiPadding Undefined => MeasurementPadding.Undefined;

        public static MeasurementMultiPadding All(MeasurementPadding value)
        {
            return new MeasurementMultiPadding
            {
                Top = value,
                Bottom = value,
                Left = value,
                Right = value
            };
        }

        public static implicit operator MeasurementMultiPadding(MeasurementPadding value) => All(value);
    }

    /// <summary>
    /// CSS: padding - Shorthand for setting all padding values (top, right, bottom, left)
    /// </summary>
    public MeasurementMultiPadding Padding
    {
        set
        {
            PaddingLeft = value.Left;
            PaddingRight = value.Right;
            PaddingTop = value.Top;
            PaddingBottom = value.Bottom;
        }
    }

    /// <summary>
    /// Property field for <see cref="PaddingTop"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementPadding> PaddingTopProperty =
        AvaloniaProperty.Register<Node, MeasurementPadding>(
            name:         nameof(PaddingTop),
            defaultValue: MeasurementPadding.Undefined,
            onChanged:    (node, value) => node.NodeInternal.PaddingTop = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: padding-top - Sets the top padding space inside the element
    /// </summary>
    [Property]
    public partial MeasurementPadding PaddingTop { get; set; }

    /// <summary>
    /// Property field for <see cref="PaddingBottom"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementPadding> PaddingBottomProperty =
        AvaloniaProperty.Register<Node, MeasurementPadding>(
            name:         nameof(PaddingBottom),
            defaultValue: MeasurementPadding.Undefined,
            onChanged:    (node, value) => node.NodeInternal.PaddingBottom = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: padding-bottom - Sets the bottom padding space inside the element
    /// </summary>
    [Property]
    public partial MeasurementPadding PaddingBottom { get; set; }

    /// <summary>
    /// Property field for <see cref="PaddingLeft"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementPadding> PaddingLeftProperty =
        AvaloniaProperty.Register<Node, MeasurementPadding>(
            name:         nameof(PaddingLeft),
            defaultValue: MeasurementPadding.Undefined,
            onChanged:    (node, value) => node.NodeInternal.PaddingLeft = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: padding-left - Sets the left padding space inside the element
    /// </summary>
    [Property]
    public partial MeasurementPadding PaddingLeft { get; set; }

    /// <summary>
    /// Property field for <see cref="PaddingRight"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementPadding> PaddingRightProperty =
        AvaloniaProperty.Register<Node, MeasurementPadding>(
            name:         nameof(PaddingRight),
            defaultValue: MeasurementPadding.Undefined,
            onChanged:    (node, value) => node.NodeInternal.PaddingRight = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: padding-right - Sets the right padding space inside the element
    /// </summary>
    [Property]
    public partial MeasurementPadding PaddingRight { get; set; }

    [TypeConverter(typeof(MeasurementMultiBorderTypeConverter))]
    public struct MeasurementMultiBorder
    {
        public class MeasurementMultiBorderTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                if (value is string str)
                {
                    var trimmed = str.AsSpan().Trim();
                    if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                    {
                        return Undefined;
                    }

                    var idx = 0;
                    var sides = new InlineArray4<float>();
                    foreach (var elementRange in trimmed.SplitAny(',', ' '))
                    {
                        var element = trimmed[elementRange];

                        if (element.EndsWith("px"))
                        {
                            if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                            {
                                sides[idx] = pointValue;
                            }
                        }
                        else
                        {
                            if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                            {
                                sides[idx] = pointValue;
                            }
                        }

                        idx++;
                    }

                    if (idx == 1)
                    {
                        return All(sides[0]);
                    }

                    if (idx == 2)
                    {
                        return new MeasurementMultiBorder
                        {
                            Top = sides[0],
                            Bottom = sides[0],
                            Left = sides[1],
                            Right = sides[1]
                        };
                    }

                    if (idx == 4)
                    {
                        return new MeasurementMultiBorder
                        {
                            Top = sides[0],
                            Right = sides[1],
                            Bottom = sides[2],
                            Left = sides[3]
                        };
                    }

                    throw new FormatException($"Cannot convert '{str}' to MeasurementMultiMargin. Expected '<number>px' or '<number>', as 1, 2 or 4 elements, in order top-right-bottom-left, separated by comma or space.");
                }
                return base.ConvertFrom(context, culture, value);
            }
        }

        public InlineArray4<float?> Sides;
        public float? Top
        {
            get => Sides[0];
            set => Sides[0] = value;
        }
        public float? Bottom
        {
            get => Sides[1];
            set => Sides[1] = value;
        }
        public float? Left
        {
            get => Sides[2];
            set => Sides[2] = value;
        }
        public float? Right
        {
            get => Sides[3];
            set => Sides[3] = value;
        }

        public static MeasurementMultiBorder Undefined => All(null);

        public static MeasurementMultiBorder All(float? value)
        {
            return new MeasurementMultiBorder
            {
                Top = value,
                Bottom = value,
                Left = value,
                Right = value
            };
        }

        public static implicit operator MeasurementMultiBorder(float? value) => All(value);
    }

    /// <summary>
    /// CSS: border - Shorthand for setting all border widths
    /// </summary>
    public MeasurementMultiBorder Border
    {
        set
        {
            BorderLeft = value.Left;
            BorderRight = value.Right;
            BorderTop = value.Top;
            BorderBottom = value.Bottom;
        }
    }

    /// <summary>
    /// Property field for <see cref="BorderTop"/>.
    /// </summary>
    public static readonly StyledProperty<float?> BorderTopProperty =
        AvaloniaProperty.Register<Node, float?>(
            name:         nameof(BorderTop),
            onChanged:    (node, value) => node.NodeInternal.BorderTop = (value * XamlG.Scale) ?? YG.YGUndefined);

    /// <summary>
    /// CSS: border-top-width - Sets the width of the top border
    /// </summary>
    [TypeConverter(typeof(PixelsOrUndefinedConverter))]
    [Property]
    public partial float? BorderTop { get; set; }

    /// <summary>
    /// Property field for <see cref="BorderBottom"/>.
    /// </summary>
    public static readonly StyledProperty<float?> BorderBottomProperty =
        AvaloniaProperty.Register<Node, float?>(
            name:         nameof(BorderBottom),
            onChanged:    (node, value) => node.NodeInternal.BorderBottom = (value * XamlG.Scale) ?? YG.YGUndefined);

    /// <summary>
    /// CSS: border-bottom-width - Sets the width of the bottom border
    /// </summary>
    [TypeConverter(typeof(PixelsOrUndefinedConverter))]
    [Property]
    public partial float? BorderBottom { get; set; }

    /// <summary>
    /// Property field for <see cref="BorderLeft"/>.
    /// </summary>
    public static readonly StyledProperty<float?> BorderLeftProperty =
        AvaloniaProperty.Register<Node, float?>(
            name:         nameof(BorderLeft),
            onChanged:    (node, value) => node.NodeInternal.BorderLeft = (value * XamlG.Scale) ?? YG.YGUndefined);

    /// <summary>
    /// CSS: border-left-width - Sets the width of the left border
    /// </summary>
    [TypeConverter(typeof(PixelsOrUndefinedConverter))]
    [Property]
    public partial float? BorderLeft { get; set; }

    /// <summary>
    /// Property field for <see cref="BorderRight"/>.
    /// </summary>
    public static readonly StyledProperty<float?> BorderRightProperty =
        AvaloniaProperty.Register<Node, float?>(
            name:         nameof(BorderRight),
            onChanged:    (node, value) => node.NodeInternal.BorderRight = (value * XamlG.Scale) ?? YG.YGUndefined);

    /// <summary>
    /// CSS: border-right-width - Sets the width of the right border
    /// </summary>
    [TypeConverter(typeof(PixelsOrUndefinedConverter))]
    [Property]
    public partial float? BorderRight { get; set; }

    [TypeConverter(typeof(MeasurementGapTypeConverter))]
    public struct MeasurementGap
    {
        public class MeasurementGapTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                if (value is string str)
                {
                    var trimmed = str.AsSpan().Trim();
                    if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                    {
                        return Undefined;
                    }
                    if (trimmed.EndsWith("%", StringComparison.OrdinalIgnoreCase))
                    {
                        if (float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentValue))
                        {
                            return Percent(percentValue);
                        }
                    }
                    else if (trimmed.EndsWith("px"))
                    {
                        if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }
                    else
                    {
                        if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }

                    throw new FormatException($"Cannot convert '{str}' to MeasurementGap. Expected '<number>px', '<number>%', or '<number>'.");
                }
                return base.ConvertFrom(context, culture, value);
            }
        }

        internal YGValue InternalValue;
        public YgUnit Unit => InternalValue.unit.ToNfmUnit();
        public float Value => InternalValue.value;
        public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
        public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

        public static implicit operator MeasurementGap(float value)
        {
            return new MeasurementGap
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }
        public static implicit operator MeasurementGap(YGValue value)
        {
            return new MeasurementGap
            {
                InternalValue = value
            };
        }
        public static implicit operator YGValue(MeasurementGap value)
        {
            return value.InternalValue;
        }

        public static MeasurementGap Undefined => new()
        {
            InternalValue = new YGValue
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementGap Percent(float value)
        {
            return new MeasurementGap
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPercent,
                    value = value
                }
            };
        }
        public static MeasurementGap Point(float value)
        {
            return new MeasurementGap
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }

        public MeasurementGap Scale(float scale)
        {
            if (InternalValue.unit == YGUnit.YGUnitPoint)
            {
                return Point(InternalValue.value * scale);
            }

            return this;
        }
    }

    /// <summary>
    /// CSS: gap - Shorthand for setting row-gap and column-gap
    /// </summary>
    public MeasurementGap Gap
    {
        set
        {
            GapColumn = value;
            GapRow = value;
        }
    }

    /// <summary>
    /// Property field for <see cref="GapColumn"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementGap> GapColumnProperty =
        AvaloniaProperty.Register<Node, MeasurementGap>(
            name:         nameof(GapColumn),
            defaultValue: MeasurementGap.Undefined,
            onChanged:    (node, value) => node.NodeInternal.GapColumn = value);

    /// <summary>
    /// CSS: column-gap - Sets the gap between columns in a flex container
    /// </summary>
    [Property]
    public partial MeasurementGap GapColumn { get; set; }

    /// <summary>
    /// Property field for <see cref="GapRow"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementGap> GapRowProperty =
        AvaloniaProperty.Register<Node, MeasurementGap>(
            name:         nameof(GapRow),
            defaultValue: MeasurementGap.Undefined,
            onChanged:    (node, value) => node.NodeInternal.GapRow = value);

    /// <summary>
    /// CSS: row-gap - Sets the gap between rows in a flex container
    /// </summary>
    [Property]
    public partial MeasurementGap GapRow { get; set; }

    /// <summary>
    /// Property field for <see cref="BoxSizing"/>.
    /// </summary>
    public static DirectProperty<Node, YgBoxSizing> BoxSizingProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, YgBoxSizing>(
            name:         nameof(BoxSizing),
            getter:       node => node.NodeInternal.BoxSizing.ToNfmBoxSizing(),
            setter:       (node, value) => node.NodeInternal.BoxSizing = value.ToYogaBoxSizing(),
            defaultValue: YgBoxSizing.BorderBox);

    /// <summary>
    /// CSS: box-sizing - Defines how width/height calculations include padding/border (content-box/border-box)
    /// </summary>
    [Property]
    public partial YgBoxSizing BoxSizing { get; set; }

    [TypeConverter(typeof(MeasurementWidthHeightTypeConverter))]
    public struct MeasurementWidthHeight
    {
        /// <summary>
        /// Type converter for Node.MeasurementWidthHeight.
        /// Parses values like "100", "50%", "auto", "stretch", etc.
        /// </summary>
        public class MeasurementWidthHeightTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                if (value is string str)
                {
                    var trimmed = str.Trim();

                    if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
                    {
                        return Auto();
                    }
                    if (trimmed.Equals("stretch", StringComparison.OrdinalIgnoreCase))
                    {
                        return Stretch();
                    }
                    if (trimmed.Equals("fit-content", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Equals("fitcontent", StringComparison.OrdinalIgnoreCase))
                    {
                        return FitContent();
                    }
                    if (trimmed.Equals("max-content", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Equals("maxcontent", StringComparison.OrdinalIgnoreCase))
                    {
                        return MaxContent();
                    }
                    if (trimmed.EndsWith('%'))
                    {
                        if (float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentValue))
                        {
                            return Percent(percentValue);
                        }
                    }
                    else if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                    {
                        if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                        {
                            return Point(floatValue);
                        }
                    }
                    else if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                    {
                        return Point(floatValue);
                    }

                    throw new FormatException($"Cannot convert {str} to MeasurementWidthHeight. Expected a number, percentage, 'auto', 'stretch', 'fit-content', or 'max-content'.");
                }
                return base.ConvertFrom(context, culture, value);
            }
        }

        internal YGValue InternalValue;
        public YgUnit Unit => InternalValue.unit.ToNfmUnit();
        public float Value => InternalValue.value;
        public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
        public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

        public static implicit operator MeasurementWidthHeight(float value)
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }
        public static implicit operator MeasurementWidthHeight(YGValue value)
        {
            return new MeasurementWidthHeight
            {
                InternalValue = value
            };
        }
        public static implicit operator YGValue(MeasurementWidthHeight value)
        {
            return value.InternalValue;
        }

        public static MeasurementWidthHeight Undefined => new()
        {
            InternalValue = new YGValue
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementWidthHeight Auto()
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitAuto
                }
            };
        }
        public static MeasurementWidthHeight Percent(float value)
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPercent,
                    value = value
                }
            };
        }
        public static MeasurementWidthHeight Point(float value)
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }

        public static MeasurementWidthHeight FitContent()
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitFitContent
                }
            };
        }
        public static MeasurementWidthHeight MaxContent()
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitMaxContent
                }
            };
        }

        public static MeasurementWidthHeight Stretch()
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitStretch
                }
            };
        }

        public MeasurementWidthHeight Scale(float scale)
        {
            if (InternalValue.unit == YGUnit.YGUnitPoint)
            {
                return Point(InternalValue.value * scale);
            }

            return this;
        }
    }

    /// <summary>
    /// Property field for <see cref="Width"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementWidthHeight> WidthProperty =
        AvaloniaProperty.Register<Node, MeasurementWidthHeight>(
            name:         nameof(Width),
            defaultValue: MeasurementWidthHeight.Undefined,
            onChanged:    (node, value) => node.NodeInternal.Width = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: width - Sets the width of the element
    /// </summary>
    [Property]
    public partial MeasurementWidthHeight Width { get; set; }

    /// <summary>
    /// Property field for <see cref="Height"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementWidthHeight> HeightProperty =
        AvaloniaProperty.Register<Node, MeasurementWidthHeight>(
            name:         nameof(Height),
            defaultValue: MeasurementWidthHeight.Undefined,
            onChanged:    (node, value) => node.NodeInternal.Height = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: height - Sets the height of the element
    /// </summary>
    [Property]
    public partial MeasurementWidthHeight Height { get; set; }

    /// <summary>
    /// Property field for <see cref="MinWidth"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementWidthHeight> MinWidthProperty =
        AvaloniaProperty.Register<Node, MeasurementWidthHeight>(
            name:         nameof(MinWidth),
            defaultValue: MeasurementWidthHeight.Undefined,
            onChanged:    (node, value) => node.NodeInternal.MinWidth = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: min-width - Sets the minimum width of the element
    /// </summary>
    [Property]
    public partial MeasurementWidthHeight MinWidth { get; set; }

    /// <summary>
    /// Property field for <see cref="MinHeight"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementWidthHeight> MinHeightProperty =
        AvaloniaProperty.Register<Node, MeasurementWidthHeight>(
            name:         nameof(MinHeight),
            defaultValue: MeasurementWidthHeight.Undefined,
            onChanged:    (node, value) => node.NodeInternal.MinHeight = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: min-height - Sets the minimum height of the element
    /// </summary>
    [Property]
    public partial MeasurementWidthHeight MinHeight { get; set; }

    /// <summary>
    /// Property field for <see cref="MaxWidth"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementWidthHeight> MaxWidthProperty =
        AvaloniaProperty.Register<Node, MeasurementWidthHeight>(
            name:         nameof(MaxWidth),
            defaultValue: MeasurementWidthHeight.Undefined,
            onChanged:    (node, value) => node.NodeInternal.MaxWidth = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: max-width - Sets the maximum width of the element
    /// </summary>
    [Property]
    public partial MeasurementWidthHeight MaxWidth { get; set; }

    /// <summary>
    /// Property field for <see cref="MaxHeight"/>.
    /// </summary>
    public static readonly StyledProperty<MeasurementWidthHeight> MaxHeightProperty =
        AvaloniaProperty.Register<Node, MeasurementWidthHeight>(
            name:         nameof(MaxHeight),
            defaultValue: MeasurementWidthHeight.Undefined,
            onChanged:    (node, value) => node.NodeInternal.MaxHeight = value.Scale(XamlG.Scale));

    /// <summary>
    /// CSS: max-height - Sets the maximum height of the element
    /// </summary>
    [Property]
    public partial MeasurementWidthHeight MaxHeight { get; set; }

    /// <summary>
    /// Property field for <see cref="AspectRatio"/>.
    /// </summary>
    public static DirectProperty<Node, float?> AspectRatioProperty { get; } =
        AvaloniaProperty.RegisterDirect<Node, float?>(
            name:         nameof(AspectRatio),
            getter:       node => node.NodeInternal.AspectRatio is var v && !float.IsNaN(v) ? v : null,
            setter:       (node, value) => node.NodeInternal.AspectRatio = value ?? float.NaN);

    /// <summary>
    /// CSS: aspect-ratio - Sets the preferred aspect ratio for the element (width / height)
    /// </summary>
    [TypeConverter(typeof(PixelsConverter))]
    [Property]
    public partial float? AspectRatio { get; set; }

    #endregion

    private float _lastScale = 1f;

    static Node()
    {
        Config = YGConfigPtr.GetDefault();
        Config.UseWebDefaults = true;
    }

    ~Node()
    {
        Dispose(false);
    }

    private void ReleaseUnmanagedResources()
    {
        NodeInternal.Dispose();
    }

    protected virtual void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
            // Free any other managed objects here.
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Do not use directly.
    /// </summary>
    /// <returns>true if scale changed</returns>
    internal bool Rescale()
    {
        if (Math.Abs(_lastScale - XamlG.Scale) > 0.001f)
        {
            // Update all size related properties to trigger re-calculation with new scale
#pragma warning disable CA2245
            Width = Width;
            Height = Height;
            MinWidth = MinWidth;
            MinHeight = MinHeight;
            MaxWidth = MaxWidth;
            MaxHeight = MaxHeight;
            MarginTop = MarginTop;
            MarginBottom = MarginBottom;
            MarginLeft = MarginLeft;
            MarginRight = MarginRight;
            PaddingTop = PaddingTop;
            PaddingBottom = PaddingBottom;
            PaddingLeft = PaddingLeft;
            PaddingRight = PaddingRight;
            BorderTop = BorderTop;
            BorderBottom = BorderBottom;
            BorderLeft = BorderLeft;
            BorderRight = BorderRight;
            GapColumn = GapColumn;
            GapRow = GapRow;
            FlexBasis = FlexBasis;
            Left = Left;
            Top = Top;
            Right = Right;
            Bottom = Bottom;
#pragma warning restore CA2245

            _lastScale = XamlG.Scale;

            return true;
        }

        return false;
    }

    protected virtual void OnScaleChanged()
    {
    }

    /// <summary>
    /// DO NOT OVERRIDE. Override OnScaleChanged() instead.
    /// </summary>
    internal virtual void RescaleRecursive()
    {
        if (Rescale())
        {
            OnScaleChanged();
        }
    }

    protected virtual void RenderBackground(Vector2 position, Vector2 size)
    {
    }

    protected virtual void RenderBorder(Vector2 position, Vector2 size)
    {
    }

    protected virtual void RenderContent(Vector2 position, Vector2 size)
    {
    }

    protected virtual void Render()
    {
        XamlG.Alpha = Opacity;
        RenderBackground(LayoutPaddingPosition, LayoutPaddingSize);
        RenderBorder(LayoutBorderPosition, LayoutBorderSize);
        RenderContent(LayoutContentPosition, LayoutContentSize);
        XamlG.Alpha = 1f;
    }

    internal virtual void RenderRecursive(Vector2 root, float rootOpacity = 1f)
    {
        _root = root;
        if (Display != YgDisplay.None && Visibility == Visibility.Visible && Opacity > 0f)
        {
            var ownOpacity = rootOpacity * Opacity;
            XamlG.Alpha = ownOpacity;
            Render();
            XamlG.Alpha = 1f;
        }
    }

    protected virtual void GameTick()
    {
    }

    public void LayoutAndRender(Vector2 availableSize, Vector2? origin = null)
    {
#if DEBUG
        __INTERNAL_YogaRootsThisFrame.Add(this);
#endif

        RescaleRecursive();
        NodeInternal.CalculateLayout(availableSize, YGDirection.YGDirectionLTR);
        AnimationFrameBegan?.Invoke();
        RenderRecursive(origin ?? Vector2.Zero);
    }

    /// <summary>
    /// DO NOT OVERRIDE. Override GameTick() instead.
    /// </summary>
    public virtual void Update()
    {
        GameTick();
    }

    public void NotifyAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        Mounted.Trigger();
    }

    public override void NotifyDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        Unounted.Trigger();
    }

    public AnimationTrigger Mounted { get; } = new();
    public AnimationTrigger Unounted { get; } = new();
}
