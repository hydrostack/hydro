using System.Linq.Expressions;

namespace Hydro;

internal static class ExpressionExtensions
{
    private const string JsIndicationStart = "HYDRO_JS(";
    private const string JsIndicationEnd = ")HYDRO_JS";

    public static (string Name, IDictionary<string, object> Parameters)? GetNameAndParameters(this LambdaExpression expression)
    {
        if (expression is not { Body: MethodCallExpression methodCall })
        {
            return null;
        }
    
        var name = methodCall.Method.Name;
        var paramInfos = methodCall.Method.GetParameters();
        var arguments = methodCall.Arguments;

        if (arguments.Count == 0)
        {
            return (name, null);
        }

        var parameters = new Dictionary<string, object>();

        for (var i = 0; i < arguments.Count; i++)
        {
            var paramName = paramInfos[i].Name!;
            parameters[paramName] = EvaluateExpressionValue(arguments[i]);
        }

        return (name, parameters);
    }

    internal static object EvaluateExpressionValue(Expression expression)
    {
        switch (expression)
        {
            case ConstantExpression constantExpression:
                return constantExpression.Value;
            
            case MemberExpression memberExpression:
                var objectMember = Expression.Convert(memberExpression, typeof(object));
                var getterLambda = Expression.Lambda<Func<object>>(objectMember);
                var getter = getterLambda.Compile();
                return getter();

            case MethodCallExpression callExpression
                when callExpression.Method.DeclaringType == typeof(Param)
                     && callExpression.Method.Name == nameof(Param.JS)
                     && callExpression.Arguments.Any()
                     && callExpression.Arguments[0] is ConstantExpression constantExpression:

                var value = ReplaceJsQuotes(constantExpression.Value?.ToString() ?? string.Empty);

                return EncodeJsExpression(value);

            default:
                throw new NotSupportedException("Unsupported expression type: " + expression.GetType().Name);
        }
    }

    internal static string ReplaceJsQuotes(string value) =>
        value
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    
    internal static string DecodeJsExpressionsInJson(string json) =>
        json.Replace("\"" + JsIndicationStart, "")
            .Replace(JsIndicationEnd + "\"", "");
    
    private static string EncodeJsExpression(object expression) =>
        $"{JsIndicationStart}{expression}{JsIndicationEnd}";
}