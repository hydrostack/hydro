namespace Hydro;

/// <summary>
/// Skips serialization of property marked by this attributes
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class TransientAttribute : Attribute
{
}