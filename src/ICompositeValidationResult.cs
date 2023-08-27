using System.ComponentModel.DataAnnotations;

namespace Hydro;

/// <summary>
/// Provides data of complex model validation
/// </summary>
public interface ICompositeValidationResult
{
    /// <summary>
    /// Result of complex object validation
    /// </summary>
    IEnumerable<ValidationResult> Results { get; }
}