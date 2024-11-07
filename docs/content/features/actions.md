---
outline: deep
---

# Actions

Actions are methods defined in the component class. They allow you to react to user interactions.

Let's take a look at the following component:

```c#
// Counter.cshtml.cs

public class Counter : HydroComponent
{
    public int Count { get; set; }
    
    public void Add()
    {
        Count++;
    }
}
```

A browser event of an element can be attached to an action method by using the `on` tag helper:

```razor
<!-- Counter.cshtml -->

@model Counter
<div>
  Count: <strong>@Model.Count</strong>
  <button on:click="@(() => Model.Add())">
    Add
  </button>
</div>
```

```razor
<!-- Counter.cshtml -->

@model Counter
<div>
  Count: <strong>@Model.Count</strong>
  <button on:click="@(() => Model.Add())">
    Add
  </button>
</div>
```

Alternatively, you can use the `hydro-on` tag helper:

```razor
<button on:click="@(() => Model.Add())">
  Add
</button>
```

The attribute `on` can be defined as:

> on:**event**="**expression**"

where:
- **event**: a definition of the event handler that is compatible with Alpine.js's [x-on directive](https://alpinejs.dev/directives/on)
- **expression**: C# lambda expression that calls the the callback method

Examples:

```razor
<button on:click="@(() => Model.Add(20))">

<div on:click.outside="@(() => Model.Close())">

<input type="text" on:keyup.shift.enter="@(() => Model.Save())" />
```

## Parameters

If your action contains parameters, you can pass them in a regular way.

Example:

```c#
// Counter.cshtml.cs

public class Counter : HydroComponent
{
    public int Count { get; set; }
    
    public void Set(int newValue)
    {
        Count = newValue;
    }
}
```

```razor
<!-- Counter.cshtml -->

@model Counter
<div>
  Count: <strong>@Model.Count</strong>
  <button on:click="@(() => Model.Set(20))">
    Set to 20
  </button>
</div>
```

## Results

Hydro provides multiple component results that can be returned from an action:

- `ComponentResults.Challenge`

    Calls `HttpContext.ChallangeAsync` and handles further redirections

- `ComponentResults.SignIn`

    Calls `HttpContext.SignInAsync` and handles further redirections

- `ComponentResults.SignOut`

    Calls `HttpContext.SignOutAsync` and handles further redirections

- `ComponentResults.File`
    
    Returns a file from the server



Examples:

```c#
// ShowInvoice.cshtml.cs

public class ShowInvoice : HydroComponent
{
    public IComponentResult Download()
    {
        return ComponentResults.File("./storage/file.pdf", MediaTypeNames.Application.Pdf);
    }
}
```

```c#
// Profile.cshtml.cs

public class Profile : HydroComponent
{
    public IComponentResult LoginWithGitHub()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = RedirectUri, 
            IsPersistent = true
        };

        return ComponentResults.Challenge(properties, [GitHubAuthenticationDefaults.AuthenticationScheme]);
    }
}
```

## JavaScript expression as a parameter

In some cases, like integrating with JavaScript libraries like maps, rich-text editors, etc. it might be useful to
call a Hydro action with parameters evaluated on client side via JavaScript expression. You can use then `Param.JS<T>(string value)` method, where:
- `T`: type of the parameter
- `value`: JavaScript expression to evaluate

If your parameter type is a `string`, you can use a shorter version:

`Param.JS(string value)`

Example:

```c#
// Content.cshtml.cs

public class Content : HydroComponent
{
    public void Update(string value)
    {
        // ...
    }
}
```

```razor
<!-- Content.cshtml -->

@model Content
<div>
  <input type="text" id="myInput" />
  <button on:click="@(() => Model.Update(Param.JS("window.myInput.value")))">
    Update content
  </button>
</div>
```

After clicking the button from the code above, Hydro will execute the expression
`window.myInput.value` on the client side, and pass it as a `value` parameter to the `Update` action.

> NOTE: In case of using widely this feature in your component, you can add:
>
> ```@using static Hydro.Param``` and call `JS` without `Param.` prefix.

## Asynchronous actions

If you want to use asynchronous operations in your actions, just change the signature of the method as in this example:
```c#
public async Task Add()
{
    await database.Add();
}

```