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
    private const string EventAttribute = "bind-event";
    private const string DebounceAttribute = "bind-debounce";

    /// <summary>
    /// Bind
    /// </summary>
    [HtmlAttributeName(TagAttribute)]
    public bool Bind { get; set; } = true;
    
    /// <summary>
    /// Debounce milliseconds
    /// </summary>
    [HtmlAttributeName(DebounceAttribute)]
    public int? Debounce { get; set; }
    
    /// <summary>
    /// Bind event
    /// </summary>
    [HtmlAttributeName(EventAttribute)]
    public string BindEvent { get; set; } = "change";
    
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

        var bindAttributeName = new List<string> { "x-hydro-bind" };

        if (Debounce != null)
        {
            bindAttributeName.Add($"debounce.{Debounce}");
        }
        
        output.Attributes.Add(new(string.Join(".", bindAttributeName), BindEvent));
        output.Attributes.Add(new("x-data"));
    }
}
