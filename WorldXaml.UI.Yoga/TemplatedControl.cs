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
public class TemplatedControl : Node
{
    private Node? _templateRoot;

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
            NodeInternal.RemoveChild(_templateRoot.NodeInternal);
            _templateRoot.LogicalParent = null;
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
        root.LogicalParent = this;
        NodeInternal.InsertChild(root.NodeInternal, 0);

        // Set TemplatedParent on all nodes in the template tree so
        // {Binding ..., RelativeSource={RelativeSource TemplatedParent}} resolves correctly
        SetTemplatedParentRecursive(root, this);

        // Find ContentPresenter(s) in the template tree and wire them up
        WireContentPresenters(root);
    }

    private static void SetTemplatedParentRecursive(Node node, TemplatedControl parent)
    {
        node.TemplatedParent = parent;
        if (node is Box box)
        {
            foreach (var child in box.Children)
                SetTemplatedParentRecursive(child, parent);
        }
    }

    private static void ClearTemplatedParentRecursive(Node node)
    {
        node.TemplatedParent = null;
        if (node is Box box)
        {
            foreach (var child in box.Children)
                ClearTemplatedParentRecursive(child);
        }
    }

    private void WireContentPresenters(Node node)
    {
        if (node is ContentPresenter presenter)
        {
            presenter.TemplatedParent = this;
            return;
        }

        if (node is Box box)
        {
            foreach (var child in box.Children)
                WireContentPresenters(child);
        }
    }

    private static void ClearContentPresenter(Node node)
    {
        if (node is ContentPresenter presenter)
        {
            presenter.TemplatedParent = null;
            return;
        }

        if (node is Box box)
        {
            foreach (var child in box.Children)
                ClearContentPresenter(child);
        }
    }

    #region Recursive overrides — delegate to _templateRoot

    internal override void RescaleRecursive()
    {
        if (Rescale())
        {
            OnScaleChanged();
            _templateRoot?.RescaleRecursive();
        }
    }

    public sealed override void Update()
    {
        base.Update();
        _templateRoot?.Update();
    }

    internal override void RenderRecursive(Vector2 root, float rootOpacity = 1f)
    {
        OnAnimationFrameBegan();
        _root = root;
        if (Display != YgDisplay.None && Visibility == Visibility.Visible && Opacity > 0f)
        {
            var ownOpacity = rootOpacity * Opacity;
            XamlG.Alpha = ownOpacity;
            Render();
            _templateRoot?.RenderRecursive(root + new Vector2(LayoutX, LayoutY), ownOpacity);
            XamlG.Alpha = 1f;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_templateRoot != null)
            {
                ClearContentPresenter(_templateRoot);
                (_templateRoot as IDisposable)?.Dispose();
                _templateRoot = null;
            }
        }

        base.Dispose(disposing);
    }

    #endregion
}
