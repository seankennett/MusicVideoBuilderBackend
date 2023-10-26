using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoDataAccess.DTOEntities;
using VideoDataAccess.Entities;

namespace VideoDataAccess.Helpers
{
    internal static class ClipHelper
    {
        internal static Clip HydrateClip(IEnumerable<IGrouping<int, ClipDisplayLayerDTO>> groupedClipDisplayLayers, IEnumerable<IGrouping<int, LayerClipDisplayLayerDTO>> groupedLayerClipDisplayLayers, Clip clip)
        {
            var groupedClipDisplayLayer = groupedClipDisplayLayers.FirstOrDefault(gu => gu.Key == clip.ClipId);
            if (groupedClipDisplayLayer != null)
            {
                clip.ClipDisplayLayers = groupedClipDisplayLayer.OrderBy(x => x.Order).Select(x =>
                {
                    var clipDisplayLayer = new ClipDisplayLayer
                    {
                        DisplayLayerId = x.DisplayLayerId,
                        Reverse = x.Reverse,
                        FlipHorizontal = x.FlipHorizontal,
                        FlipVertical = x.FlipVertical,
                        Colour = x.Colour,
                        FadeType = (FadeTypes?)x.FadeTypeId
                    };

                    var groupedLayerClipDisplayLayer = groupedLayerClipDisplayLayers.FirstOrDefault(gl => gl.Key == x.ClipDisplayLayerId);
                    if (groupedLayerClipDisplayLayer != null)
                    {
                        clipDisplayLayer.LayerClipDisplayLayers = groupedLayerClipDisplayLayer.Select(glc => new LayerClipDisplayLayer
                        {
                            Colour = glc.Colour,
                            EndColour = glc.EndColour,
                            LayerId = glc.LayerId
                        });
                    }
                    return clipDisplayLayer;
                });
            }

            return clip;
        }
    }
}
