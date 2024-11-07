using Microsoft.AspNetCore.Http;

namespace Hydro;

/// <summary>
/// Component result
/// </summary>
public interface IComponentResult
{
    /// <summary>
    /// Execute the result
    /// </summary>
    Task ExecuteAsync(HttpContext httpContext, HydroComponent component);
}

internal class ComponentResult : IComponentResult
{
    private readonly IResult _result;
    private readonly ComponentResultType _type;

    internal ComponentResult(IResult result, ComponentResultType type)
    {
        _result = result;
        _type = type;
    }

    public async Task ExecuteAsync(HttpContext httpContext, HydroComponent component)
    {
        var response = httpContext.Response;
        
        response.Headers.TryAdd(HydroConsts.ResponseHeaders.SkipOutput, "True");

        if (_type == ComponentResultType.File)
        {
            response.Headers.Append("Content-Disposition", "inline");
        }
        
        await _result.ExecuteAsync(httpContext);
        
        if (response.Headers.Remove("Location", out var location))
        {
            response.StatusCode = StatusCodes.Status200OK;
            component.Redirect(location);
        }
    }
}

internal enum ComponentResultType
{
    Empty,
    File,
    Challenge,
    SignIn,
    SignOut
}