using System.Numerics;

namespace WorldXaml.UI.Yoga.Events;

public readonly record struct MouseEvent(
    Vector2 Position,
    MouseButton Button,
    MouseButtons Buttons,
    bool CtrlKey,
    bool MetaKey,
    bool ShiftKey,
    Vector2 RelativePosition
);