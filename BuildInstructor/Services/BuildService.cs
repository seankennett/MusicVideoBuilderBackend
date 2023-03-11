using Azure.Storage.Queues;
using DataAccessLayer.Repositories;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.Extensions.Options;
using SharedEntities;
using SharedEntities.Extensions;
using SharedEntities.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuildInstructor.Services
{
    public class BuildService : IBuildService
    {
        private readonly IBuildRepository _buildRepository;
        private readonly IVideoRepository _videoRepository;
        private readonly IFfmpegService _ffmpegService;
        private readonly IStorageService _storageService;
        private readonly Lazy<QueueClient> _queueClientFree;
        private readonly Lazy<QueueClient> _queueClientHd;
        private readonly string _poolId;
        private readonly BatchSharedKeyCredentials _batchCredentials;

        private const string AllFramesConcatFileName = "concatAllFrames.txt";
        private const string SplitFramesConcatFileName = "concatSplitFrames.txt";
        private const string AllFramesVideoName = "allframes";

        public BuildService(IBuildRepository buildRepository, IVideoRepository videoRepository, IFfmpegService ffmpegService, IStorageService storageService, IOptions<Connections> connections)
        {
            _buildRepository = buildRepository;
            _videoRepository = videoRepository;
            _ffmpegService = ffmpegService;
            _storageService = storageService;
            _queueClientFree = new Lazy<QueueClient>(() => new QueueClient(connections.Value.PrivateStorageConnectionString, Resolution.Free.GetBlobPrefixByResolution() + SharedConstants.BuilderQueueSuffix, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            }));
            _queueClientHd = new Lazy<QueueClient>(() => new QueueClient(connections.Value.PrivateStorageConnectionString, Resolution.Hd.GetBlobPrefixByResolution() + SharedConstants.BuilderQueueSuffix, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            }));

            _batchCredentials = new BatchSharedKeyCredentials(connections.Value.BatchServiceEndpoint, connections.Value.BatchServiceName, connections.Value.BatchServiceKey);
            _poolId = connections.Value.PoolName;
        }
        public async Task InstructBuildAsync(string paymentIntentId)
        {
            UserBuild build = await _buildRepository.GetByPaymentIntentId(paymentIntentId);
            if (build == null)
            {
                throw new Exception($"payment intent {paymentIntentId} not in build table");
            }

            build.BuildStatus = BuildStatus.BuildingPending;
            await _buildRepository.SaveAsync(build, build.UserObjectId);
            await InstructBuildAsync(build);
        }

        public async Task InstructBuildAsync(UserBuild build)
        {
            var userContainerName = GuidHelper.GetUserContainerName(build.UserObjectId);
            var hasAudio = build.HasAudio;
            var buildId = build.BuildId;
            var resolution = build.Resolution;
            var outputBlobPrefix = buildId.ToString();
            var tempBlobPrefix = GuidHelper.GetTempBlobPrefix(buildId);
            var video = await _videoRepository.GetAsync(build.UserObjectId, build.VideoId);
            var videoFileName = $"{video.VideoName}.{video.Format}";
            var allFrameVideoFileName = $"{AllFramesVideoName}.{video.Format}";

            var uniqueClips = video.Clips.DistinctBy(x => x.ClipId);

            var splitFrameCommands = new List<FfmpegIOCommand>();
            var videoLengthSeconds = video.Clips.Sum(x => x.BeatLength) * TimeSpan.FromMinutes(1).TotalSeconds / video.BPM;
            var splitVideoTotal = (int)Math.Ceiling(videoLengthSeconds / SharedConstants.VideoSplitLengthSeconds);
            var is4KFormat = build.Resolution == Resolution.FourK;
            for (var i = 0; i < splitVideoTotal; i++)
            {
                var splitFrameVideoName = $"s{i}.{video.Format}";
                splitFrameCommands.Add(new FfmpegIOCommand
                {
                    FfmpegCode = _ffmpegService.GetSplitFrameCommand(is4KFormat, tempBlobPrefix, TimeSpan.FromSeconds(SharedConstants.VideoSplitLengthSeconds * i), TimeSpan.FromSeconds(SharedConstants.VideoSplitLengthSeconds), allFrameVideoFileName, splitFrameVideoName, i == 0 ? video.VideoDelayMilliseconds : null),
                    VideoName = splitFrameVideoName
                });
            }

            var concatClipFileContents = _ffmpegService.GetConcatCode(video.Clips.Select(x => $"{x.ClipId}.{video.Format}"));
            await _storageService.UploadTextFile(userContainerName, tempBlobPrefix, AllFramesConcatFileName, concatClipFileContents, !hasAudio);

            var concatSplitFrameFileContents = _ffmpegService.GetConcatCode(splitFrameCommands.Select(x => x.VideoName));
            await _storageService.UploadTextFile(userContainerName, tempBlobPrefix, SplitFramesConcatFileName, concatSplitFrameFileContents, false);

            if (is4KFormat)
            {
                var outputContainerSasUri = _storageService.GetContainerSasUri(userContainerName, TimeSpan.FromDays(1));

                List<ResourceFile> allFrameVideoInputs = new List<ResourceFile> { ResourceFile.FromAutoStorageContainer(userContainerName, null, $"{tempBlobPrefix}/{AllFramesConcatFileName}") };
                var clipTasks = new List<CloudTask>();
                foreach (var clip in uniqueClips)
                {
                    // directory will not be created in clipcode's case so putting file flat on file system
                    var clipCommand = _ffmpegService.GetClipCode(clip, resolution, video.Format, video.BPM, true, ".");
                    var clipTaskId = $"c-{buildId}-{clip.ClipId}";
                    var clipFileName = $"{clip.ClipId}.{video.Format}";

                    List<ResourceFile> inputFiles = new List<ResourceFile>();
                    if (clip.Layers != null)
                    {
                        foreach (var userLayer in clip.Layers)
                        {
                            inputFiles.Add(ResourceFile.FromAutoStorageContainer(userLayer.LayerId.ToString(), userLayer.LayerId.ToString(), resolution.GetBlobPrefixByResolution()));
                        }
                    }

                    var outputBlobName = $"{tempBlobPrefix}/{clipFileName}";
                    CloudTask clipTask = SetUpTask(outputBlobName, outputContainerSasUri, $"{tempBlobPrefix}/{clip.ClipId}", clipCommand, clipTaskId, inputFiles, clipFileName);
                    clipTasks.Add(clipTask);

                    allFrameVideoInputs.Add(ResourceFile.FromAutoStorageContainer(userContainerName, null, outputBlobName));
                }

                var allFrameVideoCommand = _ffmpegService.GetMergeCode(true, tempBlobPrefix, allFrameVideoFileName, null, AllFramesConcatFileName);
                var allFramesTaskId = $"a-{buildId}";
                var allFramesOutputBlobName = $"{tempBlobPrefix}/{allFrameVideoFileName}";
                var allFramesTask = SetUpTask(allFramesOutputBlobName, outputContainerSasUri, $"{tempBlobPrefix}/{AllFramesVideoName}", allFrameVideoCommand, allFramesTaskId, allFrameVideoInputs, allFramesOutputBlobName);
                allFramesTask.DependsOn = TaskDependencies.OnTasks(clipTasks);

                List<ResourceFile> splitFrameInputs = new List<ResourceFile> { ResourceFile.FromAutoStorageContainer(userContainerName, null, allFramesOutputBlobName) };
                List<ResourceFile> splitFrameMergeInputs = new List<ResourceFile> { ResourceFile.FromAutoStorageContainer(userContainerName, null, $"{tempBlobPrefix}/{SplitFramesConcatFileName}") };
                if (hasAudio)
                {
                    splitFrameMergeInputs.Add(ResourceFile.FromAutoStorageContainer(userContainerName, null, $"{tempBlobPrefix}/{SharedConstants.AudioFileName}"));
                }

                var splitFramesTasks = new List<CloudTask>();
                for (int i = 0; i < splitFrameCommands.Count; i++)
                {
                    var splitFrameCommand = splitFrameCommands[i];
                    var splitTaskId = $"s-{buildId}-{i}";
                    var splitFrameOutputBlobName = $"{tempBlobPrefix}/{splitFrameCommand.VideoName}";
                    CloudTask splitFrameTask = SetUpTask(splitFrameOutputBlobName, outputContainerSasUri, $"{tempBlobPrefix}/split", splitFrameCommand.FfmpegCode, splitTaskId, splitFrameInputs, splitFrameOutputBlobName);
                    splitFrameTask.DependsOn = TaskDependencies.OnTasks(allFramesTask);
                    splitFramesTasks.Add(splitFrameTask);

                    splitFrameMergeInputs.Add(ResourceFile.FromAutoStorageContainer(userContainerName, null, splitFrameOutputBlobName));
                }

                var splitMergeVideoCommand = _ffmpegService.GetMergeCode(true, tempBlobPrefix, outputBlobPrefix, videoFileName, hasAudio ? SharedConstants.AudioFileName : null, SplitFramesConcatFileName);
                var splitMergeTaskId = $"v-{buildId}";
                var splitMergeBlobName = $"{outputBlobPrefix}/{videoFileName}";
                var splitMergeTask = SetUpTask(splitMergeBlobName, outputContainerSasUri, tempBlobPrefix, splitMergeVideoCommand, splitMergeTaskId, splitFrameMergeInputs, splitMergeBlobName);
                splitMergeTask.DependsOn = TaskDependencies.OnTasks(splitFramesTasks);

                var allTasks = clipTasks.Union(splitFramesTasks).Union(new List<CloudTask> { allFramesTask, splitMergeTask });
                using (var batchClient = BatchClient.Open(_batchCredentials))
                {
                    var job = batchClient.JobOperations.CreateJob(splitMergeTaskId, new PoolInformation { PoolId = _poolId }); // get pool from config
                    job.OnAllTasksComplete = OnAllTasksComplete.TerminateJob;
                    job.UsesTaskDependencies = true;
                    job.Constraints = new JobConstraints { MaxTaskRetryCount = 1, MaxWallClockTime = TimeSpan.FromDays(1) };

                    await job.CommitAsync();
                    await batchClient.JobOperations.AddTaskAsync(job.Id, allTasks);
                }
            }
            else
            {
                BuilderMessage builderMessage = new BuilderMessage
                {
                    OutputBlobPrefix = outputBlobPrefix,
                    UserContainerName = userContainerName,
                    TemporaryBlobPrefix = tempBlobPrefix,
                    ClipCommands = uniqueClips.Select(uc => new FfmpegIOCommand
                    {
                        FfmpegCode = _ffmpegService.GetClipCode(uc, resolution, video.Format, video.BPM, false, tempBlobPrefix),
                        VideoName = $"{uc.ClipId}.{video.Format}"
                    }),
                    ClipMergeCommand = new FfmpegIOCommand
                    {
                        FfmpegCode = _ffmpegService.GetMergeCode(false, tempBlobPrefix, allFrameVideoFileName, null, AllFramesConcatFileName),
                        VideoName = allFrameVideoFileName
                    },
                    SplitFrameCommands = splitFrameCommands,
                    SplitFrameMergeCommand = new FfmpegIOCommand
                    {
                        FfmpegCode = _ffmpegService.GetMergeCode(false, tempBlobPrefix, outputBlobPrefix, videoFileName, hasAudio ? SharedConstants.AudioFileName : null, SplitFramesConcatFileName),
                        VideoName = videoFileName
                    },
                    AssetsDownload = new AssetsDownload
                    {
                        LayerIds = uniqueClips.Where(x => x.Layers != null).SelectMany(x => x.Layers).Select(x => x.LayerId.ToString()).Distinct(),
                        TemporaryFiles = new List<string> { AllFramesConcatFileName, SplitFramesConcatFileName }
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

        private CloudTask SetUpTask(string outputBlobName, Uri outputContainerSasUri, string errorBlobPrefix, string command, string taskId, List<ResourceFile> inputFiles, string filePath)
        {
            CloudTask cloudTask = new CloudTask(taskId, command);
            cloudTask.ResourceFiles = inputFiles;
            cloudTask.Constraints = new TaskConstraints { MaxWallClockTime = TimeSpan.FromHours(1), RetentionTime = TimeSpan.Zero };

            List<OutputFile> outputFileList = new List<OutputFile>();
            OutputFileBlobContainerDestination outputFileBlobContainerDestination = new OutputFileBlobContainerDestination(outputContainerSasUri.ToString(), outputBlobName);
            OutputFileBlobContainerDestination errorOutputFileBlobContainerDestination = new OutputFileBlobContainerDestination(outputContainerSasUri.ToString(), errorBlobPrefix);
            outputFileList.Add(new OutputFile(filePath,
                                                   new OutputFileDestination(outputFileBlobContainerDestination),
                                                   new OutputFileUploadOptions(OutputFileUploadCondition.TaskSuccess)));
            outputFileList.Add(new OutputFile(@"../std*.txt",
                                                   new OutputFileDestination(errorOutputFileBlobContainerDestination),
                                                   new OutputFileUploadOptions(OutputFileUploadCondition.TaskFailure)));

            cloudTask.OutputFiles = outputFileList;
            return cloudTask;
        }

    }    
}
