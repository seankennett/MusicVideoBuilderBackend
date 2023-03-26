using SharedEntities;
using SharedEntities.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BuildInstructorFunction.Services
{
    public class FfmpegService : IFfmpegService
    {
        private readonly IFfmpegComplexOperations _ffmpegComplexOperations;

        public FfmpegService(IFfmpegComplexOperations ffmpegComplexOperations)
        {
            _ffmpegComplexOperations = ffmpegComplexOperations;
        }

        public string GetClipCode(Clip clip, Resolution resolution, Formats format, byte bpm, bool fromCommandLine, string outputBlobPrefix, string watermarkFilePath)
        {
            var command = new StringBuilder();
            if (fromCommandLine)
            {
                command.Append("/bin/bash -c 'ffmpeg ");
            }

            command.Append("-y ");

            var inputList = _ffmpegComplexOperations.BuildInputList(command, clip, bpm, null, resolution, watermarkFilePath);

            command.Append($"-filter_complex \"");

            if ((clip.BackgroundColour != null && clip.Layers != null) || (clip.BackgroundColour != null && watermarkFilePath != null))
            {
                inputList = _ffmpegComplexOperations.SetBackgroundColourMaxFrames(command, clip.BackgroundColour, inputList, "b");
                _ffmpegComplexOperations.BuildClipByOverlayAndTrim(command, clip, inputList, watermarkFilePath);
            }
            else if (clip.BackgroundColour != null)
            {
                _ffmpegComplexOperations.SetBackgroundColourMaxFrames(command, clip.BackgroundColour, inputList, null);
            }
            else
            {
                _ffmpegComplexOperations.BuildClipByOverlayAndTrim(command, clip, inputList, watermarkFilePath);
            }

            // make all clips same format so demuxer can be used later
            command.Append($"format=yuv420p\" {outputBlobPrefix}/{clip.ClipId}.{format}");
            if (fromCommandLine)
            {
                command.Append("'");
            }

            return command.ToString();
        }

        public string GetMergeCode(bool fromCommandLine, string blobPrefix, string outputVideoName, string audioFileName, string concatFileName)
        {
            return GetMergeCode(fromCommandLine, blobPrefix, blobPrefix, outputVideoName, audioFileName, concatFileName);
        }

        public string GetMergeCode(bool fromCommandLine, string blobPrefix, string outputBlobPrefix, string outputVideoName, string audioFileName, string concatFileName)
        {
            var command = new StringBuilder();
            if (fromCommandLine)
            {
                command.Append("/bin/bash -c 'ffmpeg ");
            }

            command.Append($"-y -f concat -i {blobPrefix}/{concatFileName} ");
            if (audioFileName != null)
            {
                command.Append($"-i {blobPrefix}/{audioFileName} ");
            }

            command.Append($"-c copy {outputBlobPrefix}/{outputVideoName}");
            if (fromCommandLine)
            {
                command.Append("'");
            }
            return command.ToString();
        }

        public string GetConcatCode(IEnumerable<string> files)
        {
            StringBuilder command = new StringBuilder();
            foreach (var file in files)
            {
                command.AppendLine($"file '{file}'");
            }

            return command.ToString();
        }

        public string GetSplitFrameCommand(bool fromCommandLine, string blobPrefix, TimeSpan startTime, TimeSpan duration, string videoToSplit, string outputVideoName, int? videoDelayMilliseconds)
        {
            var command = new StringBuilder();
            if (fromCommandLine)
            {
                command.Append("/bin/bash -c 'ffmpeg ");
            }

            command.Append($"-y -ss {startTime:c} -i {blobPrefix}/{videoToSplit} -t {duration:c} -filter_complex \"fps={SharedConstants.OutputFrameRate}");
            if (videoDelayMilliseconds.HasValue)
            {
                command.Append($",tpad=start_duration={videoDelayMilliseconds.Value}ms:start_mode=clone");
            }

            command.Append($"\" {blobPrefix}/{outputVideoName}");
            if (fromCommandLine)
            {
                command.Append("'");
            }
            return command.ToString();
        }
    }
}
