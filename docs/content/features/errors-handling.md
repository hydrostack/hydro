---
outline: deep
---

# Error handling

In regular cases, expected error handling can be done manually by `try/catch` statements.
But in cases of unhandled exceptions, we want to gracefully inform the user of the situation.

One of the ways to do this when working with Hydro components is to create a global messages
component for showing alerts, which can be rendered in your layout. Such a component could
subscribe to your own custom events with messages, but it can also listen to Hydro's
built-in unhandled error event: `UnhandledHydroError`.

```c#
public class Toasts : HydroComponent
{
    public List<Toast> ToastsList { get; set; } = new();

    public Toasts()
    {
        Subscribe<UnhandledHydroError>(Handle);
    }

    private void Handle(UnhandledHydroError data) =>
        ToastsList.Add(new Toast(
            Id: Guid.NewGuid().ToString("N"),
            Message: data.Message,
            Type: ToastType.Error
        ));

    public void Close(string id) =>
        ToastsList.RemoveAll(t => t.Id == id);

    public record Toast(string Id, string Message, ToastType Type);
}
```

Hydro will send `UnhandledHydroError` in the case of an unhandled error, and by default
it will contain the response from the server that ASP.NET MVC produces for exceptions,
which might be too expressive. To customize that, you can configure the ASP.NET MVC exception
handling:

```c#
app.UseExceptionHandler(b => b.Run(async context =>
{
    if (!context.IsHydro())
    {
        context.Response.Redirect("/Error");
        return;
    }
    
    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
    switch (contextFeature?.Error)
    {
        // custom cases for custom exception types if needed

        default:
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
           
            await context.Response.WriteAsJsonAsync(new UnhandledHydroError(
                Message: "There was a problem with this operation and it wasn't finished",
                Data: null
            ));
            
            return;
    }
}));
```

In the code above we are creating a JSON response containing an `UnhandledHydroError` event that
will be consumed in our `Toasts` component.
