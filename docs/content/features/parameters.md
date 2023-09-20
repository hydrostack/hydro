---
outline: deep
---

# Parameters

Parameters are public properties on a component and are used to store component's state. They also allow the passing of data or settings from a parent component to a child component. Parameters can include any types of values, such as integers, strings, and complex objects.

Let's look at the Counter component:

```csharp
// Counter.cshtml.cs

public class Counter : HydroComponent
{
    public int Count { get; set; }
}
```

The `Count` property can be set by a parameter, when this component is triggered from outside:

```html
<!-- Index.cshtml -->

<hydro name="Counter" param-count="10"/>
```

Or when using `vc:*` tag helpers:

```html
<!-- Index.cshtml -->

<vc:counter parameters="@(new { Count = 10 })"/>
```

or

```c#
<!-- Index.cshtml -->
    
@(await Component.InvokeAsync<Counter>(new { parameters = new { Count = 10 } }))
```

## State of the parameters in time

The values are passed to the component only once and any update to the parameters won't refresh the parent component. If you want the component to refresh its state, you would have to use events or change the [key parameter](#key) of the component.  

## Key

When rendering a Hydro component, you can provide an optional `key` argument:

```razor
<hydro name="MyComponent" key="1"/>
```

It's used when you have multiple components of the same type on the page to make it possible to distinguish them during DOM updates.

Usage examples:

```razor
@foreach (var item in Items)
{
  <hydro name="Product" key="@item.Id"/>
}
```

or

```razor
<hydro name="Product" key="1"/>
<hydro name="Product" key="2" param-name="@("Item 2")"/>
```