namespace SharedEntities.Models
{
    public class LayerFinder : Layer
    {
        public IEnumerable<string> Tags { get; set; }

        public long UserCount { get; set; }
    }
}
