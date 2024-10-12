using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Hydro.Validation;

/// <summary>
/// Validate collection of objects that also contain inner validation
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidateCollectionAttribute : ValidationAttribute
{
    /// <inheritdoc />
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is not IEnumerable enumerable)
        {
            return ValidationResult.Success;
        }

        var index = 0;

        var returnResults = new List<ValidationResult>();

        foreach (var item in enumerable)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(item);
            Validator.TryValidateObject(item, context, results, true);

            if (results.Count != 0)
            {
                returnResults.AddRange(results.Select(result => new ValidationResult(result.ErrorMessage, result.MemberNames.Select(m => $"{validationContext.MemberName}[{index}].{m}").ToList())));
            }

            index++;
        }

        if (returnResults.Count == 0)
        {
            return ValidationResult.Success;
        }

        var compositeResults = new CompositeValidationResult($"Validation for {validationContext.DisplayName} failed!", new[] { validationContext.DisplayName });
        returnResults.ForEach(compositeResults.AddResult);
        return compositeResults;
    }
}

/// <inheritdoc cref="ValidationResult" />
public class CompositeValidationResult : ValidationResult, ICompositeValidationResult
{
    private readonly List<ValidationResult> _results = new();

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Results => _results;

    /// <inheritdoc />
    public CompositeValidationResult(string errorMessage) : base(errorMessage) { }

    /// <inheritdoc />
    public CompositeValidationResult(string errorMessage, IEnumerable<string> memberNames) : base(errorMessage, memberNames) { }

    /// <inheritdoc />
    protected CompositeValidationResult(ValidationResult validationResult) : base(validationResult) { }

    internal void AddResult(ValidationResult validationResult)
    {
        _results.Add(validationResult);
    }
}
