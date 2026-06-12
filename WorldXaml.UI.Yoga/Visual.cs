using System.Numerics;
using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga.Events;

namespace WorldXaml.UI.Yoga;

public abstract partial class Visual : BindableObject
{
    /// <summary>
    /// <para>
    /// Gets the Yoga node associated with this visual element representing its contents.
    /// </para>
    ///
    /// <para>
    /// For a visual element which is itself a node, this is the backing Yoga node.
    /// </para>
    /// 
    /// <para>
    /// For a visual element which is a collection of nodes, this should be a parent Yoga node that contains all the
    /// child nodes as its children. This allows the visual element to manage a group of nodes as a single unit for
    /// layout and rendering purposes. The node's lifetime should last as long as the parent visual element.
    /// </para>
    ///
    /// <para>
    /// For a visual element which is a template, this should be a Yoga node that contains the template's layout tree
    /// (its chrome). The node's lifetime should last as long as the parent visual element.
    /// </para>
    ///
    /// <para>
    /// The node behind this property should not change during the lifetime of a visual element, because changes to it
    /// will not automatically be reflected in the parent Yoga node. Thus if the visual element needs to change the Yoga
    /// node it uses for its contents, it is desirable to provide a wrapper Yoga node with <see cref="YgDisplay"/> set
    /// to <see cref="YgDisplay.Contents"/> instead.
    /// </para>
    /// </summary>
    internal abstract YGNodePtr Contents { get; }

    /// <summary>
    /// Gets the visual children of this visual element. Visual elements are ones that participate in the layout tree,
    /// receive hit testing, game tick updates, and draw calls.
    /// </summary>
    public abstract IReadOnlyList<Visual> VisualChildren { get; }
    
    [Property]
    public partial bool IsFocusable { get; set; }

    public abstract Vector2 FocusOrigin { get; }
    
    public abstract Vector2 FocusSize { get; }
    
    [Property]
    public partial bool IsFocused { get; set; }
    
    [Property]
    public partial int TabOrder { get; set; }

    // Reusable snapshot buffer so dispatch methods don't allocate a new list
    // every time VisualChildren is iterated. Allocated once per Visual, cleared
    // and repopulated on each use.
    private List<Visual>? _childSnapshot;

    private protected List<Visual> GetChildSnapshot()
    {
        var list = _childSnapshot ??= [];
        list.Clear();
        list.AddRange(VisualChildren);
        return list;
    }

    internal virtual void NotifyUiScaleChanged()
    {
        foreach (var child in GetChildSnapshot())
        {
            child.NotifyUiScaleChanged();
        }
    }

    public virtual void Update(FocusManager focusManager)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.Update(focusManager);
        }
    }

    public virtual void Render(RenderContext context)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.Render(context);
        }
    }

    public virtual void DispatchMouseMoved(FocusManager focusManager, BaseMouseMoveEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseMoved(focusManager, @event);
        }
    }

    public virtual void DispatchMouseEntered(FocusManager focusManager, BaseMouseMoveEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseEntered(focusManager, @event);
        }
    }

    public virtual void DispatchMouseLeft(FocusManager focusManager, BaseMouseMoveEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseLeft(focusManager, @event);
        }
    }

    public virtual void DispatchMousePressed(FocusManager focusManager, BaseMouseEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMousePressed(focusManager, @event);
        }
    }

    public virtual void DispatchMouseReleased(FocusManager focusManager, BaseMouseEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseReleased(focusManager, @event);
        }
    }

    public virtual void DispatchMouseDragged(FocusManager focusManager, BaseMouseDragEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseDragged(focusManager, @event);
        }
    }

    public virtual void DispatchMouseScrolled(FocusManager focusManager, BaseMouseWheelEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseScrolled(focusManager, @event);
        }
    }

    public virtual void DispatchKeyPressed(FocusManager focusManager, KeyboardEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchKeyPressed(focusManager, @event);
        }
    }

    public virtual void DispatchKeyReleased(FocusManager focusManager, KeyboardEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchKeyReleased(focusManager, @event);
        }
    }

    public virtual void DispatchKeyTyped(FocusManager focusManager, KeyboardTypedEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchKeyTyped(focusManager, @event);
        }
    }
}