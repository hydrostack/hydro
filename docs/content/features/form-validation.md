---
outline: deep
---

# Form validation

Hydro components provide model validation capabilities similar to traditional ASP.NET Core MVC models. Use Data Annotations to define validation rules in your components.

```csharp
// ProductForm.cshtml.cs

public class ProductForm : HydroComponent
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }
    
    public async Task Submit()
    {
        // your submit logic
    }
}
```

```razor
<!-- ProductForm.cshtml -->

@model ProductForm

<form hydro-action="Model.Submit">
  <label asp-for="Name"></label>
  <input asp-for="Name"/>
  <span asp-validation-for="Name"></span>  

  <button type="submit">Submit</button>
</form>
```

## Custom validation

It's possible to execute custom validation, either written manually or by running libraries like Fluent Validation. For example:

```csharp
// Counter.cshtml.cs

public class Counter : HydroComponent
{
    public int Count { get; set; }
    
    public void Add()
    {
        if (Count > 5)
        {
            ModelState.AddModelError(nameof(Count), "Value is too high");
            return;
        }
        
        Count++;
    }
}
```