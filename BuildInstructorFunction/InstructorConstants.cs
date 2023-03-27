
using VideoDataAccess;

namespace BuildInstructorFunction
{
    public class InstructorConstants
    {
        public const string AllFramesConcatFileName = "concatAllFrames.txt";
        public const string SplitFramesConcatFileName = "concatSplitFrames.txt";
        public const string AllFramesVideoName = "allframes";
        public const byte MinimumBpm = 90;
        public const byte OutputFrameRate = 24;
        public const byte FramesInLayer = 64;
        public static readonly byte FramesPerBeat = FramesInLayer / VideoDataAccessConstants.BeatsPerLayer;
        public const int VideoSplitLengthSeconds = 15;
        public const short FourKWidth = 3840;
        public const short FourKHeight = 2160;
        public const short HdWidth = 1920;
        public const short HdHeight = 1080;
        public const short FreeWidth = 384;
        public const short FreeHeight = 216;
        public const string BuilderQueueSuffix = "-builder";
    }
}
