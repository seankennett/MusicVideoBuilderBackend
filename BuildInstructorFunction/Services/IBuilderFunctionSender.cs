using BuildEntities;
using BuilderEntities.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildInstructorFunction.Services
{
    public interface IBuilderFunctionSender
    {
        Task SendFreeBuilderFunctionMessage(string userContainerName, bool hasAudio, Resolution resolution, string outputBlobPrefix, string tempBlobPrefix, List<string> uniqueLayers, List<FfmpegIOCommand> clipCommands, FfmpegIOCommand clipMergeCommand, List<FfmpegIOCommand> splitFrameCommands, FfmpegIOCommand splitFrameMergeCommand);
    }
}
