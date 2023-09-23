---
outline: deep
---

# User interface utilities

## Boost

With `hydro-boost` attribute relative links in your application can be loaded in the background and applied to the current document instead of doing the full page reload.

Examples:

Attribute applied directly on a link:
```html
<a href="/page" hydro-boost>My page</a>
```

Attribute applied directly on a parent of the links:
```html
<ul hydro-boost>
  <li><a href="/page1">My page 1</a></li>
  <li><a href="/page2">My page 2</a></li>
</ul>
```

## Styles

### `.hydro-request`
CSS class added to elements that triggered a Hydro operation and is currently being processed

### `.hydro-loading`
CSS class added to `body` element when a page is loading using boost functionality

### `disabled`
Attribute added to elements that triggered a Hydro operation and is currently being processed

## Styling examples:

### Page loading indicator

```html
<style>
#page-loading {
  display: none;
  position: absolute;
  left: 0;
  top: 0;
  width: 0;
  height: 2px;
  background: red;
  z-index: 2;
  animation: loadPage 5s forwards ease-out;
  animation-delay: 0.1s;
}

.hydro-loading #page-loading {
  display: block;
}

@keyframes loadPage {
  0% { width: 0; }
  100% { width: 100%; }
}
</style>

<body>
  <div id="page-loading"></div>
</body>
```

### Hydro action loading indicator

```css
/* MyComponent.cshtml.css */

.loader {
  display: none;
}

.hydro-request .loader {
  display: inline-block;
}
```

```razor
<!-- MyComponent.cshtml -->

@model MyComponent

<button hydro-action="Submit">Submit <div class="loader">...</div></button>
```
