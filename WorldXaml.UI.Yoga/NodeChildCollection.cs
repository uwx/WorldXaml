using System.Collections.ObjectModel;
using Avalonia.LogicalTree;

namespace WorldXaml.UI.Yoga;

public class NodeChildCollection(Node parent) : ObservableCollection<Node>, IReadOnlyList<ILogical>
{
    IEnumerator<ILogical> IEnumerable<ILogical>.GetEnumerator()
    {
        return GetEnumerator();
    }

    protected override void InsertItem(int index, Node item)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        item.LogicalParent = parent;
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, Node item)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        var oldItem = Items[index];
        oldItem.LogicalParent = null;
        item.LogicalParent = parent;
        base.SetItem(index, item);
    }

    protected override void ClearItems()
    {
        foreach (var node in Items)
        {
            node.LogicalParent = null;
        }
        base.ClearItems();
    }

    protected override void RemoveItem(int index)
    {
        var item = Items[index];
        item.LogicalParent = null;
        base.RemoveItem(index);
    }

    ILogical IReadOnlyList<ILogical>.this[int index] => this[index];
}