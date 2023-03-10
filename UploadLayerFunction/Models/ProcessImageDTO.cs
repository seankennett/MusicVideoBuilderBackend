namespace UploadLayerFunction.Models
{
    internal class ProcessImageDTO
    {
        public int Index { get; set; }
        public string OriginalName { get; set; }
        public bool ShouldImageBeOpaque { get; set; }
        public string ContainerName { get; set; }
    }
}
