using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.LogicalTree;
using WorldXaml.UI.Base;

namespace WorldXaml.UI.Yoga;

/// <summary>
/// Represents a single node in the Yoga layout system without configurable properties for internal use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[DebuggerDisplay("{DebugToString()}")]
public partial class PlainNode : Visual, INamed, IDisposable, ILogical
{
    [Property]
    public partial string? Name { get; set; }

    internal static readonly YGConfigPtr Config;

    internal YGNodePtr NodeInternal = new(Config);

    internal readonly string __INTERNAL_CtorCallerFilePath = "";
    internal readonly int __INTERNAL_CtorCallerLineNumber = 0;
    internal readonly string __INTERNAL_CtorCallerMemberName = "";

    internal static readonly List<Node> __INTERNAL_YogaRootsThisFrame = new();

    public override IReadOnlyList<ILogical> LogicalChildren => [];
    public override IReadOnlyList<Visual> VisualChildren => [];
    
    internal override YGNodePtr Contents => NodeInternal;

#if DEBUG
    [MethodImpl(MethodImplOptions.NoInlining)]
#endif
    public PlainNode()
    {
#if DEBUG
        var stackTrace = new StackTrace(1, true);
        // skip inherited constructors
        var stackFrame = stackTrace.GetFrames()
            .FirstOrDefault(e => e.GetMethod()?.DeclaringType?.IsAssignableTo(typeof(Node)) != true);
        __INTERNAL_CtorCallerFilePath = stackFrame?.GetFileName() ?? "";
        __INTERNAL_CtorCallerLineNumber = stackFrame?.GetFileLineNumber() ?? 0;
        __INTERNAL_CtorCallerMemberName = stackFrame?.GetMethod()?.Name ?? "";
#endif
    }

    static PlainNode()
    {
        Config = YGConfigPtr.GetDefault();
        Config.UseWebDefaults = true;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual string DebugToString()
    {
        return $"PlainNode(Name={Name})";
    }

    ~PlainNode()
    {
        Dispose(false);
    }

    private void ReleaseUnmanagedResources()
    {
        NodeInternal.Dispose();
    }

    protected virtual void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
            // Free any other managed objects here.
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

}