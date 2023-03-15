using BuildInstructorFunction.Services;
using SharedEntities.Models;

namespace BuildInstructorFunction.Test.Services
{
    [TestClass]
    public class FfmpegServiceTests
    {
        [TestMethod]
        public void GetClipCodeAll()
        {
            var layer1 = Guid.NewGuid();
            var layer2 = Guid.NewGuid();
            var layer3 = Guid.NewGuid();

            var clip = new Clip
            {
                ClipId = 2,
                ClipName = "second",
                BackgroundColour = "000000",
                Layers = new List<Layer>
                {
                    new Layer{LayerId = layer1 },
                    new Layer{LayerId = layer2 },
                    new Layer{LayerId = layer3 },
                },
                BeatLength = 4,
                StartingBeat = 1
            };

            var sut = new FfmpegService(new FfmpegComplexOperations());

            var result = sut.GetClipCode(clip, Resolution.FourK, Formats.api, 90, true, "ouputprefix");
            Assert.AreEqual($"/bin/bash -c 'ffmpeg -y -framerate 2160/90 -i {layer1}/4k/%d.png -framerate 2160/90 -i {layer2}/4k/%d.png -framerate 2160/90 -i {layer3}/4k/%d.png -f lavfi -i color=0x000000@1:s=3840x2160:r=2160/90 -filter_complex \"[3:v]trim=end_frame=64[b];[b][0:v]overlay[o0];[o0][1:v]overlay[o1];[o1][2:v]overlay,format=yuv420p\" ouputprefix/2.api'", result);
        }

        [TestMethod]
        public void GetClipCodeLayers()
        {
            var layer1 = Guid.NewGuid();
            var layer2 = Guid.NewGuid();
            var layer3 = Guid.NewGuid();

            var clip = new Clip
            {
                ClipId = 2,
                ClipName = "second",
                BackgroundColour = null,
                Layers = new List<Layer>
                {
                    new Layer{LayerId = layer1 },
                    new Layer{LayerId = layer2 },
                    new Layer{LayerId = layer3 },
                },
                BeatLength = 4,
                StartingBeat = 1
            };

            var sut = new FfmpegService(new FfmpegComplexOperations());

            var result = sut.GetClipCode(clip, Resolution.FourK, Formats.api, 90, true, "ouputprefix");
            Assert.AreEqual($"/bin/bash -c 'ffmpeg -y -framerate 2160/90 -i {layer1}/4k/%d.png -framerate 2160/90 -i {layer2}/4k/%d.png -framerate 2160/90 -i {layer3}/4k/%d.png -filter_complex \"[0:v][1:v]overlay[o0];[o0][2:v]overlay,format=yuv420p\" ouputprefix/2.api'", result);
        }

        [TestMethod]
        public void GetClipCodeLayer()
        {
            var layer1 = Guid.NewGuid();
            var layer2 = Guid.NewGuid();
            var layer3 = Guid.NewGuid();

            var clip = new Clip
            {
                ClipId = 2,
                ClipName = "second",
                BackgroundColour = null,
                Layers = new List<Layer>
                {
                    new Layer{LayerId = layer1 },
                },
                BeatLength = 4,
                StartingBeat = 1
            };

            var sut = new FfmpegService(new FfmpegComplexOperations());

            var result = sut.GetClipCode(clip, Resolution.FourK, Formats.api, 90, true, "ouputprefix");
            Assert.AreEqual($"/bin/bash -c 'ffmpeg -y -framerate 2160/90 -i {layer1}/4k/%d.png -filter_complex \"[0:v]format=yuv420p\" ouputprefix/2.api'", result);
        }

        [TestMethod]
        public void GetClipCodeBackgroundColour()
        {
            var clip = new Clip
            {
                ClipId = 2,
                ClipName = "second",
                BackgroundColour = "000000",
                Layers = null,
                BeatLength = 4,
                StartingBeat = 1
            };

            var sut = new FfmpegService(new FfmpegComplexOperations());

            var result = sut.GetClipCode(clip, Resolution.FourK, Formats.api, 90, true, "ouputprefix");
            Assert.AreEqual($"/bin/bash -c 'ffmpeg -y -f lavfi -i color=0x000000@1:s=3840x2160:r=2160/90 -filter_complex \"[0:v]trim=end_frame=64,format=yuv420p\" ouputprefix/2.api'", result);
        }

        [TestMethod]
        public void GetConcatCode()
        {
            var clip1 = new Clip
            {
                ClipId = 1,
                ClipName = "second",
                BackgroundColour = "000000",
                Layers = null,
                BeatLength = 4,
                StartingBeat = 1
            };
            var clip2 = new Clip
            {
                ClipId = 2,
                ClipName = "second",
                BackgroundColour = "000000",
                Layers = null,
                BeatLength = 4,
                StartingBeat = 1
            };

            var video = new Video { Format = Formats.mov, Clips = new[] { clip2, clip1, clip1, clip2 } };

            var sut = new FfmpegService(new FfmpegComplexOperations());

            var result = sut.GetConcatCode(video.Clips.Select(x => $"{x.ClipId}.{video.Format}"));
            Assert.AreEqual("file '2.mov'\r\nfile '1.mov'\r\nfile '1.mov'\r\nfile '2.mov'\r\n", result);
        }

        [TestMethod]
        public void GetMergeCode()
        {
            var sut = new FfmpegService(new FfmpegComplexOperations());

            var result = sut.GetMergeCode(false, "folder1/tmp", "output.mov", null, "concat.txt");
            Assert.AreEqual("-y -f concat -i folder1/tmp/concat.txt -c copy folder1/tmp/output.mov", result);
        }

        [TestMethod]
        public void GetMergeCodeAudio()
        {
            var sut = new FfmpegService(new FfmpegComplexOperations());

            var result = sut.GetMergeCode(false, "folder1/tmp", "folder1", "output.mov", "something.mp3", "concat.txt");
            Assert.AreEqual("-y -f concat -i folder1/tmp/concat.txt -i folder1/tmp/something.mp3 -c copy folder1/output.mov", result);
        }
    }
}