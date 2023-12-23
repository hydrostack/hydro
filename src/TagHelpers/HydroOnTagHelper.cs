using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Hydro.TagHelpers;

/// <summary>
/// Tag helper for event handlers
/// </summary>
[HtmlTargetElement("*", Attributes = $"{HandlersPrefix}*")]
public sealed class HydroOnTagHelper : TagHelper
{
    private const string HandlersPrefix = "hydro-on:";

    private IDictionary<string, Expression<Action>> _handlers;

    /// <summary />
    [HtmlAttributeName(DictionaryAttributePrefix = HandlersPrefix)]
    public IDictionary<string, Expression<Action>> Handlers
    {
        get => _handlers ??= new Dictionary<string, Expression<Action>>(StringComparer.OrdinalIgnoreCase);
        set => _handlers = value;
    }
    
    /// <summary>
    /// Disable during execution
    /// </summary>
    [HtmlAttributeName("hydro-disable")]
    public bool Disable { get; set; }

    /// <summary />
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var modelType = ViewContext?.ViewData.ModelMetadata.ContainerType ?? ViewContext?.ViewData.Model?.GetType();

        if (modelType == null || _handlers == null)
        {
            return;
        }
        
        foreach (var eventItem in _handlers)
        {
            var eventData = eventItem.Value.GetNameAndParameters();

            if (eventData == null)
            {
                continue;
            }
            
            var eventDefinition = eventItem.Key;

            var invokeData = JsonConvert.SerializeObject(new
            {
                eventData.Value.Name,
                eventData.Value.Parameters
            }, JsonSettings.SerializerSettings);

            output.Attributes.RemoveAll(HandlersPrefix + eventDefinition);
            output.Attributes.Add(new TagHelperAttribute($"x-on:{eventDefinition}", new HtmlString($"invoke($event, {invokeData})"), HtmlAttributeValueStyle.SingleQuotes));

            if (Disable || new[] { "click", "submit" }.Any(e => e.StartsWith(e)))
            {
                output.Attributes.Add(new("data-loading-disable"));
            }
        }
    }
}