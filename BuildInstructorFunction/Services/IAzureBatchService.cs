using BuildEntities;
using BuilderEntities.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildInstructorFunction.Services
{
    public interface IAzureBatchService
    {
        Task SendBatchRequest(string userContainerName, bool hasAudio, Guid buildId, Resolution resolution, string outputBlobPrefix, string tempBlobPrefix, Dictionary<int, IEnumerable<string>> layerIdsPerClip, List<FfmpegIOCommand> clipCommands, FfmpegIOCommand clipMergeCommand, List<FfmpegIOCommand> splitFrameCommands, FfmpegIOCommand splitFrameMergeCommand);
    }
}
