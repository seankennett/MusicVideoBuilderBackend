using SharedEntities.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SharedEntities.Models
{
    public class LayerUpload
    {
        [Required]
        [MaxLength(50)]
        [RegularExpression(@"^[a-zA-Z0-9]*$")]
        public string LayerName { get; set; }

        [Required]
        public LayerTypes LayerType { get; set; }

        [Required]
        [MinLength(5)]
        [MaxLength(10)]
        [MaxLengthList(20)]
        [RegularExpressionList(@"^[a-zA-Z0-9]*$")]
        public IEnumerable<string> Tags { get; set; }
    }
}