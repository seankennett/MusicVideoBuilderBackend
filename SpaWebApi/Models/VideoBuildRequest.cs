using System.ComponentModel.DataAnnotations;
using BuildEntities;
using SpaWebApi.Extensions;

namespace SpaWebApi.Models
{
    public class VideoBuildRequest
    {
        [Required]
        public Guid BuildId { get; set; }

        [Required]
        [FreeResolutionLicense(ErrorMessage ="Free resolution must have personal license")]
        public Resolution Resolution { get; set; }

        [Required]
        public License License { get; set; }
    }
}
