using DataAccessLayer.Repositories;
using SharedEntities;
using SharedEntities.Extensions;
using SharedEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuildInstructorFunction.Services
{
    public class BuildService : IBuildService
    {
        private readonly IBuildRepository _buildRepository;
        private readonly IVideoRepository _videoRepository;
        private readonly IFfmpegService _ffmpegService;
        private readonly IStorageService _storageService;
        private readonly IBuilderFunctionSender _builderFunctionSender;
        private readonly IAzureBatchService _azureBatchService;
        private readonly IUserLayerRepository _userLayerRepository;

        public BuildService(IBuildRepository buildRepository, IVideoRepository videoRepository, IFfmpegService ffmpegService, IStorageService storageService, IBuilderFunctionSender builderFunctionSender, IAzureBatchService azureBatchService, IUserLayerRepository userLayerRepository)
        {
            _buildRepository = buildRepository;
            _videoRepository = videoRepository;
            _ffmpegService = ffmpegService;
            _storageService = storageService;
            _builderFunctionSender = builderFunctionSender;
            _azureBatchService = azureBatchService;
            _userLayerRepository = userLayerRepository;
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
            var video = await _videoRepository.GetAsync(build.UserObjectId, build.VideoId);

            var buildId = build.BuildId;
            var resolution = build.Resolution;
            var tempBlobPrefix = GuidHelper.GetTempBlobPrefix(buildId);

            var is4KFormat = build.Resolution == Resolution.FourK;
            var watermarkFilePath = build.Resolution == Resolution.Free ? $"{tempBlobPrefix}/watermark.png" : null;
            var uniqueClips = video.Clips.DistinctBy(x => x.ClipId).ToList();
            var layerIdsPerClip = new Dictionary<int, IEnumerable<string>>();
            var uniqueLayers = new List<string>();
            var clipCommands = new List<FfmpegIOCommand>();
            for (var i = 0; i < uniqueClips.Count(); i++)
            {
                var clip = uniqueClips[i];
                clipCommands.Add(new FfmpegIOCommand
                {
                    //_ffmpegService.GetClipCode(clip, resolution, video.Format, video.BPM, true, ".")
                    FfmpegCode = _ffmpegService.GetClipCode(clip, resolution, video.Format, video.BPM, is4KFormat,
                // directory will not be created in clipcode's batch case so putting file flat on file system
                is4KFormat ? "." : tempBlobPrefix,
                watermarkFilePath
                ),
                    VideoName = $"{clip.ClipId}.{video.Format}"
                });

                if (clip.Layers != null)
                {
                    var layerIds = clip.Layers.Select(l => l.LayerId.ToString());
                    layerIdsPerClip[i] = layerIds;
                    uniqueLayers = uniqueLayers.Union(layerIds).ToList();
                }
            }

            if (resolution != Resolution.Free)
            {
                await _userLayerRepository.SaveUserLayersAsync(uniqueLayers.Select(u => Guid.Parse(u)), build.UserObjectId, build.BuildId);
            }

            var userContainerName = GuidHelper.GetUserContainerName(build.UserObjectId);
            var hasAudio = build.HasAudio;
            var outputBlobPrefix = buildId.ToString();

            var videoFileName = $"{video.VideoName}.{video.Format}";
            var allFrameVideoFileName = $"{InstructorConstants.AllFramesVideoName}.{video.Format}";


            //_ffmpegService.GetMergeCode(true, tempBlobPrefix, allFrameVideoFileName, null, AllFramesConcatFileName)
            var clipMergeCommand = new FfmpegIOCommand
            {
                FfmpegCode = _ffmpegService.GetMergeCode(is4KFormat, tempBlobPrefix, allFrameVideoFileName, null, InstructorConstants.AllFramesConcatFileName),
                VideoName = allFrameVideoFileName
            };

            var splitFrameCommands = new List<FfmpegIOCommand>();
            var videoLengthSeconds = video.Clips.Sum(x => x.BeatLength) * TimeSpan.FromMinutes(1).TotalSeconds / video.BPM;
            var splitVideoTotal = (int)Math.Ceiling(videoLengthSeconds / SharedConstants.VideoSplitLengthSeconds);
            for (var i = 0; i < splitVideoTotal; i++)
            {
                var splitFrameVideoName = $"s{i}.{video.Format}";
                splitFrameCommands.Add(new FfmpegIOCommand
                {
                    FfmpegCode = _ffmpegService.GetSplitFrameCommand(is4KFormat, tempBlobPrefix, TimeSpan.FromSeconds(SharedConstants.VideoSplitLengthSeconds * i), TimeSpan.FromSeconds(SharedConstants.VideoSplitLengthSeconds), allFrameVideoFileName, splitFrameVideoName, i == 0 ? video.VideoDelayMilliseconds : null),
                    VideoName = splitFrameVideoName
                });
            }

            var splitFrameMergeCommand = new FfmpegIOCommand
            {
                FfmpegCode = _ffmpegService.GetMergeCode(is4KFormat, tempBlobPrefix, outputBlobPrefix, videoFileName, hasAudio ? SharedConstants.AudioFileName : null, InstructorConstants.SplitFramesConcatFileName),
                VideoName = videoFileName
            };

            var concatClipFileContents = _ffmpegService.GetConcatCode(video.Clips.Select(x => $"{x.ClipId}.{video.Format}"));
            await _storageService.UploadTextFile(userContainerName, tempBlobPrefix, InstructorConstants.AllFramesConcatFileName, concatClipFileContents, !hasAudio);

            var concatSplitFrameFileContents = _ffmpegService.GetConcatCode(splitFrameCommands.Select(x => x.VideoName));
            await _storageService.UploadTextFile(userContainerName, tempBlobPrefix, InstructorConstants.SplitFramesConcatFileName, concatSplitFrameFileContents, false);

            if (is4KFormat)
            {
                await _azureBatchService.SendBatchRequest(userContainerName, hasAudio, buildId, resolution, outputBlobPrefix, tempBlobPrefix, layerIdsPerClip, clipCommands, clipMergeCommand, splitFrameCommands, splitFrameMergeCommand);
            }
            else
            {
                await _builderFunctionSender.SendBuilderFunctionMessage(userContainerName, hasAudio, resolution, outputBlobPrefix, tempBlobPrefix, uniqueLayers, clipCommands, clipMergeCommand, splitFrameCommands, splitFrameMergeCommand, watermarkFilePath != null);
            }
        }
    }
}
