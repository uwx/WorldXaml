using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Yoga;

namespace WorldXaml.UI.Yoga;

/// <summary>
/// Represents a container node that can hold multiple child nodes but does not participate in layouting by itself.
/// </summary>
[DebuggerDisplay("{DebugToString()}")]
public class ContentsPanel : PlainNode
{
    [Content]
    public NodeChildCollection Children { get; }

    public override IReadOnlyList<ILogical> LogicalChildren => Children;
    public override IReadOnlyList<Visual> VisualChildren => Children;

    public ContentsPanel()
    {
        Children = new NodeChildCollection(this);
        NodeInternal.Display = YGDisplay.YGDisplayContents;
    }

    /// <summary>
    /// Renders children, offsetting the context by this node's Yoga layout position
    /// so that the visual tree stays aligned with the Yoga layout tree.
    /// </summary>
    public override void Render(RenderContext context)
    {
        var adjusted = new RenderContext(
            context.TopLeft + new Vector2(NodeInternal.LayoutX, NodeInternal.LayoutY),
            context.InheritedOpacity);
        foreach (var child in VisualChildren)
            child.Render(adjusted);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override string DebugToString()
    {
        var sb = new StringBuilder();
        sb.Append($"ContentsPanel(Name={Name})");
        foreach (var child in Children)
        {
            sb.AppendLine();
            sb.Append('{');
            sb.Append((child is Node node ? node.DebugToString() : child.ToString() ?? "").Replace("\n", "\n  "));
            sb.Append('}');
        }
        return sb.ToString();
    }
}