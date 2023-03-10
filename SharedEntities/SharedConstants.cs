namespace SharedEntities
{
    public class SharedConstants
    {
        public const byte MinimumBpm = 90;
        public const byte OutputFrameRate = 24;
        public const byte FramesInLayer = 64;
        public const byte BeatsPerLayer = 4;
        public static readonly byte FramesPerBeat = FramesInLayer / BeatsPerLayer;
        public const string TempBlobPrefix = "temp";
        public const string AudioFileName = "audio.mp3";
        public const int VideoSplitLengthSeconds = 15;
        public const string ImageProcessQueue = "image-process";
        public const string BuilderQueueSuffix = "-builder";
        public const string FourKStorageContainer = "4k-zips";
        public const string HDStorageContainer = "hd-zips";
        public const string SpriteStorageContainer = "sprites";
        public const short FourKWidth = 3840;
        public const short FourKHeight = 2160;
        public const short HdWidth = 1920;
        public const short HdHeight = 1080;
        public const short FreeWidth = 384;
        public const short FreeHeight = 216;
        public const short MaximumLayerPerClip = 9;
    }
}
