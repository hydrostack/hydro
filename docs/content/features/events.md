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
// ProductList.cshtml.cs~~~~

public class ProductList : HydroComponent
{
    public void Add() =>
        Dispatch(new OpenAddModal());
}
```

In this case using a Hydro action might be an overkill, since it will cause an unnecessary additional request to the
backend.

To avoid that, you can dispatch actions straight from your client code by using a `hydro-dispatch` tag helper:

```razor
<button type="button"
    hydro-dispatch="@(new OpenAddModal())"
    event-scope="@Scope.Global">
  Add
</button>
```

Now, after clicking the button, the event `OpenAddModal` will be triggered without calling Hydro action first.

## Events scope

By default, the events are dispatched only to their parent component. To publish a global event use the following
method:

```c#
Dispatch(new ShowMessage(Content), Scope.Global);
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