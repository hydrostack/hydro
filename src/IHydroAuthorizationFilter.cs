using Microsoft.AspNetCore.Http;

namespace Hydro;

/// <summary>
/// A filter that confirms component authorization
/// </summary>
public interface IHydroAuthorizationFilter
{   
    /// <summary>
    /// Called early in the component pipeline to confirm request is authorized
    /// </summary>
    /// <param name="httpContext">HttpContext</param>
    /// <param name="component">Hydro component instance</param>
    /// <returns>Indication if the the operation is authorized</returns>
    Task<bool> AuthorizeAsync(HttpContext httpContext, object component);
}