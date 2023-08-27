using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Hydro.TagHelpers;

/// <summary>
/// Provides a mechanism to load the target url in the background and witch the body content when ready
/// </summary>
[HtmlTargetElement("*", Attributes = TagAttribute)]
public sealed class HydroBoostTagHelper : TagHelper
{
    private const string TagAttribute = "hydro-boost";

    /// <summary>
    /// Attribute that triggers the boost behavior
    /// </summary>
    [HtmlAttributeName(TagAttribute)]
    public bool Boost { get; set; } = true;
    
    /// <summary>
    /// Processes the tag helper
    /// </summary>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        output.Attributes.Add("x-data", "");
        output.Attributes.Add("x-boost", "");
    }
}
