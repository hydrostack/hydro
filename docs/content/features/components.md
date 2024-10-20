---
outline: deep
---

# Components

Hydro components are extended version of View Components from ASP.NET Core.

To build a component you will need:
- component view (a cshtml file)
- component code-behind (a class that derives from `HydroComponent`)
- (optional) component styles

The place for keeping the components depends on your project settings. In Razor Pages by default it will be either `~/Pages/Components/` or `~/Components/` folder, but it can be customized. You can decide if you want to create separate folders for each component or not.

Let's see it in action:
```c#
// ~/Pages/Components/PageCounter.cshtml.cs

public class PageCounter : HydroComponent
{
    public int Count { get; set; }
    
    public void Add()
    {
        Count++;
    }
}
```

```razor
<!-- ~/Pages/Components/PageCounter.cshtml -->

@model PageCounter

<div>
  Count: <strong>@Model.Count</strong>
  <button on:click="@(() => Model.Add())">
    Add
  </button>
</div>
```

Notes about the above code:
1. Component's view model is set to be the Hydro component. It's because all the state lays there. If you want to extract the model to a separate file, you can do it and reference it as one property on the component.
2. Each component view must have only one root element, in our case it's the top level `div`.
3. On the component view you can use `Model` to access your component's state, in our case: `Model.Count`
4. Use `on` attribute to attach the browser events to the methods on the component. [Read more here](actions).

## Usage

To use your new component, you can render it in your Razor Page (e.g. `Index.cshtml`) or in another Hydro component. There are several ways to do it:

by calling a custom tag using dash-case notation:
```razor
<page-counter />
```

by calling a generic tag helper:

```razor
<hydro name="PageCounter"/>
```

or by calling one of the extension methods:
```razor
@await Html.Hydro("PageCounter")
```
```razor
@(await Html.Hydro<PageCounter>())
```

## State

State of the components is serialized, encrypted and stored on the rendered page. Whenever there is call from that component to the back-end, the state is attached to the request headers.

## Components nesting

Hydro components can be nested, which means you can put one Hydro component inside the other. When the state of the parent component changes, the nested components won't be updated, so if there is a need to update the nested components, it has to be communicated via events or the key of the component has to change ([key parameter](/features/parameters#key) used for rendering the component).

## Same component rendered multiple times on the same view

It's possible to use the same component multiple times on the same view, but then you have to provide a unique key for each instance. For more details go to the [parameters](/features/parameters#key) page.

## Component's lifecycle

Extensive example of a component to describe the lifecycle:
```c#
public class EditUserForm : HydroComponent
{
    private readonly IDatabase _database;

    public EditUserForm(IDatabase database)
    {
        _database = database;
        
        Subscribe<SystemMessageEvent>(Handle);
    }
    
    public string UserId { get; set; }

    [Required]
    public string Name { get; set; }

    public override async Task MountAsync()
    {
        var formData = ...; // fetch data from database

        Name = formData.Name;
    }
    
    public override void Render()
    {
        ViewBag.IsLongName = Name.Length > 20;
    }

    public async Task Save()
    {
        await _database.UpdateUser(UserId, Name); // save the data
    }
    
    public void Handle(SystemMessageEvent message)
    {
        Message = message.Text;
    }
}

```

### Constructor (optional)

Use the constructor to initialize your component, inject necessary services or subscribe to a Hydro events. The  constructor is called on each request and the injections are done by the ASP.NET Core DI.

### Mount or MountAsync

`Mount` is called only once on when component is instantiated on the page.

### Render or RenderAsync

`Render` is called on each request when this component should render. You can use it to pass additional data calculated on the fly or to get temporary data from the database that you don't want to store in the component persistent state.

### Custom actions

`Save` method is a custom action triggered by the browser's events like `click` or `submit`.

### Event handlers

`Handle` method is used here to catch custom event `SystemMessageEvent` that we subscribed to in the constructor. It's called whenever such event is triggered in the application.