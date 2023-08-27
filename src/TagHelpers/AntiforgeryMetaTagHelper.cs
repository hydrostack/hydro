using System.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Hydro.Configuration;
using Newtonsoft.Json;

namespace Hydro.TagHelpers;

/// <summary>
/// Provides Hydro options serialized to a meta tag
/// </summary>
[HtmlTargetElement("meta", Attributes = "[name=hydro-config]")]
public sealed class HydroConfigMetaTagHelper : TagHelper
{
    /// <summary>
    /// View context
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// Processes the output
    /// </summary>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var hydroOptions = ViewContext.HttpContext.RequestServices.GetService<HydroOptions>();
        
        var config = JsonConvert.SerializeObject(GetConfig(hydroOptions));

        output.Attributes.RemoveAll("content");

        output.Attributes.Add(new TagHelperAttribute(
            "content",
            new HtmlString(config),
            HtmlAttributeValueStyle.SingleQuotes)
        );
    }

    private object GetConfig(HydroOptions options) => new
    {
        Antiforgery = GetAntiforgeryConfig(options)
    };

    private AntiforgeryConfig GetAntiforgeryConfig(HydroOptions options)
    {
        if (!options.AntiforgeryTokenEnabled)
        {
            return null;
        }
        
        var antiforgery = ViewContext.HttpContext.RequestServices.GetService<IAntiforgery>();

        return antiforgery?.GetAndStoreTokens(ViewContext.HttpContext) is { } tokens
            ? new AntiforgeryConfig(tokens)
            : null;
    }

    private class AntiforgeryConfig
    {
        public AntiforgeryConfig(AntiforgeryTokenSet antiforgery)
        {
            ArgumentNullException.ThrowIfNull(antiforgery);

            HeaderName = antiforgery.HeaderName;
            Token = HttpUtility.HtmlAttributeEncode(antiforgery.RequestToken)!;
        }

        public string HeaderName { get; set; }
        public string Token { get; set; }
    }
}