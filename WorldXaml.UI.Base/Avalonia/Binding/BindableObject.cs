using System.ComponentModel;
using System.Reactive;
using Avalonia;
using Avalonia.LogicalTree;

namespace WorldXaml.UI.Base;

public interface IBindingTarget : IGetSetValue
{
    object? DataContext { get; }

    IDisposable Bind<TValue>(StyledProperty<TValue> property, IObservable<TValue> source);
}

public abstract class BindableObject : PropertyObject, ILogical, IBindingTarget, IDataContextProvider
{
    // Stores active bindings (IDisposable subscriptions)
    private protected readonly Dictionary<int, IDisposable> _bindings = new();

    public static readonly StyledProperty<object?> DataContextProperty =
        AvaloniaProperty.RegisterDirect<PropertyObject, object?>(nameof(DataContext), null);

    /// <summary>
    /// Sets the type of the associated data context for this object.
    /// </summary>
    public object? DataContext
    {
        get => GetValue(DataContextProperty);
        set => SetValue(DataContextProperty, value);
    }
    
    public static readonly StyledProperty<Type?> DataTypeProperty =
        AvaloniaProperty.Register<PropertyObject, Type?>(nameof(DataType), null);

    /// <summary>
    /// Sets the compile-time type of the <see cref="BindableObject.DataContext"/>.
    /// </summary>
    [Property]
    public Type? DataType
    {
        get => GetValue(DataTypeProperty);
        set => SetValue(DataTypeProperty, value);
    }

    #region Parent/child tree

    private ILogicalRoot? _root;

    public event EventHandler<LogicalTreeAttachmentEventArgs>? AttachedToLogicalTree;
    public event EventHandler<LogicalTreeAttachmentEventArgs>? DetachedFromLogicalTree;

    bool ILogical.IsAttachedToLogicalTree => _root != null;

    // TODO if a parent's parent is detached then the child should be detached too, but we don't have a way to track that right now.
    public ILogical? LogicalParent
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            OnParentChanged();
        }
    }
    
    public abstract IReadOnlyList<ILogical> LogicalChildren { get; }

    private IDisposable? _parentDataContextBinding;

    public BindableObject()
    {
        _root = this as ILogicalRoot;
    }

    public virtual void NotifyAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
    }

    public virtual void NotifyDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
    }

    private void OnParentChanged()
    {
        // Update logical tree attachment and raise events as needed.
        
        var newRoot = FindLogicalRoot(this);

        if (_root != newRoot)
        {
            if (_root != null)
            {
                var e = new LogicalTreeAttachmentEventArgs(_root, this, LogicalParent);
                NotifyDetachedFromLogicalTree(e);
                DetachedFromLogicalTree?.Invoke(this, e);
            }

            if (newRoot is not null)
            {
                var e = new LogicalTreeAttachmentEventArgs(newRoot, this, LogicalParent);
                NotifyAttachedToLogicalTree(e);
                AttachedToLogicalTree?.Invoke(this, e);
                _root = newRoot;
            }
        }

        // Drop the old inherited DataContext binding.
        {
            var parentDataContextBinding = _parentDataContextBinding;
            _parentDataContextBinding = null;
            parentDataContextBinding?.Dispose();
        }

        if (LogicalParent is not IGetSetValue gsv) return;

        // If DataContext has not been set locally, inherit from parent.
        // We watch the parent's DataContext observable so that future
        // changes also propagate automatically.
        if (!_values.ContainsKey(DataContextProperty.Id))
        {
            _parentDataContextBinding = gsv
                .GetObservable(DataContextProperty)
                .Subscribe(Observer.Create<object?>(dc =>
                    SetValueCore(DataContextProperty, dc)));
        }
    }
    
    private static ILogicalRoot? FindLogicalRoot(ILogical? e)
    {
        while (e != null)
        {
            if (e is ILogicalRoot root)
            {
                return root;
            }

            e = e.LogicalParent;
        }

        return null;
    }
    
    #endregion

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void SetValue<TValue>(StyledProperty<TValue> property, TValue value)
    {
        ClearBinding(property); // local value wins, kill any binding
        base.SetValue(property, value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void SetValue<TOwner, TValue>(DirectProperty<TOwner, TValue> property, TValue value)
    {
        if (property.Setter is null)
            throw new InvalidOperationException($"Property '{property.Name}' is read-only.");

        ClearBinding(property);
        
        base.SetValue(property, value);
    }

    public IDisposable Bind<TValue>(StyledProperty<TValue> property, IObservable<TValue> source)
    {
        ClearBinding(property);
        var sub = source.Subscribe(Observer.Create<TValue>(v => SetValueCore(property, v)));
        _bindings[property.Id] = sub;
        return sub;
    }

    // Called by XamlX-generated IL when the XAML value is an IXamlBinding.
    // Signature must be non-generic so XamlX can find it easily;
    // the cast is safe because the transformer verified types at compile time.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void BindFromXaml<TValue>(StyledProperty<TValue> property, IXamlBinding binding)
    {
        ClearBinding(property);
        var sub = binding.Apply(this, property);
        _bindings[property.Id] = sub;
    }

    // BindFromXaml overload so the XamlX BindingSetter works with direct properties too.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void BindFromXaml<TOwner, TValue>(DirectProperty<TOwner, TValue> property, IXamlBinding binding)
        where TOwner : PropertyObject
    {
        ClearBinding(property);
        var sub = binding.Apply(this, property);
        _bindings[property.Id] = sub;
    }

    private void ClearBinding(AvaloniaProperty property)
    {
        if (_bindings.Remove(property.Id, out var sub))
            sub.Dispose();
    }
}