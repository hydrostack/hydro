using JetBrains.Annotations;

namespace Hydro;

/// <summary>
/// Actions that are evaluated on the client side
/// </summary>
public class HydroClientActions
{
    private readonly HydroComponent _hydroComponent;

    internal HydroClientActions(HydroComponent hydroComponent) =>
        _hydroComponent = hydroComponent;

    /// <summary>
    /// Execute JavaScript expression on client side
    /// </summary>
    /// <param name="jsExpression">JavaScript expression</param>
    public void ExecuteJs([LanguageInjection(InjectedLanguage.JAVASCRIPT)] string jsExpression) =>
        _hydroComponent.AddClientScript(jsExpression);

    /// <summary>
    /// Invoke JavaScript expression on client side
    /// </summary>
    /// <param name="jsExpression">JavaScript expression</param>
    [Obsolete("Use ExecuteJs instead.")]
    public void Invoke([LanguageInjection(InjectedLanguage.JAVASCRIPT)] string jsExpression) =>
        ExecuteJs(jsExpression);
}