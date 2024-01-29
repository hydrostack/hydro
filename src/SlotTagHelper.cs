using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Hydro;

/// <summary>
/// Defines content for a view slot
/// </summary>
[HtmlTargetElement("slot", Attributes="name")]
public sealed class SlotTagHelper : TagHelper
{
    /// <summary>
    /// Slot name
    /// </summary>
    [HtmlAttributeName("name")]
    public string Name { get; set; }

    /// <inheritdoc />
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var slotContext = (SlotContext)context.Items.Values.LastOrDefault(i => i.GetType() == typeof(SlotContext));

        if (slotContext == null)
        {
            throw new InvalidOperationException("Cannot use slot without hydro tag helper as a parent");
        }

        var childTagHelperContent = await output.GetChildContentAsync();
        var childContent = childTagHelperContent.GetContent();
        
        slotContext.Items.TryAdd(Name, new HtmlString(childContent));
        output.SuppressOutput();
    }
}