---
outline: deep
---

# Navigation

There are two way of navigating from Hydro component to another page:
1. Smooth navigation - changing the location without page reload.
2. Hard navigation - redirection with page reload.

## Smooth navigation

Let's take a look at he following code:

```csharp
// MyPage.cshtml.cs

public class MyPage : HydroComponent
{
    public void About()
    {
        Location(Url.Page("/About/Index"));
    }
}
```

After triggering Hydro action `About` user will be navigated to page `/About/Index` in a smooth manner, without reload.

### Passing the payload

Sometimes it's needed to pass a payload object from one page to another. For such cases, there is a second optional parameter called `payload`:

```csharp
// Products.cshtml.cs

public class Products : HydroComponent
{
    public HashSet<string> SelectedProductsIds { get; set; }
    
    // ... product page logic
    
    public void AddToCart()
    {
        Location(Url.Page("/Cart/Index"), new CartPayload(SelectedProductsIds));
    }
}
```
```csharp
// CartPayload.cs

public record CartPayload(HashSet<string> ProductsIds);
```

## Hard navigation

If full page reload is needed, use the `Redirect` method:

```csharp
// MyPage.cshtml.cs

public class MyPage : HydroComponent
{
    public void Logout()
    {
        // logout logic
    
        Redirect(Url.Page("/Home/Index"));
    }
}
```