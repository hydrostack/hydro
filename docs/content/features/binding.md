---
outline: deep
---

# Binding

You can bind your component's properties to any input/select/textarea element by using `hydro-bind`. It will synchronize the client value with server value on the chosen event (`change` as default).

Example:

```csharp
// NameForm.cshtml.cs

public class NameForm : HydroComponent
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```

```razor
<!-- NameForm.cshtml -->

@model NameForm

<div>
  <input asp-for="FirstName" hydro-bind />
  <input asp-for="LastName" hydro-bind />
  <span>Full name: @Model.FirstName @Model.LastName</span>
</div>
```

## Trigger event

The default event used for binding is `change`. To choose another event, use `bind-event` attribute:

```razor
<input asp-for="Search" hydro-bind bind-event="input" />
```

## Debouncing

By default, all the events are debounced with 200ms. To change it, use `bind-debounce` attribute:
```razor
<input asp-for="Search" hydro-bind bind-event="input" bind-debounce="500" />
```