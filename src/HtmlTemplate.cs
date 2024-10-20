using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hydro;

/// <summary>
/// Html template
/// </summary>
public delegate IHtmlContent HtmlTemplate(object obj);

/// <summary>
/// Extensions for HtmlTemplate
/// </summary>
public static class HtmlTemplateExtensions
{
    /// <summary>
    /// Renders HtmlTemplate
    /// </summary>
    /// <param name="htmlTemplate">Instance of HtmlTemplate</param>
    /// <returns>Html content</returns>
    public static IHtmlContent Render(this HtmlTemplate htmlTemplate) =>
        htmlTemplate(null);

    /// <summary>
    /// Prepares template to be rendered
    /// </summary>
    /// <param name="htmlHelper">Html helper</param>
    /// <param name="content">Html template followed by @ sign</param>
    public static IHtmlContent Template(this IHtmlHelper htmlHelper, Func<object, IHtmlContent> content) => content(null);
}
