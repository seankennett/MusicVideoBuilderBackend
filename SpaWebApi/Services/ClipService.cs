using CollectionDataAccess.Services;
using CollectionEntities.Entities;
using VideoDataAccess;
using VideoDataAccess.Entities;
using VideoDataAccess.Repositories;

namespace SpaWebApi.Services
{
    public class ClipService : IClipService
    {
        private readonly IClipRepository _clipRepository;
        private readonly IVideoRepository _videoRepository;
        private readonly ICollectionService _collectionService;

        public ClipService(IClipRepository clipRepository, IVideoRepository videoRepository, ICollectionService collectionService)
        {
            _clipRepository = clipRepository;
            _videoRepository = videoRepository;
            _collectionService = collectionService;
        }
        public async Task DeleteAsync(Guid userObjectId, int clipId)
        {
            var videos = await _videoRepository.GetAllAsync(userObjectId);
            if (videos.Any(v => v.VideoClips.Any(c => c.ClipId == clipId)))
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

            await ValidateClipDisplayLayersAgainstCollections(clip.ClipDisplayLayers);   

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
                    databaseClip.EndBackgroundColour == clip.EndBackgroundColour &&
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

        private async Task ValidateClipDisplayLayersAgainstCollections(IEnumerable<ClipDisplayLayer>? clipDisplayLayers)
        {
            if (clipDisplayLayers != null)
            {
                foreach (var clipDisplayLayer in clipDisplayLayers)
                {
                    if (clipDisplayLayer.FadeType == null && clipDisplayLayer.Colour != null)
                    {
                        throw new Exception("Clip display layer colour must be null if fade type is null");
                    }

                    var collections = await _collectionService.GetAllCollectionsAsync();
                    var collection = collections.FirstOrDefault(c => c.DisplayLayers.Any(d => d.DisplayLayerId == clipDisplayLayer.DisplayLayerId));
                    if (collection == null)
                    {
                        throw new Exception("Unknown display Layer in clip display layer");
                    }

                    if (collection.CollectionType == CollectionType.Background && clipDisplayLayer.FadeType != null && clipDisplayLayer.Colour == null)
                    {
                        throw new Exception("Background fades must have colour defined");
                    }

                    if (collection.CollectionType == CollectionType.Foreground && clipDisplayLayer.FadeType != null && clipDisplayLayer.Colour != null)
                    {
                        throw new Exception("Foreground fades must not have colour defined");
                    }

                    var displayLayer = collection.DisplayLayers.First(d => d.DisplayLayerId == clipDisplayLayer.DisplayLayerId);
                    if (clipDisplayLayer.LayerClipDisplayLayers != null)
                    {
                        foreach (var layerClipDisplayLayer in clipDisplayLayer.LayerClipDisplayLayers)
                        {
                            if (!displayLayer.Layers.Any(l => l.LayerId == layerClipDisplayLayer.LayerId))
                            {
                                throw new Exception("Layer clip display layer not belonging inside display layer");
                            }

                            if (clipDisplayLayer.FadeType != null && layerClipDisplayLayer.EndColour != null)
                            {
                                throw new Exception("Can't have fade and layer end colours defined");
                            }
                        }
                    }
                }
            }
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
                if (databaseClipDisplayLayer.DisplayLayerId != clipDisplayLayer.DisplayLayerId || 
                    databaseClipDisplayLayer.Reverse != clipDisplayLayer.Reverse ||
                    databaseClipDisplayLayer.FlipHorizontal != clipDisplayLayer.FlipHorizontal ||
                    databaseClipDisplayLayer.FlipVertical != clipDisplayLayer.FlipVertical ||
                    databaseClipDisplayLayer.FadeType != clipDisplayLayer.FadeType ||
                    databaseClipDisplayLayer.Colour != clipDisplayLayer.Colour)
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
                        if (databaseLayerClipDisplayLayer.LayerId != layerClipDisplayLayer.LayerId || 
                            databaseLayerClipDisplayLayer.Colour != layerClipDisplayLayer.Colour ||
                            databaseLayerClipDisplayLayer.EndColour != layerClipDisplayLayer.EndColour)
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
