---
outline: deep
---

# Overview

**Hydro** is an extension to ASP.NET Core MVC and Razor Pages. It extends View Components to make them reactive and stateful with ability to communicate with each other without page reloads. As a result, you can create powerful components and make your application to feel like SPA with zero or minimal amount of the JavaScript code (depending on the needs) and without separate front-end build step. It also works well with existing ASP.NET Core applications.

## How it works

Hydro utilizes the following technologies to make it all work:

- **Razor views (\*.cshtml)**  
  Razor views form the backbone of Hydro's UI generation. They allow for a familiar, server-side rendering strategy that has been a foundation of .NET web development for many years. These *.cshtml files enable a seamless mix of HTML and C# code, allowing for robust and dynamic webpage generation.


- **AJAX**  
  AJAX calls are used to communicate between the client and the server, specifically to send the application state to the server, receive updates and responses back, and then store this state to be used in subsequent requests. This ensures that each request has the most up-to-date context and information.


- **Alpine.js**  
  Alpine.js stands as a base for requests execution and  DOM swapping. But beyond that, Alpine.js also empowers users by providing a framework for adding rich, client-side interactivity to the standard HTML. So, not only does it serve Hydro's internal operations, but it also provides an expansion point for users to enhance their web applications with powerful, interactive experiences.
