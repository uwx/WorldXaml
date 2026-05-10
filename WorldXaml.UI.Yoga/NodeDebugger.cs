namespace WorldXaml.UI.Yoga;

public static class NodeDebugger
{
    public readonly struct DebugInfo(
        string ctorCallerFilePath,
        int ctorCallerLineNumber,
        string ctorCallerMemberName
    )
    {
        public readonly string CtorCallerFilePath = ctorCallerFilePath;
        public readonly string CtorCallerMemberName = ctorCallerMemberName;
        public readonly int CtorCallerLineNumber = ctorCallerLineNumber;
    }

    public static IReadOnlyList<Node> YogaRootsThisFrame => Node.__INTERNAL_YogaRootsThisFrame;

    public static DebugInfo GetDebugInfo(Node node)
    {
        return new DebugInfo(
            node.__INTERNAL_CtorCallerFilePath,
            node.__INTERNAL_CtorCallerLineNumber,
            node.__INTERNAL_CtorCallerMemberName
        );
    }

    public static void NewFrame()
    {
        Node.__INTERNAL_YogaRootsThisFrame.Clear();
    }
}