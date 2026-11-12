using System.ComponentModel.DataAnnotations;

namespace Beerly.Models
{
    public class CountryCodeAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // A missing value is [Required]'s job, not ours
            if (value is null) return ValidationResult.Success;

            if (value is string code && Countries.Codes.Contains(code))
                return ValidationResult.Success;

            return new ValidationResult("Country must be a valid two-letter country code.");
        }
    }
}