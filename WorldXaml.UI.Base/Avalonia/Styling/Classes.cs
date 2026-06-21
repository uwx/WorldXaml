using System.Collections;
using System.Collections.Specialized;
using WorldXaml.UI.Base;

namespace Avalonia.Controls;

/// <summary>
/// A collection of CSS-like class names for an element.
/// Styles can target elements by class name.
/// Usage: &lt;Button Classes="primary large" /&gt;
/// </summary>
public class Classes : IList<string>, INotifyCollectionChanged
{
    private readonly List<string> _classes = [];
    private readonly BindableObject _owner;

    public Classes(BindableObject owner)
    {
        _owner = owner;
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => _classes.Count;
    public bool IsReadOnly => false;

    public string this[int index]
    {
        get => _classes[index];
        set
        {
            var old = _classes[index];
            _classes[index] = value;
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, old, index));
        }
    }

    public void Add(string item)
    {
        if (_classes.Contains(item)) return;
        _classes.Add(item);
        CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _classes.Count - 1));
    }

    public bool Remove(string item)
    {
        var idx = _classes.IndexOf(item);
        if (idx < 0) return false;
        _classes.RemoveAt(idx);
        CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, idx));
        return true;
    }

    public void Clear()
    {
        if (_classes.Count == 0) return;
        _classes.Clear();
        CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool Contains(string item) => _classes.Contains(item);
    public void CopyTo(string[] array, int arrayIndex) => _classes.CopyTo(array, arrayIndex);
    public int IndexOf(string item) => _classes.IndexOf(item);
    public void Insert(int index, string item) => _classes.Insert(index, item);
    public void RemoveAt(int index) => _classes.RemoveAt(index);

    IEnumerator<string> IEnumerable<string>.GetEnumerator() => _classes.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _classes.GetEnumerator();

    /// <summary>
    /// Batch-add classes from a space-separated string.
    /// </summary>
    public void AddRange(string? classes)
    {
        if (string.IsNullOrWhiteSpace(classes)) return;
        foreach (var c in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            Add(c);
    }

    /// <summary>
    /// Returns true if the element has ALL listed classes.
    /// </summary>
    public bool HasAll(params string[] names)
        => names.All(n => _classes.Contains(n));

    /// <summary>
    /// Returns true if the element has ANY of the listed classes.
    /// </summary>
    public bool HasAny(params string[] names)
        => names.Any(n => _classes.Contains(n));
}
