using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using WorldXaml.UI.Yoga.Events;

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

    private static Visual? HitTestRecursive(Visual node, Vector2 pos)
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

    // ─── Hover tracking with ancestor-chain diffing ────────────────────

    private List<Visual> _hoveredChain = [];

    /// <summary>
    /// The deepest element currently under the mouse cursor (or null).
    /// </summary>
    public Visual? HoveredElement => _hoveredChain.Count > 0 ? _hoveredChain[^1] : null;

    /// <summary>
    /// All focusable ancestors of the hovered element, from root-most to leaf.
    /// Empty when nothing is hovered.
    /// </summary>
    public IReadOnlyList<Visual> HoveredChain => _hoveredChain;

    /// <summary>Fired when the deepest hovered element changes.</summary>
    public event Action<Visual?>? HoveredChanged;

    /// <summary>
    /// Hit-tests and returns the full ancestor chain (root→leaf) of focusable
    /// elements at <paramref name="screenPos"/>. Empty when nothing hit.
    /// </summary>
    public List<Visual> HitTestChain(FlexPanel root, Vector2 screenPos)
    {
        var chain = new List<Visual>();
        HitTestChainRecursive(root, screenPos, chain);
        return chain;
    }

    private static bool HitTestChainRecursive(Visual node, Vector2 pos, List<Visual> chain)
    {
        var selfHit = false;

        if (node.IsFocusable)
        {
            var bounds = new RectangleF(
                node.FocusOrigin.X, node.FocusOrigin.Y,
                node.FocusSize.X, node.FocusSize.Y);

            if (bounds.Contains(pos.X, pos.Y))
            {
                chain.Add(node);       // ancestor added before children
                selfHit = true;
            }
        }

        // If self isn't hit, children can't be hit either (they're contained within self).
        // Exception: non-focusable containers — they pass through even though selfHit=false.
        if (!selfHit && node.IsFocusable)
            return false;

        var children = node.VisualChildren
            .OrderBy(c => c.TabOrder)
            .Reverse();

        foreach (var visual in children)
        {
            if (visual is Node child && (child.Visibility == Visibility.Hidden
                || child.Display == YgDisplay.None || child.Opacity == 0.0f))
                continue;

            if (HitTestChainRecursive(visual, pos, chain))
                return true;           // deepest child hit — stop
        }

        return selfHit;
    }

    /// <summary>
    /// Hit-tests under the cursor, diffs against the previous ancestor chain,
    /// and dispatches MouseEntered / MouseLeft / MouseMoved as appropriate.
    /// Call once per frame from your MouseMoved handler.
    /// </summary>
    public void DispatchMouseMove(FlexPanel root, BaseMouseMoveEvent @event)
    {
        var newChain = HitTestChain(root, @event.Position);

        // Find divergence index — first position where chains differ
        int diverge = 0;
        while (diverge < _hoveredChain.Count && diverge < newChain.Count
               && _hoveredChain[diverge] == newChain[diverge])
            diverge++;

        // MouseLeft — fire on old chain from leaf up to (not including) divergence
        for (int i = _hoveredChain.Count - 1; i >= diverge; i--)
            _hoveredChain[i].DispatchMouseLeft(this, @event);

        // MouseEntered — fire on new chain from divergence down to leaf
        for (int i = diverge; i < newChain.Count; i++)
            newChain[i].DispatchMouseEntered(this, @event);

        var oldLeaf = _hoveredChain.Count > 0 ? _hoveredChain[^1] : null;
        var newLeaf = newChain.Count > 0 ? newChain[^1] : null;

        _hoveredChain = newChain;

        if (oldLeaf != newLeaf)
            HoveredChanged?.Invoke(newLeaf);

        // MouseMoved — always fire from root so it propagates to all children
        root.DispatchMouseMoved(this, @event);
    }

    /// <summary>Clear focus entirely.</summary>
    public void ClearFocus() => FocusedElement = null;
}