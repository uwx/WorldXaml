using System.Numerics;

namespace WorldXaml.UI.Yoga.Events;

public readonly record struct BaseMouseDragEvent(
    Vector2 DragStart,
    Vector2 Position,
    byte Button,
    MouseButtons Buttons,
    bool CtrlKey,
    bool MetaKey,
    bool ShiftKey
);