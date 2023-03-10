using System.Collections.Generic;

namespace UploadLayerFunction.Models
{
    internal class CleanUpBlobsDTO
    {
        public IEnumerable<string> BlobNames { get; set; }
        public string ContainerName { get; set; }
    }
}
