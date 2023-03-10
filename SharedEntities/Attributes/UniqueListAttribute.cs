using System.ComponentModel.DataAnnotations;

namespace SharedEntities.Attributes
{
    public class UniqueListAttribute : ValidationAttribute
    {
        readonly string _propertyName;
        public UniqueListAttribute(string propertyName)
        {
            _propertyName = propertyName;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            var iEnumerableType = value.GetType();
            Type? customType;

            // type is IEnumerable<T>;
            if (iEnumerableType.IsGenericType && iEnumerableType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                customType = iEnumerableType.GetGenericArguments()[0];
            }
            else
            {
                // type implements/extends IEnumerable<T>;
                customType = iEnumerableType.GetInterfaces()
                                        .Where(t => t.IsGenericType &&
                                               t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                        .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();

                if (customType == null)
                {
                    return new ValidationResult("Can only use unique list validator on IEnumerables");
                }
            }

            var propertyInfo = customType.GetProperty(_propertyName);
            if (propertyInfo != null)
            {
                var list = value as IEnumerable<object>;
                if (list != null && list.DistinctBy(x => propertyInfo.GetValue(x, null)).Count() == list.Count())
                {
                    return ValidationResult.Success;
                }

                return new ValidationResult($"Property {_propertyName} is not unique");
            }

            return new ValidationResult($"Property {_propertyName} does not exist on type {customType.Name}");
        }
    }
}
