using System.ComponentModel.DataAnnotations;

namespace SharedEntities.Models
{
    public class LayerUploadMessage : LayerUpload
    {
        [Required]
        public Guid LayerId { get; set; }
        public Guid? AuthorObjectId { get; set; }
    }
}