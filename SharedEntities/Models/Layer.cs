namespace SharedEntities.Models
{
    public class Layer
    {
        public string? LayerName { get; set; }
        public Guid LayerId { get; set; }
        public LayerTypes LayerType { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}