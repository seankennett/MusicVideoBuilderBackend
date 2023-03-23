using Azure.Storage.Queues;
using DataAccessLayer.Repositories;
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
        private readonly IStorageService _storageService;
        private readonly IBuildRepository _buildRepository;
        private readonly IPaymentService _paymentService;

        public VideoAssetService(IVideoRepository videoRepository, IStorageService storageService, IOptions<Connections> connections, IBuildRepository buildRepository, ILogger<VideoAssetService> logger, IPaymentService paymentService, IUserLayerRepository userLayerRepository)
        {
            _videoRepository = videoRepository;
            _storageService = storageService;
            _buildRepository = buildRepository;
            _paymentService = paymentService;
        }

        public async Task<IEnumerable<VideoAsset>> GetAllAsync(Guid userObjectId)
        {
            var builds = await _buildRepository.GetAllAsync(userObjectId);

            var userContainerName = GuidHelper.GetUserContainerName(userObjectId);

            var result = new List<VideoAsset>();
            foreach (var build in builds.Where(x => x.BuildStatus != BuildStatus.Failed && x.BuildStatus != BuildStatus.PaymentAuthorisationPending))
            {
                Uri? downloadLink = null;
                if (build.BuildStatus == BuildStatus.Complete)
                {
                    downloadLink = await _storageService.GetSASLink(userContainerName, build.BuildId.ToString(), $"/{SharedConstants.TempBlobPrefix}/", build.DateUpdated.AddDays(28));
                }

                result.Add(new VideoAsset
                {
                    BuildStatus = build.BuildStatus,
                    DateCreated = build.DateUpdated,
                    DownloadLink = downloadLink,
                    VideoId = build.VideoId
                });
            }
            return result;
        }

        public async Task<VideoAsset> BuildFreeVideoAsync(Guid userObjectId, int videoId, Guid buildId)
        {            
            var currentBuild = await GetAndValidateBuild(userObjectId, videoId, buildId);

            if (currentBuild != null)
            {
                currentBuild.BuildStatus = BuildStatus.BuildingPending;
                await _buildRepository.SaveAsync(currentBuild, userObjectId);
            }
            else
            {
                await GetAndValidateVideo(userObjectId, videoId);

                currentBuild = new Build { BuildId = buildId, BuildStatus = BuildStatus.BuildingPending, HasAudio = false, License = License.Personal, Resolution = Resolution.Free, VideoId = videoId };
                await _buildRepository.SaveAsync(currentBuild, userObjectId);
            }

            var userBuild = new UserBuild(currentBuild);
            userBuild.UserObjectId = userObjectId;

            await _storageService.SendToBuildInstructorQueueAsync(userBuild);

            return new VideoAsset { BuildStatus = currentBuild.BuildStatus, DateCreated = DateTimeOffset.UtcNow, DownloadLink = null, VideoId = videoId };
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
            var currentBuild = await GetAndValidateBuild(userObjectId, videoId, buildId);
            if (resolution != Resolution.Free && currentBuild == null)
            {
                throw new Exception($"Could not find build {buildId} for resolution {resolution} for video {videoId}");
            }

            if (currentBuild == null)
            {
                await GetAndValidateVideo(userObjectId, videoId);
            }

            var blobName = GuidHelper.GetAudioBlobName(buildId);
            return await _storageService.CreateBlobAsync(GuidHelper.GetUserContainerName(userObjectId), blobName, true, DateTimeOffset.UtcNow.AddMinutes(10));
        }

        public async Task ValidateAudioBlob(Guid userObjectId, int videoId, Guid buildId, Resolution resolution)
        {
            var currentBuild = await GetAndValidateBuild(userObjectId, videoId, buildId);
            if (resolution != Resolution.Free && currentBuild == null)
            {
                throw new Exception($"Could not find build {buildId} for resolution {resolution} for video {videoId}");
            }

            var userContainerName = GuidHelper.GetUserContainerName(userObjectId);

            var isValidAudio = await _storageService.ValidateAudioBlob(userContainerName, GuidHelper.GetAudioBlobName(buildId));
            if (!isValidAudio)
            {
                throw new Exception($"Problem finding audio blob or blob too big for user {userObjectId}");
            }

            if (resolution == Resolution.Free)
            {
                await GetAndValidateVideo(userObjectId, videoId);
                await _buildRepository.SaveAsync(new Build { BuildId = buildId, BuildStatus = BuildStatus.PaymentAuthorisationPending, HasAudio = true, License = License.Personal, Resolution = resolution, PaymentIntentId = null, VideoId = videoId }, userObjectId);
            }
            else if (currentBuild != null)
            {
                currentBuild.HasAudio = true;
                await _buildRepository.SaveAsync(currentBuild, userObjectId);
            }
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
