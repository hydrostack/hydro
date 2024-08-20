using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using static Hydro.ExpressionExtensions;

namespace Hydro.TagHelpers;

/// <summary>
/// Tag helper for event handlers
/// </summary>
[HtmlTargetElement("*", Attributes = $"{HandlersPrefix}*")]
public sealed class HydroOnTagHelper : TagHelper
{
    private const string HandlersPrefix = "hydro-on:";

    private IDictionary<string, Expression> _handlers;

    /// <summary />
    [HtmlAttributeName(DictionaryAttributePrefix = HandlersPrefix)]
    public IDictionary<string, Expression> Handlers
    {
        get => _handlers ??= new Dictionary<string, Expression>(StringComparer.OrdinalIgnoreCase);
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
            if (eventItem.Value is not LambdaExpression actionExpression)
            {
                throw new InvalidOperationException($"Wrong event handler statement in component for {modelType.Namespace}");
            }
            
            var jsExpression = GetJsExpression(actionExpression);

            if (jsExpression == null)
            {
                continue;
            }

            var eventDefinition = eventItem.Key;
            output.Attributes.RemoveAll(HandlersPrefix + eventDefinition);
            output.Attributes.Add(new TagHelperAttribute($"x-on:{eventDefinition}", new HtmlString(jsExpression), HtmlAttributeValueStyle.SingleQuotes));

            if (Disable || new[] { "click", "submit" }.Any(e => e.StartsWith(e)))
            {
                output.Attributes.Add(new("data-loading-disable"));
            }
        }
    }

    private static string GetJsExpression(LambdaExpression expression)
    {
        var clientAction = GetJsClientActionExpression(expression);

        if (clientAction != null)
        {
            return clientAction;
        }

        return GetJsInvokeExpression(expression);
    }

    private static string GetJsClientActionExpression(LambdaExpression expression)
    {
        if (expression is not { Body: MethodCallExpression methodCall }
            || methodCall.Method.DeclaringType != typeof(HydroClientActions))
        {
            return null;
        }
        
        switch (methodCall.Method.Name)
        {
            case nameof(HydroClientActions.ExecuteJs):
            case nameof(HydroClientActions.Invoke):
                var expressionValue = EvaluateExpressionValue(methodCall.Arguments[0]);
                return ReplaceJsQuotes(expressionValue?.ToString());
            
            default:
                return null;
        }
    }

    private static string GetJsInvokeExpression(LambdaExpression expression)
    {
        var eventData = expression.GetNameAndParameters();

        if (eventData == null)
        {
            return null;
        }

        var invokeJson = JsonConvert.SerializeObject(new
        {
            eventData.Value.Name,
            eventData.Value.Parameters
        }, JsonSettings.SerializerSettings);

        var invokeJsObject = DecodeJsExpressionsInJson(invokeJson);

        return $"invoke($event, {invokeJsObject})";
    }
}