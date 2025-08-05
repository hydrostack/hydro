using System.Net.Mime;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Hydro.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Hydro;

internal static class HydroComponentsExtensions
{
    public static void MapHydroComponent(this IEndpointRouteBuilder app, Type componentType)
    {
        var componentName = componentType.Name;

        app.MapPost($"/hydro/{componentName}/{{method?}}", async (
            [FromServices] IServiceProvider serviceProvider,
            [FromServices] IViewComponentHelper viewComponentHelper,
            [FromServices] HydroOptions hydroOptions,
            [FromServices] IAntiforgery antiforgery,
            [FromServices] ILogger<HydroComponent> logger,
            HttpContext httpContext,
            string method
        ) =>
        {
            if (hydroOptions.AntiforgeryTokenEnabled)
            {
                try
                {
                    await antiforgery.ValidateRequestAsync(httpContext);
                }
                catch (AntiforgeryValidationException exception)
                {
                    logger.LogWarning(exception, "Antiforgery token not valid");
                    var requestToken = antiforgery.GetTokens(httpContext).RequestToken;
                    httpContext.Response.Headers.Append(HydroConsts.ResponseHeaders.RefreshToken, requestToken);
                    return Results.BadRequest(new { token = requestToken });
                }
            }

            if (httpContext.IsHydro())
            {
                await ExecuteRequestOperations(httpContext, method);
            }

            var htmlContent = await TagHelperRenderer.RenderTagHelper(componentType, httpContext);

            if (httpContext.Response.Headers.ContainsKey(HydroConsts.ResponseHeaders.SkipOutput))
            {
                return HydroEmptyResult.Instance;
            }
            
            var content = await GetHtml(htmlContent);
            return Results.Content(content, MediaTypeNames.Text.Html);
        });
    }

    private static async Task ExecuteRequestOperations(HttpContext context, string method)
    {
        if (!context.Request.HasFormContentType)
        {
            throw new InvalidOperationException("Hydro form doesn't contain form which is required");
        }

        var hydroData = await context.Request.ReadFormAsync();

        var formValues = hydroData
            .Where(f => !f.Key.StartsWith("__hydro"))
            .ToDictionary(f => f.Key, f => f.Value);

        var model = hydroData["__hydro_model"].First();
        var type = hydroData["__hydro_type"].First();
        var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(hydroData["__hydro_parameters"].FirstOrDefault("{}"), HydroComponent.JsonSerializerSettings);
        var eventData = JsonConvert.DeserializeObject<HydroEventPayload>(hydroData["__hydro_event"].FirstOrDefault(string.Empty));
        var componentIds = JsonConvert.DeserializeObject<string[]>(hydroData["__hydro_componentIds"].FirstOrDefault("[]"));
        var form = new FormCollection(formValues, hydroData.Files);

        context.Items.Add(HydroConsts.ContextItems.RenderedComponentIds, componentIds);
        context.Items.Add(HydroConsts.ContextItems.BaseModel, model);
        context.Items.Add(HydroConsts.ContextItems.Parameters, parameters);

        if (eventData != null)
        {
            context.Items.Add(HydroConsts.ContextItems.EventName, eventData.Name);
            context.Items.Add(HydroConsts.ContextItems.EventData, eventData.Data);
            context.Items.Add(HydroConsts.ContextItems.EventSubject, eventData.Subject);
        }

        if (!string.IsNullOrWhiteSpace(method) && type != "event")
        {
            context.Items.Add(HydroConsts.ContextItems.MethodName, method);
        }

        if (form.Any() || form.Files.Any())
        {
            context.Items.Add(HydroConsts.ContextItems.RequestForm, form);
        }
    }

    private static async Task<string> GetHtml(IHtmlContent htmlContent)
    {
        await using var writer = new StringWriter();
        htmlContent.WriteTo(writer, HtmlEncoder.Default);
        await writer.FlushAsync();
        return writer.ToString();
    }
}