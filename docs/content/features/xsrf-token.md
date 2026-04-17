---
outline: deep
---

# Anti-forgery token

Hydro supports the built-in mechanism in ASP.NET Core to prevent Cross-Site Request Forgery (XSRF/CSRF) attacks.

In your service configuration, use:
```c#
services.AddHydro(options =>
{
    options.AntiforgeryTokenEnabled = true;
});
```

Make sure you've also added the `meta` tag to the layout's `head`:
```html
<meta name="hydro-config" />
```
