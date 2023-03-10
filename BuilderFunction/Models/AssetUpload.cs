namespace BuilderFunction.Models
{
    public class AssetUpload
    {
        public string UserContainerName { get; set; }
        public string OutputBlobPrefix { get; set; }
        public string OutputFileName { get; set; }
        public string WorkingDirectory { get; set; }
    }
}