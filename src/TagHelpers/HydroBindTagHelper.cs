using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Hydro.TagHelpers;

/// <summary>
/// Tag helper for binding
/// </summary>
[HtmlTargetElement("input", Attributes = $"{EventsAttributePrefix}*")]
[HtmlTargetElement("select", Attributes = $"{EventsAttributePrefix}*")]
[HtmlTargetElement("textarea", Attributes = $"{EventsAttributePrefix}*")]
[HtmlTargetElement("input", Attributes = EventsAttribute)]
[HtmlTargetElement("select", Attributes = EventsAttribute)]
[HtmlTargetElement("textarea", Attributes = EventsAttribute)]
public sealed class HydroBindTagHelper : TagHelper
{
    private const string EventsAttributePrefix = "hydro-bind:";
    private const string EventsAttribute = "hydro-bind";

    private IDictionary<string, bool> _events;

    /// <summary />
    [HtmlAttributeName(DictionaryAttributePrefix = EventsAttributePrefix)]
    public IDictionary<string, bool> Events
    {
        get => _events ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        set => _events = value;
    }
    
    /// <summary />
    [HtmlAttributeName(EventsAttribute)]
    public bool DefaultEvent { get; set; }

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

        if (modelType == null || (_events == null && !DefaultEvent))
        {
            return;
        }

        if (DefaultEvent)
        {
            output.Attributes.Add(new("x-hydro-bind:change"));
        }
        
        if (_events != null)
        {
            foreach (var eventItem in _events)
            {
                var eventDefinition = eventItem.Key;
                
                output.Attributes.Add(new($"x-hydro-bind:{eventDefinition}"));
            }
        }
    }
}