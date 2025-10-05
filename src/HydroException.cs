namespace Hydro;

/// <summary>
/// Hydro exception
/// </summary>
public class HydroException : Exception
{
    /// <inheritdoc />
    public HydroException(string message)
        : base(message)
    {
    }
    
    /// <inheritdoc />
    public HydroException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}