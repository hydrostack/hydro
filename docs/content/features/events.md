---
outline: deep
---

# Events

Events in Hydro are a powerful feature for communication between components. Components can publish events that other
components can subscribe to. The use of events allows components to remain decoupled and promotes a clean architecture.

Here is an example:

```csharp
// CountChangedEvent.cs

public record CountChangedEvent(int Count);
```

To trigger an event from a Hydro component, use a `Dispatch` method in your action method:

```csharp
// Counter.cshtml.cs

public class Counter : HydroComponent
{
    public int Count { get; set; }
    
    public void Add()
    {
        Count++;
        Dispatch(new CountChangedEvent(Count));
    }
}
```

To subscribe to an event in a parent component, use there the `Subscribe` method:

```csharp
// Summary.cshtml.cs

public class Summary : HydroComponent
{
    public Summary()
    {
        Subscribe<CountChangedEvent>(Handle);
    }

    public int CountSummary { get; set; }
    
    public void Handle(CountChangedEvent data)
    {
        CountSummary = data.Count;
    }
}
```

When a component's subscription is triggered by an event, the component will be rerendered.

## Dispatching

As we saw in the above example, one of the ways to dispatch an event is to call `Dispatch` method:

```csharp
// Counter.cshtml.cs

public class Counter : HydroComponent
{
    public int Count { get; set; }
    
    public void Add()
    {
        Count++;
        Dispatch(new CountChangedEvent(Count));
    }
}
```

This is fine in most scenarios when dispatching is not the only one operation we want to do.
But sometimes your only intent is to dispatch an event:

```csharp
// ProductList.cshtml.cs

public class ProductList : HydroComponent
{
    public void Add() =>
        Dispatch(new OpenAddModal());
}
```

In this case using a Hydro action might be an overkill, since it will cause an unnecessary additional request and
rendering of the component. To avoid that, you can dispatch actions straight from your client code by using
`Model.Client.Dispatch`:

```razor
<button 
    on:click="@(() => Model.Client.Dispatch(new OpenAddModal())">
  Add
</button>
```

Now, after clicking the button, the event `OpenAddModal` will be triggered without calling Hydro action first.

Another way to avoid the extra render of the component is to use `[SkipOutput]` attribute on the Hydro action:

```csharp
// ProductList.cshtml.cs

public class ProductList : HydroComponent
{
    [SkipOutput]
    public void Add() =>
        Dispatch(new OpenAddModal());
}
```

You can also use SkipOutput method. It's useful when you want to conditionally skip the output:

```csharp
// ProductList.cshtml.cs

public class ProductList : HydroComponent
{
    public void Save()
    {
        if (this.Validate())
        {
            SkipOutput();
            Dispatch(new ProductSaved());
        }
    }
}
```

> **_NOTE:_** When skipping the output, any changes to the state won't be persisted.

## Synchronous vs asynchronous

By default, Hydro events are dispatched synchronously, which means they will follow one internal operation in Hydro.

Let's take a look at this example of a synchronous event:

```c#
public void Add()
{
    Count++;
    Dispatch(new CountChangedEvent(Count));
}
```

`Add` triggers the event synchronously, so the button that triggers this action will be disabled until both the action
and the event executions are done.

Now, let's compare it with the asynchronous way:

```c#
public void Add()
{
    Count++;
    Dispatch(new CountChangedEvent(Count), asynchronous: true);
}
```

`Add` triggers the event asynchronously, so the button that triggers this action will be disabled until the action is
done. The event execution won't be connected with the action's pipeline and will be run on its own.

## Event scope

By default, the events are dispatched only to their parent component. To publish a global event use the following
method:

```c#
Dispatch(new ShowMessage(Content), Scope.Global);
```

or

```c#
DispatchGlobal(new ShowMessage(Content));
```

Any component that subscribes for `ShowMessage` will be notified, no matter the component's location.

## Inlined subscription

The following code uses inlined subscription and will work same as passing the method:

```csharp
// Summary.cshtml.cs

public class Summary : HydroComponent
{
    public Summary()
    {
        Subscribe<CountChangedEvent>(data => CountSummary = data.Count);
    }

    public int CountSummary { get; set; }
}
```

## Empty subscription

Sometimes we only want to rerender the component when an event occurs:

```csharp
// ProductList.cshtml.cs

public class ProductList : HydroComponent
{
    public ProductList()
    {
        Subscribe<ProductAddedEvent>();
    }

    public override void Render()
    {
        // When ProductAddedEvent occurs, component will be rerendered
    }
}
```

## Event subject

There might be a situation where you want to filter the events you receive in your subscription handler. It means that
your component subscribes to an event, but handles it only when it contains a certain flag. That flag can be any string
and is called a `subject`.

You can imagine a page with multiple lists of todos. Each list is a Hydro component that listens to events like
`TodoAdded`, `TodoRemoved` or `TodoEdited`. When a todo is removed on one list, you don't want all the other lists to
receive and react to that event, but only the list that contained that todo item. This is solved in Hydro by using
`subject` parameter, which in this case will be the list's id. When `TodoAdded`, `TodoRemoved` or `TodoEdited` are
dispatched, `subject` is set to their list's id. The list component subscribes to those events with `subject` set to the
their list's id.

Example:

```c#
// Todo.cshtml.cs

public class Todo : HydroComponent
{
    public string TodoId { get; set; }
    public string ListId { get; set; }
    public string Text { get; set; }
    public bool IsDone { get; set; }
    
    public void Remove(string id)
    {
        DispatchGlobal(new TodoRemoved { TodoId }, subject: ListId);
    }
}
```

```c#
// TodoList.cshtml.cs

public class TodoList : HydroComponent
{
    public string ListId { get; set; }
    public List<Todo> Todos { get; set; }
    
    public TodoList()
    {
        Subscribe<TodoRemoved>(subject: () => ListId, Handle);
    }
    
    public void Handle(TodoRemoved data)
    {
        // will be called only when subject is ListId
        Todos.RemoveAll(todo => todo.TodoId == data.TodoId);
    }
}
```

In `Subscribe` method call `subject` parameter is a `Func<string>` instead of `string`,
because its value could be taken from component's properties that are not set yet, since it's
a constructor.

> NOTE: If you subscribe for an event without specifying the subject, it will catch all the events
> of that type, no matter if they were dispatched with subject or not.