namespace Hydro;

internal class HydroEventSubscription
{
    public string EventName { get; set; }
    public Delegate Action { get; set; }
}