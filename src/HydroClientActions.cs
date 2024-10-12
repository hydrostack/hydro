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
    /// Dispatch a Hydro event
    /// </summary>
    public void Dispatch<TEvent>(TEvent data, Scope scope, bool asynchronous) =>
        _hydroComponent.Dispatch(data, scope, asynchronous);
    
    /// <summary>
    /// Dispatch a Hydro event
    /// </summary>
    public void Dispatch<TEvent>(TEvent data, Scope scope) =>
        _hydroComponent.Dispatch(data, scope);
    
    /// <summary>
    /// Dispatch a Hydro event
    /// </summary>
    public void Dispatch<TEvent>(TEvent data) =>
        _hydroComponent.Dispatch(data);

    /// <summary>
    /// Dispatch a Hydro event
    /// </summary>
    public void DispatchGlobal<TEvent>(TEvent data) =>
        _hydroComponent.DispatchGlobal(data);
    
    /// <summary>
    /// Dispatch a Hydro event
    /// </summary>
    public void DispatchGlobal<TEvent>(TEvent data, string subject) =>
        _hydroComponent.DispatchGlobal(data, subject: subject);
    
    /// <summary>
    /// Invoke JavaScript expression on client side
    /// </summary>
    /// <param name="jsExpression">JavaScript expression</param>
    [Obsolete("Use ExecuteJs instead.")]
    public void Invoke([LanguageInjection(InjectedLanguage.JAVASCRIPT)] string jsExpression) =>
        ExecuteJs(jsExpression);
}