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

A browser event of an element can be attached to an action method by using the `hydro-on` tag helper:

```razor
<!-- Counter.cshtml -->

@model Counter
<div>
  Count: <strong>@Model.Count</strong>
  <button hydro-on:click="@(() => Model.Add())">
    Add
  </button>
</div>
```

The attribute `hydro-on` can be defined as:

> hydro-on:**event**="**expression**"

where:
- **event**: a definition of the event handler that is compatible with Alpine.js's [x-on directive](https://alpinejs.dev/directives/on)
- **expression**: C# lambda expression that calls the the callback method

Examples:

```razor
<button hydro-on:click="@(() => Model.Add(20))">

<div hydro-on:click.outside="@(() => Model.Close())">

<input type="text" hydro-on:keyup.shift.enter="@(() => Model.Save())">
```

## Arguments

If your action contains arguments, you can pass them in a regular way.

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
  <button hydro-on:click="@(() => Model.Set(20))">
    Set to 20
  </button>
</div>
```

## Asynchronous actions

If you want to use asynchronous operations in your actions, just change the signature of the method as in this example:
```c#
public async Task Add()
{
    await database.Add();
}

```