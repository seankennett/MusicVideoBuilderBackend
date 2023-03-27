
using LayerEntities;

namespace LayerDataAccess.Entities
{
    public class LayerFinder : Layer
    {
        public IEnumerable<string> Tags { get; set; }

        public long UserCount { get; set; }
    }
}
