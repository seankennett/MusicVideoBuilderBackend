using LayerDataAccess.Repositories;
using VideoDataAccess;
using VideoDataAccess.Entities;
using VideoDataAccess.Repositories;

namespace SpaWebApi.Services
{
    public class ClipService : IClipService
    {
        private readonly IClipRepository _clipRepository;
        private readonly IVideoRepository _videoRepository;
        private readonly ICollectionRepository _collectionRepository;

        public ClipService(IClipRepository clipRepository, IVideoRepository videoRepository, ICollectionRepository collectionRepository)
        {
            _clipRepository = clipRepository;
            _videoRepository = videoRepository;
            _collectionRepository = collectionRepository;
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
            if (VideoDataAccessConstants.BeatsPerDisplayLayer - clip.BeatLength < clip.StartingBeat - 1)
            {
                throw new Exception("Invalid starting postion for beat length");
            }

            if (clip.BackgroundColour == null && (clip.ClipDisplayLayers == null || !clip.ClipDisplayLayers.Any()))
            {
                throw new Exception("Must have a background colour or background clip defined");
            }

            if (!await AreLayerIdsInDisplayLayers(clip.ClipDisplayLayers))
            {
                throw new Exception("Layer id is not related to display layer");
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
                    databaseClip.StartingBeat == clip.StartingBeat && (databaseClip.ClipDisplayLayers == null && clip.ClipDisplayLayers == null ||
                    databaseClip.ClipDisplayLayers != null && clip.ClipDisplayLayers != null &&
                    AreClipDiplayLayersTheSame(databaseClip.ClipDisplayLayers.ToList(), clip.ClipDisplayLayers.ToList())))
                {
                    return clip;
                }
            }

            return await _clipRepository.SaveAsync(userObjectId, clip);
        }

        private async Task<bool> AreLayerIdsInDisplayLayers(IEnumerable<ClipDisplayLayer>? clipDisplayLayers)
        {
            if (clipDisplayLayers != null)
            {
                var collections = await _collectionRepository.GetAllCollectionsAsync();
                foreach (var clipDisplayLayer in clipDisplayLayers)
                {
                    var displayLayer = collections.SelectMany(c => c.DisplayLayers).FirstOrDefault(d => d.DisplayLayerId == clipDisplayLayer.DisplayLayerId);
                    if (displayLayer == null)
                    {
                        return false;
                    }

                    if (clipDisplayLayer.LayerClipDisplayLayers != null)
                    {
                        foreach (var layerClipDisplayLayer in clipDisplayLayer.LayerClipDisplayLayers)
                        {
                            if (!displayLayer.Layers.Any(l => l.LayerId == layerClipDisplayLayer.LayerId))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private bool AreClipDiplayLayersTheSame(List<ClipDisplayLayer> databaseClipDisplayLayers, List<ClipDisplayLayer> clipDisplayLayers)
        {
            if (databaseClipDisplayLayers.Count != clipDisplayLayers.Count)
            {
                return false;
            }

            for (int i = 0; i < databaseClipDisplayLayers.Count(); i++)
            {
                var databaseClipDisplayLayer = databaseClipDisplayLayers[i];
                var clipDisplayLayer = clipDisplayLayers[i];
                if (databaseClipDisplayLayer.DisplayLayerId != clipDisplayLayer.DisplayLayerId)
                {
                    return false;
                }                

                if (databaseClipDisplayLayer.LayerClipDisplayLayers == null && clipDisplayLayer.LayerClipDisplayLayers == null)
                {
                    continue;
                }

                if (databaseClipDisplayLayer.LayerClipDisplayLayers != null && clipDisplayLayer.LayerClipDisplayLayers != null)
                {
                    var databaseLayerClipDisplayLayers = databaseClipDisplayLayer.LayerClipDisplayLayers.ToList();
                    var layerClipDisplayLayers = clipDisplayLayer.LayerClipDisplayLayers.ToList();
                    if (databaseLayerClipDisplayLayers.Count != layerClipDisplayLayers.Count)
                    {
                        return false;
                    }

                    for (int j = 0; j < databaseLayerClipDisplayLayers.Count; j++)
                    {
                        var databaseLayerClipDisplayLayer = databaseLayerClipDisplayLayers[j];
                        var layerClipDisplayLayer = layerClipDisplayLayers[j];
                        if (databaseLayerClipDisplayLayer.LayerId != layerClipDisplayLayer.LayerId || databaseLayerClipDisplayLayer.ColourOverride != layerClipDisplayLayer.ColourOverride)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
