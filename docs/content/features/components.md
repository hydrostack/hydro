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

Explanation:
1. Component's view model is set to be the Hydro component. It's because all the state lays there. If you want to extract the model to a separate file, you can do it and reference it as one property on the component.
2. Each component view must have only one root element, in our case it's the top level `div`.
3. On the component view you can use `Model` to access your component's state, in our case: `Model.Count`
4. Use `hydro-action` attribute to bind your DOM to the methods on the component. [Read more here](actions).