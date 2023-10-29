---
outline: deep
---

# Authorization

Hydro provides `IHydroAuthorizationFilter` interface that can be used for creating custom
authorization attributes for Hydro components.

Attribute example:
```c#
public sealed class CustomComponentAuthorizeAttribute : Attribute, IHydroAuthorizationFilter
{
    public Task<bool> AuthorizeAsync(HttpContext httpContext, object component)
    {
        var isAuthorized = httpContext.User.Identity?.IsAuthenticated ?? false;     
        return Task.FromResult(isAuthorized);
    }
}
```

Usage:
```c#
[CustomComponentAuthorize]
public class ProductList : HydroComponent
{
}
```

If `AuthorizeAsync` returns `false`, the component won't be rendered.

## Using component state for authorization process

It might happen that authorization will be dependent on state of your component. One of the
ways to solve it is to create an interface representing the state you need for authorization.

Interface:
```c#
public interface IWorkspaceComponent
{
    string WorkspaceId { get; }
}
```

Authorization attribute:
```c#
public sealed class WorkspaceComponentAuthorizeAttribute : Attribute, IHydroAuthorizationFilter
{
    public async Task<bool> AuthorizeAsync(HttpContext httpContext, object component)
    {
        var workspaceComponent = (IWorkspaceComponent)component;
        var authorizationService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();
        return authorizationService.IsWorkspaceAuthorized(workspaceComponent.WorkspaceId);
    }
}
```

```c#
[WorkspaceComponentAuthorize]
public class ProductList : HydroComponent, IWorkspaceComponent
{
    public string WorkspaceId { get; set; }
}
```