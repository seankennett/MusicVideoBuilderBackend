using SharedEntities.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildInstructorFunction.Services
{
    public interface IBuilderFunctionSender
    {
        Task SendBuilderFunctionMessage(string userContainerName, bool hasAudio, Resolution resolution, string outputBlobPrefix, string tempBlobPrefix, List<string> uniqueLayers, List<FfmpegIOCommand> clipCommands, FfmpegIOCommand clipMergeCommand, List<FfmpegIOCommand> splitFrameCommands, FfmpegIOCommand splitFrameMergeCommand);
    }
}
