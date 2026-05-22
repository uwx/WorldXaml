namespace WorldXaml.UI.Base;

/// <summary>
/// Implemented by markup extensions (e.g. {Bind}) so XamlX can call
/// PropertyObject.BindFromXaml(property, binding) at setup time.
/// </summary>
public interface IXamlBinding
{
    IDisposable Apply<TValue>(IBindingTarget target, Property<TValue> property);
}