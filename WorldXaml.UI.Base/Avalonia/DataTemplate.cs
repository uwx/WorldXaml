using Avalonia.Metadata;

namespace Avalonia.Markup.Xaml.Templates;

public class DataTemplate
{
    [DataType]
    public Type? DataType { get; set; }

    [Content]
    [TemplateContent]
    public object? Content { get; set; }
}