using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Hydro;

internal class TagHelperRenderer
{
    private static readonly ConcurrentDictionary<string, PropertyInfo> PropertyCache = new();
    private static IList<TypeInfo> _tagHelpers;

    public static Type FindTagHelperType(string componentName, HttpContext httpContext)
    {
        if (_tagHelpers == null)
        {
            var applicationPartManager = httpContext.RequestServices.GetRequiredService<ApplicationPartManager>();

            var tagHelperFeature = new TagHelperFeature();
            applicationPartManager.PopulateFeature(tagHelperFeature);
            _tagHelpers = tagHelperFeature.TagHelpers.ToList();
        }

        return _tagHelpers.FirstOrDefault(t => t.Name == componentName)
               ?? throw new InvalidOperationException($"Hydro component {componentName} not found");
        ;
    }

    public static async Task<IHtmlContent> RenderTagHelper(Type componentType, HttpContext httpContext, IDictionary<string, object> parameters = null)
    {
        var serviceProvider = httpContext.RequestServices;
        var componentViewContext = CreateViewContext(httpContext, serviceProvider);
        return await RenderTagHelper(componentType, componentViewContext, parameters);
    }

    public static async Task<IHtmlContent> RenderTagHelper(Type componentType, ViewContext viewContext, IDictionary<string, object> parameters = null)
    {
        var tagHelperContext = new TagHelperContext(new(), new Dictionary<object, object>(), Guid.NewGuid().ToString("N"));

        var tagHelperOutput = new TagHelperOutput(
            tagName: null,
            attributes: new(parameters != null ? parameters.Select(kv => new TagHelperAttribute(kv.Key, kv.Value)) : Array.Empty<TagHelperAttribute>()),
            getChildContentAsync: (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        var tagHelper = (TagHelper)ActivatorUtilities.CreateInstance(viewContext.HttpContext.RequestServices, componentType);

        if (parameters != null)
        {
            ApplyParameters(tagHelper, componentType, parameters);
        }

        ((IViewContextAware)tagHelper).Contextualize(viewContext);
        await tagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);
        return tagHelperOutput.Content;
    }

    private static void ApplyParameters(TagHelper tagHelper, Type componentType, IDictionary<string, object> parameters)
    {
        foreach (var parameter in parameters)
        {
            var cacheKey = $"{componentType.FullName}.{parameter.Key}";
            var propertyInfo = PropertyCache.GetOrAdd(cacheKey, _ =>
                componentType.GetProperty(parameter.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(tagHelper, parameter.Value);
            }
        }
    }

    private static ViewContext CreateViewContext(HttpContext httpContext, IServiceProvider serviceProvider)
    {
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        var tempData = new TempDataDictionary(httpContext, serviceProvider.GetRequiredService<ITempDataProvider>());
        return new ViewContext(actionContext, new DummyView(), viewData, tempData, TextWriter.Null, new HtmlHelperOptions());
    }
}