using SharedEntities.Models;
using System.Collections.Generic;

namespace BuilderFunction.Models
{
    public class AssetsDownloadDTO : AssetsDownload
    {
        public AssetsDownloadDTO() { }
        public AssetsDownloadDTO(AssetsDownload assetDownload)
        {
            this.LayerIds = assetDownload.LayerIds;
            this.TemporaryFiles= assetDownload.TemporaryFiles;
        }
        public string WorkingDirectory { get; set; }
        public string TemporaryBlobPrefix { get; set; }
        public string UserContainerName { get; set; }
    }
}