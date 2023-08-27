using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Hydro.TagHelpers;

/// <summary>
/// Provides a binding the model data
/// </summary>
[HtmlTargetElement("input", Attributes = TagAttribute)]
[HtmlTargetElement("select", Attributes = TagAttribute)]
[HtmlTargetElement("textarea", Attributes = TagAttribute)]
public sealed class HydroBindTagHelper : TagHelper
{
    private const string TagAttribute = "hydro-bind";

    /// <summary>
    /// Bind
    /// </summary>
    [HtmlAttributeName(TagAttribute)]
    public bool Bind { get; set; } = true;
    
    /// <summary>
    /// View context
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// Processes the tag helper
    /// </summary>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var modelType = ViewContext?.ViewData.ModelMetadata.ContainerType ?? ViewContext?.ViewData.Model?.GetType();
        
        if (modelType == null)
        {
            return;
        }
        
        output.Attributes.Add(new("x-hydro-bind"));
        output.Attributes.Add(new("x-data"));
    }
}
