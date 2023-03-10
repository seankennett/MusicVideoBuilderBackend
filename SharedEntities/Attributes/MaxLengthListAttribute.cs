using System.ComponentModel.DataAnnotations;

namespace SharedEntities.Attributes
{
    public class MaxLengthListAttribute : MaxLengthAttribute
    {
        public MaxLengthListAttribute(int maximumLength)
            : base(maximumLength) { }

        public override bool IsValid(object? value)
        {
            if (value == null || value is not IEnumerable<string>)
            {
                return false;
            }

            foreach (var val in value as IEnumerable<string>)
            {
                if (val.Length > Length)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
