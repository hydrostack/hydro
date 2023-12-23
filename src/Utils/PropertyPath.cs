using System.Text.RegularExpressions;

namespace Hydro.Utils;

/// <summary>
/// Represents property value in a hierarchy
/// </summary>
public class PropertyPath
{
    /// <summary>
    /// Property name
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Array index, if applicable
    /// </summary>
    public int? Index { get; set; }
    
    /// <summary>
    /// Nested properties, if applicable
    /// </summary>
    public PropertyPath Child { get; set; }

    /// <summary>
    /// Returns the index of the array element, if applicable
    /// </summary>
    /// <returns></returns>
    public int GetIndex() => Index!.Value;
    
    internal static PropertyPath ExtractPropertyPath(string propertyPath)
    {
        var items = propertyPath.Split('.', 2);
        var match = Regex.Match(items[0], @"^([\w]+)(?:\[(\d+)+\])?$");

        return new PropertyPath
        {
            Name = match.Groups[1].Value,
            Index = match.Groups[2].Value != string.Empty ? int.Parse(match.Groups[2].Value) : null,
            Child = items.Length > 1 ? ExtractPropertyPath(items[1]) : null
        };
    }
}