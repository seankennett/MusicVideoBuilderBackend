using BuildEntities;
using BuildInstructorFunction.Services;
using CollectionEntities.Entities;
using VideoDataAccess.Entities;
using VideoEntities.Entities;

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

            var displayLayerId1 = Guid.NewGuid();
            var displayLayerId2 = Guid.NewGuid();

            var watermarkFilePath = "watermark.png";
            var displayLayers = new List<DisplayLayer>
                {
                new DisplayLayer
                {
                    DisplayLayerId = displayLayerId1,
                    Layers = new List<Layer>
                    {
                    new Layer{LayerId = layer1, DefaultColour = "000000"},
                    new Layer{LayerId = layer2, DefaultColour = "001100", IsOverlay = true },
                    }
                },
                new DisplayLayer
                {
                    DisplayLayerId = displayLayerId2,
                    Layers = new List<Layer>
                    {
                    new Layer{LayerId = layer3, DefaultColour = "0000FF" }
                    },
                }
                };
            var clip = new Clip
            {
                ClipId = 2,
                ClipName = "second",
                BackgroundColour = "000000",
                ClipDisplayLayers = new List<ClipDisplayLayer>
                {
                    new ClipDisplayLayer
                    {
                        DisplayLayerId = displayLayerId1,
                        Reverse = true,
                        LayerClipDisplayLayers = new List<LayerClipDisplayLayer>
                        {
                            new LayerClipDisplayLayer
                            {
                                ColourOverride = "ff0000",
                                LayerId = layer2
                            }
                        }
                    },
                    new ClipDisplayLayer
                    {
                        DisplayLayerId = displayLayerId2,
                        Reverse = true
                    }
                },
                BeatLength = 4,
                StartingBeat = 1
            };

            var sut = new FfmpegService(new FfmpegComplexOperations());

            var result = sut.GetClipCode(clip, Resolution.FourK, Formats.api, 90, true, "ouputprefix", watermarkFilePath, displayLayers);
            Assert.AreEqual($"/bin/bash -c 'ffmpeg -y -framerate 2160/90 -i {layer1}/4k/%d.png -framerate 2160/90 -i {layer2}/4k/%d.png -framerate 2160/90 -i {layer3}/4k/%d.png -f lavfi -i color=0x000000@1:s=3840x2160:r=2160/90 -i \"{watermarkFilePath}\" -filter_complex \"[3:v]trim=end_frame=64,format=gbrp[l0];[0:v]reverse,colorchannelmixer=rr=0:gg=0:bb=0,format=gbrp[l1];[1:v]reverse[l2];[2:v]reverse,colorchannelmixer=rr=0:gg=0:bb=1,format=gbrp[l3];[l0][l1]blend=all_mode=screen,format=gbrp[o0];[o0][l2]overlay,format=gbrp[o1];[o1][l3]blend=all_mode=screen,format=gbrp[o2];[o2][4:v]overlay=0:(main_h-overlay_h),format=gbrp\" ouputprefix/2.api'", result);
        }

        [TestMethod]
        public void GetClipCodeLayers()
        {
            var layer1 = Guid.NewGuid();
            var layer2 = Guid.NewGuid();
            var layer3 = Guid.NewGuid();
            var displayLayers = new List<DisplayLayer>
                {
                new DisplayLayer {
                    Layers = new List<Layer>{
                    new Layer{LayerId = layer1, DefaultColour = "000000"},
                    new Layer{LayerId = layer2, DefaultColour = "001100" },
                    new Layer{LayerId = layer3, DefaultColour = "0000FF", IsOverlay = true }
                    }
                }
                };
            var clip = new Clip
            {
                ClipId = 2,
                ClipName = "second",
                BackgroundColour = null,
                BeatLength = 3,
                StartingBeat = 2
            };

            var sut = new FfmpegService(new FfmpegComplexOperations());

            var result = sut.GetClipCode(clip, Resolution.FourK, Formats.api, 90, true, "ouputprefix", null, displayLayers);
            Assert.AreEqual($"/bin/bash -c 'ffmpeg -y -framerate 2160/90 -i {layer1}/4k/%d.png -framerate 2160/90 -i {layer2}/4k/%d.png -framerate 2160/90 -i {layer3}/4k/%d.png -filter_complex \"[0:v]colorchannelmixer=rr=0:gg=0:bb=0,format=gbrp[l0];[1:v]colorchannelmixer=rr=0:gg=0.06666666666666667:bb=0,format=gbrp[l1];[l0][l1]blend=all_mode=screen,format=gbrp[o0];[o0][2:v]overlay,format=gbrp,trim=start_frame=16:end_frame=64,setpts=PTS-STARTPTS\" ouputprefix/2.api'", result);
        //                     bin/bash -c 'ffmpeg -y -framerate 2160/90 -i         /4k/%d.png -framerate 2160/90 -i         /4k/%d.png -framerate 2160/90 -i         /4k/%d.png -filter_complex \"[0:v]colorchannelmixer=rr=0:gg=0:bb=0,format=gbrp[l1];[1:v]colorchannelmixer=rr=0:gg=0.06666666666666667:bb=0,format=gbrp[l2];[l1][l2]blend=all_mode=screen,format=gbrp[o0];[o0][2:v]overlay,format=gbrp,trim=start_frame=16:end_frame=64,setpts=PTS-STARTPTS" ouputprefix/2.api
        }

        [TestMethod]
        public void GetClipCodeLayer()
        {
            var layer1 = Guid.NewGuid();
            var displayLayers = new List<DisplayLayer>
                {
                new DisplayLayer {
                    Layers = new List<Layer>{
                        new Layer{LayerId = layer1, DefaultColour = "000000"}
                }
                }
            };

            var clip = new Clip
            {
                ClipId = 2,
                ClipName = "second",
                BackgroundColour = null,
                BeatLength = 4,
                StartingBeat = 1
            };

            var sut = new FfmpegService(new FfmpegComplexOperations());

            var result = sut.GetClipCode(clip, Resolution.FourK, Formats.api, 90, true, "ouputprefix", null, displayLayers);
            Assert.AreEqual($"/bin/bash -c 'ffmpeg -y -framerate 2160/90 -i {layer1}/4k/%d.png -filter_complex \"[0:v]colorchannelmixer=rr=0:gg=0:bb=0,format=gbrp\" ouputprefix/2.api'", result);
        }

        [TestMethod]
        public void GetClipCodeBackgroundColour()
        {
            var clip = new Clip
            {
                ClipId = 2,
                ClipName = "second",
                BackgroundColour = "000000",
                ClipDisplayLayers = null,
                BeatLength = 4,
                StartingBeat = 1
            };

            var sut = new FfmpegService(new FfmpegComplexOperations());

            var result = sut.GetClipCode(clip, Resolution.FourK, Formats.api, 90, true, "ouputprefix", null, null);
            Assert.AreEqual($"/bin/bash -c 'ffmpeg -y -f lavfi -i color=0x000000@1:s=3840x2160:r=2160/90 -filter_complex \"[0:v]trim=end_frame=64,format=gbrp\" ouputprefix/2.api'", result);
        }

        [TestMethod]
        public void GetConcatCode()
        {
            var clip1 = new Clip
            {
                ClipId = 1,
                ClipName = "second",
                BackgroundColour = "000000",
                ClipDisplayLayers = null,
                BeatLength = 4,
                StartingBeat = 1
            };
            var clip2 = new Clip
            {
                ClipId = 2,
                ClipName = "second",
                BackgroundColour = "000000",
                ClipDisplayLayers = null,
                BeatLength = 4,
                StartingBeat = 1
            };

            var clips = new[] { clip2, clip1, clip1, clip2 };

            var sut = new FfmpegService(new FfmpegComplexOperations());

            var result = sut.GetConcatCode(clips.Select(x => $"{x.ClipId}.{Formats.mov}"));
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

        [TestMethod]
        public void GetSplitFrameCommand()
        {
            var sut = new FfmpegService(new FfmpegComplexOperations());

            var result = sut.GetSplitFrameCommand(true, "folder1/tmp", TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), "input.mov", "output.mov", 100);
            Assert.AreEqual("/bin/bash -c 'ffmpeg -y -ss 00:00:15 -t 00:00:30 -i folder1/tmp/input.mov -filter_complex \"fps=24,format=yuv420p,tpad=start_duration=100ms:start_mode=clone\" folder1/tmp/output.mov'", result);
        }
    }
}