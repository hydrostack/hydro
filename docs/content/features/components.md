---
outline: deep
---

# Components

Hydro components are extended version of View Components from ASP.NET Core.

To build a component you will need:
- component view (a cshtml file)
- component code-behind (a class that derives from `HydroComponent`)
- (optional) component styles

The place of keeping the components depends on your project settings. In Razor Pages by default it will be either `~/Pages/Components/` or `~/Components/` folder. You can decide if you want to create separate folders for each component or not.

Let's see it in action:
```razor
<!-- ~/Pages/Components/Counter.cshtml -->

@model Counter

<div>
  Count: <strong>@Model.Count</strong>
  <button hydro-action="@Model.Add">Add</button>
</div>
```

```c#
// ~/Pages/Components/Counter.cshtml.cs

public class Counter : HydroComponent
{
    public int Count { get; set; }
    
    public void Add()
    {
        Count++;
    }
}
```

Here are some notes about the code above:
1. Component's view model is set to be the Hydro component. It's because all the state lays there. If you want to extract the model to a separate file, you can do it and reference it as one property on the component.
2. Each component view must have only one root element, in our case it's the top level `div`.
3. On the component view you can use `Model` to access your component's state, in our case: `Model.Count`
4. Use `hydro-action` attribute to bind your DOM to the methods on the component. [Read more here](actions).

## Usage

To use your new component, you can render it in your Razor Page (e.g. `Index.cshtml`) or in another Hydro component. There are several ways to do it:

via ASP.NET Core tag helper:
```razor
...
<vc:counter/>
...
```

via Hydro tag helper:
```razor
...
<hydro name="Counter"/>
...
```

or via ASP.NET Core extension method:
```c#
...
await Component.InvokeAsync("Counter")
...
```

## State

State of the components is serialized, encrypted and stored on the rendered page. Whenever there is call from that component to the back-end, the state is attached to the request headers.

## Components nesting

Hydro components can be nested, which means you can put one Hydro component inside the other. When the state of the parent component changes, the nested components won't be updated, so if there is a need to update the nested components, it has to be communicated via events or the key of the component has to change ([key parameter](/features/parameters#key) used for rendering the component).

## Same component rendered multiple times on the same view

It's possible to use the same component multiple times on the same view, but then you have to provide a unique key for each instance. For more details go to the [parameters](/features/parameters#key) page.