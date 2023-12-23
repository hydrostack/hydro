using System.Linq.Expressions;

namespace Hydro;

internal static class ExpressionExtensions
{
    public static (string Name, IDictionary<string, object> Parameters)? GetNameAndParameters(this Expression<Action> expression)
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

    private static object EvaluateExpressionValue(Expression expression)
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
            
            default:
                throw new NotSupportedException("Unsupported expression type: " + expression.GetType().Name);
        }
    }
}