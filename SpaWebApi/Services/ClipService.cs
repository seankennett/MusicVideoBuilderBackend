using SpaWebApi.Repositories;
using VideoDataAccess;
using VideoDataAccess.Entities;
using VideoDataAccess.Repositories;

namespace SpaWebApi.Services
{
    public class ClipService : IClipService
    {
        private readonly IClipRepository _clipRepository;
        private readonly IVideoRepository _videoRepository;

        public ClipService(IClipRepository clipRepository, IVideoRepository videoRepository)
        {
            _clipRepository = clipRepository;
            _videoRepository = videoRepository;
        }
        public async Task DeleteAsync(Guid userObjectId, int clipId)
        {
            var videos = await _videoRepository.GetAllAsync(userObjectId);
            if (videos.Any(v => v.Clips.Any(c => c.ClipId == clipId)))
            {
                throw new Exception($"Clip Id {clipId} has videos dependent on it");
            }

            var clip = await _clipRepository.GetAsync(userObjectId, clipId);
            if (clip == null)
            {
                throw new Exception($"User doesn't own clip id {clipId}");
            }

            await _clipRepository.DeleteAsync(clipId);
        }

        public Task<IEnumerable<Clip>> GetAllAsync(Guid userObjectId)
        {
            return _clipRepository.GetAllAsync(userObjectId);
        }

        public async Task<Clip> SaveAsync(Guid userObjectId, Clip clip)
        {
            if (VideoDataAccessConstants.BeatsPerLayer - clip.BeatLength < clip.StartingBeat - 1)
            {
                throw new Exception("Invalid starting postion for beat length");
            }

            if (clip.BackgroundColour == null && (clip.Layers == null || !clip.Layers.Any()))
            {
                throw new Exception("Must have a background colour or background clip defined");
            }

            if (clip.ClipId != 0)
            {
                var databaseClip = await _clipRepository.GetAsync(userObjectId, clip.ClipId);
                if (databaseClip == null)
                {
                    throw new Exception($"User doesn't own clip clip id {clip.ClipId}");
                }

                // no change so return original
                if (databaseClip.ClipName == clip.ClipName &&
                    databaseClip.BackgroundColour == clip.BackgroundColour &&
                    databaseClip.BeatLength == clip.BeatLength &&
                    databaseClip.StartingBeat == clip.StartingBeat && (databaseClip.Layers == null && clip.Layers == null ||
                    databaseClip.Layers != null && clip.Layers != null && databaseClip.Layers.Select(x => x.LayerId).SequenceEqual(clip.Layers.Select(x => x.LayerId))))
                {
                    return clip;
                }
            }

            return await _clipRepository.SaveAsync(userObjectId, clip);
        }
    }
}
