// ReSharper disable once CheckNamespace
namespace Avalonia;

public interface IDataContextProvider
{
    /// <summary>
    /// Gets or sets the element's data context.
    /// </summary>
    object? DataContext { get; set; }
}