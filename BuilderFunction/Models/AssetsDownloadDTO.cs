using SharedEntities.Models;
using System.Collections.Generic;

namespace BuilderFunction.Models
{
    public class AssetsDownloadDTO : AssetsDownload
    {
        public string WorkingDirectory { get; set; }
        public string TemporaryBlobPrefix { get; set; }
        public string UserContainerName { get; set; }
    }
}