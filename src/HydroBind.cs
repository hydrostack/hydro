namespace Hydro;

/// <summary>
/// Event used to set property value on current component
/// </summary>
/// <param name="Name">Property name</param>
/// <param name="Value">Value to set</param>
public record HydroBind(string Name, string Value);
