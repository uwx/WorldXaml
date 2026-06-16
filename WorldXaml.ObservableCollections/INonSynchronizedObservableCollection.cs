using System.Collections.Specialized;
using System.ComponentModel;
using ObservableCollections;

namespace WorldXaml.ObservableCollections;

public interface INonSynchronizedObservableCollection<T> : IReadOnlyCollection<T>
{
    event NotifyCollectionChangedEventHandler<T>? CollectionChanged;
}

public interface IReadOnlyNonSynchronizedObservableList<T> :
    IReadOnlyList<T>, INonSynchronizedObservableCollection<T>;

public interface IReadOnlyNonSynchronizedObservableDictionary<TKey, TValue> :
    IReadOnlyDictionary<TKey, TValue>, INonSynchronizedObservableCollection<KeyValuePair<TKey, TValue>>;

public interface INotifyCollectionChanged<T> : INotifyCollectionChanged, INotifyPropertyChanged;

public static class ObservableCollectionsExtensions
{
    extension<T>(INonSynchronizedObservableCollection<T> collection)
    {
        public INotifyCollectionChanged<T> ToNotifyCollectionChanged()
        {
            return new NotifyCollectionChangedImpl<T>(collection, null);
        }

        public INotifyCollectionChanged<T> ToNotifyCollectionChanged(ICollectionEventDispatcher? eventDispatcher)
        {
            return new NotifyCollectionChangedImpl<T>(collection, eventDispatcher);
        }
    }
}
