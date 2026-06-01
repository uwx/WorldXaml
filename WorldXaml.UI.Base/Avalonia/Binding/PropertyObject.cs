using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Data;
using Avalonia.LogicalTree;
using WorldXaml.UI.Base;

namespace WorldXaml.UI.Base
{
    public readonly record struct StyledPropertyChangedEventArgs(AvaloniaProperty Property, object? OldValue, object? NewValue);

    public interface IGetSetValue
    {
        event EventHandler<StyledPropertyChangedEventArgs>? StyledPropertyChanged;

        void SetValue<TValue>(StyledProperty<TValue> property, TValue value);
        TValue GetValue<TValue>(StyledProperty<TValue> property);

        /// <summary>Returns a cold observable of every value change for <paramref name="property"/>.</summary>
        public IObservable<TValue> GetObservable<TValue>(StyledProperty<TValue> property)
        {
            return Observable.Create<TValue>(observer =>
            {
                observer.OnNext(GetValue(property));
                EventHandler<StyledPropertyChangedEventArgs> handler = (_, e) =>
                {
                    if (e.Property.Id == property.Id)
                        observer.OnNext((TValue)e.NewValue!);
                };
                StyledPropertyChanged += handler;
                return () => StyledPropertyChanged -= handler;
            });
        }
    }

    public abstract class PropertyObject : IGetSetValue, INotifyPropertyChanging, INotifyPropertyChanged
    {
        // Stores local values
        private protected readonly Dictionary<int, object?> _values = new();

        public event EventHandler<StyledPropertyChangedEventArgs>? StyledPropertyChanged;
        public event PropertyChangingEventHandler? PropertyChanging;
        public event PropertyChangedEventHandler? PropertyChanged;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public TValue GetValue<TValue>(StyledProperty<TValue> property)
        {
            if (property.IsDirect)
                return (TValue)property.GetDirectValue(this)!;
            if (_values.TryGetValue(property.Id, out var raw))
                return (TValue)raw!;
            return property.DefaultValue;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual void SetValue<TValue>(StyledProperty<TValue> property, TValue value)
        {
            SetValueCore(property, value);
        }

        private protected void SetValueCore<TValue>(StyledProperty<TValue> property, TValue value)
        {
            var oldValue = GetValue(property);
            
            PropertyChanging?.Invoke(this, property.CachedChangingArgs);

            if (property.IsDirect)
                property.SetDirectValue(this, value);
            else
                _values[property.Id] = value;

            PropertyChanged?.Invoke(this, property.CachedChangedArgs);

            property.OnChanged?.Invoke(this, value);

            if (!EqualityComparer<TValue>.Default.Equals(oldValue, value))
                StyledPropertyChanged?.Invoke(this, new StyledPropertyChangedEventArgs(property, oldValue, value));
        }

        internal void WritebackValue<TValue>(StyledProperty<TValue> property, TValue value)
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

            SetValueCore(property, value);
        }
    }

    public class PropertyRegistry
    {
        public static readonly PropertyRegistry Instance = new();
        private readonly Dictionary<Type, List<AvaloniaProperty>> _registered = new();

        public void Register(Type ownerType, AvaloniaProperty property)
        {
            if (!_registered.TryGetValue(ownerType, out var list))
                _registered[ownerType] = list = new();
            list.Add(property);
        }

        public IEnumerable<AvaloniaProperty> GetRegistered(Type type)
        {
            foreach (var (ownerType, props) in _registered)
                if (ownerType.IsAssignableFrom(type))
                    foreach (var prop in props)
                        yield return prop;
        }
    }
}

namespace Avalonia
{
    public class AvaloniaProperty
    {
        private static int _nextId;
        public int Id { get; } = _nextId++;
        public string Name { get; }
        public Type PropertyType { get; }
        public Type OwnerType { get; }
        public object? DefaultValue { get; }
        public BindingMode DefaultMode { get; }
        public Action<PropertyObject, object?>? OnChanged { get; }

        /// <summary>True when this is a <see cref="DirectProperty{TOwner,TValue}"/>.</summary>
        internal virtual bool IsDirect => false;

        /// <summary>Read the backing value via the DirectProperty getter (boxes).</summary>
        internal virtual object? GetDirectValue(PropertyObject target) => throw new InvalidOperationException();

        /// <summary>Write the backing value via the DirectProperty setter (boxes).</summary>
        internal virtual void SetDirectValue(PropertyObject target, object? value) => throw new InvalidOperationException();

        internal PropertyChangingEventArgs CachedChangingArgs;
        internal PropertyChangedEventArgs CachedChangedArgs;

        private protected AvaloniaProperty(string name, Type propertyType, Type ownerType, object? defaultValue,
            BindingMode defaultMode = BindingMode.OneWay, Action<PropertyObject, object?>? onChanged = null)
        {
            Name = name;
            PropertyType = propertyType;
            OwnerType = ownerType;
            DefaultValue = defaultValue;
            DefaultMode = defaultMode;
            OnChanged = onChanged;
            CachedChangingArgs = new PropertyChangingEventArgs(name);
            CachedChangedArgs = new PropertyChangedEventArgs(name);
        }

        public static StyledProperty<TValue> Register<TOwner, TValue>(string name, TValue defaultValue = default!,
            BindingMode defaultMode = BindingMode.OneWay, Action<TOwner, TValue>? onChanged = null)
            where TOwner : PropertyObject
        {
            Action<PropertyObject, TValue>? wrapped = onChanged is null
                ? null
                : (obj, val) => onChanged((TOwner)obj, val);

            var prop = new StyledProperty<TValue>(name, typeof(TOwner), defaultValue, defaultMode, wrapped);
            PropertyRegistry.Instance.Register(typeof(TOwner), prop);
            return prop;
        }

        public override bool Equals(object? obj) => obj is AvaloniaProperty p && p.Id == Id;
        public override int GetHashCode() => Id;

        public static DirectProperty<TOwner, TValue> RegisterDirect<TOwner, TValue>(
            string name,
            Func<TOwner, TValue> getter,
            Action<TOwner, TValue>? setter = null,
            TValue defaultValue = default!,
            BindingMode? defaultMode = null) where TOwner : PropertyObject
        {
            var prop = new DirectProperty<TOwner, TValue>(name, getter, setter, defaultValue, defaultMode);
            PropertyRegistry.Instance.Register(typeof(TOwner), prop);
            return prop;
        }
    }

    public class StyledProperty<TValue> : AvaloniaProperty
    {
        public new TValue DefaultValue => (TValue)base.DefaultValue!;

        internal StyledProperty(string name, Type ownerType, TValue defaultValue, BindingMode defaultMode = BindingMode.OneWay, Action<PropertyObject, TValue>? onChanged = null)
            : base(name, typeof(TValue), ownerType, defaultValue, defaultMode, onChanged is null ? null : (obj, val) => onChanged(obj, (TValue)val!)) { }
    }
}