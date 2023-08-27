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
    public Delegate Action { get; set; }
    
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
    /// Delay of executing the action, in milliseconds
    /// </summary>
    [HtmlAttributeName("delay")]
    public int? Delay { get; set; } = 0;
    
    /// <summary>
    /// 
    /// </summary>
    [HtmlAttributeName("run")]
    public bool Run { get; set; }

    /// <summary>
    /// Processes the tag helper
    /// </summary>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        if (ViewContext?.ViewData.Model == null)
        {
            return;
        }
        
        output.Attributes.Add("x-hydro-action", $"/hydro/{ViewContext.ViewData.Model.GetType().Name}/{Action.Method.Name}".ToLower());

        if (Parameters.Any())
        {
            output.Attributes.Add(new TagHelperAttribute("hydro-parameters", new HtmlString(JsonConvert.SerializeObject(_parameters)), HtmlAttributeValueStyle.SingleQuotes));
        }

        output.Attributes.Add(new("x-data"));
        output.Attributes.Add(new("data-loading-disable"));

        if (output.TagName.ToLower() == "a" && !output.Attributes.ContainsName("href"))
        {
            output.Attributes.Add("href", "#");
        }
        
        if (Delay != null && Delay != 0)
        {
            output.Attributes.Add("hydro-delay", Delay.ToString());
        }
        
        if (!string.IsNullOrWhiteSpace(Event))
        {
            output.Attributes.Add("hydro-event", Event);
        }
        
        if (Run)
        {
            output.Attributes.Add("hydro-autorun", "true");
        }
    }
}
