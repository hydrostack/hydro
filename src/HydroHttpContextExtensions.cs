using Microsoft.AspNetCore.Http;

namespace Hydro;

/// <summary>
/// Hydro extensions for HttpContext
/// </summary>
public static class HydroHttpContextExtensions
{
    /// <summary>
    /// Indicates if the request is going through Hydro's pipeline
    /// </summary>
    /// <param name="httpContext">HttpContext instance</param>
    /// <param name="excludeBoosted">Return false for boosted requests</param>
    public static bool IsHydro(this HttpContext httpContext, bool excludeBoosted = false) =>
        httpContext.Request.Headers.ContainsKey(HydroConsts.RequestHeaders.Hydro)
        && (!excludeBoosted || !httpContext.IsHydroBoosted());

    /// <summary>
    /// Indicates if the request is using Hydro's boost functionality
    /// </summary>
    /// <param name="httpContext">HttpContext instance</param>
    public static bool IsHydroBoosted(this HttpContext httpContext) =>
        httpContext.Request.Headers.ContainsKey(HydroConsts.RequestHeaders.Boosted);
}