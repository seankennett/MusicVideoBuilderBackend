using BuildDataAccess;
using BuildEntities;
using BuilderEntities.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildInstructorFunction.Services
{
    public class BuilderFunctionSender : IBuilderFunctionSender
    {
        private readonly IStorageService _storageService;

        public BuilderFunctionSender(IStorageService storageService)
        {
            _storageService = storageService;
        }
        public async Task SendBuilderFunctionMessage(string userContainerName, bool hasAudio, Resolution resolution, string outputBlobPrefix, string tempBlobPrefix, List<string> uniqueLayers, List<FfmpegIOCommand> clipCommands, FfmpegIOCommand clipMergeCommand, List<FfmpegIOCommand> splitFrameCommands, FfmpegIOCommand splitFrameMergeCommand)
        {
            BuilderMessage builderMessage = new BuilderMessage
            {
                OutputBlobPrefix = outputBlobPrefix,
                UserContainerName = userContainerName,
                TemporaryBlobPrefix = tempBlobPrefix,
                ClipCommands = clipCommands,
                ClipMergeCommand = clipMergeCommand,
                SplitFrameCommands = splitFrameCommands,
                SplitFrameMergeCommand = splitFrameMergeCommand,
                AssetsDownload = new AssetsDownload
                {
                    LayerIds = uniqueLayers,
                    TemporaryFiles = new List<string> { InstructorConstants.AllFramesConcatFileName, InstructorConstants.SplitFramesConcatFileName },
                    Resolution = resolution
                }
            };

            if (hasAudio)
            {
                builderMessage.AssetsDownload.TemporaryFiles.Add(BuildDataAccessConstants.AudioFileName);
            }

            if (resolution == Resolution.Free)
            {
                await _storageService.SendFreeBuilderMessageAsync(builderMessage);
            }
            else
            {
                await _storageService.SendHdBuilderMessageAsync(builderMessage);
            }
        }
    }
}
