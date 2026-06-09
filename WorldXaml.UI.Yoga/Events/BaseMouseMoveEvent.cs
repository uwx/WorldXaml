using System.Numerics;

namespace WorldXaml.UI.Yoga.Events;

public readonly record struct BaseMouseMoveEvent(
    Vector2 Position,
    MouseButtons Buttons,
    bool CtrlKey,
    bool AltKey,
    bool ShiftKey
);