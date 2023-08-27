---
outline: deep
---

# Anti-forgery token

Hydro supports mechanism built-in to ASP.NET Core to prevent prevent Cross-Site Request Forgery (XSRF/CSRF) attacks.

In the configuration of services use:
```c#
services.AddHydro(options =>
{
    options.AntiforgeryTokenEnabled = true;
});
```

Make sure you've also added `meta` tag to the layout's `head`:
```html
<meta name="hydro-config" />
```
