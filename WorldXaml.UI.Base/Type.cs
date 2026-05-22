namespace WorldXaml.UI.Base;

public class TypeExtension(Type type)
{
    public Type ProvideValue(IServiceProvider serviceProvider)
    {
        return type;
    }
}