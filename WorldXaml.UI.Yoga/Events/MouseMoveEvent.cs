using System.Numerics;

namespace WorldXaml.UI.Yoga.Events;

public readonly record struct MouseMoveEvent(
    Vector2 Position,
    MouseButtons Buttons,
    bool CtrlKey,
    bool MetaKey,
    bool ShiftKey,
    Vector2 RelativePosition
);