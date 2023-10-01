namespace BuilderEntities.Entities;

public class BuilderMessage
{
    public AssetsDownload AssetsDownload { get; set; }
    public IEnumerable<FfmpegIOCommand> ClipCommands { get; set; }
    public FfmpegIOCommand ClipMergeCommand { get; set; }
    public string OutputBlobPrefix { get; set; }
    public string UserContainerName { get; set; }
    public string TemporaryBlobPrefix { get; set; }
    public IEnumerable<FfmpegIOCommand> SplitFrameCommands { get; set; }
    public FfmpegIOCommand SplitFrameMergeCommand { get; set; }
}