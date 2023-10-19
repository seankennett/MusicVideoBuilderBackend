using BuildDataAccess;
using BuildDataAccess.Extensions;
using BuildDataAccess.Repositories;
using BuildEntities;
using BuilderEntities.Entities;
using CollectionDataAccess.Services;
using SharedEntities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VideoDataAccess.Repositories;

namespace BuildInstructorFunction.Services
{
    public class BuildService : IBuildService
    {
        private readonly IBuildRepository _buildRepository;
        private readonly IVideoRepository _videoRepository;
        private readonly IClipRepository _clipRepository;
        private readonly ICollectionService _collectionService;
        private readonly IFfmpegService _ffmpegService;
        private readonly IStorageService _storageService;
        private readonly IBuilderFunctionSender _builderFunctionSender;
        private readonly IAzureBatchService _azureBatchService;
        private readonly IUserCollectionRepository _userCollectionRepository;

        public BuildService(IBuildRepository buildRepository, IVideoRepository videoRepository, IFfmpegService ffmpegService, IStorageService storageService, IBuilderFunctionSender builderFunctionSender, IAzureBatchService azureBatchService, IUserCollectionRepository userCollectionRepository, ICollectionService collectionService, IClipRepository clipRepository)
        {
            _buildRepository = buildRepository;
            _videoRepository = videoRepository;
            _clipRepository = clipRepository;
            _collectionService = collectionService;
            _ffmpegService = ffmpegService;
            _storageService = storageService;
            _builderFunctionSender = builderFunctionSender;
            _azureBatchService = azureBatchService;
            _userCollectionRepository = userCollectionRepository;
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
            var video = await _videoRepository.GetAsync(build.UserObjectId, build.VideoId.Value);
            var clips = await _clipRepository.GetAllByVideoIdAsync(build.UserObjectId, build.VideoId.Value);
            var videoClipsFull = video.VideoClips.Select(vc => clips.First(c => c.ClipId == vc.ClipId));
            var collections = await _collectionService.GetAllCollectionsAsync();

            var buildId = build.BuildId;
            var resolution = build.Resolution;
            var tempBlobPrefix = GuidHelper.GetTempBlobPrefix(buildId);

            var isForBatchService = build.Resolution == Resolution.FourK || build.Resolution == Resolution.Hd;
            var watermarkFilePath = build.Resolution == Resolution.Free ? $"{tempBlobPrefix}/watermark.png" : null;
            var uniqueVideoClips = video.VideoClips.DistinctBy(x => x.ClipId).ToList();
            var layerIdsPerClip = new Dictionary<int, IEnumerable<string>>();
            var uniqueLayers = new List<string>();
            var uniqueCollectionIds = new List<Guid>();
            var clipCommands = new List<FfmpegIOCommand>();
            for (var i = 0; i < uniqueVideoClips.Count(); i++)
            {
                var videoClip = uniqueVideoClips[i];
                var clip = clips.First(x => x.ClipId == videoClip.ClipId);
                // looks complicated but should preserve order in clip
                var orderedClipDisplayLayers = clip.ClipDisplayLayers?.Where(cdl => cdl != null).SelectMany(cdl => {
                    return collections.SelectMany(c => c.DisplayLayers).Where(d => cdl.DisplayLayerId == d.DisplayLayerId);
                }).ToList();

                clipCommands.Add(new FfmpegIOCommand
                {
                    FfmpegCode = _ffmpegService.GetClipCode(clip, resolution, video.Format, video.BPM, isForBatchService,
                isForBatchService ? "." : tempBlobPrefix,
                watermarkFilePath,
                orderedClipDisplayLayers
                ),
                    VideoName = $"{clip.ClipId}.{video.Format}"
                });

                if (clip.ClipDisplayLayers != null)
                {
                    collections.Where(c => c.DisplayLayers.Any(d => clip.ClipDisplayLayers.Any(c => c.DisplayLayerId == d.DisplayLayerId)));
                    uniqueCollectionIds = uniqueCollectionIds.Union(collections.Select(x => x.CollectionId)).ToList();
                    var layerIds = orderedClipDisplayLayers.SelectMany(d => d.Layers).Select(x => x.LayerId.ToString());                    
                    layerIdsPerClip[i] = layerIds;
                    uniqueLayers = uniqueLayers.Union(layerIds).ToList();
                }
            }

            if (resolution != Resolution.Free)
            {
                await _userCollectionRepository.SavePendingUserCollectionAsync(uniqueCollectionIds, build.UserObjectId, build.BuildId);
            }

            var userContainerName = GuidHelper.GetUserContainerName(build.UserObjectId);
            var hasAudio = build.HasAudio;
            var outputBlobPrefix = buildId.ToString();

            var videoFileName = $"{video.VideoName}.{video.Format}";
            var allFrameVideoFileName = $"{InstructorConstants.AllFramesVideoName}.{video.Format}";

            var clipMergeCommand = new FfmpegIOCommand
            {
                FfmpegCode = _ffmpegService.GetMergeCode(isForBatchService, tempBlobPrefix, allFrameVideoFileName, null, InstructorConstants.AllFramesConcatFileName),
                VideoName = allFrameVideoFileName
            };

            var splitFrameCommands = new List<FfmpegIOCommand>();
            var videoLengthSeconds = videoClipsFull.Sum(x => x.BeatLength) * TimeSpan.FromMinutes(1).TotalSeconds / video.BPM;
            var splitVideoTotal = (int)Math.Ceiling(videoLengthSeconds / InstructorConstants.VideoSplitLengthSeconds);
            for (var i = 0; i < splitVideoTotal; i++)
            {
                var splitFrameVideoName = $"s{i}.{video.Format}";
                splitFrameCommands.Add(new FfmpegIOCommand
                {
                    FfmpegCode = _ffmpegService.GetSplitFrameCommand(isForBatchService, tempBlobPrefix, TimeSpan.FromSeconds(InstructorConstants.VideoSplitLengthSeconds * i), TimeSpan.FromSeconds(InstructorConstants.VideoSplitLengthSeconds), allFrameVideoFileName, splitFrameVideoName, i == 0 ? video.VideoDelayMilliseconds : null),
                    VideoName = splitFrameVideoName
                });
            }

            var splitFrameMergeCommand = new FfmpegIOCommand
            {
                FfmpegCode = _ffmpegService.GetMergeCode(isForBatchService, tempBlobPrefix, outputBlobPrefix, videoFileName, hasAudio ? BuildDataAccessConstants.AudioFileName : null, InstructorConstants.SplitFramesConcatFileName),
                VideoName = videoFileName
            };

            var concatClipFileContents = _ffmpegService.GetConcatCode(video.VideoClips.Select(x => $"{x.ClipId}.{video.Format}"));
            await _storageService.UploadTextFile(userContainerName, tempBlobPrefix, InstructorConstants.AllFramesConcatFileName, concatClipFileContents, !hasAudio);

            var concatSplitFrameFileContents = _ffmpegService.GetConcatCode(splitFrameCommands.Select(x => x.VideoName));
            await _storageService.UploadTextFile(userContainerName, tempBlobPrefix, InstructorConstants.SplitFramesConcatFileName, concatSplitFrameFileContents, false);

            if (isForBatchService)
            {
                await _azureBatchService.SendBatchRequest(userContainerName, hasAudio, buildId, resolution, outputBlobPrefix, tempBlobPrefix, layerIdsPerClip, clipCommands, clipMergeCommand, splitFrameCommands, splitFrameMergeCommand);
            }
            else
            {
                await _builderFunctionSender.SendFreeBuilderFunctionMessage(userContainerName, hasAudio, resolution, outputBlobPrefix, tempBlobPrefix, uniqueLayers, clipCommands, clipMergeCommand, splitFrameCommands, splitFrameMergeCommand);
            }
        }
    }
}
