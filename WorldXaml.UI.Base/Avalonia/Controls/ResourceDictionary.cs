using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Avalonia.Controls;

/// <summary>
/// A keyed collection of resources with support for merged dictionaries.
/// Resources are looked up by key, falling through to MergedDictionaries.
/// </summary>
public class ResourceDictionary : IList<KeyValuePair<object, object?>>,
    IDictionary<object, object?>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly List<KeyValuePair<object, object?>> _entries = [];
    private readonly List<ResourceDictionary> _mergedDictionaries = [];

    public IList<ResourceDictionary> MergedDictionaries => _mergedDictionaries;

    // ── Indexed access ──────────────────────────────────────────────────

    public object? this[object key]
    {
        get
        {
            if (TryGetValue(key, out var value))
                return value;
            throw new KeyNotFoundException($"Resource '{key}' not found.");
        }
        set
        {
            var idx = IndexOfKey(key);
            if (idx >= 0)
            {
                var old = _entries[idx].Value;
                _entries[idx] = new KeyValuePair<object, object?>(key, value);
                OnReplaced(old, value);
            }
            else
            {
                _entries.Add(new KeyValuePair<object, object?>(key, value));
                OnAdded(value);
            }
            OnPropertyChanged(nameof(Count));
        }
    }

    public bool TryGetValue(object key, out object? value)
    {
        // Search local entries first
        foreach (var (k, v) in _entries)
        {
            if (Equals(k, key))
            {
                value = v;
                return true;
            }
        }
        // Fall through to merged dictionaries (last wins)
        for (int i = _mergedDictionaries.Count - 1; i >= 0; i--)
        {
            if (_mergedDictionaries[i].TryGetValue(key, out value))
                return true;
        }
        value = null;
        return false;
    }

    public bool ContainsKey(object key) => TryGetValue(key, out _);

    public ICollection<object> Keys
    {
        get
        {
            var keys = new HashSet<object>();
            for (int i = _mergedDictionaries.Count - 1; i >= 0; i--)
            {
                foreach (var k in _mergedDictionaries[i].Keys)
                    keys.Add(k);
            }
            foreach (var (k, _) in _entries)
                keys.Add(k);
            return keys;
        }
    }

    public ICollection<object?> Values
    {
        get
        {
            var values = new List<object?>();
            for (int i = _mergedDictionaries.Count - 1; i >= 0; i--)
            {
                foreach (var v in _mergedDictionaries[i].Values)
                    values.Add(v);
            }
            foreach (var (_, v) in _entries)
                values.Add(v);
            return values;
        }
    }

    public void Add(object key, object? value)
        => this[key] = value;

    public bool Remove(object key)
    {
        var idx = IndexOfKey(key);
        if (idx < 0) return false;
        var removed = _entries[idx].Value;
        _entries.RemoveAt(idx);
        OnRemoved(removed);
        OnPropertyChanged(nameof(Count));
        return true;
    }

    public void Clear()
    {
        if (_entries.Count == 0) return;
        var old = _entries.ToList();
        _entries.Clear();
        foreach (var (_, v) in old)
            OnRemoved(v);
        OnPropertyChanged(nameof(Count));
        OnCollectionReset();
    }

    // ── IList<KeyValuePair<object, object?>> ────────────────────────────

    public int Count => _entries.Count + _mergedDictionaries.Sum(d => d.Count);
    public bool IsReadOnly => false;

    KeyValuePair<object, object?> IList<KeyValuePair<object, object?>>.this[int index]
    {
        get => throw new NotSupportedException("Indexed access not supported; use key lookup.");
        set => throw new NotSupportedException("Indexed access not supported; use key lookup.");
    }

    public int IndexOf(KeyValuePair<object, object?> item) => -1;
    public void Insert(int index, KeyValuePair<object, object?> item) => Add(item.Key, item.Value);
    public void RemoveAt(int index) => throw new NotSupportedException();

    void ICollection<KeyValuePair<object, object?>>.Add(KeyValuePair<object, object?> item)
        => Add(item.Key, item.Value);

    public bool Contains(KeyValuePair<object, object?> item)
        => TryGetValue(item.Key, out var v) && Equals(v, item.Value);

    public void CopyTo(KeyValuePair<object, object?>[] array, int arrayIndex)
    {
        foreach (var (k, v) in _entries)
            array[arrayIndex++] = new KeyValuePair<object, object?>(k, v);
    }

    public bool Remove(KeyValuePair<object, object?> item)
        => Remove(item.Key);

    IEnumerator<KeyValuePair<object, object?>> IEnumerable<KeyValuePair<object, object?>>.GetEnumerator()
        => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _entries.GetEnumerator();

    // ── Helpers ─────────────────────────────────────────────────────────

    private int IndexOfKey(object key)
    {
        for (int i = 0; i < _entries.Count; i++)
            if (Equals(_entries[i].Key, key))
                return i;
        return -1;
    }

    // ── Change notification ─────────────────────────────────────────────

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnAdded(object? value)
        => CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value));

    private void OnRemoved(object? value)
        => CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, value));

    private void OnReplaced(object? old, object? @new)
        => CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, @new, old));

    private void OnCollectionReset()
        => CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
