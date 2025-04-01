using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.Validations
{
    public class FirstLetterUppercaseAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null || string.IsNullOrEmpty(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var valueString = value.ToString();
            if (string.IsNullOrEmpty(valueString))
            {
                return ValidationResult.Success;
            }

            var firstLetter = valueString[0].ToString();

            if (firstLetter != firstLetter.ToUpper())
            {
                return new ValidationResult("The first letter must be uppercase");
            }

            return ValidationResult.Success;
        }
    }
}
