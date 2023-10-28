namespace Hydro;

/// <summary>
/// Event called in case of unhandled exception in Hydro component
/// </summary>
/// <param name="Message">Exception message</param>
/// <param name="Data">Payload</param>
public record UnhandledHydroError(string Message, object Data);