---
outline: deep
---

# Using JavaScript

With Hydro you can create web applications without writing JavaScript, but
sometimes there are very specific use cases where using small portions of JavaScript is needed to improve
the user experience. Those use cases usually refer to creating reusable components, not the domain specific components. Examples where JavaScript is a nice addition to Hydro:
- selecting the content of an element when focused
- operating on existing JS libraries, like maps
- changing the currently highlighted element in a list using arrows
- ...

In practice, there shouldn't be many places where JS is used, but it's good to have
an option to utilize it when needed.

## Using Alpine.js

Hydro is using [Alpine.js](https://alpinejs.dev/) as the backbone for handling all interactions on the client side,
and it enables by default all the great features from that library. It means you can create
Hydro components that utilize Alpine.js directives like [x-on](https://alpinejs.dev/directives/on), [x-data](https://alpinejs.dev/directives/data), [x-text](https://alpinejs.dev/directives/text), [x-ref](https://alpinejs.dev/directives/ref) and all the rest.

Example. Select the content of an input when focused:
```razor
@model Search

<div>
  Count: <strong>@Model.Count</strong>
  <input asp-for="Phrase" hydro-bind x-on:focus="$el.select()" />
</div>
```

Example. Create local JS state and operate on it.
```razor
@model Search

<div>
  <div x-data="{ index: 0 }">
    <strong x-text="index"></strong>
    <button x-on:click="index = index + 1">Add</button>
  </div>
</div>
```

The only limitation is that you can't set a custom `x-data` attribute on the root element, that's why in the above example a nested div is introduced.

## Using Hydro

You can execute JavaScript code using [Hydro action handlers](/features/actions) in views or components code-behind.

### Example of invoking JavaScript expression in the view:

```razor
<!-- Search.cshtml -->

@model Search

<div>
  <button
    type="button"
    hydro-on:click="@(() => Model.Client.ExecuteJs("alert('test')"))">
      Click me
  </button>
</div>
```

### Example of invoking JavaScript expression in the action handler:

```c#
// Counter.cshtml.cs

public class Counter : HydroComponent
{
    public int Count { get; set; }
    
    public void Add()
    {
        Count++;
        Client.ExecuteJs($"console.log({Count})");
    }
}
```

### Execution Context

The context of execution the JS expression is the component DOM element, and can be accessed via `this`. Example:

```c#
// ProductDialog.cshtml.cs

public class ProductDialog : HydroComponent
{
    [SkipOutput]
    public void Close()
    {
        DispatchGlobal(new CloseDialog(nameof(ProductDialog)));
        Client.ExecuteJs($"this.remove()");
    }
}
```

In the above example, first we dispatch an event to notify dialogs container to change the state, and then we invoke JS expression
to remove dialog component DOM element immediately, without waiting for the state update.