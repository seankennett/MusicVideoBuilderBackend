using BuilderEntities.Entities;

namespace BuilderFunction.Models
{
    public class FfmpegIOCommandDTO : FfmpegIOCommand
    {
        public FfmpegIOCommandDTO() { }
        public FfmpegIOCommandDTO(FfmpegIOCommand command)
        {
            FfmpegCode = command.FfmpegCode;
            VideoName = command.VideoName;
        }

        public string TemporaryBlobPrefix { get; set; }
        public string WorkingDirectory { get; set; }
    }
}