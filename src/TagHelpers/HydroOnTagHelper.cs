using System.Linq.Expressions;
using Hydro.Utils;
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
[HtmlTargetElement("*", Attributes = $"{HandlersPrefixShort}*")]
public sealed class HydroOnTagHelper : TagHelper
{
    private const string HandlersPrefix = "hydro-on:";
    private const string HandlersPrefixShort = "on:";

    private IDictionary<string, Expression> _handlers;
    private IDictionary<string, Expression> _handlersShort;

    /// <summary />
    [HtmlAttributeName(DictionaryAttributePrefix = HandlersPrefix)]
    public IDictionary<string, Expression> Handlers
    {
        get => _handlers ??= new Dictionary<string, Expression>(StringComparer.OrdinalIgnoreCase);
        set => _handlers = value;
    }
    
    /// <summary />
    [HtmlAttributeName(DictionaryAttributePrefix = HandlersPrefixShort)]
    public IDictionary<string, Expression> HandlersShort
    {
        get => _handlersShort ??= new Dictionary<string, Expression>(StringComparer.OrdinalIgnoreCase);
        set => _handlersShort = value;
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

        var handlers = _handlersShort ?? _handlers;
        
        if (modelType == null || handlers == null)
        {
            return;
        }

        foreach (var eventItem in handlers.Where(h => h.Value != null))
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
        if (expression is not { Body: MethodCallExpression methodCall })
        {
            throw new InvalidOperationException("Hydro action should contain a method call.");
        }

        var methodDeclaringType = methodCall.Method.DeclaringType;
        
        if (methodDeclaringType == typeof(HydroClientActions))
        {
            return GetClientActionExpression(expression);
        }

        return GetActionInvokeExpression(expression);
    }
    
    private static string GetClientActionExpression(LambdaExpression expression)
    {
        var methodCall = (MethodCallExpression)expression.Body;

        var methodName = methodCall.Method.Name;
        
        switch (methodName)
        {
            case nameof(HydroClientActions.ExecuteJs):
            case nameof(HydroClientActions.Invoke):
                var jsExpressionValue = EvaluateExpressionValue(methodCall.Arguments[0]);
                return ReplaceJsQuotes(jsExpressionValue?.ToString());

            case nameof(HydroComponent.Dispatch):
            case nameof(HydroComponent.DispatchGlobal):
                return GetDispatchInvokeExpression(expression, methodName);

            default:
                return null;
        }
    }

    private static string GetDispatchInvokeExpression(LambdaExpression expression, string methodName)
    {
        var dispatchData = expression.GetNameAndParameters();

        if (dispatchData == null)
        {
            return null;
        }

        var parameters = dispatchData.Value.Parameters;
                
        var data = parameters["data"];
        var scope = methodName == nameof(HydroComponent.DispatchGlobal) 
            ? Scope.Global 
            : GetParam<Scope>(parameters, "scope");
        var subject = GetParam<string>(parameters, "subject");

        var invokeData = new
        {
            name = GetFullTypeName(data.GetType()),
            data = Base64.Serialize(data),
            scope = scope.ToString().ToLower(),
            subject = subject
        };

        var invokeJson = JsonConvert.SerializeObject(invokeData, JsonSettings.SerializerSettings);
        var invokeJsObject = DecodeJsExpressionsInJson(invokeJson);

        return $"dispatch($event, {invokeJsObject})";
    }

    private static T GetParam<T>(IDictionary<string, object> parameters, string name, T fallback = default) =>
        (T)(parameters.TryGetValue(name, out var value)
            ? value is T ? value : default(T)
            : default(T)
        );

    private static string GetFullTypeName(Type type) =>
        type.DeclaringType != null
            ? type.DeclaringType.Name + "+" + type.Name
            : type.Name;

    private static string GetActionInvokeExpression(LambdaExpression expression)
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