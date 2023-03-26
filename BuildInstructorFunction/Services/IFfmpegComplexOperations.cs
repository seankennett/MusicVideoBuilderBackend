using SharedEntities.Models;
using System.Collections.Generic;
using System.Text;

namespace BuildInstructorFunction.Services
{
    public interface IFfmpegComplexOperations
    {
        List<(string id, string ffmpegReference)> BuildInputList(StringBuilder command, Clip clips, byte bpm, string audioFileName, Resolution resolution, string watermarkFilePath);
        void BuildClipByOverlayAndTrim(StringBuilder command, Clip clip, List<(string id, string ffmpegReference)> splitLayers, string watermarkFilePath);
        List<(string id, string ffmpegReference)> SetBackgroundColourMaxFrames(StringBuilder command, string backgroundColour, List<(string id, string ffmpegReference)> inputList, string ffmpegOutputPrefix);
    }
}
