---
outline: deep
---

# Motivation

Hydro is a library for ASP.NET Core MVC and Razor Pages that
introduces stateful and interactive components via Razor views (cshtml), view components and tag helpers.
As a result, developers can create applications with server-side rendering (SSR),
and single-page application (SPA) feeling without writing JavaScript.

The motivation for building Hydro was to make the web development in .NET
simple, streamlined and enjoyable for both back-end and front-end, while keeping all the powerful features of the .NET ecosystem.
The main goal was to use the well-known patterns and practices to let developers smoothly deliver new features.

Hydro is a response for growing complexity of the front-end ecosystem. Thousands of packages
needed to run a simple application feels like an overkill; communication between front-end and back-end introduces
unnecessary problems to handle; front-end frameworks become more and more sophisticated, often because of issues in their foundation.

## Hydro features

Hydro offers the following features:
- server side rendering
- navigation enhancments
- component state persistence across requests
- UI interactivity

Above statements are achieved by using simple, yet powerful, and well-tested techniques like Razor views (cshtml), view components and AJAX calls (internally via Alpine.js).

Let's take a look at a simplified sequence of events that happens in a Hydro application:
- application renders pages and Hydro components on the first call using Razor view engine (in Razor Pages or MVC)
- state of the components is serialized and stored in the DOM
- components contain actions used for running business logic and changing the component state; those actions are run via HTTP requests and can be triggered by the browser events like click, submit, keydown, etc.
- on each request to the Hydro component, application generates HTML for affected components and gracefully morph the DOM with the changes

Such approach makes state management very easy, since all the components keep the state across requests using DOM on the client side as a temporary storage, so no matter at what point in time, the state will be always the same as it was on the last render. Any connectivity issues, like lost
connections, have no impact here.

## Transport

Some might argue that using HTTP requests as the base for interactivity
might cause performance issues related to request overhead or to request waterfall.
In practice, it's not the case for most scenarios, since Hydro is performing micro-requests not requiring much resources. 
They are also queued in a way to provide consistency in the state between the operations. In a situation of slow connection, the problem exists anyway in any other kind of application that has to synchronize front-end state with the back-end.

As a side note: it's a good exercise to open some popular SPA application and turn on the network analysis to see how many
requests they perform.

## The future

Hydro's goal is to make web development smooth,
to allow working in one .NET ecosystem, have one build and enjoy
the simplicity of crafting component based software without writing JavaScript.

To get started [click here](/introduction/getting-started).
