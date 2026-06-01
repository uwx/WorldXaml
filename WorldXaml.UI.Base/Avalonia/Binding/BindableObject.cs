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
        AvaloniaProperty.Register<BindableObject, object?>(nameof(DataContext), null);

    /// <summary>
    /// The templated control that owns the template tree this object belongs to.
    /// Set automatically during template application. Used by bindings with
    /// <c>RelativeSource={RelativeSource TemplatedParent}</c>.
    /// </summary>
    public static readonly StyledProperty<object?> TemplatedParentProperty =
        AvaloniaProperty.Register<BindableObject, object?>(nameof(TemplatedParent), null);

    /// <summary>
    /// Sets the type of the associated data context for this object.
    /// </summary>
    public object? DataContext
    {
        get => GetValue(DataContextProperty);
        set => SetValue(DataContextProperty, value);
    }

    public object? TemplatedParent
    {
        get => GetValue(TemplatedParentProperty);
        set => SetValue(TemplatedParentProperty, value);
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

    /// <summary>
    /// Triggered when the object is mounted onto the logical tree.
    /// </summary>
    public AnimationTrigger Mounted { get; } = new();
    
    /// <summary>
    /// Triggered when the object is unmounted from the logical tree.
    /// </summary>
    public AnimationTrigger Unmounted { get; } = new();

    private IDisposable? _parentDataContextBinding;

    public BindableObject()
    {
        _root = this as ILogicalRoot;
        if (_root != null)
        {
            Mounted.Trigger();
        }
    }

    private void OnDetachedFromLogicalTreeCore(LogicalTreeAttachmentEventArgs args)
    {
        if (_root != null)
        {
            Mounted.Reset();
            DetachedFromLogicalTree?.Invoke(this, args);
            Unmounted.Trigger();

            var logicalChildren = LogicalChildren;
            var logicalChildrenCount = logicalChildren.Count;

            for (var i = 0; i < logicalChildrenCount; i++)
            {
                if (logicalChildren[i] is BindableObject child && child._root != args.Root) // child may already have been attached within an event handler
                {
                    child.OnDetachedFromLogicalTreeCore(args);
                }
            }
        }
        
        _root = null;
    }

    private void OnAttachedToLogicalTreeCore(LogicalTreeAttachmentEventArgs args)
    {
        if (_root == null)
        {
            Unmounted.Reset();
            AttachedToLogicalTree?.Invoke(this, args);
            Mounted.Trigger();

            var logicalChildren = LogicalChildren;
            var logicalChildrenCount = logicalChildren.Count;

            for (var i = 0; i < logicalChildrenCount; i++)
            {
                if (logicalChildren[i] is BindableObject child && child._root != args.Root) // child may already have been attached within an event handler
                {
                    child.OnAttachedToLogicalTreeCore(args);
                }
            }

            _root = args.Root;
        }
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
                OnDetachedFromLogicalTreeCore(e);
            }

            if (newRoot is not null)
            {
                var e = new LogicalTreeAttachmentEventArgs(newRoot, this, LogicalParent);
                OnAttachedToLogicalTreeCore(e);
            }
        }

        // Drop the old inherited DataContext binding.
        {
            var parentDataContextBinding = _parentDataContextBinding;
            _parentDataContextBinding = null;
            parentDataContextBinding?.Dispose();
        }

        if (LogicalParent is IGetSetValue gsv)
        {
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