---
# https://vitepress.dev/reference/default-theme-home-page
layout: home

hero:
  name: "Hydro"
  text: "Create .NET apps with SPA feeling without JS"
  tagline: Bring stateful and reactive components to ASP.NET Core without writing JavaScript
  actions:
    - theme: brand
      text: Get started
      link: /introduction/getting-started
    - theme: brand
      text: Why Hydro
      link: /introduction/motivation

features:
  - icon: ♥️
    title: Razor Pages and MVC
    details: Use familiar, server-side rendering strategy that has been a foundation of .NET web development for many years 
  - icon: 🧩️
    title: Components
    details: Build your application using components, which make it easy to structure and separate your codebase  
  - icon: ⚡️
    title: Reactive and stateful
    details: No full page reloads. After each action only update parts of the page that need it. 
---

<style scoped>
@import './home-layout-wrapper.css';
</style>

<div class="vp-doc home-wrapper">

### Quick example: binding

```razor
<!-- NameForm.cshtml -->

@model NameForm

<div>
  <input asp-for="Name" hydro-bind:keydown>
  <div>Hello @Model.Name</div>
</div>
```

```c#
// NameForm.cs

public class NameForm : HydroComponent
{
    public string Name { get; set; }
}
```

### Quick example: validation

```razor
<!-- NameForm.cshtml -->

@model NameForm

<form hydro-on:submit="@(() => Model.Save())">
  <input asp-for="Name" hydro-bind>
  <span asp-validation-for="Name"></span>
  
  <button type="submit">Save</button>
  
  @if (Model.Message != null)
  {
    <div>@Model.Message</div>
  }
</form>
```

```c#
// NameForm.cs

public class NameForm : HydroComponent
{
    [Required, MaxLength(50)]
    public string Name { get; set; }
    
    public string Message { get; set; }
    
    public void Save()
    {
        if (!Validate())
        {
            return;
        }
        
        Message = "Success!";
        Name = "";
    }
}
```

### Quick example: calling actions

```razor
<!-- Counter.cshtml -->

@model Counter

<div>
  Count: @Model.Count
  
  <button hydro-on:click="@(() => Model.Add(10))">
    Add
  </button>
</div>
```

```c#
// Counter.cs

public class Counter : HydroComponent
{
    public int Count { get; set; }
    
    public void Add(int value)
    {
        Count += value;
    }
}
```

</div>