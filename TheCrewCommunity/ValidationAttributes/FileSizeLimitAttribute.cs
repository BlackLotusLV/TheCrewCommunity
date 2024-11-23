using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.ValidationAttributes;

public class FileSizeLimitAttribute(int maxFileSize) : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (ValidationResult.Success is null) throw new InvalidOperationException();
        if (value is not IFormFile file) return ValidationResult.Success;
        return file.Length > maxFileSize ? new ValidationResult(GetErrorMessage()) : ValidationResult.Success;
    }
    private string GetErrorMessage()
    {
        return $"Maximum allowed file size is { maxFileSize / 1024 / 1024} MB.";
    }
}