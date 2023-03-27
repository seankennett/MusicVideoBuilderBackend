using System.ComponentModel.DataAnnotations;
using BuildEntities;

namespace SpaWebApi.Models
{
    public class VideoBuildRequest
    {
        [Required]
        public Guid BuildId { get; set; }

        [Required]
        public Resolution Resolution { get; set; }
    }
}
