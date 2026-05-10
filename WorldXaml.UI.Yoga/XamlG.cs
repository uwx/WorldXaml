using System.Numerics;
using Microsoft.Extensions.Logging;

namespace WorldXaml.UI.Yoga;

internal static class XamlG
{
    public static float Scale
    {
        get => IXamlGraphicsBackend.Backend.Scale;
    }

    public static float Alpha
    {
        set => IXamlGraphicsBackend.Backend.Graphics.Alpha = value;
    }
}

public interface IXamlGraphicsBackend
{
    /// <summary>
    /// Assign this in your project to provide the graphics implementation for WorldXaml.UI.Yoga. This must be set
    /// before any UI elements are created or used.
    /// </summary>
    public static IXamlGraphicsBackend Backend
    {
        internal get
        {
            return field ?? ThrowNotInitialized();

            IXamlGraphicsBackend ThrowNotInitialized()
            {
                throw new InvalidOperationException($"{nameof(IXamlGraphicsBackend)}.{nameof(Backend)} needs to be set before it can be used.");
            }
        }
        set;
    }

    /// <summary>
    /// Set this to the global scale to apply to all elements. This is useful for things like DPI scaling or in-game UI
    /// scaling.
    /// </summary>
    float Scale { get; }
    
    /// <summary>
    /// Set this to the size of your game's viewport in pixels. This is used for things like percentage-based sizes and
    /// for clipping.
    /// </summary>
    Vector2 Viewport { get; }
    
    /// <summary>
    /// Set this to an implementation of IXamlGraphics.
    /// </summary>
    IXamlGraphics Graphics { get; }
}

public interface IXamlGraphics
{
    /// <summary>
    /// We'll set this property based on the `Opacity` property of a given element, right before rendering it.
    /// </summary>
    float Alpha { set; }
}