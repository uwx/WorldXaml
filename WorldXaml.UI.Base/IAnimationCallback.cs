namespace WorldXaml.UI.Base;

public interface IAnimationCallback
{
    /// <summary>
    /// Invoked on every frame, right before element or its children are rendered.
    /// </summary>
    public event Action? AnimationFrameBegan;
}