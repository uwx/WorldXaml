using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using WorldXaml.UI.Base;

// ReSharper disable once CheckNamespace

namespace WorldXaml.UI.Base
{
    // In Avalonia, Inline extends TextElement. However we want to avoid forcing consumers into any particular graphics
    // or font rendering paradigm so we do it the other way around.
    public abstract class Inline : BindableObject, IInline
    {
        protected IInlineHost? Host { get; private set; }

        public abstract override IReadOnlyList<IInline> LogicalChildren { get; }
        
        public virtual void AttachHost(IInlineHost? host)
        {
            Host = host;
            OnInlineHostChanged();
        }

        public virtual void OnInlineHostChanged()
        {
        }
    }
    
    public interface IInlineHost
    {
        /// <summary>
        /// Notify the host that a change to a contained <see cref="IInline"/> has occurred.
        /// </summary>
        void Invalidate();
    }

    public interface IInline : ILogical;
}


namespace Avalonia.Controls.Documents
{
    [WhitespaceSignificantCollection]
    public class InlineCollection(ILogical parent) : ObservableCollection<Inline>, IReadOnlyList<IInline>
    {
        private IInlineHost? _host = parent as IInlineHost;

        public void AttachHost(IInlineHost? host)
        {
            _host = host;
        }

        IEnumerator<IInline> IEnumerable<IInline>.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected override void InsertItem(int index, Inline item)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
            item.LogicalParent = parent;
            item.AttachHost(_host);
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, Inline item)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
            var oldItem = Items[index];
            oldItem.LogicalParent = null;
            oldItem.AttachHost(null);
            item.LogicalParent = parent;
            item.AttachHost(_host);
            base.SetItem(index, item);
        }

        protected override void ClearItems()
        {
            foreach (var node in Items)
            {
                node.LogicalParent = null;
                node.AttachHost(null);
            }
            base.ClearItems();
        }

        protected override void RemoveItem(int index)
        {
            var item = Items[index];
            item.LogicalParent = null;
            item.AttachHost(null);
            base.RemoveItem(index);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            _host?.Invalidate();
        }

        IInline IReadOnlyList<IInline>.this[int index] => this[index];
    }
}