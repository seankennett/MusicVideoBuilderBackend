using System.ComponentModel.DataAnnotations;
using SharedEntities.Models;

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
