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

## Choosing the target selector during navigation

Often when navigating to another page, the only part of the page that is changing is the content section, while layout remains the same. In those cases
we can disable layout rendering, send only the content section, and instruct Hydro where to put it. It can be achieved by using `HydroTarget`:

```razor
// Layout.cshtml

<html>
<head>
  <meta name="hydro-config" />
  <title>Test</title>
  <script defer src="~/hydro/hydro.js" asp-append-version="true"></script>
  <script defer src="~/hydro/alpine.js" asp-append-version="true"></script>
</head>

<body>

<ul hydro-link>
  <a href="/">Home</a>
  <a href="/products">Product</a>
</ul>

<div id="content">
  @RenderBody()
</div>

</body>
</html>
```

```razor
// Index.cshtml

@{
  if (HttpContext.IsHydro())
  {
    Layout = null;
    this.HydroTarget("#content");
  }
}

Content of the page
```

We are using here `#content` as the target, but it's also possible to use Hydro's predefined identifier `#hydro`, example:

```razor
<div id="@HydroComponent.LocationTargetId">
  @RenderBody()
</div>

or

<div id="hydro">
  @RenderBody()
</div>
```

```c#
this.HydroTarget(); // selector will be set to #hydro
```

`HydroTarget` has also an optional parameter `title`, which is used to set the title of loaded page. Example:

```razor
// Index.cshtml

@{
  if (HttpContext.IsHydro())
  {
    Layout = null;
    this.HydroTarget(title: "Home");
  }
}

Content of the page
```

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