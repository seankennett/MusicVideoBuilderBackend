using BuildEntities;
using System;
using System.Collections.Generic;
using VideoDataAccess.Entities;

namespace BuildInstructorFunction.Services
{
    public interface IFfmpegService
    {
        string GetClipCode(Clip clip, Resolution resolution, Formats format, byte bpm, bool fromCommandLine, string ouputBlobPrefix, string watermarkFilePath);
        string GetConcatCode(IEnumerable<string> files);
        string GetMergeCode(bool fromCommandLine, string blobPrefix, string outputVideoName, string audioFileName, string concatFileName);
        string GetMergeCode(bool fromCommandLine, string blobPrefix, string ouputBlobPrefix, string outputVideoName, string audioFileName, string concatFileName);
        string GetSplitFrameCommand(bool fromCommandLine, string blobPrefix, TimeSpan startTime, TimeSpan duration, string videoToSplit, string outputVideoName, int? VideoDelayMilliseconds);
    }
}