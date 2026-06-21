using Avalonia;
using Avalonia.Controls;
using WorldXaml.UI.Base;

namespace Avalonia.Styling;

/// <summary>
/// Defines a set of property values that can be applied to elements.
/// Styles can target elements by type and/or class.
///
/// Usage:
/// &lt;Style Selector="Button.primary"&gt;
///     &lt;Setter Property="Background" Value="Blue" /&gt;
/// &lt;/Style&gt;
/// 
/// &lt;Style Selector="Button"&gt;
///     &lt;Setter Property="FontSize" Value="18" /&gt;
/// &lt;/Style&gt;
/// </summary>
public class Style
{
    private Type? _targetType;
    private readonly List<string> _classes = [];

    /// <summary>
    /// CSS-like selector string (e.g., "Button.primary", "TextBlock.h1").
    /// Parsed into TargetType and Classes.
    /// </summary>
    public string? Selector
    {
        get => _targetType is null ? null : $"{_targetType.Name}{(_classes.Count > 0 ? "." + string.Join(".", _classes) : "")}";
        set
        {
            _targetType = null;
            _classes.Clear();
            Setters.Clear();

            if (string.IsNullOrWhiteSpace(value)) return;

            var parts = value!.Split('.', StringSplitOptions.TrimEntries);
            if (parts.Length == 0) return;

            // First part is the type name — resolved later via XmlnsDefinition
            // For now, store the selector string and resolve lazily
            _selectorString = value;
        }
    }

    private string? _selectorString;

    /// <summary>
    /// The type this style targets. Set explicitly or parsed from Selector.
    /// </summary>
    public Type? TargetType
    {
        get => _targetType;
        set => _targetType = value;
    }

    /// <summary>
    /// Class names from the selector that must be present on the element.
    /// </summary>
    public IList<string> Classes => _classes;

    /// <summary>
    /// Property setters applied to matching elements.
    /// </summary>
    public List<Setter> Setters { get; set; } = [];

    /// <summary>
    /// Local resources owned by this style.
    /// </summary>
    public ResourceDictionary? Resources { get; set; }

    /// <summary>
    /// Returns true when the given element matches this style's selector.
    /// </summary>
    public bool Matches(BindableObject element)
    {
        // Type check
        if (_targetType is not null)
        {
            if (!_targetType.IsAssignableFrom(element.GetType()))
                return false;
        }
        else if (_selectorString is not null)
        {
            // Lazy-resolve type from the first segment
            var parts = _selectorString.Split('.', StringSplitOptions.TrimEntries);
            if (parts.Length > 0)
            {
                var typeName = parts[0];
                var resolved = Type.GetType(typeName)
                    ?? AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => a.GetType(typeName))
                        .FirstOrDefault(t => t is not null);
                if (resolved is not null)
                    _targetType = resolved;
                else
                    return false; // Type not found
            }
        }

        // Class check — all specified classes must be present
        if (_classes.Count > 0)
        {
            var elementClasses = element.Classes;
            foreach (var c in _classes)
            {
                if (!elementClasses.Contains(c))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Apply this style's setters to the target element.
    /// </summary>
    public void Apply(BindableObject target)
    {
        foreach (var setter in Setters)
            setter.Apply(target);
    }
}
