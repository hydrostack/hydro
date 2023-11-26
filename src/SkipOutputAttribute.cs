namespace Hydro;


/// <summary>
/// Skips generating the HTML output after executing the decorated Hydro action.
/// Any changes to the state won't be persisted.
/// Useful when action is performing only side effects that do not cause changes to the current component's HTML content.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class SkipOutputAttribute : Attribute
{
}