using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Hydro;

/// <summary>
/// Results for Hydro actions
/// </summary>
public static class ComponentResults
{
    /// <summary>
    /// Create a ChallengeHttpResult
    /// </summary>
    public static IComponentResult Challenge(
        AuthenticationProperties properties = null,
        IList<string> authenticationSchemes = null)
        => new ComponentResult(Results.Challenge(properties, authenticationSchemes), ComponentResultType.Challenge);

    /// <summary>
    /// Creates a SignInHttpResult
    /// </summary>
    public static IComponentResult SignIn(
        ClaimsPrincipal principal,
        AuthenticationProperties properties = null,
        string authenticationScheme = null)
        => new ComponentResult(Results.SignIn(principal, properties, authenticationScheme), ComponentResultType.SignIn);

    /// <summary>
    /// Creates a SignOutHttpResult
    /// </summary>
    public static IComponentResult SignOut(AuthenticationProperties properties = null, IList<string> authenticationSchemes = null)
        => new ComponentResult(Results.SignOut(properties, authenticationSchemes), ComponentResultType.SignOut);

    /// <summary>
    /// Creates a FileContentHttpResult
    /// </summary>
    public static IComponentResult File(
        byte[] fileContents,
        string contentType = null,
        string fileDownloadName = null,
        bool enableRangeProcessing = false,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue entityTag = null)
        => new ComponentResult(Results.File(fileContents, contentType, fileDownloadName, enableRangeProcessing, lastModified, entityTag), ComponentResultType.File);


    /// <summary>
    /// Creates a FileStreamHttpResult
    /// </summary>
    public static IComponentResult File(
        Stream fileStream,
        string contentType = null,
        string fileDownloadName = null,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue entityTag = null,
        bool enableRangeProcessing = false)
        => new ComponentResult(Results.File(fileStream, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing), ComponentResultType.File);

    /// <summary>
    /// Returns either PhysicalFileHttpResult or VirtualFileHttpResult
    /// </summary>
    public static IComponentResult File(
        string path,
        string contentType = null,
        string fileDownloadName = null,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue entityTag = null,
        bool enableRangeProcessing = false)
        => new ComponentResult(Results.File(path, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing), ComponentResultType.File);
}