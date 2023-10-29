---
outline: deep
---

# Getting started

Create your web application with the following command. If you already have an app using ASP.NET Core 6+ Razor Pages / MVC project, you can skip this step.

```console
dotnet new webapp -o MyApp
cd MyApp
```

Install Hydro [NuGet package](https://www.nuget.org/packages/Hydro/):
```console
dotnet add package Hydro
```

In application's startup code (either `Program.cs` or `Startup.cs`) add:

```c#
builder.Services.AddHydro();

...

app.UseHydro(builder.Environment);
```

> **_NOTE:_** Make sure that `UseHydro` is called after `UseRouting`.
> 
Sample Program.cs file:

```c#
using Hydro.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHydro(); // for Hydro

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.UseHydro(builder.Environment); // for Hydro

app.Run();
```

In `_ViewImports.cshtml` add:
```razor
@addTagHelper *, {Your project assembly name}
@addTagHelper *, Hydro
````

In layout's `head` tag:
```html
<!-- meta -->
<meta name="hydro-config" />

<!-- Alpine.js libraries -->
<script defer src="https://cdn.jsdelivr.net/npm/@@alpinejs/morph@3.x.x/dist/cdn.min.js"></script>
<script defer src="https://cdn.jsdelivr.net/npm/alpinejs@3.x.x/dist/cdn.min.js"></script>
~~~~
<!-- Hydro script -->
<script src="~/hydro.js" asp-append-version="true"></script>
```

For Alpine.js you can use CDN as shown above or you can host it yourself.

Now you are ready to create you first [component](/features/components).