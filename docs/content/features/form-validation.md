---
outline: deep
---

# Form validation

Hydro components provide model validation capabilities similar to traditional ASP.NET Core MVC models. Use Data Annotations to define validation rules in your components.

```csharp
// ProductForm.cshtml.cs

public class ProductForm : HydroComponent
{
    [Required, MaxLength(50)]
    public string Name { get; set; }
    
    public async Task Submit()
    {
        if (!Validate())
        {
            return;
        }
        
        // your submit logic
    }
}
```

```razor
<!-- ProductForm.cshtml -->

@model ProductForm

<form on:submit="@(() => Model.Submit())">
  <label asp-for="Name"></label>
  <input asp-for="Name" bind />
  <span asp-validation-for="Name"></span>  

  <button type="submit">Submit</button>
</form>
```

## Validating nested models and collections

Data Annotations don't offer a way to validate nested models or collections, that's why Hydro provides custom validators to handle these scenarios.

Nested model:

```csharp
// ProductForm.cshtml.cs

public class ProductForm : HydroComponent
{
    [ValidateObject]
    public ProductData Product { get; set; }
    
    // ...
}

public class ProductData
{
    [Required, MaxLength(50)]
    public string Name { get; set; }
    
    [Range(0, 100)]
    public decimal Price { get; set; }
}
```

Collection:

```csharp
// ProductForm.cshtml.cs

public class InvoiceForm : HydroComponent
{
    [ValidateCollection]
    public List<LineData> Lines { get; set; }
    
    // ...
}

public class LineData
{
    [Required, MaxLength(50)]
    public string Name { get; set; }
    
    [Range(0, 100)]
    public decimal Price { get; set; }
}
```

## Custom validation

It's possible to execute also custom validation. For example:

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

Attaching Fluent Validation:

```csharp
// Counter.cshtml.cs

public class Counter(IValidator<Counter> validator) : HydroComponent
{
    public int Count { get; set; }
    
    public void Add()
    {
        if (!this.Validate(validator))
        {
            return;
        }
        
        Count++;
    }
    
    public class Validator : AbstractValidator<Counter>
    {
        public Validator()
        {
            RuleFor(c => c.Count).LessThan(5);
        }
    }
}

// HydroValidationExtensions.cs

public static class HydroValidationExtensions
{
    public static bool Validate<TComponent>(this TComponent component, IValidator<TComponent> validator) where TComponent : HydroComponent
    {
        component.IsModelTouched = true;
        var result = validator.Validate(component);

        foreach (var error in result.Errors) 
        {
            component.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return result.IsValid;
    }
}

```