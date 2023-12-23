using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Hydro.TagHelpers;

/// <summary>
/// Provides a binding from the DOM element to the Hydro action
/// </summary>
[HtmlTargetElement("*", Attributes = TagAttribute)]
public sealed class HydroActionTagHelper : TagHelper
{
    private const string TagAttribute = "hydro-action";
    private const string ParametersDictionaryName = "params";
    private const string ParametersPrefix = "param-";

    private IDictionary<string, object> _parameters;

    /// <summary>
    /// View context
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// Hydro component's action to execute
    /// </summary>
    [HtmlAttributeName(TagAttribute)]
    public string Method { get; set; }

    /// <summary>
    /// Parameters passed to the action
    /// </summary>
    [HtmlAttributeName(ParametersDictionaryName, DictionaryAttributePrefix = ParametersPrefix)]
    public IDictionary<string, object> Parameters
    {
        get => _parameters ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        set => _parameters = value;
    }

    /// <summary>
    /// Triggering event
    /// </summary>
    [HtmlAttributeName("hydro-event")]
    public string Event { get; set; }

    /// <summary>
    /// Disable during execution
    /// </summary>
    [HtmlAttributeName("hydro-disable")]
    public bool Disable { get; set; }

    /// <summary>
    /// Processes the tag helper
    /// </summary>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var eventName = Event ?? (context.TagName.ToLower() == "form" ? "submit" : "click");
        
        if (Disable || new[] { "click", "submit" }.Contains(eventName))
        {
            output.Attributes.Add(new("data-loading-disable"));
        }

        var invokeData = JsonConvert.SerializeObject(new
        {
            name = Method,
            parameters = _parameters
        });

        output.Attributes.Add(new TagHelperAttribute($"x-on:{eventName}.prevent", new HtmlString($"invoke($event, {invokeData})"), HtmlAttributeValueStyle.SingleQuotes));
    }
}