using System.Collections.ObjectModel;
using Avalonia.LogicalTree;

namespace WorldXaml.UI.Yoga;

public class NodeChildCollection(PlainNode parent) : ObservableCollection<Visual>
{
    protected override void InsertItem(int index, Visual item)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        item.LogicalParent = parent;
        Console.WriteLine(Environment.StackTrace);
        parent.NodeInternal.InsertChild(item.Contents, (uint)index);
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, Visual item)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        var oldItem = Items[index];
        oldItem.LogicalParent = null;
        item.LogicalParent = parent;
        parent.NodeInternal.SwapChild(item.Contents, (uint)index);
        base.SetItem(index, item);
    }

    protected override void ClearItems()
    {
        foreach (var node in Items)
        {
            node.LogicalParent = null;
        }
        parent.NodeInternal.RemoveAllChildren();
        base.ClearItems();
    }

    protected override void RemoveItem(int index)
    {
        var item = Items[index];
        item.LogicalParent = null;
        parent.NodeInternal.RemoveChild(item.Contents);
        base.RemoveItem(index);
    }
}