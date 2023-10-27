using BuildEntities;
using BuilderEntities.Extensions;
using CollectionEntities.Entities;
using System;
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
                        if (nextTargetId == watermarkFilePath)
                        {
                            command.Append($"=0:(main_h-overlay_h)");
                        }
                    }

                    command.Append(",format=gbrp");

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

        private Color ConvertToColor(string hexCode)
        {
            ColorConverter converter = new ColorConverter();
            return (Color)converter.ConvertFromString("#" + hexCode);
        }

        public List<(string id, string ffmpegReference)> BuildLayerCommand(StringBuilder command, Clip clip, List<(string id, string ffmpegReference)> splitLayers, List<DisplayLayer> uniqueDisplayLayers, string watermarkFilePath)
        {
            var hasMultipleLayers = CreateTargetIds(clip.BackgroundColour, uniqueDisplayLayers?.SelectMany(x => x.Layers).ToList(), watermarkFilePath).Count > 1;
            var i = 0;
            if (clip.BackgroundColour != null)
            {
                var matchingInputindex = splitLayers.FindIndex(x => x.id == clip.BackgroundColour);
                var matchedReference = splitLayers[matchingInputindex].ffmpegReference;
                command.Append($"{matchedReference}trim=end_frame={InstructorConstants.FramesInLayer},format=gbrp");
                if (hasMultipleLayers)
                {
                    UpdateFfmpegReference(command, splitLayers, i, matchingInputindex);
                    i++;
                }
            }

            if (uniqueDisplayLayers != null)
            {
                foreach (var displayLayer in uniqueDisplayLayers)
                {
                    var matchedClipDisplayLayer = clip.ClipDisplayLayers.First(c => c.DisplayLayerId == displayLayer.DisplayLayerId);
                    foreach (var layer in displayLayer.Layers)
                    {
                        var matchingInputindex = splitLayers.FindIndex(x => x.id == layer.LayerId.ToString());
                        var matchedReference = splitLayers[matchingInputindex].ffmpegReference;

                        var hasUsedReference = false;
                        if (matchedClipDisplayLayer.Reverse)
                        {
                            command.Append($"{matchedReference}reverse");
                            hasUsedReference = true;
                        }

                        if (matchedClipDisplayLayer.FlipHorizontal)
                        {
                            command.Append($"{(hasUsedReference ? "," : matchedReference)}hflip");
                            hasUsedReference = true;
                        }

                        if (matchedClipDisplayLayer.FlipVertical)
                        {
                            command.Append($"{(hasUsedReference ? "," : matchedReference)}vflip");
                            hasUsedReference = true;
                        }                        

                        if (!layer.IsOverlay)
                        {
                            var matchedOverrideLayer = matchedClipDisplayLayer.LayerClipDisplayLayers.First(x => x.LayerId == layer.LayerId);
                            var startColour = ConvertToColor(matchedOverrideLayer.Colour);
                            if (matchedOverrideLayer.EndColour == null)
                            {
                                command.Append($"{(hasUsedReference ? "," : matchedReference)}geq=r='r(X,Y)*({startColour.R}/255)':b='b(X,Y)*({startColour.B}/255)':g='g(X,Y)*({startColour.G}/255)'");
                            }
                            else
                            {
                                var framesInLayer = InstructorConstants.FramesInLayer - 1;
                                var endColour = ConvertToColor(matchedOverrideLayer.EndColour);
                                command.Append($"{(hasUsedReference ? "," : matchedReference)}geq=r='r(X,Y)/{framesInLayer}*(N*({endColour.R}/255)+{framesInLayer}*({startColour.R}/255)-N*({startColour.R}/255))':b='b(X,Y)/{framesInLayer}*(N*({endColour.B}/255)+{framesInLayer}*({startColour.B}/255)-N*({startColour.B}/255))':g='g(X,Y)/{framesInLayer}*(N*({endColour.G}/255)+{framesInLayer}*({startColour.G}/255)-N*({startColour.G}/255))'");
                            }
                            command.Append(",format=gbrp");
                            hasUsedReference = true;
                        }

                        if (matchedClipDisplayLayer.FadeType != null)
                        {
                            var fadeCommand = matchedClipDisplayLayer.FadeType.ToString().ToLower();
                            command.Append($"{(hasUsedReference ? "," : matchedReference)}fade={fadeCommand}:s=0:n={InstructorConstants.FramesInLayer}");
                            if (matchedClipDisplayLayer.Colour != null)
                            {
                                command.Append($":c={"#" + matchedClipDisplayLayer.Colour}");
                            }
                            else if (layer.IsOverlay)
                            {
                                command.Append(":alpha=1");
                            }

                            hasUsedReference = true;
                        }

                        if (hasUsedReference && hasMultipleLayers)
                        {
                            UpdateFfmpegReference(command, splitLayers, i, matchingInputindex);
                        }
                        i++;
                    }
                }
            }

            return splitLayers;
        }

        private static void UpdateFfmpegReference(StringBuilder command, List<(string id, string ffmpegReference)> splitLayers, int index, int matchingInputindex)
        {
            string output = $"[l{index}]";
            command.Append($"{output};");
            splitLayers[matchingInputindex] = new(splitLayers[matchingInputindex].id, output);
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
