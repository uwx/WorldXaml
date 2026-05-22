// ReSharper disable once CheckNamespace
namespace Avalonia.LogicalTree;

/// <summary>
/// Represents a root of a logical tree.
/// </summary>
public interface ILogicalRoot : ILogical;

/// <summary>
/// Holds the event arguments for the <see cref="ILogical.AttachedToLogicalTree"/> and 
/// <see cref="ILogical.DetachedFromLogicalTree"/> events.
/// </summary>
public class LogicalTreeAttachmentEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicalTreeAttachmentEventArgs"/> class.
    /// </summary>
    /// <param name="root">The root of the logical tree.</param>
    /// <param name="source">The control being attached/detached.</param>
    /// <param name="parent">The <see cref="Parent"/>.</param>
    public LogicalTreeAttachmentEventArgs(
        ILogicalRoot root,
        ILogical source,
        ILogical? parent)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Parent = parent;
    }

    /// <summary>
    /// Gets the root of the logical tree that the control is being attached to or detached from.
    /// </summary>
    public ILogicalRoot Root { get; }

    /// <summary>
    /// Gets the control that was attached or detached from the logical tree.
    /// </summary>
    /// <remarks>
    /// Logical tree attachment events travel down the attached logical tree from the point of
    /// attachment/detachment, so this control may be different from the control that the
    /// event is being raised on.
    /// </remarks>
    public ILogical Source { get; }

    /// <summary>
    /// Gets the control that <see cref="Source"/> is being attached to or detached from.
    /// </summary>
    /// <remarks>
    /// For logical tree attachment, holds the new logical parent of <see cref="Source"/>. For
    /// detachment, holds the old logical parent of <see cref="Source"/>. If the detachment event
    /// was caused by a top-level control being closed, then this property will be null.
    /// </remarks>
    public ILogical? Parent { get; }
}

/// <summary>
/// Represents a node in the logical tree.
/// </summary>
public interface ILogical
{
    /// <summary>
    /// Raised when the control is attached to a rooted logical tree.
    /// </summary>
    event EventHandler<LogicalTreeAttachmentEventArgs>? AttachedToLogicalTree;
    
    /// <summary>
    /// Raised when the control is detached from a rooted logical tree.
    /// </summary>
    event EventHandler<LogicalTreeAttachmentEventArgs>? DetachedFromLogicalTree;
    
    /// <summary>
    /// Gets a value indicating whether the element is attached to a rooted logical tree.
    /// </summary>
    bool IsAttachedToLogicalTree { get; }

    /// <summary>
    /// Gets the logical parent.
    /// </summary>
    ILogical? LogicalParent { get; }

    /// <summary>
    /// Gets the logical children.
    /// </summary>
    IReadOnlyList<ILogical> LogicalChildren { get; }

    // /// <summary>
    // /// Notifies the control that it is being attached to a rooted logical tree.
    // /// </summary>
    // /// <param name="e">The event args.</param>
    // /// <remarks>
    // /// This method will be called automatically by the framework, you should not need to call
    // /// this method yourself.
    // /// </remarks>
    // void NotifyAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e);
    //
    // /// <summary>
    // /// Notifies the control that it is being detached from a rooted logical tree.
    // /// </summary>
    // /// <param name="e">The event args.</param>
    // /// <remarks>
    // /// This method will be called automatically by the framework, you should not need to call
    // /// this method yourself.
    // /// </remarks>
    // void NotifyDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e);
    //
    // /// <summary>
    // /// Notifies the control that a change has been made to resources that apply to it.
    // /// </summary>
    // /// <param name="e">The event args.</param>
    // /// <remarks>
    // /// This method will be called automatically by the framework, you should not need to call
    // /// this method yourself.
    // /// </remarks>
    // void NotifyResourcesChanged(ResourcesChangedEventArgs e);
}