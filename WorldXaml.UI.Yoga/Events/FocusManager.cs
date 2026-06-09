using System.Drawing;
using System.Numerics;

namespace WorldXaml.UI.Yoga;

public class FocusManager
{
    private Visual? _focusedElement;

    public Visual? FocusedElement
    {
        get => _focusedElement;
        set
        {
            if (_focusedElement == value) return;

            var old = _focusedElement;
            _focusedElement = value;

            old?.IsFocused = false;
            value?.IsFocused = true;

            FocusedChanged?.Invoke(old, value);
        }
    }

    public event Action<Visual?, Visual?>? FocusedChanged;

    /// <summary>Move focus to the next focusable element in Tab order.</summary>
    public bool FocusNext(FlexPanel root)
    {
        var all = GetFocusableDescendants(root).ToList();
        if (all.Count == 0) return false;

        var idx = _focusedElement is not null
            ? all.IndexOf(_focusedElement)
            : -1;

        var next = (idx + 1) % all.Count;
        FocusedElement = all[next];
        return true;
    }

    /// <summary>Move focus to the previous focusable element.</summary>
    public bool FocusPrev(FlexPanel root)
    {
        var all = GetFocusableDescendants(root).ToList();
        if (all.Count == 0) return false;

        var idx = _focusedElement is not null
            ? all.IndexOf(_focusedElement)
            : all.Count;

        var prev = (idx - 1 + all.Count) % all.Count;
        FocusedElement = all[prev];
        return true;
    }

    /// <summary>
    /// Hit-test: find the topmost focusable Node at a screen position.
    /// Walks children in reverse (topmost rendered last).
    /// </summary>
    public Visual? HitTest(FlexPanel root, Vector2 screenPos)
    {
        return HitTestRecursive(root, screenPos);
    }

    private Visual? HitTestRecursive(Visual node, Vector2 pos)
    {
        // Walk children back-to-front for correct z-order
        var children = node.VisualChildren
            .OrderBy(c => c.TabOrder)
            .Reverse();
    
        foreach (var visual in children)
        {
            if (visual is Node child && (child.Visibility == Visibility.Hidden || child.Display == YgDisplay.None || child.Opacity == 0.0f))
                continue;

            var result = HitTestRecursive(visual, pos);
            if (result is not null) return result;
        }
        
        // Check self
        if (node.IsFocusable)
        {
            var bounds = new RectangleF(
                node.FocusOrigin.X,
                node.FocusOrigin.Y,
                node.FocusSize.X,
                node.FocusSize.Y);

            if (bounds.Contains(pos.X, pos.Y))
                return node;
        }

        return null;
    }

    /// <summary>
    /// Depth-first enumeration of all focusable Nodes under a root.
    /// Respects Visibility (skips Hidden/Collapsed subtrees).
    /// </summary>
    private IEnumerable<Visual> GetFocusableDescendants(Visual root)
    {
        foreach (var child in root.VisualChildren)
        {
            if (child is Node node && (node.Visibility == Visibility.Hidden || node.Display == YgDisplay.None || node.Opacity == 0.0f))
                continue;

            if (child.IsFocusable)
                yield return child;

            foreach (var descendant in GetFocusableDescendants(child))
                yield return descendant;
        }
    }

    /// <summary>Clear focus entirely.</summary>
    public void ClearFocus() => FocusedElement = null;
}