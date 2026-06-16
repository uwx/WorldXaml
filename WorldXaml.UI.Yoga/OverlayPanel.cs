using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Numerics;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using ObservableCollections;
using WorldXaml.ObservableCollections;
using WorldXaml.UI.Base;

namespace WorldXaml.UI.Yoga;

/// <summary>
/// A panel that renders its children with absolute positioning, as if they were the only child, disregarding each
/// other's layout.
/// </summary>
public class OverlayPanel : Visual
{
    private readonly FlexPanel _overlayContainer;

    public OverlayPanel()
    {
        _overlayContainer = new FlexPanel
        {
            Flex = 1,
            Position = YgPositionType.Relative, // establishes containing block for absolute children
            LogicalParent = this
        };
        
        ContentChildren.CollectionChanged += ContentChildrenChanged;
    }

    private void ContentChildrenChanged(in NotifyCollectionChangedEventArgs<Node> e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.IsSingleItem)
                {
                    e.NewItem.Position = YgPositionType.Absolute;
                    _overlayContainer.Children.Add(e.NewItem);
                }
                else
                {
                    foreach (var item in e.NewItems)
                    {
                        item.Position = YgPositionType.Absolute;
                        _overlayContainer.Children.Add(item);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.IsSingleItem)
                    _overlayContainer.Children.Remove(e.OldItem);
                else
                    foreach (var item in e.OldItems)
                        _overlayContainer.Children.Remove(item);
                break;
            case NotifyCollectionChangedAction.Replace:
                if (e.IsSingleItem)
                {
                    var oldItem = e.OldItem;
                    var newItem = e.NewItem;
                    var idx = _overlayContainer.Children.IndexOf(oldItem);
                    if (idx >= 0)
                    {
                        _overlayContainer.Children.RemoveAt(idx);
                        newItem.Position = YgPositionType.Absolute;
                        _overlayContainer.Children.Insert(idx, newItem);
                    }
                }
                else
                {
                    for (var i = 0; i < e.NewItems.Length; i++)
                    {
                        var oldItem = e.OldItems[i];
                        var newItem = e.NewItems[i];
                        var idx = _overlayContainer.Children.IndexOf(oldItem);
                        if (idx >= 0)
                        {
                            _overlayContainer.Children.RemoveAt(idx);
                            newItem.Position = YgPositionType.Absolute;
                            _overlayContainer.Children.Insert(idx, newItem);
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Move:
                // Absolute positioning means visual/tree order is irrelevant.
                break;
            case NotifyCollectionChangedAction.Reset:
                _overlayContainer.Children.Clear();
                foreach (var item in ContentChildren)
                {
                    item.Position = YgPositionType.Absolute;
                    _overlayContainer.Children.Add(item);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e.Action), e.Action, null);
        }
    }

    internal override YGNodePtr Contents => _overlayContainer.NodeInternal;
    public override IReadOnlyList<Visual> VisualChildren => [_overlayContainer];
    
    public override Vector2 FocusOrigin => Vector2.Zero;
    public override Vector2 FocusSize => Vector2.Zero;

    /// <summary>
    /// Children supplied by the user of this control (the content written inside the XAML tag).
    /// These are NOT rendered directly — they are injected into the template's
    /// <see cref="ContentPresenter"/> when the template is applied.
    /// This is a plain collection (not NodeChildCollection) because these children should not
    /// become Yoga children of the TemplatedControl itself — they become Yoga children of the
    /// ContentPresenter when the template is applied.
    /// </summary>
    [Content]
    public NonSynchronizedObservableList<Node> ContentChildren { get; } = new();

    public override IReadOnlyList<ILogical> LogicalChildren => ContentChildren;
}
