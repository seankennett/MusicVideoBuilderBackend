using Azure.Storage.Queues;
using DataAccessLayer.Repositories;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.Extensions.Options;
using SharedEntities;
using SharedEntities.Extensions;
using SharedEntities.Models;
using SpaWebApi.Models;
using System.Text.Json;

namespace SpaWebApi.Services
{
    public class VideoAssetService : IVideoAssetService
    {
        private readonly IVideoRepository _videoRepository;
        private readonly IFfmpegService _ffmpegService;
        private readonly IStorageService _storageService;
        private readonly BatchSharedKeyCredentials _batchCredentials;
        private readonly string _poolId;
        private readonly Lazy<QueueClient> _queueClientFree;
        private readonly Lazy<QueueClient> _queueClientHd;
        private readonly ILogger<VideoAssetService> _logger;
        private readonly IBuildRepository _buildRepository;
        private readonly IPaymentService _paymentService;
        private const string AllFramesConcatFileName = "concatAllFrames.txt";
        private const string SplitFramesConcatFileName = "concatSplitFrames.txt";
        private const string AllFramesVideoName = "allframes";
        public VideoAssetService(IVideoRepository videoRepository, IFfmpegService ffmpegService, IStorageService storageService, IOptions<Connections> connections, IBuildRepository buildRepository, ILogger<VideoAssetService> logger, IPaymentService paymentService)
        {
            _videoRepository = videoRepository;
            _ffmpegService = ffmpegService;
            _storageService = storageService;
            _buildRepository = buildRepository;
            _paymentService = paymentService;
            _batchCredentials = new BatchSharedKeyCredentials(connections.Value.BatchServiceEndpoint, connections.Value.BatchServiceName, connections.Value.BatchServiceKey);
            _poolId = connections.Value.PoolName;
            _queueClientFree = new Lazy<QueueClient>(() => new QueueClient(connections.Value.PrivateStorageConnectionString, Resolution.Free.GetBlobPrefixByResolution() + SharedConstants.BuilderQueueSuffix, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            }));
            _queueClientHd = new Lazy<QueueClient>(() => new QueueClient(connections.Value.PrivateStorageConnectionString, Resolution.Hd.GetBlobPrefixByResolution() + SharedConstants.BuilderQueueSuffix, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            }));
            _logger = logger;
        }

        public async Task<IEnumerable<VideoAsset>> GetAllAsync(Guid userObjectId)
        {
            var builds = await _buildRepository.GetAllAsync(userObjectId);

            var userContainerName = GetUserContainerName(userObjectId);

            var result = new List<VideoAsset>();
            foreach (var build in builds.Where(x => x.BuildStatus != BuildStatus.Failed && x.BuildStatus != BuildStatus.PaymentAuthorisationPending))
            {
                result.Add(new VideoAsset
                {
                    BuildStatus = build.BuildStatus,
                    DateCreated = build.DateUpdated,
                    DownloadLink = await _storageService.GetSASLink(userContainerName, build.BuildId.ToString(), $"/{SharedConstants.TempBlobPrefix}/", build.DateUpdated.AddDays(28)),
                    VideoId = build.VideoId
                });
            }
            return result;
        }

        public async Task<VideoAsset> BuildFreeVideoAsync(Guid userObjectId, int videoId, Guid buildId)
        {
            Video video = await GetAndValidateVideo(userObjectId, videoId);
            var currentBuild = await GetAndValidateBuild(userObjectId, videoId, buildId);

            var userContainerName = GetUserContainerName(userObjectId);
            var hasAudio = currentBuild != null && currentBuild.HasAudio;
            if (hasAudio)
            {
                var isValidAudio = await _storageService.ValidateAudioBlob(userContainerName, GetAudioBlobName(buildId));
                if (!isValidAudio)
                {
                    throw new Exception($"Problem finding audio blob or blob too big for user {userObjectId}");
                }
            }

            // save build status then send onto build conductor

            var outputBlobPrefix = buildId.ToString();
            var tempBlobPrefix = GetTempBlobPrefix(buildId);
            var videoFileName = $"{video.VideoName}.{video.Format}";
            var allFrameVideoFileName = $"{AllFramesVideoName}.{video.Format}";

            var uniqueClips = video.Clips.DistinctBy(x => x.ClipId);

            var splitFrameCommands = new List<FfmpegIOCommand>();
            var videoLengthSeconds = video.Clips.Sum(x => x.BeatLength) * TimeSpan.FromMinutes(1).TotalSeconds / video.BPM;
            var splitVideoTotal = (int)Math.Ceiling(videoLengthSeconds / SharedConstants.VideoSplitLengthSeconds);
            //var is4KFormat = resolution == Resolution.FourK;
            for (var i = 0; i < splitVideoTotal; i++)
            {
                var splitFrameVideoName = $"s{i}.{video.Format}";
                splitFrameCommands.Add(new FfmpegIOCommand
                {
                    FfmpegCode = _ffmpegService.GetSplitFrameCommand(false/*is4KFormat*/, tempBlobPrefix, TimeSpan.FromSeconds(SharedConstants.VideoSplitLengthSeconds * i), TimeSpan.FromSeconds(SharedConstants.VideoSplitLengthSeconds), allFrameVideoFileName, splitFrameVideoName, i == 0 ? video.VideoDelayMilliseconds : null),
                    VideoName = splitFrameVideoName
                });
            }

            var concatClipFileContents = _ffmpegService.GetConcatCode(video.Clips.Select(x => $"{x.ClipId}.{video.Format}"));
            await _storageService.UploadTextFile(userContainerName, tempBlobPrefix, AllFramesConcatFileName, concatClipFileContents, !hasAudio);

            var concatSplitFrameFileContents = _ffmpegService.GetConcatCode(splitFrameCommands.Select(x => x.VideoName));
            await _storageService.UploadTextFile(userContainerName, tempBlobPrefix, SplitFramesConcatFileName, concatSplitFrameFileContents, false);

            //if (is4KFormat)
            //{
            //    var outputContainerSasUri = await _storageService.CreateContainerAsync(userContainerName, false, TimeSpan.FromDays(1));

            //    List<ResourceFile> allFrameVideoInputs = new List<ResourceFile> { ResourceFile.FromAutoStorageContainer(userContainerName, null, $"{tempBlobPrefix}/{AllFramesConcatFileName}") };
            //    var clipTasks = new List<CloudTask>();
            //    foreach (var clip in uniqueClips)
            //    {
            //        // directory will not be created in clipcode's case so putting file flat on file system
            //        var clipCommand = _ffmpegService.GetClipCode(clip, resolution, video.Format, video.BPM, true, ".");
            //        var clipTaskId = $"c{video.VideoId}-{clip.ClipId}-{date}";
            //        var clipFileName = $"{clip.ClipId}.{video.Format}";

            //        List<ResourceFile> inputFiles = new List<ResourceFile>();
            //        if (clip.Layers != null)
            //        {
            //            foreach (var userLayer in clip.Layers)
            //            {
            //                inputFiles.Add(ResourceFile.FromAutoStorageContainer(userLayer.LayerId.ToString(), userLayer.LayerId.ToString(), resolution.GetBlobPrefixByResolution()));
            //            }
            //        }

            //        var outputBlobName = $"{tempBlobPrefix}/{clipFileName}";
            //        CloudTask clipTask = SetUpTask(outputBlobName, outputContainerSasUri, $"{tempBlobPrefix}/{clip.ClipId}", clipCommand, clipTaskId, inputFiles, clipFileName);
            //        clipTasks.Add(clipTask);

            //        allFrameVideoInputs.Add(ResourceFile.FromAutoStorageContainer(userContainerName, null, outputBlobName));
            //    }

            //    var allFrameVideoCommand = _ffmpegService.GetMergeCode(true, tempBlobPrefix, allFrameVideoFileName, null, AllFramesConcatFileName);
            //    var allFramesTaskId = $"a{video.VideoId}-{date}";
            //    var allFramesOutputBlobName = $"{tempBlobPrefix}/{allFrameVideoFileName}";
            //    var allFramesTask = SetUpTask(allFramesOutputBlobName, outputContainerSasUri, $"{tempBlobPrefix}/{AllFramesVideoName}", allFrameVideoCommand, allFramesTaskId, allFrameVideoInputs, allFramesOutputBlobName);
            //    allFramesTask.DependsOn = TaskDependencies.OnTasks(clipTasks);

            //    List<ResourceFile> splitFrameInputs = new List<ResourceFile> { ResourceFile.FromAutoStorageContainer(userContainerName, null, allFramesOutputBlobName) };
            //    List<ResourceFile> splitFrameMergeInputs = new List<ResourceFile> { ResourceFile.FromAutoStorageContainer(userContainerName, null, $"{tempBlobPrefix}/{SplitFramesConcatFileName}") };
            //    if (audioFileName != null)
            //    {
            //        splitFrameMergeInputs.Add(ResourceFile.FromAutoStorageContainer(userContainerName, null, $"{tempBlobPrefix}/{audioFileName}"));
            //    }

            //    var splitFramesTasks = new List<CloudTask>();
            //    for (int i = 0; i < splitFrameCommands.Count; i++)
            //    {
            //        var splitFrameCommand = splitFrameCommands[i];
            //        var splitTaskId = $"s{video.VideoId}-{i}-{date}";
            //        var splitFrameOutputBlobName = $"{tempBlobPrefix}/{splitFrameCommand.VideoName}";
            //        CloudTask splitFrameTask = SetUpTask(splitFrameOutputBlobName, outputContainerSasUri, $"{tempBlobPrefix}/split", splitFrameCommand.FfmpegCode, splitTaskId, splitFrameInputs, splitFrameOutputBlobName);
            //        splitFrameTask.DependsOn = TaskDependencies.OnTasks(allFramesTask);
            //        splitFramesTasks.Add(splitFrameTask);

            //        splitFrameMergeInputs.Add(ResourceFile.FromAutoStorageContainer(userContainerName, null, splitFrameOutputBlobName));
            //    }

            //    var splitMergeVideoCommand = _ffmpegService.GetMergeCode(true, tempBlobPrefix, blobPrefix, videoFileName, audioFileName, SplitFramesConcatFileName);
            //    var splitMergeTaskId = $"v{video.VideoId}-{date}";
            //    var splitMergeBlobName = $"{blobPrefix}/{videoFileName}";
            //    var splitMergeTask = SetUpTask(splitMergeBlobName, outputContainerSasUri, tempBlobPrefix, splitMergeVideoCommand, splitMergeTaskId, splitFrameMergeInputs, splitMergeBlobName);
            //    splitMergeTask.DependsOn = TaskDependencies.OnTasks(splitFramesTasks);

            //    var allTasks = clipTasks.Union(splitFramesTasks).Union(new List<CloudTask> { allFramesTask, splitMergeTask });
            //    using (var batchClient = BatchClient.Open(_batchCredentials))
            //    {
            //        var job = batchClient.JobOperations.CreateJob(splitMergeTaskId, new PoolInformation { PoolId = _poolId }); // get pool from config
            //        job.OnAllTasksComplete = OnAllTasksComplete.TerminateJob;
            //        job.UsesTaskDependencies = true;
            //        job.Constraints = new JobConstraints { MaxTaskRetryCount = 1, MaxWallClockTime = TimeSpan.FromDays(1) };

            //        await job.CommitAsync();
            //        await batchClient.JobOperations.AddTaskAsync(job.Id, allTasks);
            //    }
            //}
            //else
            //{
            BuilderMessage builderMessage = new BuilderMessage
            {
                OutputBlobPrefix = outputBlobPrefix,
                UserContainerName = userContainerName,
                TemporaryBlobPrefix = tempBlobPrefix,
                ClipCommands = uniqueClips.Select(uc => new FfmpegIOCommand
                {
                    FfmpegCode = _ffmpegService.GetClipCode(uc, Resolution.Free/*resolution*/, video.Format, video.BPM, false, tempBlobPrefix),
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

            //if (resolution == Resolution.Free)
            //{
            await _queueClientFree.Value.SendMessageAsync(JsonSerializer.Serialize(builderMessage));
            //}
            //else
            //{
            //    await _queueClientHd.Value.SendMessageAsync(JsonSerializer.Serialize(builderMessage));
            //}
            //}

            if (currentBuild != null)
            {
                currentBuild.BuildStatus = BuildStatus.BuildingPending;
                await _buildRepository.SaveAsync(currentBuild, userObjectId);
            }
            else
            {
                currentBuild = new Build { BuildId = buildId, BuildStatus = BuildStatus.BuildingPending, HasAudio = false, License = License.Personal, Resolution = Resolution.Free, VideoId = videoId };
                await _buildRepository.SaveAsync(currentBuild, userObjectId);
            }

            return new VideoAsset { BuildStatus = currentBuild.BuildStatus, DateCreated = DateTimeOffset.UtcNow, DownloadLink = null, VideoId = videoId };
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

        private string GetUserContainerName(Guid userObjectId)
        {
            return $"user-{userObjectId}";
        }
        private static string GetTempBlobPrefix(Guid buildId)
        {
            return $"{buildId}/{SharedConstants.TempBlobPrefix}";
        }
        private static string GetAudioBlobName(Guid buildId)
        {
            return $"{GetTempBlobPrefix(buildId)}/{SharedConstants.AudioFileName}";
        }

        private async Task<Video> GetAndValidateVideo(Guid userObjectId, int videoId)
        {
            var video = await _videoRepository.GetAsync(userObjectId, videoId);
            if (video == null)
            {
                throw new Exception($"User does not own video {videoId}");
            }

            return video;
        }

        private async Task<Build?> GetAndValidateBuild(Guid userObjectId, int videoId, Guid buildId)
        {
            var builds = await _buildRepository.GetAllAsync(userObjectId);
            if (builds != null && builds.Any(b => b.VideoId == videoId && b.BuildStatus != BuildStatus.Complete && b.BuildStatus != BuildStatus.Failed && b.BuildStatus != BuildStatus.PaymentAuthorisationPending))
            {
                throw new Exception($"User is already building video {videoId}");
            }

            return builds?.FirstOrDefault(b => b.BuildId == buildId);
        }

        public async Task<Uri> CreateUserAudioBlobUri(Guid userObjectId, int videoId, Guid buildId, Resolution resolution)
        {
            await GetAndValidateVideo(userObjectId, videoId);
            var currentBuild = await GetAndValidateBuild(userObjectId, videoId, buildId);
            if (resolution != Resolution.Free && currentBuild == null)
            {
                throw new Exception($"Could not find build for resolution {resolution} for video {videoId}");
            }

            var blobName = GetAudioBlobName(buildId);
            var returnUrl = await _storageService.CreateBlobAsync(GetUserContainerName(userObjectId), blobName, true, TimeSpan.FromMinutes(10));
            if (resolution == Resolution.Free)
            {
                await _buildRepository.SaveAsync(new Build { BuildId = buildId, BuildStatus = BuildStatus.PaymentAuthorisationPending, HasAudio = true, License = License.Personal, Resolution = resolution, PaymentIntentId = null, VideoId = videoId }, userObjectId);
            }
            else if (currentBuild != null)
            {
                currentBuild.HasAudio = true;
                await _buildRepository.SaveAsync(currentBuild, userObjectId);
            }

            return returnUrl;
        }

        public async Task<string> CreatePaymentIntent(Guid userObjectId, int videoId, PaymentIntentRequest paymentIntentRequest)
        {
            var video = await GetAndValidateVideo(userObjectId, videoId);
            var currentBuild = await GetAndValidateBuild(userObjectId, videoId, paymentIntentRequest.BuildId);
            if (currentBuild != null)
            {
                throw new Exception($"Build {paymentIntentRequest.BuildId} already exists");
            }

            return await _paymentService.CreatePaymentIntent(video, paymentIntentRequest, userObjectId);
        }
    }
}
