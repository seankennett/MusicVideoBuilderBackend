using BuildEntities;
using BuilderEntities.Extensions;
using LayerDataAccess.Entities;
using LayerEntities;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using VideoDataAccess;
using VideoDataAccess.Entities;

namespace BuildInstructorFunction.Services
{
    public class FfmpegComplexOperations : IFfmpegComplexOperations
    {
        public List<(string id, string ffmpegReference)> BuildInputList(StringBuilder command, string backgroundColour, byte bpm, string audioFileName, Resolution resolution, string watermarkFilePath, List<Layer> uniqueLayers)
        {
            List<(string id, string ffmpegReference)> inputList = new List<(string id, string ffmpegReference)>();
            var framerate = $"{bpm * InstructorConstants.OutputFrameRate}/{InstructorConstants.MinimumBpm}";
            var overallIndex = 0;

            if (uniqueLayers != null)
            {
                for (var i = 0; i < uniqueLayers.Count; i++)
                {
                    var userLayer = uniqueLayers[i];
                    command.Append($"-framerate {framerate} -i {userLayer.LayerId}/{resolution.GetBlobPrefixByResolution()}/%d.png ");
                    inputList.Add((userLayer.LayerId.ToString(), $"[{overallIndex}:v]"));
                    overallIndex++;
                }
            }

            if (backgroundColour != null)
            {
                command.Append($"-f lavfi -i color=0x{backgroundColour.ToUpper()}@1:s={GetFormatedResolution(resolution)}:r={framerate} ");
                inputList.Add((backgroundColour, $"[{overallIndex}:v]"));
                overallIndex++;
            }

            if (watermarkFilePath != null)
            {
                command.Append($"-i \"{watermarkFilePath}\" ");
                inputList.Add((watermarkFilePath, $"[{overallIndex}:v]"));
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
                    return $"{InstructorConstants.HdWidth}x{InstructorConstants.HdHeight}";
                case Resolution.FourK:
                    return $"{InstructorConstants.FourKWidth}x{InstructorConstants.FourKHeight}";
                default:
                    return $"{InstructorConstants.FreeWidth}x{InstructorConstants.FreeHeight}";
            }
        }

        public void BuildClipCommand(StringBuilder command, string backgroundColour, List<(string id, string ffmpegReference)> splitLayers, string watermarkFilePath, List<Layer> uniqueLayers)
        {
            List<string> targetIds = CreateTargetIds(backgroundColour, uniqueLayers, watermarkFilePath);

            // either solid background colour or one layer with no watermark will not do anything
            if (targetIds.Count > 1)
            {
                string previousOutputReference = "";
                for (var j = 0; j < targetIds.Count - 1; j++)
                {
                    if (j == 0)
                    {
                        var targetId = targetIds[j];
                        previousOutputReference = splitLayers.First(x => x.id == targetId).ffmpegReference;
                    }

                    var nextTargetId = targetIds[j + 1];
                    var nextFfmpegReference = splitLayers.First(x => x.id == nextTargetId).ffmpegReference;

                    var outputReference = $"[o{j}]";

                    var nextMatchingLayer = uniqueLayers?.FirstOrDefault(x => x.LayerId.ToString() == nextTargetId);
                    if (nextMatchingLayer != null && !nextMatchingLayer.IsOverlay)
                    {
                        command.Append($"{previousOutputReference}{nextFfmpegReference}blend=all_mode=screen");
                    }
                    else
                    {
                        command.Append($"{previousOutputReference}{nextFfmpegReference}overlay");
                    }

                    if (nextTargetId == watermarkFilePath)
                    {
                        command.Append($"=0:(main_h-overlay_h)");
                    }

                    // if not last iteration
                    if (j != targetIds.Count - 2)
                    {
                        command.Append($"{outputReference};");
                    }

                    previousOutputReference = outputReference;
                }
            }
        }

        private static List<string> CreateTargetIds(string backgroundColour, List<Layer> uniqueLayers, string watermark)
        {
            var targetIds = new List<string>();
            if (backgroundColour != null)
            {
                targetIds.Add(backgroundColour);
            }

            if (uniqueLayers != null)
            {
                targetIds.AddRange(uniqueLayers.Select(x => x.LayerId.ToString()));
            }
            
            if (watermark != null)
            {
                targetIds.Add(watermark);
            }

            return targetIds;
        }

        private string ConvertToColorChannelMixerMatrix(string hexCode)
        {
            ColorConverter converter = new ColorConverter();
            Color color = (Color)converter.ConvertFromString("#" + hexCode);
            return $"{(double)color.R / byte.MaxValue}:0:0:0:{(double)color.G / byte.MaxValue}:0:0:0:{(double)color.B / byte.MaxValue}:0:0:0";
        }

        public List<(string id, string ffmpegReference)> BuildLayerCommand(StringBuilder command, Clip clip, List<(string id, string ffmpegReference)> splitLayers, List<Layer> uniqueLayers, string watermarkFilePath)
        {
            List<string> targetIds = CreateTargetIds(clip.BackgroundColour, uniqueLayers, watermarkFilePath);
            for (var i = 0; i < targetIds.Count; i++)
            {
                var targetId = targetIds[i];
                var matchingInputindex = splitLayers.FindIndex(x => x.id == targetId);
                var matchedReference = splitLayers[matchingInputindex].ffmpegReference;
                var matchedLayer = uniqueLayers?.FirstOrDefault(x => x.LayerId.ToString() == targetId);
                if (matchedLayer != null)
                {
                    var matchedOverrideLayer = clip.ClipDisplayLayers?.Where(x => x.LayerClipDisplayLayers != null).SelectMany(x => x.LayerClipDisplayLayers).FirstOrDefault(x => x.LayerId.ToString() == targetId);
                    var hexCode = matchedOverrideLayer?.ColourOverride ?? matchedLayer.DefaultColour;
                    command.Append($"{matchedReference}colorchannelmixer={ConvertToColorChannelMixerMatrix(hexCode)},format=gbrp");
                    UpdateFfmpegReference(command, splitLayers, targetIds, i, matchingInputindex);
                }
                else if (clip.BackgroundColour != null && targetId == clip.BackgroundColour)
                {
                    command.Append($"{matchedReference}trim=end_frame={InstructorConstants.FramesInLayer},format=gbrp");
                    UpdateFfmpegReference(command, splitLayers, targetIds, i, matchingInputindex);
                }
            }

            return splitLayers;
        }

        private static void UpdateFfmpegReference(StringBuilder command, List<(string id, string ffmpegReference)> splitLayers, List<string> targetIds, int index, int matchingInputindex)
        {
            if (targetIds.Count > 1)
            {
                string output = $"[l{index}]";
                command.Append($"{output};");
                splitLayers[matchingInputindex] = new(splitLayers[matchingInputindex].id, output);
            }
        }

        public void BuildClipFilterCommand(StringBuilder command, Clip clip)
        {
            if (clip.BeatLength != VideoDataAccessConstants.BeatsPerDisplayLayer)
            {
                // for plain background no watermark or layers as using commas and trim
                if (command.ToString().Contains("trim=end_frame="))
                {
                    command.Append("[c0];[c0]");
                }
                else
                {
                    command.Append(",");
                }

                var startFrame = (clip.StartingBeat - 1) * InstructorConstants.FramesPerBeat;
                var endFrame = startFrame + clip.BeatLength * InstructorConstants.FramesPerBeat;
                // when adding more filters then need to think about [c0] or , (using [c0] for safe just backgound)
                command.Append($"trim=start_frame={startFrame}:end_frame={endFrame},setpts=PTS-STARTPTS");
            }
        }
    }
}
