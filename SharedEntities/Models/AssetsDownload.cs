namespace SharedEntities.Models
{
    public class AssetsDownload
    {
        public IEnumerable<string> LayerIds { get; set; }
        public List<string> TemporaryFiles { get; set; }
        public bool ShouldWatermark { get; set; }
    }
}