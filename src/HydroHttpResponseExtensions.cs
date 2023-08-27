using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Hydro;

/// <summary>
/// Hydro extensions for HttpResponse
/// </summary>
public static class HydroHttpResponseExtensions
{
    /// <summary>
    /// Add a response header that instructs Hydro to redirect to a specific page
    /// </summary>
    /// <param name="response">HttpResponse instance</param>
    /// <param name="url">URL to redirect to</param>
    /// <param name="hard">Set to true, to make a full page reload</param>
    public static void HydroRedirect(this HttpResponse response, string url, bool hard = false)
    {
        if (hard)
        {
            response.Headers.Add("Hydro-Redirect", new StringValues(url));
        }
        else
        {
            response.Headers.Add("Hydro-Location", new StringValues(JsonConvert.SerializeObject(new { path = url, target = "#app" })));
        }
    }
}