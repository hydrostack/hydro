using System.Reflection;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Hydro;

/// <summary>
/// Abstraction for a Razor view
/// </summary>
public abstract class HydroView : TagHelper
{
    /// <summary />
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <summary />
    private HtmlString _mainSlot;

    /// <summary />
    private readonly Dictionary<string, HtmlString> _slots = new();
    
    /// <summary />
    public Dictionary<string, object> Attributes { get; private set; }

    /// <summary />
    public object Attribute(string name) =>
        Attributes.GetValueOrDefault(name);
    
    /// <summary>
    /// Renders slot content
    /// </summary>
    /// <param name="name">Name of the slot or null when main slot</param>
    public HtmlString Slot(string name = null) =>
        name == null ? _mainSlot : _slots.GetValueOrDefault(name);
    
    /// <summary>
    /// Checks if given slot is defined
    /// </summary>
    /// <param name="name">Name of the slot</param>
    public bool HasSlot(string name) =>
        _slots.ContainsKey(name);

    /// <summary />
    public ModelStateDictionary ModelState => ViewContext.ViewData.ModelState;

    /// <inheritdoc />
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        await UpdateSlots(context, output);

        ApplyObjectFromDictionary(this, Attributes);
        
        var html = await GetViewHtml();

        output.TagName = null;
        output.Content.SetHtmlContent(html);
    }
    
    /// <summary>
    /// Used to reference other components in the event handlers
    /// </summary>
    public T Reference<T>() => default;

    private async Task<string> GetViewHtml()
    {
        var services = ViewContext.HttpContext.RequestServices;
        var compositeViewEngine = services.GetService<ICompositeViewEngine>();
        var modelMetadataProvider = services.GetService<IModelMetadataProvider>();

        var viewType = GetType();
        
        var view = GetView(compositeViewEngine, viewType)
            ?? GetView(compositeViewEngine, viewType, path => path.Replace("TagHelper.cshtml", ".cshtml"));
        
        await using var writer = new StringWriter();
        var viewDataDictionary = new ViewDataDictionary(modelMetadataProvider, ModelState)
        {
            Model = this
        };
        
        var viewContext = new ViewContext(ViewContext, view, viewDataDictionary, writer);

        await view.RenderAsync(viewContext);
        await writer.FlushAsync();
        return writer.ToString();
    }

    private async Task UpdateSlots(TagHelperContext context, TagHelperOutput output)
    {
        var slotContext = new SlotContext();
        context.Items.Add($"SlotContext{context.UniqueId}", slotContext);
        var childContent = await output.GetChildContentAsync();

        _mainSlot = new HtmlString(childContent.GetContent());

        foreach (var slot in slotContext.Items)
        {
            _slots.Add(slot.Key, slot.Value);
        }
        
        Attributes = context.AllAttributes.ToDictionary(a => a.Name, a => a.Value);
    }

    /// <summary>
    /// Get the view path based on the type
    /// </summary>
    private IView GetView(IViewEngine viewEngine, Type type, Func<string, string> nameConverter = null)
    {
        var assemblyName = type.Assembly.GetName().Name;
        var path = $"{type.FullName!.Replace(assemblyName!, "~").Replace(".", "/")}.cshtml";
        var adjustedPath = nameConverter != null ? nameConverter(path) : path;
        return viewEngine.GetView(null, adjustedPath, false).View;
    }
    
    private void ApplyObjectFromDictionary<T>(T target, IDictionary<string, object> source)
    {
        if (source == null || target == null)
        {
            return;
        }

        var targetType = target.GetType();

        foreach (var sourceProperty in source)
        {
            var targetProperty = targetType.GetProperty(sourceProperty.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (targetProperty == null || !targetProperty.CanWrite)
            {
                continue;
            }

            var value = sourceProperty.Value;

            if (value != null && !targetProperty.PropertyType.IsInstanceOfType(value))
            {
                throw new InvalidCastException($"Type mismatch in {sourceProperty.Key} parameter.");
            }

            targetProperty.SetValue(target, value);
        }
    }
}

/// <summary />
[Obsolete("Use HydroView type instead")]
public abstract class HydroTagHelper : HydroView
{
}