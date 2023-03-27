using System.ComponentModel.DataAnnotations;
using VideoDataAccess.Entities;

namespace VideoDataAccess.Attributes
{
    public class ClipLengthAttribute : ValidationAttribute
    {
        private readonly string _bpmProperty;
        private readonly int _maxTimeInMinutes;

        public ClipLengthAttribute(string bpmProperty, int maxTimeInMinutes)
        {
            _bpmProperty = bpmProperty;
            _maxTimeInMinutes = maxTimeInMinutes;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ErrorMessage = ErrorMessageString;
            var clips = (IEnumerable<Clip>)value;

            var property = validationContext.ObjectType.GetProperty(_bpmProperty);

            if (property == null)
            {
                throw new ArgumentException("Bpm property not found");
            }

            var bpm = (byte)property.GetValue(validationContext.ObjectInstance);

            if (bpm * _maxTimeInMinutes < clips.Sum(c => c.BeatLength))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}
