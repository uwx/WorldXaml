using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace WorldXaml.UI.Yoga;

/// <summary>
/// Represents a container node that can hold multiple child nodes.
/// </summary>
[DebuggerDisplay("{DebugToString()}")]
public class Box : Node
{
    [Content]
    public NodeChildCollection Children { get; }

    public override IReadOnlyList<ILogical> LogicalChildren => Children;

    public Box()
    {
        Children = new NodeChildCollection(this);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public new string DebugToString()
    {
        var sb = new StringBuilder();
        sb.Append($"Node(Name={Name}, LayoutX={LayoutX}, LayoutY={LayoutY}, LayoutWidth={LayoutWidth}, LayoutHeight={LayoutHeight})");
        foreach (var child in Children)
        {
            sb.AppendLine();
            sb.Append('{');
            sb.Append(child.DebugToString().Replace("\n", "\n  "));
            sb.Append('}');
        }
        return sb.ToString();
    }

    internal override void RescaleRecursive()
    {
        if (Rescale())
        {
            OnScaleChanged();
            foreach (var child in Children)
            {
                child.RescaleRecursive();
            }
        }
    }

    public sealed override void Update()
    {
        base.Update();
        foreach (var child in Children)
        {
            child.Update();
        }
    }

    internal override void RenderRecursive(Vector2 root, float rootOpacity = 1)
    {
        _root = root;
        if (Display != YgDisplay.None && Visibility == Visibility.Visible && Opacity > 0f)
        {
            var ownOpacity = rootOpacity * Opacity;
            XamlG.Alpha = ownOpacity;
            Render();
            foreach (var child in Children)
            {
                child.RenderRecursive(root + new Vector2(LayoutX, LayoutY), ownOpacity); // todo should this be LayoutContentPosition
            }
            XamlG.Alpha = 1f;
        }
    }
}