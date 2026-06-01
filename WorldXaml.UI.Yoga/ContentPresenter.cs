using System.Numerics;
using Avalonia.LogicalTree;

namespace WorldXaml.UI.Yoga;

/// <summary>
/// A placeholder node used inside a <see cref="Avalonia.Markup.Xaml.Templates.ControlTemplate"/>
/// to mark where the templated control's content children should be inserted.
/// </summary>
public class ContentPresenter : Box
{
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

    public ContentPresenter()
    {
        Display = YgDisplay.Contents;
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

    private void OnContentChildrenChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                foreach (Node item in e.NewItems!)
                    Children.Add(item);
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                foreach (Node item in e.OldItems!)
                    Children.Remove(item);
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                for (var i = 0; i < e.NewItems!.Count; i++)
                    Children[e.NewStartingIndex + i] = (Node)e.NewItems[i]!;
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                Children.Clear();
                if (_templatedParent != null)
                {
                    foreach (var child in _templatedParent.ContentChildren)
                        Children.Add(child);
                }
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            DetachContent();

        base.Dispose(disposing);
    }
}
