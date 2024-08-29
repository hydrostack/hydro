using Microsoft.AspNetCore.Mvc.Razor;

namespace Hydro;

/// <summary />
public static class HydroPageExtensions
{
    /// <summary>
    /// Specifies selector to use when replacing content of the page
    /// </summary>
    public static void HydroTarget(this IRazorPage page, string selector = $"#{HydroComponent.LocationTargetId}", string title = null)
    {
        page.ViewContext.HttpContext.Response.Headers.TryAdd(HydroConsts.ResponseHeaders.LocationTarget, selector);

        if (!string.IsNullOrEmpty(title))
        {
            page.ViewContext.HttpContext.Response.Headers.TryAdd(HydroConsts.ResponseHeaders.LocationTitle, title);
        }
    }
}