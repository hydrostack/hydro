using Microsoft.AspNetCore.Html;

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
}
