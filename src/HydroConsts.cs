namespace Hydro;

internal static class HydroConsts
{
    public static class RequestHeaders
    {
        public const string Boosted = "Hydro-Boosted";
        public const string Hydro = "Hydro-Request";
        public const string OperationId = "Hydro-Operation-Id";
        public const string Payload = "Hydro-Payload";
    }

    public static class ResponseHeaders
    {
        public const string Trigger = "Hydro-Trigger";
        public const string LocationTarget = "Hydro-Location-Target";
        public const string LocationTitle = "Hydro-Location-Title";
        public const string OperationId = "Hydro-Operation-Id";
        public const string SkipOutput = "Hydro-Skip-Output";
        public const string RefreshToken = "Refresh-Antiforgery-Token";
        public const string Scripts = "Hydro-Js";
    }

    public static class ContextItems
    {
        public const string RenderedComponentIds = "hydro-all-ids";
        public const string EventName = "hydro-event";
        public const string MethodName = "hydro-method";
        public const string BaseModel = "hydro-base-model";
        public const string RequestForm = "hydro-request-form";
        public const string Parameters = "hydro-parameters";
        public const string EventData = "hydro-event-model";
        public const string EventSubject = "hydro-event-subject";
        public const string IsRootRendered = "hydro-root-rendered";
    }

    public static class Component
    {
        public const string ParentComponentId = "ParentId";
        public const string EventMethodName = "event";
    }
}
