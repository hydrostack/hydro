---
outline: deep
---

# Events

Events in Hydro are a powerful feature for communication between components. Components can publish events that other components can subscribe to. The use of events allows components to remain decoupled and promotes a clean architecture.

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

## Events scope

By default, the events are dispatched only to their parent component. To publish a global event use the following method:

```c#
Dispatch(new ShowMessage(Content), Scope.Global);
```

Any component that subscribes for `ShowMessage` will be notified, no matter the component's location.