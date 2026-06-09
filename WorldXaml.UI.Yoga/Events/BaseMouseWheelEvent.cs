using System.Numerics;

namespace WorldXaml.UI.Yoga.Events;

public readonly record struct BaseMouseWheelEvent(
    Vector3 Delta,
    Vector2 Position,
    MouseButtons Buttons,
    bool CtrlKey,
    bool MetaKey,
    bool ShiftKey
);