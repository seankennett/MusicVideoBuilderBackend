using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using SharedEntities;
using SharedEntities.Extensions;
using SharedEntities.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuildInstructorFunction.Services
{
    public class BuilderFunctionSender : IBuilderFunctionSender
    {
        private readonly Lazy<QueueClient> _queueClientFree;
        private readonly Lazy<QueueClient> _queueClientHd;

        public BuilderFunctionSender(IOptions<Connections> connections)
        {
            _queueClientFree = new Lazy<QueueClient>(() => new QueueClient(connections.Value.PrivateStorageConnectionString, Resolution.Free.GetBlobPrefixByResolution() + SharedConstants.BuilderQueueSuffix, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            }));
            _queueClientHd = new Lazy<QueueClient>(() => new QueueClient(connections.Value.PrivateStorageConnectionString, Resolution.Hd.GetBlobPrefixByResolution() + SharedConstants.BuilderQueueSuffix, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            }));
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
                    TemporaryFiles = new List<string> { InstructorConstants.AllFramesConcatFileName, InstructorConstants.SplitFramesConcatFileName }
                }
            };

            if (hasAudio)
            {
                builderMessage.AssetsDownload.TemporaryFiles.Add(SharedConstants.AudioFileName);
            }

            if (resolution == Resolution.Free)
            {
                await _queueClientFree.Value.SendMessageAsync(JsonSerializer.Serialize(builderMessage));
            }
            else
            {
                await _queueClientHd.Value.SendMessageAsync(JsonSerializer.Serialize(builderMessage));
            }
        }
    }
}
