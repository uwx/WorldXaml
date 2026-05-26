using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace WorldXaml.XamlX;

/// <summary>
/// Runs after ConstructableObjectTransformer. For each {Animation} node that was
/// wrapped in <see cref="XamlMarkupExtensionNode"/> (because Animation has
/// ProvideValue for IDE integration), unwraps the inner constructable object,
/// resolves the trigger path at build time, and produces a plain
/// <see cref="XamlAstConstructableObjectNode"/> with compiled Path and optional
/// NamedObject properties.
///
/// Because the original <see cref="XamlMarkupExtensionNode"/> has return type
/// <c>object</c>, the parent <see cref="XamlPropertyAssignmentNode"/> may not
/// contain a <see cref="IXamlILOptimizedEmitablePropertySetter"/> that accepts
/// <c>IXamlBinding</c>. This transformer also patches the parent's
/// <see cref="XamlPropertyAssignmentNode.PossibleSetters"/> so that the
/// <c>BindingSetter</c> from <see cref="PropertyObjectTransformer"/> is used
/// instead of falling through to the dynamic setter path.
///
/// Syntax: {Animation [NamedObject.]PropertyPath, Easing=..., KeyFrameFrom=..., ...}
/// - NamedObject: optional name of a sibling/parent element (found via NameScope at runtime).
///   If omitted, the target is the element the property is applied to.
/// - PropertyPath: dot-separated property chain (only properties, not fields) to an AnimationTrigger.
/// </summary>
#if !XAMLX_INTERNAL
public
#endif
class AnimationAutoCompileTransformer(
    string animationFqn,
    string clrPropertyInfoFqn,
    string resolvedPathFqn) : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        // MarkupExtensionTransformer wraps {Animation} in XamlMarkupExtensionNode
        // because Animation has a ProvideValue method (for Rider tooling).
        // Unwrap to get the inner constructable object.
        XamlAstConstructableObjectNode? animation;
        if (node is XamlMarkupExtensionNode meNode
            && meNode.Value is XamlAstConstructableObjectNode inner
            && inner.Type.GetClrType().FullName == animationFqn)
        {
            animation = inner;
        }
        else
        {
            return node;
        }

        // ── Extract the path string from the positional argument ─────────────
        string? pathStr = null;
        if (animation.Arguments.Count > 0 && animation.Arguments[0] is XamlAstTextNode textArg)
            pathStr = textArg.Text;

        if (pathStr == null)
            return node; // No path to resolve

        // ── Parse: [NamedObject.]PropertyPath ────────────────────────────────
        string? namedObject = null;
        string propertyPath;

        var dotIndex = pathStr.IndexOf('.');
        if (dotIndex >= 0)
        {
            namedObject = pathStr.Substring(0, dotIndex);
            propertyPath = pathStr.Substring(dotIndex + 1);
        }
        else
        {
            propertyPath = pathStr;
        }

        // ── Resolve the root type for path resolution ────────────────────────
        IXamlType rootType;

        if (namedObject != null)
        {
            // Search the XAML tree for an element with Name matching namedObject.
            // Use the outermost parent (the root of the document tree) as the search root.
            var documentRoot = context.ParentNodes().LastOrDefault();
            if (documentRoot == null)
            {
                context.ReportTransformError(
                    "Cannot search for named elements: no parent nodes available.", node);
                return node;
            }
            var foundType = FindNamedElementType(documentRoot, namedObject);
            if (foundType == null)
            {
                context.ReportTransformError(
                    $"Named element '{namedObject}' not found in the current XAML document.", node);
                return node;
            }
            rootType = foundType;
        }
        else
        {
            // Use the declaring type of the property being assigned
            var assignmentNode = context.ParentNodes()
                .OfType<XamlPropertyAssignmentNode>()
                .FirstOrDefault();
            if (assignmentNode == null)
            {
                context.ReportTransformError(
                    "Cannot determine target element type for Animation.", node);
                return node;
            }
            rootType = assignmentNode.Property.DeclaringType;
        }

        // ── Resolve the property path against the root type ──────────────────
        var ts = context.Configuration.TypeSystem;
        var segments = propertyPath.Split('.');
        var steps = new List<ResolvedPathStep>();
        var currentType = rootType;

        foreach (var seg in segments)
        {
            var prop = currentType.GetAllProperties().FirstOrDefault(p => p.Name == seg);
            if (prop?.Getter == null)
            {
                context.ReportTransformError(
                    $"Property '{seg}' not found on type '{currentType.GetFqn()}'.", node);
                return node;
            }
            steps.Add(new ResolvedPathStep(prop, currentType));
            currentType = prop.Getter.ReturnType;
        }

        // ── Build the resolved path node ─────────────────────────────────────
        var clrPropertyInfoType = ts.GetType(clrPropertyInfoFqn);
        var resolvedPathType = ts.GetType(resolvedPathFqn);
        var resolvedNode = new ResolvedBindingPathNode(
            node, steps, currentType, clrPropertyInfoType, resolvedPathType);

        // ── Build a new Animation node with parameterless ctor ───────────────
        var animationType = animation.Type.GetClrType();
        var defaultCtor = animationType.Constructors
            .FirstOrDefault(c => !c.IsStatic && c.IsPublic && c.Parameters.Count == 0)
            ?? throw new XamlTypeSystemException(
                $"Parameterless constructor not found on '{animationFqn}'.");

        var children = new List<IXamlAstNode>();

        // Add Path property assignment
        var pathProp = animationType.GetAllProperties().First(p => p.Name == "Path");
        var pathClrProp = new XamlAstClrProperty(
            node, pathProp.Name, animationType, pathProp.Getter,
            [pathProp.Setter], null);
        children.Add(new XamlPropertyAssignmentNode(
            node, pathClrProp, pathClrProp.Setters, [resolvedNode]));

        // Add NamedObject property assignment if present
        if (namedObject != null)
        {
            var namedObjProp = animationType.GetAllProperties().First(p => p.Name == "NamedObject");
            var namedObjClrProp = new XamlAstClrProperty(
                node, namedObjProp.Name, animationType, namedObjProp.Getter,
                [namedObjProp.Setter], null);
            children.Add(new XamlPropertyAssignmentNode(
                node, namedObjClrProp, namedObjClrProp.Setters,
                [new XamlAstTextNode(node, namedObject, type: context.Configuration.WellKnownTypes.String)]));
        }

        // Copy existing property assignments (Easing, KeyFrameFrom, etc.)
        foreach (var child in animation.Children)
            children.Add(child);

        var result = new XamlAstConstructableObjectNode(
            node,
            new XamlAstClrTypeReference(node, animationType, false),
            defaultCtor,
            [],
            children);

        // ── Patch parent PossibleSetters ──────────────────────────────────────
        // The parent XamlPropertyAssignmentNode was created by
        // ConvertPropertyValuesToAssignmentsTransformer when the value was still
        // XamlMarkupExtensionNode (return type: object). Its PossibleSetters
        // therefore do not include the BindingSetter (which takes IXamlBinding).
        // We must inject it so PropertyAssignmentEmitter uses
        // IXamlILOptimizedEmitablePropertySetter.EmitWithArguments instead of
        // falling through to the dynamic-setter code path (which emits method
        // names with <> that are invalid in C# source).
        var parentAssignment = context.ParentNodes()
            .OfType<XamlPropertyAssignmentNode>()
            .FirstOrDefault();
        if (parentAssignment != null)
        {
            // Find the BindingSetter from Property.Setters — it was placed there
            // by PropertyObjectTransformer and accepts IXamlBinding.
            var bindingSetter = parentAssignment.Property.Setters
                .OfType<IXamlILOptimizedEmitablePropertySetter>()
                .FirstOrDefault(s =>
                    s.Parameters.Count == 1
                    && s.Parameters[0].IsAssignableFrom(animationType));

            if (bindingSetter != null)
            {
                parentAssignment.PossibleSetters.Clear();
                parentAssignment.PossibleSetters.Add(bindingSetter);
            }
        }

        return result;
    }

    /// <summary>
    /// Recursively searches the XAML AST tree for an element node with
    /// a Name property assignment matching <paramref name="name"/>.
    /// Returns the CLR type of that element, or null if not found.
    /// </summary>
    private static IXamlType? FindNamedElementType(IXamlAstNode root, string name)
    {
        var visitor = new NameSearchVisitor(name);
        root.Visit(visitor);
        return visitor.Result;
    }

    private sealed class NameSearchVisitor(string name) : IXamlAstVisitor
    {
        public IXamlType? Result { get; private set; }

        public IXamlAstNode Visit(IXamlAstNode node)
        {
            if (Result != null) return node;

            if (node is XamlAstConstructableObjectNode obj)
            {
                foreach (var child in obj.Children)
                {
                    if (child is XamlPropertyAssignmentNode propAssign
                        && propAssign.Property.Name == "Name"
                        && propAssign.Values is [XamlAstTextNode text]
                        && text.Text == name)
                    {
                        Result = obj.Type.GetClrType();
                        return node;
                    }
                }
            }

            return node;
        }

        public void Push(IXamlAstNode node) { }
        public void Pop() { }
    }
}
