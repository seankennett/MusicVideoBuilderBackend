using BuildDataAccess.Repositories;
using VideoDataAccess.Entities;
using VideoDataAccess.Repositories;

namespace SpaWebApi.Services
{
    public class VideoService : IVideoService
    {
        private readonly IVideoRepository _videoRepository;
        private readonly IClipRepository _clipRepository;
        private readonly IBuildRepository _buildRepository;

        public VideoService(IVideoRepository videoRepository, IClipRepository clipRepository, IBuildRepository buildRepository)
        {
            _videoRepository = videoRepository;
            _clipRepository = clipRepository;
            _buildRepository = buildRepository;
        }
        public async Task DeleteAsync(Guid userObjectId, int videoId)
        {
            var video = await _videoRepository.GetAsync(userObjectId, videoId);
            if (video == null)
            {
                throw new Exception($"User doesn't own video id {videoId}");
            }

            var builds = await _buildRepository.GetAllAsync(userObjectId);
            if (builds.Any(b => b.VideoId.HasValue && b.VideoId.Value == videoId && (b.BuildStatus == SharedEntities.Models.BuildStatus.BuildingPending || b.BuildStatus == SharedEntities.Models.BuildStatus.PaymentChargePending)))
            {
                throw new Exception($"Video {videoId} is building so can't delete yet");
            }

            await _videoRepository.DeleteAsync(videoId);
        }

        public Task<IEnumerable<Video>> GetAllAsync(Guid userObjectId)
        {
            return _videoRepository.GetAllAsync(userObjectId);
        }

        public async Task<Video> SaveAsync(Guid userObjectId, Video video)
        {
            var clips = await _clipRepository.GetAllAsync(userObjectId);
            if (video.Clips.Any(v => !clips.Any(ul => ul.ClipId == v.ClipId)))
            {
                throw new Exception("Video contains clips user does not have");
            }

            IEnumerable<Video>? videos = null;
            if (video.VideoId == 0)
            {
                videos = await _videoRepository.GetAllAsync(userObjectId);
                if (videos.Count() >= 2)
                {
                    throw new Exception("Maximum of 2 videos");
                }
            }

            if (video.VideoId != 0)
            {
                var databaseVideo = videos?.FirstOrDefault(v => v.VideoId == video.VideoId);
                if (databaseVideo == null)
                {
                    databaseVideo = await _videoRepository.GetAsync(userObjectId, video.VideoId);
                }

                if (databaseVideo == null)
                {
                    throw new Exception($"User doesn't own video id {video.VideoId}");
                }

                // no change so return original
                if (databaseVideo.VideoName == video.VideoName &&
                    databaseVideo.BPM == video.BPM &&
                    databaseVideo.Format == video.Format &&
                    databaseVideo.VideoDelayMilliseconds == video.VideoDelayMilliseconds &&
                    databaseVideo.Clips.Select(x => x.ClipId).SequenceEqual(video.Clips.Select(x => x.ClipId)))
                {
                    return video;
                }
            }

            return await _videoRepository.SaveAsync(userObjectId, video);
        }
    }
}
