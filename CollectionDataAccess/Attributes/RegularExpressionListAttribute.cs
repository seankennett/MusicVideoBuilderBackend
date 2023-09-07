using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace LayerDataAccess.Attributes
{
    public class RegularExpressionListAttribute : RegularExpressionAttribute
    {
        public RegularExpressionListAttribute(string pattern)
            : base(pattern) { }

        public override bool IsValid(object? value)
        {
            if (value == null || value is not IEnumerable<string>)
            {
                return false;
            }

            foreach (var val in value as IEnumerable<string>)
            {
                if (!Regex.IsMatch(val, Pattern))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
