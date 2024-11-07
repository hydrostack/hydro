using Microsoft.AspNetCore.Http;

namespace Hydro;

internal sealed class HydroEmptyResult : IResult
{
    private HydroEmptyResult()
    {
    }

    public static HydroEmptyResult Instance { get; } = new();
    public Task ExecuteAsync(HttpContext httpContext) => Task.CompletedTask;
}
