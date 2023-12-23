---
outline: deep
---

# Using JavaScript

With Hydro you can create web applications without writing JavaScript, but
sometimes there are very specific use cases where using small portions of JavaScript is needed to improve
the user experience. Those use cases usually refer to creating reusable components, not the domain specific components. Examples where JavaScript is a nice addition to Hydro:
- selecting the content of an element when focused
- operating on existing JS libraries, like maps
- changing the currently highlighted element in a list using arrows
- ...

In practice it shouldn't be many places where JS is used, but it's good to have
an option to utilize it when needed.

## Usage

Hydro is using [Alpine.js](https://alpinejs.dev/) as the backbone for handling all interactions on the client side,
and it enables by default all the great features from that library. It means you can create
Hydro components that utilize Alpine.js directives like [x-on](https://alpinejs.dev/directives/on), [x-data](https://alpinejs.dev/directives/data), [x-text](https://alpinejs.dev/directives/text), [x-ref](https://alpinejs.dev/directives/ref) and all the rest.

Example. Select the content of an input when focused:
```razor
@model Search

<div>
  Count: <strong>@Model.Count</strong>
  <input asp-for="Phrase" hydro-bind x-on:focus="$el.select()" />
</div>
```

Example. Create local JS state and operate on it.
```razor
@model Search

<div>
  <div x-data="{ index: 0 }">
    <strong x-text="index"></strong>
    <button x-on:click="index = index + 1">Add</button>
  </div>
</div>
```

The only limitation is that you can't set a custom `x-data` attribute on the root element, that's why in the above example a nested div is introduced.