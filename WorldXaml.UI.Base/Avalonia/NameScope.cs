using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace WorldXaml.UI.Base.Avalonia;

public sealed class NameScope(ILogical node) : INameScope
{
    public object? Find(string name)
    {
        return Find<ILogical>(name);
    }
    
    /// <summary>
    /// Recursively finds a child node by name.
    /// </summary>
    public T? Find<T>(string name) where T : ILogical
    {
        return FindChildByNameRecursive<T>(node, name);
    }

    private static T? FindChildByNameRecursive<T>(ILogical parent, string name) where T : ILogical
    {
        foreach (var child in parent.LogicalChildren)
        {
            if (child is INamed { Name: var childName } && childName == name && child is T typed)
                return typed;

            var found = FindChildByNameRecursive<T>(child, name);
            if (found != null)
                return found;
        }

        return default;
    }
}