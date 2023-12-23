---
outline: deep
---

# Comparisons

Hydro provides a flexible way to create web applications, but there are other frameworks
providing similar functionalities that you can use together with .NET, so it's good to understand the differences
between them before the final choice.

## Blazor

Blazor introduced new component model called Razor Components that is quite different
from the one used in regular Razor Pages or MVC. It allows to do similar things as Hydro,
but it uses different techniques to handle user interaction and component state.

- Blazor Server - using web sockets to handle state and exchange information between server and client
- Blazor WebAssembly - using web assembly to run your client .NET code in the browser

Hydro doesn't use neither web sockets or web assembly. Hydro utilizes regular HTTP request/response model
with rendering on the back-end side, morphing the client HTML when needed, and is keeping the state on the page instead in the connection scope.

We believe the Hydro stack is simple, reliable, easy to debug and easy to scale, using well-known and well-tested solutions.

## HTMX

HTMX is a library for handling interactions between client and server. You can for example add event handlers
to your document that will call the back-end and replace the content of chosen elements, so it also
adds a "SPA feeling" to your web application, since the content can be updated without full-page reload.

The main difference from Hydro is lack of state management or concept of components that keep the state. In Hydro
you can define a component with set of properties that will be persisted across the requests without any additional configuration.

## Turbo

Turbo is similar to HTMX with small differences. It introduces streams which allow you to control the content replacement from the back-end side.

The differences between Hydro and Turbo are similar to the ones between Hydro and HTMX.