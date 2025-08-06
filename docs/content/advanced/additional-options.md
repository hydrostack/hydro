---
outline: deep
---

# Additional options

Hydro provides additional options that can be configured to customize its behavior. These options can be set in the `AddHydro` method when configuring Hydro in your application.

### Serializer settings

You can customize the JSON serialization settings used by Hydro:

```csharp
builder.Services.AddHydro(options =>
{
    options.JsonSerializerSettings.Converters.Add(new MyCustomConverter());
});
```