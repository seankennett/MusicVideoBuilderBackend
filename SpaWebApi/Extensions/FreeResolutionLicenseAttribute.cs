using BuildEntities;
using System.ComponentModel.DataAnnotations;

namespace SpaWebApi.Extensions
{
    public class FreeResolutionLicenseAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ErrorMessage = ErrorMessageString;
            var reolution = (Resolution)value;
            if (reolution != Resolution.Free)
            {
                return ValidationResult.Success;
            }

            var property = validationContext.ObjectType.GetProperty("License");
            if (property == null)
            {
                throw new ArgumentException("Property License not found");
            }

            var license = (License)property.GetValue(validationContext.ObjectInstance);

            if (license != License.Personal)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}
