using System.ComponentModel.DataAnnotations;

namespace Hydro.Validation;

/// <summary>
/// Validate complex object that also contains inner validations
/// </summary>
public class ValidateObjectAttribute : ValidationAttribute
{
    /// <inheritdoc />
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();
        
        if (value != null)
        {
            var context = new ValidationContext(value);
            Validator.TryValidateObject(value, context, results, true);
        }

        if (results.Count == 0)
        {
            return ValidationResult.Success;
        }

        var compositeResults = new CompositeValidationResult($"Validation for {validationContext.DisplayName} failed!");

        foreach (var result in results)
        {
            compositeResults.AddResult(new ValidationResult(result.ErrorMessage, result.MemberNames.Select(m => $"{validationContext.MemberName}.{m}").ToList()));
        }
        
        return compositeResults;
    }
}