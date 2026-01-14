namespace GymCRM.SchedulingAPI.Models.DTOs;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];

    private ValidationResult(bool isValid, IEnumerable<string> errors)
    {
        IsValid = isValid;
        Errors = errors.ToList();
    }
    
    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() 
        => new(true, Array.Empty<string>());

    /// <summary>
    /// Creates a failed validation result with a single error message.
    /// </summary>
    public static ValidationResult Fail(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Error message cannot be null or empty.", nameof(error));
        }

        return new ValidationResult(false, new[] { error });
    }

    /// <summary>
    /// Creates a failed validation result with multiple error messages.
    /// </summary>
    public static ValidationResult Fail(params string[] errors)
    {
        if (errors == null || errors.Length == 0)
        {
            throw new ArgumentException("At least one error message is required.", nameof(errors));
        }

        return new ValidationResult(false, errors);
    }

    /// <summary>
    /// Creates a failed validation result with multiple error messages.
    /// </summary>
    public static ValidationResult Fail(IEnumerable<string> errors)
    {
        var errorList = errors?.ToList() ?? new List<string>();
        
        if (errorList.Count == 0)
        {
            throw new ArgumentException("At least one error message is required.", nameof(errors));
        }

        return new ValidationResult(false, errorList);
    }
}