using BuildEntities;
using CollectionEntities.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using VideoDataAccess.Entities;
using VideoEntities.Entities;

namespace BuildInstructorFunction.Services
{
    public class FfmpegService : IFfmpegService
    {
        private readonly IFfmpegComplexOperations _ffmpegComplexOperations;

        public FfmpegService(IFfmpegComplexOperations ffmpegComplexOperations)
        {
            _ffmpegComplexOperations = ffmpegComplexOperations;
        }

        public string GetClipCode(Clip clip, Resolution resolution, Formats format, byte bpm, bool fromCommandLine, string outputBlobPrefix, string watermarkFilePath, List<Layer> orderedLayers)
        {
            var command = new StringBuilder();
            if (fromCommandLine)
            {
                command.Append("/bin/bash -c 'ffmpeg ");
            }

            command.Append("-y ");

            var inputList = _ffmpegComplexOperations.BuildInputList(command, clip.BackgroundColour, bpm, null, resolution, watermarkFilePath, orderedLayers);

            command.Append($"-filter_complex \"");

            inputList = _ffmpegComplexOperations.BuildLayerCommand(command, clip, inputList, orderedLayers, watermarkFilePath);
            _ffmpegComplexOperations.BuildClipCommand(command, clip.BackgroundColour, inputList, watermarkFilePath, orderedLayers);
            _ffmpegComplexOperations.BuildClipFilterCommand(command, clip);

            // make all clips same format so demuxer can be used later
            command.Append($"\" {outputBlobPrefix}/{clip.ClipId}.{format}");
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

            command.Append($"-y -ss {startTime:c} -i {blobPrefix}/{videoToSplit} -t {duration:c} -filter_complex \"fps={InstructorConstants.OutputFrameRate}");
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
