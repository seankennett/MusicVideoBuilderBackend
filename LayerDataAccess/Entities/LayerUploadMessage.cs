using System.ComponentModel.DataAnnotations;

namespace LayerDataAccess.Entities
{
    public class LayerUploadMessage : LayerUpload
    {
        [Required]
        public Guid LayerId { get; set; }
        public Guid? AuthorObjectId { get; set; }
    }
}