namespace Hydro;

/// <summary>
/// Enable long polling for action decorated with this attribute
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class PollAttribute : Attribute
{
    /// <summary>
    /// How often action will be called. In milliseconds.
    /// </summary>
    public int Interval { get; set; } = 3_000;
}