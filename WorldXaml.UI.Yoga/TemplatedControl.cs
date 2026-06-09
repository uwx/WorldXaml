using System.Collections.ObjectModel;
using System.Numerics;
using Avalonia;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;
using WorldXaml.UI.Base;

namespace WorldXaml.UI.Yoga;

/// <summary>
/// Base class for controls whose visual tree is defined by a <see cref="ControlTemplate"/>.
/// User-supplied content children (written inside the element in XAML) are captured into
/// <see cref="ContentChildren"/> and injected into the template's <see cref="ContentPresenter"/> slot.
/// </summary>
public class TemplatedControl : Visual
{
    // Logical parent tree: TemplatedControl -> ContentsPanel -> TemplateRoot
    // It could be better for the template root to be a direct logical child of the TemplatedControl, but this is
    // simpler to implement and retains the DataContext preservation we care about (since the ContentsPanel is invisible
    // and doesn't have a DataContext of its own, it just inherits the TemplatedControl's DataContext and passes it
    // through to the template root and its descendants).
    
    // Since the template may or may not be null
    private readonly ContentsPanel _templateContainer;

    private Node? _templateRoot;

    public TemplatedControl()
    {
        _templateContainer = new ContentsPanel
        {
            LogicalParent = this
        };
    }

    internal override YGNodePtr Contents => _templateContainer.NodeInternal;
    public override IReadOnlyList<Visual> VisualChildren => [_templateContainer];
    
    public override Vector2 FocusOrigin => Vector2.Zero;
    public override Vector2 FocusSize => Vector2.Zero;

    /// <summary>
    /// Children supplied by the user of this control (the content written inside the XAML tag).
    /// These are NOT rendered directly — they are injected into the template's
    /// <see cref="ContentPresenter"/> when the template is applied.
    /// This is a plain collection (not NodeChildCollection) because these children should not
    /// become Yoga children of the TemplatedControl itself — they become Yoga children of the
    /// ContentPresenter when the template is applied.
    /// </summary>
    [Content]
    public ObservableCollection<Node> ContentChildren { get; } = new();

    public override IReadOnlyList<ILogical> LogicalChildren => ContentChildren;

    public static readonly StyledProperty<ControlTemplate?> TemplateProperty =
        AvaloniaProperty.Register<TemplatedControl, ControlTemplate?>(
            nameof(Template),
            onChanged: static (obj, _) => ((TemplatedControl)obj).ApplyTemplate());

    public ControlTemplate? Template
    {
        get => GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    /// <summary>
    /// Instantiates the current <see cref="Template"/>, inserts the template visual tree as the
    /// sole Yoga child, and wires up any <see cref="ContentPresenter"/> found inside.
    /// </summary>
    public void ApplyTemplate()
    {
        // Tear down previous template
        if (_templateRoot != null)
        {
            ClearContentPresenter(_templateRoot);
            ClearTemplatedParentRecursive(_templateRoot);
            _templateContainer.Children.Remove(_templateRoot);
            _templateRoot = null;
        }

        var template = Template;
        if (template == null)
            return;

        var sp = AvaloniaXamlLoader.CreateDefaultServiceProvider(this);
        var built = template.Build(sp);
        if (built is not Node root)
            return;

        _templateRoot = root;
        _templateContainer.Children.Add(_templateRoot);

        // Set TemplatedParent on all nodes in the template tree so
        // {Binding ..., RelativeSource={RelativeSource TemplatedParent}} resolves correctly
        SetTemplatedParentRecursive(_templateRoot, this);

        // Find ContentPresenter(s) in the template tree and wire them up
        WireContentPresenters(_templateRoot);
    }

    private static void SetTemplatedParentRecursive(Visual node, TemplatedControl parent)
    {
        node.TemplatedParent = parent;
        if (node is FlexPanel box)
        {
            foreach (var child in box.Children)
                SetTemplatedParentRecursive(child, parent);
        }
    }

    private static void ClearTemplatedParentRecursive(Visual node)
    {
        node.TemplatedParent = null;
        foreach (var child in node.VisualChildren)
            ClearTemplatedParentRecursive(child);
    }

    private void WireContentPresenters(Visual node)
    {
        if (node is ContentPresenter presenter)
        {
            presenter.TemplatedParent = this;
            return;
        }

        foreach (var child in node.VisualChildren)
            WireContentPresenters(child);
    }

    private static void ClearContentPresenter(Visual node)
    {
        if (node is ContentPresenter presenter)
        {
            presenter.TemplatedParent = null;
            return;
        }

        foreach (var child in node.VisualChildren)
            ClearContentPresenter(child);
    }
}
