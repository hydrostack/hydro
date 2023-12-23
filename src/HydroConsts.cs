namespace Hydro;

internal static class HydroConsts
{
    public static class RequestHeaders
    {
        public const string Model = "hydro-model";
        public const string EventName = "hydro-event-name";
        public const string ClientEventName = "Hydro-Client-Event-Name";
        public const string Boosted = "Hydro-Boosted";
        public const string Hydro = "Hydro-Request";
        public const string Parameters = "Hydro-Parameters";
        public const string OperationId = "Hydro-Operation-Id";
        public const string Payload = "Hydro-Payload";
        public const string RenderedComponentIds = "hydro-all-ids";
    }

    public static class ResponseHeaders
    {
        public const string Trigger = "Hydro-Trigger";
        public const string OperationId = "Hydro-Operation-Id";
        public const string SkipOutput = "Hydro-Skip-Output";
    }

    public static class ContextItems
    {
        public const string RenderedComponentIds = "hydro-all-ids";
        public const string EventName = "hydro-event";
        public const string MethodName = "hydro-method";
        public const string IsBind = "hydro-bind";
        public const string BaseModel = "hydro-base-model";
        public const string RequestForm = "hydro-request-form";
        public const string RequestData = "hydro-request-model";
        public const string EventData = "hydro-event-model";
        public const string IsRootRendered = "hydro-root-rendered";
    }

    public static class Component
    {
        public const string ParentComponentId = "ParentId";
        public const string EventMethodName = "event";
    }
}
