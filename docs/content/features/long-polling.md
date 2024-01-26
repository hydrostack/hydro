---
outline: deep
---

# Long polling

Hydro provides a feature to poll the component's action at regular intervals. It is useful when you want to make sure that in one of the places on the page you're displaying the most recent data.

To enable long polling on an action, decorate it with `[Poll]` attribute. Default interval is set to 3 seconds (`3_000` milliseconds). To customize it, change the `Interval` property to the desired value in milliseconds. Actions decorated with `[Poll]` attribute have to be parameterless.

In the example below the `Refresh` action will be called every 60 seconds:

```csharp
public class NotificationsIndicator(INotifications notifications) : HydroComponent
{
    public int NotificationsCount { get; set; }
    
    [Poll(Interval = 60_000)]
    public async Task Refresh()
    {
        NotificationsCount = await notifications.GetCount();
    }
}
```

```razor
@model NotificationsIndicator

<div>
  Notifications: <strong>@Model.NotificationsCount</strong>
</div>
```

## Polling pauses

When a page with a polling component is hidden (not in the currently open tab), polling will stop and restart once the tab becomes visible again.