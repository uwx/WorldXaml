using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.LogicalTree;

namespace WorldXaml.UI.Base;

public readonly record struct PropertyChangedEventArgs(Property Property, object? OldValue, object? NewValue);

public interface IGetSetValue
{
    event EventHandler<PropertyChangedEventArgs>? PropertyChanged;
    
    void SetValue<TValue>(Property<TValue> property, TValue value);
    TValue GetValue<TValue>(Property<TValue> property);
    
    /// <summary>Returns a cold observable of every value change for <paramref name="property"/>.</summary>
    public IObservable<TValue> GetObservable<TValue>(Property<TValue> property)
    {
        return Observable.Create<TValue>(observer =>
        {
            observer.OnNext(GetValue(property));
            EventHandler<PropertyChangedEventArgs> handler = (_, e) =>
            {
                if (e.Property.Id == property.Id)
                    observer.OnNext((TValue)e.NewValue!);
            };
            PropertyChanged += handler;
            return () => PropertyChanged -= handler;
        });
    }
}

public abstract class PropertyObject : IGetSetValue
{
    // Stores local values
    private protected readonly Dictionary<int, object?> _values = new();

    public event EventHandler<PropertyChangedEventArgs>? PropertyChanged;
    
    public static readonly Property<object?> DataContextProperty =
        Property.Register<PropertyObject, object?>("DataContext", null);
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public TValue GetValue<TValue>(Property<TValue> property)
    {
        if (_values.TryGetValue(property.Id, out var raw))
            return (TValue)raw!;
        return (TValue)property.DefaultValue!;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual void SetValue<TValue>(Property<TValue> property, TValue value)
    {
        SetValueCore(property, value);
    }

    private protected void SetValueCore<TValue>(Property<TValue> property, TValue value)
    {
        var oldValue = GetValue(property);
        _values[property.Id] = value;
        
        property.OnChanged?.Invoke(this, value);
        
        if (!EqualityComparer<TValue>.Default.Equals(oldValue, value))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property, oldValue, value));
    }

    internal void WritebackValue<TValue>(Property<TValue> property, TValue value)
    {
        // Deliberately skips ClearBinding - this is a binding-driven write,
        // not a "local value wins" operation.
        SetValueCore(property, value);
    }
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public TValue GetValue<TOwner, TValue>(DirectProperty<TOwner, TValue> property)
        where TOwner : PropertyObject
    {
        return property.Getter((TOwner)this);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual void SetValue<TOwner, TValue>(DirectProperty<TOwner, TValue> property, TValue value)
        where TOwner : PropertyObject
    {
        if (property.Setter is null)
            throw new InvalidOperationException($"Property '{property.Name}' is read-only.");

        var oldValue = property.Getter((TOwner)this);
        property.Setter((TOwner)this, value);
        var newValue = property.Getter((TOwner)this); // re-read in case setter coerces

        if (!EqualityComparer<TValue>.Default.Equals(oldValue, newValue))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property, oldValue, newValue));
    }
}

public class Property
{
    private static int _nextId;
    public int Id { get; } = _nextId++;
    public string Name { get; }
    public Type PropertyType { get; }
    public Type OwnerType { get; }
    public object? DefaultValue { get; }
    public BindingMode DefaultMode { get; }
    public Action<PropertyObject, object?>? OnChanged { get; }

    private protected Property(string name, Type propertyType, Type ownerType, object? defaultValue, BindingMode defaultMode = BindingMode.OneWay, Action<PropertyObject, object?>? onChanged = null)
    {
        Name = name;
        PropertyType = propertyType;
        OwnerType = ownerType;
        DefaultValue = defaultValue;
        DefaultMode = defaultMode;
        OnChanged = onChanged;
    }

    public static Property<TValue> Register<TOwner, TValue>(string name, TValue defaultValue = default!, BindingMode defaultMode = BindingMode.OneWay, Action<TOwner, TValue>? onChanged = null)
        where TOwner : PropertyObject
    {
        Action<PropertyObject, TValue>? wrapped = onChanged is null
            ? null
            : (obj, val) => onChanged((TOwner)obj, val);

        var prop = new Property<TValue>(name, typeof(TOwner), defaultValue, defaultMode, wrapped);
        PropertyRegistry.Instance.Register(typeof(TOwner), prop);
        return prop;
    }

    public override bool Equals(object? obj) => obj is Property p && p.Id == Id;
    public override int GetHashCode() => Id;
}

public class PropertyRegistry
{
    public static readonly PropertyRegistry Instance = new();
    private readonly Dictionary<Type, List<Property>> _registered = new();

    public void Register(Type ownerType, Property property)
    {
        if (!_registered.TryGetValue(ownerType, out var list))
            _registered[ownerType] = list = new();
        list.Add(property);
    }

    public IEnumerable<Property> GetRegistered(Type type)
    {
        foreach (var (ownerType, props) in _registered)
            if (ownerType.IsAssignableFrom(type))
                foreach (var prop in props)
                    yield return prop;
    }
}

public class Property<TValue> : Property
{
	public new TValue DefaultValue => (TValue)base.DefaultValue!;

    internal Property(string name, Type ownerType, TValue defaultValue, BindingMode defaultMode = BindingMode.OneWay, Action<PropertyObject, TValue>? onChanged = null)
        : base(name, typeof(TValue), ownerType, defaultValue, defaultMode, onChanged is null ? null : (obj, val) => onChanged(obj, (TValue)val!)) { }
}