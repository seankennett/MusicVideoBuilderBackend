using SharedEntities.Models;
using System.Collections.Generic;

namespace UploadLayerFunction.Models
{
    internal class SpriteSaveCleanDTO
    {
        public byte[][] SpriteImages { get; set; }
        public string ContainerName { get; set; }
        public IEnumerable<string> BlobNames { get; set; }
        public LayerUploadMessage LayerUploadMessage { get; set; }
    }
}
