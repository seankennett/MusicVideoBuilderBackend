using SharedEntities.Models;

namespace BuilderFunction.Models
{
    public class FfmpegIOCommandDTO : FfmpegIOCommand
    {
        public string TemporaryBlobPrefix { get; set; }
        public string WorkingDirectory { get; set; }
    }
}