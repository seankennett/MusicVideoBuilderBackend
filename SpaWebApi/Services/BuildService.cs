using BuildDataAccess;
using BuildDataAccess.Entities;
using BuildDataAccess.Extensions;
using BuildDataAccess.Repositories;
using BuildEntities;
using SharedEntities.Models;
using SpaWebApi.Models;
using VideoDataAccess.Entities;
using VideoDataAccess.Repositories;
using VideoEntities.Entities;

namespace SpaWebApi.Services
{
    public class BuildService : IBuildService
    {
        private readonly IVideoRepository _videoRepository;
        private readonly IStorageService _storageService;
        private readonly IBuildRepository _buildRepository;
        private readonly IPaymentService _paymentService;

        public BuildService(IVideoRepository videoRepository, IStorageService storageService, IBuildRepository buildRepository, IPaymentService paymentService)
        {
            _videoRepository = videoRepository;
            _storageService = storageService;
            _buildRepository = buildRepository;
            _paymentService = paymentService;
        }

        public async Task<IEnumerable<BuildAsset>> GetAllAsync(Guid userObjectId)
        {
            IEnumerable<Build> builds = await GetValidBuilds(userObjectId);

            var userContainerName = GuidHelper.GetUserContainerName(userObjectId);

            var result = new List<BuildAsset>();
            foreach (var build in builds.Where(x => x.BuildStatus != BuildStatus.Failed && x.BuildStatus != BuildStatus.PaymentAuthorisationPending).OrderByDescending(x => x.DateUpdated))
            {
                Uri? downloadLink = null;
                if (build.BuildStatus == BuildStatus.Complete)
                {
                    downloadLink = await _storageService.GetSASLink(userContainerName, build.BuildId.ToString(), $"/{BuildDataAccessConstants.TempBlobPrefix}/", DateTimeOffset.UtcNow.AddDays(0.5));
                }

                result.Add(new BuildAsset
                {
                    BuildStatus = build.BuildStatus,
                    DateCreated = build.DateUpdated,
                    DownloadLink = downloadLink,
                    VideoId = build.VideoId,
                    Format = build.Format,
                    VideoName = build.VideoName,
                    License = build.License,
                    Resolution = build.Resolution
                });
            }
            return result;
        }        

        public async Task BuildFreeVideoAsync(Guid userObjectId, int videoId, VideoBuildRequest videoBuildRequest)
        {            
            var currentBuild = await GetAndValidateBuild(userObjectId, videoId, videoBuildRequest);
            Video? video = null;

            // make certain that if request is not free on license and resolution that amount is 0
            if (videoBuildRequest.Resolution != Resolution.Free && videoBuildRequest.License != License.Personal)
            {
                video = await GetAndValidateFreeVideo(userObjectId, videoId, videoBuildRequest);
            }

            if (currentBuild != null)
            {
                currentBuild.BuildStatus = BuildStatus.BuildingPending;
                await _buildRepository.SaveAsync(currentBuild, userObjectId);
            }
            else
            {
                // free license and resolution
                if (video == null)
                {
                    video = await GetAndValidateVideo(userObjectId, videoId);
                }

                currentBuild = new Build { BuildId = videoBuildRequest.BuildId, BuildStatus = BuildStatus.BuildingPending, HasAudio = false, License = videoBuildRequest.License, Resolution = videoBuildRequest.Resolution, VideoId = videoId, VideoName = video.VideoName, Format = video.Format };
                await _buildRepository.SaveAsync(currentBuild, userObjectId);
            }

            var userBuild = new UserBuild(currentBuild);
            userBuild.UserObjectId = userObjectId;

            await _storageService.SendToBuildInstructorQueueAsync(userBuild);
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

        private async Task<Video> GetAndValidateFreeVideo(Guid userObjectId, int videoId, VideoBuildRequest videoBuildRequest)
        {
            var video = await GetAndValidateVideo(userObjectId, videoId);
            var cost = _paymentService.GetVideoCost(video, videoBuildRequest.Resolution, videoBuildRequest.License, userObjectId);
            return video;
        }

        private async Task<Build?> GetAndValidateBuild(Guid userObjectId, int videoId, Guid buildId)
        {
            var builds = await GetValidBuilds(userObjectId);
            if (builds != null && builds.Any(b => b.VideoId == videoId && b.BuildStatus != BuildStatus.Complete && b.BuildStatus != BuildStatus.Failed && b.BuildStatus != BuildStatus.PaymentAuthorisationPending))
            {
                throw new Exception($"User is already building video {videoId}");
            }

            return builds?.FirstOrDefault(x => x.BuildId == buildId);
        }

        private async Task<Build?> GetAndValidateBuild(Guid userObjectId, int videoId, VideoBuildRequest videoBuildRequest)
        {
            var build = await GetAndValidateBuild(userObjectId, videoId, videoBuildRequest.BuildId);
            if (build != null && (build.License != videoBuildRequest.License || build.Resolution != videoBuildRequest.Resolution))
            {
                throw new Exception("Previous build resolution and license not same as request");
            }

            return build;
        }

        public async Task<Uri> CreateUserAudioBlobUri(Guid userObjectId, int videoId, VideoBuildRequest videoBuildRequest)
        {            
            var currentBuild = await GetAndValidateBuild(userObjectId, videoId, videoBuildRequest);
            // only on freebie purchase as no payment intent step
            if (currentBuild == null)
            {
                await GetAndValidateFreeVideo(userObjectId, videoId, videoBuildRequest);
            }

            var blobName = GuidHelper.GetAudioBlobName(videoBuildRequest.BuildId);
            return await _storageService.CreateBlobAsync(GuidHelper.GetUserContainerName(userObjectId), blobName, true, DateTimeOffset.UtcNow.AddMinutes(10));
        }

        public async Task ValidateAudioBlob(Guid userObjectId, int videoId, VideoBuildRequest videoBuildRequest)
        {
            var currentBuild = await GetAndValidateBuild(userObjectId, videoId, videoBuildRequest);
            // only on freebie purchase as no payment intent step
            if (currentBuild == null)
            {
                await GetAndValidateFreeVideo(userObjectId, videoId, videoBuildRequest);
            }

            var userContainerName = GuidHelper.GetUserContainerName(userObjectId);

            var isValidAudio = await _storageService.ValidateAudioBlob(userContainerName, GuidHelper.GetAudioBlobName(videoBuildRequest.BuildId));
            if (!isValidAudio)
            {
                throw new Exception($"Problem finding audio blob or blob too big for user {userObjectId}");
            }

            if (currentBuild == null)
            {
                var video = await GetAndValidateFreeVideo(userObjectId, videoId, videoBuildRequest);
                await _buildRepository.SaveAsync(new Build { BuildId = videoBuildRequest.BuildId, BuildStatus = BuildStatus.PaymentAuthorisationPending, HasAudio = true, License = videoBuildRequest.License, Resolution = videoBuildRequest.Resolution, PaymentIntentId = null, VideoId = videoId, VideoName = video.VideoName, Format = video.Format }, userObjectId);
            }
            else
            {
                currentBuild.HasAudio = true;
                await _buildRepository.SaveAsync(currentBuild, userObjectId);
            }
        }

        public async Task<string> CreatePaymentIntent(Guid userObjectId, int videoId, PaymentIntentRequest paymentIntentRequest, string email)
        {
            var video = await GetAndValidateVideo(userObjectId, videoId);
            var currentBuild = await GetAndValidateBuild(userObjectId, videoId, paymentIntentRequest.BuildId);
            if (currentBuild != null)
            {
                throw new Exception($"Build {paymentIntentRequest.BuildId} already exists");
            }

            return await _paymentService.CreatePaymentIntent(video, paymentIntentRequest, userObjectId, email);
        }

        private async Task<IEnumerable<Build>> GetValidBuilds(Guid userObjectId)
        {
            return (await _buildRepository.GetAllAsync(userObjectId)).Where(b => b.DateUpdated.ToUniversalTime().AddDays(7) > DateTimeOffset.UtcNow);
        }
    }
}
