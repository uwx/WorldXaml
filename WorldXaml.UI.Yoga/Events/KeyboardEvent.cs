namespace WorldXaml.UI.Yoga.Events;

public readonly record struct KeyboardEvent(
    Key KeyChar,
    Key KeyCode,
    Keys Keys
);