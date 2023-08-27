namespace Hydro.Configuration;

/// <summary>
/// Hydro options
/// </summary>
public class HydroOptions
{
    /// <summary>
    /// Indicates if antiforgery token should be exchanged during the communication
    /// </summary>
    public bool AntiforgeryTokenEnabled { get; set; }
}