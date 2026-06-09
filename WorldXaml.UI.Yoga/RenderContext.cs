using System.Numerics;

namespace WorldXaml.UI.Yoga;

public readonly record struct RenderContext(Vector2 TopLeft, float InheritedOpacity = 1f);