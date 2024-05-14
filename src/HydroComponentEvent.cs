namespace Hydro;

internal class HydroComponentEvent
{
    public string Name { get; init; }
    public string Subject { get; init; }
    public object Data { get; init; }
    public string Scope { get; set; }
    public string OperationId { get; set; }
}