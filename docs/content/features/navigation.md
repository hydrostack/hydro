---
outline: deep
---

# Navigation

There are 3 kinds of managed navigation in applications using Hydro:
1. Navigation via links.
2. Navigation initiated in components (without page reload).
3. Navigation initiated in components (with full page reload).

## Navigation via links

With `hydro-link` attribute relative links in your application can be loaded in the background and applied to the current document instead of doing the full page reload.

Examples:

Attribute applied directly on a link:
```html
<a href="/page" hydro-link>My page</a>
```

Attribute applied directly on a parent of the links:
```html
<ul hydro-link>
  <li><a href="/page1">My page 1</a></li>
  <li><a href="/page2">My page 2</a></li>
</ul>
```

## Navigation initiated in components (without page reload)

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

### Reading the payload

Use the following method to read the previously passed payload:

```csharp
// CartSummary.cshtml.cs

public class CartSummary : HydroComponent
{
    public CartPayload Payload { get; set; }

    public override void Mount()
    {
        Payload = GetPayload<CartPayload>();
    }
    
    // ...
}
```


## Navigation initiated in components (with full page reload)

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