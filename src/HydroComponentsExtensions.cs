using System.Net.Mime;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Hydro.Configuration;
using Newtonsoft.Json;

namespace Hydro;

internal static class HydroComponentsExtensions
{
    public static void MapHydroComponent(this IEndpointRouteBuilder app, Type componentType)
    {
        var componentName = componentType.Name;

        app.MapPost($"/hydro/{componentName}/{{method?}}", async ([FromServices] IServiceProvider serviceProvider, [FromServices] IViewComponentHelper viewComponentHelper, [FromServices] HydroOptions hydroOptions, [FromServices] IAntiforgery antiforgery, HttpContext httpContext, string method) =>
        {
            if (hydroOptions.AntiforgeryTokenEnabled)
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            
            if (httpContext.IsHydro())
            {
                await ExecuteRequestOperations(httpContext, method);
            }

            var viewContext = CreateViewContext(httpContext, serviceProvider);
            ((DefaultViewComponentHelper)viewComponentHelper).Contextualize(viewContext);
            var htmlContent = await viewComponentHelper.InvokeAsync(componentType);

            var content = await GetHtml(htmlContent);

            return Results.Content(content, MediaTypeNames.Text.Html);
        });
    }

    private static async Task ExecuteRequestOperations(HttpContext context, string method)
    {
        var requestModel = context.Request.Headers[HydroConsts.RequestHeaders.Model];
        var renderedComponentIDs = context.Request.Headers[HydroConsts.RequestHeaders.RenderedComponentIds];
        context.Items.Add(HydroConsts.ContextItems.RenderedComponentIds, JsonConvert.DeserializeObject<string[]>(renderedComponentIDs));

        if (!string.IsNullOrWhiteSpace(requestModel))
        {
            context.Items.Add(HydroConsts.ContextItems.BaseModel, requestModel.ToString());
        }

        if (!string.IsNullOrEmpty(method))
        {
            if (method == HydroConsts.Component.EventMethodName)
            {
                var eventName = context.Request.Headers[HydroConsts.RequestHeaders.EventName];

                if (!string.IsNullOrWhiteSpace(eventName))
                {
                    context.Items.Add(HydroConsts.ContextItems.EventName, eventName.ToString());
                }
            }
            else if (!string.IsNullOrWhiteSpace(method))
            {
                context.Items.Add(HydroConsts.ContextItems.MethodName, method);
            }
            else
            {
                context.Items.Add(HydroConsts.ContextItems.IsBind, true);
            }
        }

        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync();
            context.Items.Add(HydroConsts.ContextItems.RequestForm, form);
        }
        else
        {
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();

            if (!string.IsNullOrWhiteSpace(body))
            {
                if (method == "event")
                {
                    context.Items.Add(HydroConsts.ContextItems.EventData, body);
                }
                else
                {
                    context.Items.Add(HydroConsts.ContextItems.RequestData, body);
                }
            }
        }
    }

    private static async Task<string> GetHtml(IHtmlContent htmlContent)
    {
        await using var writer = new StringWriter();
        htmlContent.WriteTo(writer, HtmlEncoder.Default);
        await writer.FlushAsync();
        return writer.ToString();
    }

    private static ViewContext CreateViewContext(HttpContext httpContext, IServiceProvider serviceProvider)
    {
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        var tempData = new TempDataDictionary(httpContext, serviceProvider.GetRequiredService<ITempDataProvider>());
        return new ViewContext(actionContext, new DummyView(), viewData, tempData, TextWriter.Null, new HtmlHelperOptions());
    }
}