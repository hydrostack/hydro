using JetBrains.Annotations;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Hydro;

/// <summary />
public static class HydroHtmlExtensions
{
    /// <summary>
    /// Renders hydro component
    /// </summary>
    public static Task<IHtmlContent> Hydro<TComponent>(this IViewComponentHelper helper, object parameters = null, string key = null) where TComponent : HydroComponent
    {
        var arguments = parameters != null || key != null
            ? new { parameters, key } 
            : null;
        
        return helper.InvokeAsync(typeof (TComponent), arguments);
    }
    
    /// <summary>
    /// Renders hydro component
    /// </summary>
    public static Task<IHtmlContent> Hydro(this IViewComponentHelper helper, [AspMvcViewComponent]string hydroComponentName, object parameters = null, string key = null)
    {
        var arguments = parameters != null || key != null
            ? new { parameters, key } 
            : null;
        
        return helper.InvokeAsync(hydroComponentName, arguments);
    }
}