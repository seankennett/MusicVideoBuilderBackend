﻿using BuildEntities;
using CollectionEntities.Entities;
using System.Collections.Generic;
using System.Text;
using VideoDataAccess.Entities;

namespace BuildInstructorFunction.Services
{
    public interface IFfmpegComplexOperations
    {
        List<(string id, string ffmpegReference)> BuildInputList(StringBuilder command, string backgroundColour, byte bpm, string audioFileName, Resolution resolution, string watermarkFilePath, List<Layer> uniqueLayers);
        List<(string id, string ffmpegReference)> BuildLayerCommand(StringBuilder command, Clip clip, List<(string id, string ffmpegReference)> splitLayers, List<DisplayLayer> orderedDisplayLayers, string watermarkFilePath, bool fromCommandLine);
        void BuildClipCommand(StringBuilder command, string backgroundColour, List<(string id, string ffmpegReference)> splitLayers, string watermarkFilePath, List<Layer> orderedLayers);
        void BuildClipFilterCommand(StringBuilder command, Clip clip);
    }
}
