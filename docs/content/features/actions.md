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

A DOM element can be bound to an action method by using the `hydro-action` tag helper:

```razor
<!-- Counter.cshtml -->

@model Counter
<div>
  Count: <strong>@Model.Count</strong>
  <button hydro-action="Add">Add</button>
</div>
```

The attribute `hydro-action` expects a name of the callback method, in our case `Add`.

## Arguments

If your action contains arguments, you can pass it using `param-*` attributes.

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
  <button hydro-action="Set" param-newValue="20">Add</button>
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