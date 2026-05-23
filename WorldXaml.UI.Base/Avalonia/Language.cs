// ReSharper disable once CheckNamespace

namespace System.Windows.Markup
{
    /// <summary>
    /// Maps an XML namespace to a CLR namespace for use in XAML.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class XmlnsDefinitionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlnsDefinitionAttribute"/> class.
        /// </summary>
        /// <param name="xmlNamespace">The URL of the XML namespace.</param>
        /// <param name="clrNamespace">The CLR namespace.</param>
        public XmlnsDefinitionAttribute(string xmlNamespace, string clrNamespace)
        {
            XmlNamespace = xmlNamespace;
            ClrNamespace = clrNamespace;
        }

        /// <summary>
        /// Gets or sets the URL of the XML namespace.
        /// </summary>
        public string XmlNamespace { get; }

        /// <summary>
        /// Gets or sets the CLR namespace.
        /// </summary>
        public string ClrNamespace { get; }
    }

    /// <summary>
    /// Use to predefine the prefix associated to an xml namespace in a xaml file
    /// </summary>
    /// <remarks>
    /// example:
    /// [assembly: XmlnsPrefix("https://github.com/avaloniaui", "av")]
    /// xaml:
    /// xmlns:av="https://github.com/avaloniaui"
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class XmlnsPrefixAttribute : Attribute
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xmlNamespace">XML namespce</param>
        /// <param name="prefix">recommended prefix</param>
        public XmlnsPrefixAttribute(string xmlNamespace, string prefix)
        {
            XmlNamespace = xmlNamespace ?? throw new ArgumentNullException(nameof(xmlNamespace));

            Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        }

        /// <summary>
        /// XML Namespace
        /// </summary>
        public string XmlNamespace { get; }

        /// <summary>
        /// New Xml Namespace
        /// </summary>
        public string Prefix { get; }
    }
}

namespace Avalonia.Metadata
{
    public interface IAddChild
    {
        void AddChild(object child);
    }

    public interface IAddChild<T> : IAddChild
    {
        void AddChild(T child);
    }

    /// <summary>
    /// Defines the property that contains the object's content in markup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ContentAttribute : Attribute;

    /// <summary>
    /// Indicates that a collection type should be processed as being whitespace significant by a XAML processor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class WhitespaceSignificantCollectionAttribute : Attribute;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class TrimSurroundingWhitespaceAttribute : Attribute;

    /// <summary>
    /// Marks a class as usable during XAML initialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class UsableDuringInitializationAttribute : Attribute
    {
        public UsableDuringInitializationAttribute(bool usable)
        {
            
        }
    }

    /// <summary>
    /// Defines the property that contains the object's content in markup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class TemplateContentAttribute : Attribute
    {
        public Type? TemplateResultType { get; set; }
    }


    /// <summary>
    /// Defines the property that contains type that should be used as a type information for compiled bindings.
    /// </summary>
    /// <remarks>
    /// Used on DataTemplate.DataType property so it can be inherited in compiled bindings inside of the template.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DataTypeAttribute : Attribute
    {

    }
}