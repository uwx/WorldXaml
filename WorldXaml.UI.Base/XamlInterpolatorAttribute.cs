namespace WorldXaml.UI.Base;

/// <summary>
/// <para>
/// Marks a method with the following signature:
/// <code>
///    public static MyType Interpolate(MyType from, MyType to, float alpha);
/// </code>
/// as a value interpolator to be registered into WorldXaml.
/// </para>
///
/// <para>
/// The interpolator must be contained in a non-nested, non-generic, internally visible type. The method should not have
/// overloads.
/// </para>
/// 
/// <para>
/// A basic interpolator implementation could look like:
/// <code>
/// from + (to - from) * alpha
/// </code>
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class XamlInterpolatorAttribute : Attribute;