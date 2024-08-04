using JetBrains.Annotations;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Text.Encodings.Web;

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

        return helper.InvokeAsync(typeof(TComponent), arguments);
    }

    /// <summary>
    /// Renders hydro component
    /// </summary>
    public static Task<IHtmlContent> Hydro(this IViewComponentHelper helper, [AspMvcViewComponent] string hydroComponentName, object parameters = null, string key = null)
    {
        var arguments = parameters != null || key != null
            ? new { parameters, key }
            : null;

        return helper.InvokeAsync(hydroComponentName, arguments);
    }

    internal class WriteBody : IHtmlContent
    {
        private readonly IHtmlContent _body;

        public WriteBody(IHtmlContent body)
        {
            _body = body;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            writer.Write("<div id='hydro-location'>");
            _body.WriteTo(writer, encoder);
            writer.Write("</div>");
        }
    }

    /// <summary>
    /// To use @Html.HydroBody(RenderBody()) instead of RenderBody() to support ajax loads auto-target. This way in your _ViewStart you could set: Layout = Context.IsHydroBoosted() ? null : "_Layout"; and enjoy faster loads.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="bodyContent"></param>
    /// <returns></returns>
    public static IHtmlContent HydroBody(this IHtmlHelper htmlHelper, IHtmlContent body)
        => new WriteBody(body);

}