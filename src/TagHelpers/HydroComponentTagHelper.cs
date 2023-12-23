using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Hydro.TagHelpers;

/// <summary>
/// Provides a binding from the DOM element to the Hydro action
/// </summary>
[HtmlTargetElement("hydro", Attributes = NameAttribute)]
public sealed class HydroComponentTagHelper : TagHelper
{
    private const string NameAttribute = "name";
    private const string ParametersAttribute = "params";
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
    [HtmlAttributeName(NameAttribute)]
    [AspMvcViewComponent]
    public string Name { get; set; }

    /// <summary>
    /// Key of the component. Use it to distinguish same Hydro components on the same view 
    /// </summary>
    [HtmlAttributeName("key")]
    public string Key { get; set; }

    /// <summary>
    /// Parameters passed to the component
    /// </summary>
    [HtmlAttributeName(DictionaryAttributePrefix = ParametersPrefix)]
    public IDictionary<string, object> ParametersDictionary
    {
        get => _parameters ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        set => _parameters = value;
    }
    
    /// <summary>
    /// Parameters passed to the component
    /// </summary>
    [HtmlAttributeName(ParametersAttribute)]
    public object Parameters { get; set; }

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
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        output.TagName = null;
        
        var viewComponentHelper = ViewContext.HttpContext.RequestServices.GetService<IViewComponentHelper>();
        ((IViewContextAware)viewComponentHelper).Contextualize(ViewContext);

        var componentHtml = await viewComponentHelper.InvokeAsync(Name, new
        {
            parameters = Parameters ?? _parameters,
            key = Key
        });
        
        output.Content.SetHtmlContent(componentHtml);
    }
}