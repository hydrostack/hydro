---
outline: deep
---

# Binding

You can bind your component's properties to any input/select/textarea element by using `hydro-bind`. It will synchronize the client value with server value on element's `change` event (soon more events supported).

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
  <input asp-for="FirstName" hydro-bind/>
  <input asp-for="LastName" hydro-bind/>
  <span>Full name: @Model.FirstName @Model.LastName</span>
</div>

```
