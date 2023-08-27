---
outline: deep
---

# Request queuing

With AJAX operations it's possible to run into a situation known as a "race condition" of requests. For example, suppose we have two AJAX calls, Call A and Call B, initiated from the client side to update the same piece of data on the server. The issue arises when Call B is initiated before Call A has received its response from the server, resulting in Call B working with stale or outdated data.

Hydro addresses this challenge elegantly by ensuring that all requests made from the UI are chained and executed one after another, not in parallel. This essentially creates a queue of requests. Once a request is completed and the response is received, the next request in the queue is initiated.

This strategy ensures that each request has the most recent data from the previous request response, thereby effectively eliminating the potential for race conditions.

This also ensures consistency of the state on the client side, as it always reflects the most recent state of the server, even in highly interactive and dynamic UIs.

Therefore, you can confidently make multiple asynchronous calls to the server without worrying about potential race conditions. Hydro handles the complexity for you, allowing you to focus on building your application logic.