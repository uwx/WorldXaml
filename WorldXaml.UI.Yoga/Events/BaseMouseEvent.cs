using System.Numerics;

namespace WorldXaml.UI.Yoga.Events;

public readonly record struct BaseMouseEvent(
    Vector2 Position,
    MouseButton Button,
    MouseButtons Buttons,
    bool CtrlKey,
    bool AltKey,
    bool ShiftKey
);