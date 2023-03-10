using SharedEntities;
using SharedEntities.Extensions;
using SharedEntities.Models;
using System.Text;

namespace SpaWebApi.Services
{
    public class FfmpegComplexOperations : IFfmpegComplexOperations
    {
        public List<(string id, string ffmpegReference)> BuildInputList(StringBuilder command, Clip clip, byte bpm, string? audioFileName, Resolution resolution)
        {
            List<(string id, string ffmpegReference)> inputList = new List<(string id, string ffmpegReference)>();
            var uniqueUserLayers = clip.Layers != null ? clip.Layers.DistinctBy(x => x.LayerId).ToList() : new List<Layer>();
            var framerate = $"{bpm * SharedConstants.OutputFrameRate}/{SharedConstants.MinimumBpm}";
            for (var i = 0; i < uniqueUserLayers.Count; i++)
            {
                var userLayer = uniqueUserLayers[i];
                command.Append($"-framerate {framerate} -i {userLayer.LayerId}/{resolution.GetBlobPrefixByResolution()}/%d.png ");
                inputList.Add((userLayer.LayerId.ToString(), $"[{i}:v]"));
            }

            if (clip.BackgroundColour != null)
            {
                command.Append($"-f lavfi -i color=0x{clip.BackgroundColour.ToUpper()}@1:s={GetFormatedResolution(resolution)}:r={framerate} ");
                inputList.Add((clip.BackgroundColour, $"[{uniqueUserLayers.Count}:v]"));
            }

            if (audioFileName != null)
            {
                command.Append($"-i \"{audioFileName}\" ");
            }

            return inputList;
        }

        private string GetFormatedResolution(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Hd:
                    return $"{SharedConstants.HdWidth}x{SharedConstants.HdHeight}";
                case Resolution.FourK:
                    return $"{SharedConstants.FourKWidth}x{SharedConstants.FourKHeight}";
                default:
                    return $"{SharedConstants.FreeWidth}x{SharedConstants.FreeHeight}";
            }
        }

        public List<(string id, string ffmpegReference)> SetBackgroundColourMaxFrames(StringBuilder command, string backgroundColour, List<(string id, string ffmpegReference)> inputList, string? ffmpegOutputPrefix)
        {
            var matchingInputindex = inputList.FindIndex(x => x.id == backgroundColour);
            var output = $"[{ffmpegOutputPrefix}]";
            command.Append($"{inputList[matchingInputindex].ffmpegReference}trim=end_frame={SharedConstants.FramesInLayer}");
            if (ffmpegOutputPrefix != null)
            {
                command.Append($"{output};");
            }
            else
            {
                command.Append(",");
            }

            inputList[matchingInputindex] = new(inputList[matchingInputindex].id, output);

            return inputList;
        }

        public void BuildClipByOverlayAndTrim(StringBuilder command, Clip clip, List<(string id, string ffmpegReference)> layers)
        {
            var targetIds = new List<string>();
            if (clip.BackgroundColour != null)
            {
                targetIds.Add(clip.BackgroundColour);
            }

            if (clip.Layers != null)
            {
                targetIds.AddRange(clip.Layers.Select(x => x.LayerId.ToString()));
            }

            if (targetIds.Count == 1)
            {
                var matchedReference = layers.First(x => x.id == targetIds.First()).ffmpegReference;
                if (clip.BeatLength != SharedConstants.BeatsPerLayer)
                {
                    var startFrame = (clip.StartingBeat - 1) * SharedConstants.FramesPerBeat;
                    var endFrame = startFrame + clip.BeatLength * SharedConstants.FramesPerBeat;
                    command.Append($"{matchedReference}trim=start_frame={startFrame}:end_frame={endFrame},setpts=PTS-STARTPTS,");
                }
                else
                {
                    command.Append(matchedReference);
                }
            }
            else
            {
                string previousOutputReference = "";
                for (var j = 0; j < targetIds.Count - 1; j++)
                {
                    if (j == 0)
                    {
                        var targetId = targetIds[j];
                        previousOutputReference = layers.First(x => x.id == targetId).ffmpegReference;
                    }

                    var nextTargetId = targetIds[j + 1];
                    var nextUserLayerReference = layers.First(x => x.id == nextTargetId).ffmpegReference;


                    var outputReference = $"[o{j}]";

                    command.Append($"{previousOutputReference}{nextUserLayerReference}overlay");

                    // last
                    if (j == targetIds.Count - 2)
                    {
                        // beat length is not standard
                        if (clip.BeatLength != SharedConstants.BeatsPerLayer)
                        {
                            var startFrame = (clip.StartingBeat - 1) * SharedConstants.FramesPerBeat;
                            var endFrame = startFrame + clip.BeatLength * SharedConstants.FramesPerBeat;
                            command.Append($",trim=start_frame={startFrame}:end_frame={endFrame},setpts=PTS-STARTPTS,");
                        }
                        else
                        {
                            command.Append(",");
                        }
                    }

                    if (j != targetIds.Count - 2)
                    {
                        command.Append($"{outputReference};");
                    }

                    previousOutputReference = outputReference;
                }
            }
        }
    }
}
