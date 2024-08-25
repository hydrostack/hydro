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
    /// Add a response header that instructs Hydro to redirect to a specific page with page reload
    /// </summary>
    /// <param name="response">HttpResponse instance</param>
    /// <param name="url">URL to redirect to</param>
    public static void HydroRedirect(this HttpResponse response, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("url is not provided", nameof(url));
        }
        
        response.Headers.Append("Hydro-Redirect", new StringValues(url));
    }

    /// <summary>
    /// Add a response header that instructs Hydro to redirect to a specific page without page reload
    /// </summary>
    /// <param name="response">HttpResponse instance</param>
    /// <param name="url">URL to redirect to</param>
    /// <param name="payload">Object to pass to destination page</param>
    public static void HydroLocation(this HttpResponse response, string url, object payload = null)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("url is not provided", nameof(url));
        }
        
        var data = new
        {
            path = url,
            target = "body",
            payload
        };

        response.Headers.Append("Hydro-Location", new StringValues(JsonConvert.SerializeObject(data)));
    }
}