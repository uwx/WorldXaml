using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Avalonia.LogicalTree;
using ObservableCollections;

namespace WorldXaml.UI.Yoga;

/// <summary>
/// A placeholder node used inside a <see cref="Avalonia.Markup.Xaml.Templates.ControlTemplate"/>
/// to mark where the templated control's content children should be inserted.
/// </summary>
[DebuggerDisplay("{DebugToString()}")]
public class ContentPresenter : ContentsPanel
{
    public NodeChildCollection Children { get; }

    public override IReadOnlyList<ILogical> LogicalChildren => Children;
    public override IReadOnlyList<Visual> VisualChildren => Children;

    public ContentPresenter()
    {
        Children = new NodeChildCollection(this);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override string DebugToString()
    {
        var sb = new StringBuilder();
        sb.Append($"ContentPresenter(Name={Name})");
        foreach (var child in Children)
        {
            sb.AppendLine();
            sb.Append('{');
            sb.Append((child is PlainNode node ? node.DebugToString() : child.ToString() ?? "").Replace("\n", "\n  "));
            sb.Append('}');
        }
        return sb.ToString();
    }
    
    private TemplatedControl? _templatedParent;

    /// <summary>
    /// The <see cref="TemplatedControl"/> whose content this presenter displays.
    /// Set automatically during template application.
    /// </summary>
    public TemplatedControl? TemplatedParent
    {
        get => _templatedParent;
        internal set
        {
            if (_templatedParent == value) return;

            if (_templatedParent != null)
                DetachContent();

            _templatedParent = value;

            if (_templatedParent != null)
                AttachContent();
        }
    }

    private void AttachContent()
    {
        if (_templatedParent == null) return;

        foreach (var child in _templatedParent.ContentChildren)
            Children.Add(child);

        _templatedParent.ContentChildren.CollectionChanged += OnContentChildrenChanged;
    }

    private void DetachContent()
    {
        if (_templatedParent == null) return;

        _templatedParent.ContentChildren.CollectionChanged -= OnContentChildrenChanged;
        Children.Clear();
    }

    private void OnContentChildrenChanged(in NotifyCollectionChangedEventArgs<Node> e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.IsSingleItem)
                    Children.Add(e.NewItem);
                else
                    foreach (var item in e.NewItems)
                        Children.Add(item);
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.IsSingleItem)
                    Children.Remove(e.OldItem);
                else
                    foreach (var item in e.OldItems)
                        Children.Remove(item);
                break;
            case NotifyCollectionChangedAction.Replace:
                if (e.IsSingleItem)
                    Children[e.NewStartingIndex] = e.NewItem;
                else
                    for (var i = 0; i < e.NewItems.Length; i++)
                        Children[e.NewStartingIndex + i] = e.NewItems[i];
                break;
            case NotifyCollectionChangedAction.Reset:
                Children.Clear();
                if (_templatedParent != null)
                {
                    foreach (var child in _templatedParent.ContentChildren)
                        Children.Add(child);
                }
                break;
            case NotifyCollectionChangedAction.Move:
                // Ignored
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e.Action), e.Action, null);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            DetachContent();

        base.Dispose(disposing);
    }
}
