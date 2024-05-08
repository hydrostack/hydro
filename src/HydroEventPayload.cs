namespace Hydro;

internal class HydroEventPayload
{
    public string Name { get; set; }
    public string Subject { get; set; }
    public object Data { get; set; }
    public Scope Scope { get; set; }
    public string OperationId { get; set; }
}