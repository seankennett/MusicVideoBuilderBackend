﻿using BuildEntities;
using CollectionEntities.Entities;
using System;
using System.Collections.Generic;
using VideoDataAccess.Entities;
using VideoEntities.Entities;

namespace BuildInstructorFunction.Services
{
    public interface IFfmpegService
    {
        string GetClipCode(Clip clip, Resolution resolution, Formats format, byte bpm, bool fromCommandLine, string ouputBlobPrefix, string watermarkFilePath, List<DisplayLayer> orderedLayers);
        string GetConcatCode(IEnumerable<string> files);
        string GetMergeCode(bool fromCommandLine, string blobPrefix, string outputVideoName, string audioFileName, string concatFileName);
        string GetMergeCode(bool fromCommandLine, string blobPrefix, string ouputBlobPrefix, string outputVideoName, string audioFileName, string concatFileName);
        string GetSplitFrameCommand(bool fromCommandLine, string blobPrefix, TimeSpan startTime, TimeSpan duration, string videoToSplit, string outputVideoName, int? VideoDelayMilliseconds);
    }
}