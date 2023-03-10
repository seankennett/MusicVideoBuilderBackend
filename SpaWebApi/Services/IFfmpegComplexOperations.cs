using SharedEntities.Models;
using System.Text;

namespace SpaWebApi.Services
{
    public interface IFfmpegComplexOperations
    {
        List<(string id, string ffmpegReference)> BuildInputList(StringBuilder command, Clip clips, byte bpm, string? audioFileName, Resolution resolution);
        void BuildClipByOverlayAndTrim(StringBuilder command, Clip clip, List<(string id, string ffmpegReference)> splitLayers);
        List<(string id, string ffmpegReference)> SetBackgroundColourMaxFrames(StringBuilder command, string backgroundColour, List<(string id, string ffmpegReference)> inputList, string? ffmpegOutputPrefix);
    }
}
