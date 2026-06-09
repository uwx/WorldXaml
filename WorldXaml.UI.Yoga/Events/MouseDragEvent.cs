using System.Numerics;

namespace WorldXaml.UI.Yoga.Events;

public readonly record struct MouseDragEvent(
    Vector2 DragStart,
    Vector2 RelativeDragStart,
    Vector2 Position,
    byte Button,
    MouseButtons Buttons,
    bool CtrlKey,
    bool MetaKey,
    bool ShiftKey,
    Vector2 RelativePosition
);