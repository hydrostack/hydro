using System.Net.Mime;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hydro;

/// <summary />
public static class HydroHtmlExtensions
{
    /// <summary>
    /// Renders hydro component
    /// </summary>
    public static async Task<IHtmlContent> Hydro<TComponent>(this IHtmlHelper helper, object parameters = null, string key = null) where TComponent : HydroComponent
    {
        var arguments = parameters != null || key != null
            ? new { Params = parameters, Key = key }
            : null;

        return await TagHelperRenderer.RenderTagHelper(
            componentType: typeof(TComponent),
            httpContext: helper.ViewContext.HttpContext,
            parameters: PropertyExtractor.GetPropertiesFromObject(arguments)
        );
    }

    /// <summary>
    /// Renders hydro component
    /// </summary>
    public static async Task<IHtmlContent> Hydro(this IHtmlHelper helper, string hydroComponentName, object parameters = null, string key = null)
    {
        var tagHelper = TagHelperRenderer.FindTagHelperType(hydroComponentName, helper.ViewContext.HttpContext);

        var arguments = parameters != null || key != null
            ? new { Params = parameters, Key = key }
            : null;

        return await TagHelperRenderer.RenderTagHelper(
            componentType: tagHelper,
            httpContext: helper.ViewContext.HttpContext,
            parameters: PropertyExtractor.GetPropertiesFromObject(arguments)
        );
    }
}