namespace Hydro;

internal class HydroEventSubscription
{
    public string EventName { get; set; }
    public Func<string> SubjectRetriever { get; set; }
    public Delegate Action { get; set; }
}