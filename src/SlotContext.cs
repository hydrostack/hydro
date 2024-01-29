using Microsoft.AspNetCore.Html;

namespace Hydro;

internal class SlotContext
{
    public Dictionary<string, HtmlString> Items { get; set; } = new();
}