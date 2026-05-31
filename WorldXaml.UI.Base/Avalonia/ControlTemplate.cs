using Avalonia.Metadata;

namespace Avalonia.Markup.Xaml.Templates;

public class ControlTemplate
{
    public Type? TargetType { get; set; }

    [Content]
    [TemplateContent]
    public object? Content { get; set; }

    /// <summary>
    /// Builds the template content tree by invoking the deferred content factory.
    /// </summary>
    public object? Build(IServiceProvider? serviceProvider)
    {
        if (Content is Func<IServiceProvider?, object> factory)
            return factory(serviceProvider);
        return null;
    }
}
