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

> **_NOTE:_** Make sure that `UseHydro` is called after `UseStaticFiles` and `UseRouting`, which are required.
> 
Sample Program.cs file:

```c#
using Hydro.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHydro(); // Hydro

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.UseHydro(builder.Environment); // Hydro

app.Run();
```

In `_ViewImports.cshtml` add:
```razor
@addTagHelper *, Hydro
@addTagHelper *, {Your project assembly name}
````

In layout's `head` tag:
```html
<meta name="hydro-config" />
<script defer src="~/hydro/hydro.js" asp-append-version="true"></script>
<script defer src="~/hydro/alpine.js" asp-append-version="true"></script>
```

> NOTE: Hydro provides Alpine.js v3.14.3 with extensions combined into one file (`~/hydro/alpine.js`) for convenience. If you don't want to rely on the scripts provided by Hydro, you can manually specify Alpine.js sources. Make sure to include Alpine.js core script and Morph plugin.

Now you are ready to create you first [component](/features/components).