using Hydro.Utils;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Hydro.TagHelpers;

/// <summary>
/// Provides a binding from the DOM element to the Hydro action
/// </summary>
[HtmlTargetElement("*", Attributes = DispatchAttribute)]
[Obsolete("Use hydro-on instead")]
public sealed class HydroDispatchTagHelper : TagHelper
{
    private const string DispatchAttribute = "hydro-dispatch";
    private const string ScopeAttribute = "event-scope";
    private const string TriggerAttribute = "event-trigger";

    /// <summary>
    /// View context
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// Triggering event
    /// </summary>
    [HtmlAttributeName(DispatchAttribute)]
    public object Data { get; set; }
    
    /// <summary>
    /// Bind event
    /// </summary>
    [HtmlAttributeName(ScopeAttribute)]
    public Scope Scope { get; set; } = Scope.Parent;
    
    /// <summary>
    /// Triggering event
    /// </summary>
    [HtmlAttributeName(TriggerAttribute)]
    public string BrowserTriggerEvent { get; set; }
    
    /// <summary>
    /// Processes the tag helper
    /// </summary>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);
        
        var data = new
        {
            name = GetFullTypeName(Data.GetType()),
            data = Base64.Serialize(Data),
            scope = Scope
        };
        
        output.Attributes.Add(new(
            "x-hydro-dispatch",
            new HtmlString(JsonConvert.SerializeObject(data)),
            HtmlAttributeValueStyle.SingleQuotes)
        );
        
        if (!string.IsNullOrWhiteSpace(BrowserTriggerEvent))
        {
            output.Attributes.Add("hydro-event", BrowserTriggerEvent);
        }
    }
    
    private static string GetFullTypeName(Type type) =>
        type.DeclaringType != null
            ? type.DeclaringType.Name + "+" + type.Name
            : type.Name;
}
