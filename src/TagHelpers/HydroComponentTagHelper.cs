using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Hydro.TagHelpers;

/// <summary>
/// Provides a binding from the DOM element to the Hydro action
/// </summary>
[HtmlTargetElement("hydro", Attributes = NameAttribute, TagStructure = TagStructure.WithoutEndTag)]
public sealed class HydroComponentTagHelper : TagHelper
{
    private const string NameAttribute = "name";
    private const string ParametersAttribute = "params";
    private const string ParametersPrefix = "param-";

    private Dictionary<string, object> _parameters;

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
    public Dictionary<string, object> ParametersDictionary
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
    /// Component's HTML behavior when the key changes
    /// </summary>
    [HtmlAttributeName("key-behavior")]
    public KeyBehavior KeyBehavior { get; set; } = KeyBehavior.Replace;

    /// <summary>
    /// Processes the tag helper
    /// </summary>
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        output.TagName = null;
        var componentHtml = await GetTagHelperHtml();
        output.Content.SetHtmlContent(componentHtml);
    }

    private async Task<IHtmlContent> GetTagHelperHtml()
    {
        var tagHelper = TagHelperRenderer.FindTagHelperType(Name, ViewContext.HttpContext);

        var parameters = (Parameters != null ? PropertyExtractor.GetPropertiesFromObject(Parameters) : _parameters ?? new())
            .Append(new(nameof(Key), Key))
            .Append(new(nameof(KeyBehavior), KeyBehavior))
            .ToDictionary(p => p.Key, p => p.Value);

        return await TagHelperRenderer.RenderTagHelper(tagHelper, ViewContext, parameters);
    }
}