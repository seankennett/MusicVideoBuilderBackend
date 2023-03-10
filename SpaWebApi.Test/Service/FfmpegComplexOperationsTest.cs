using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharedEntities.Models;
using SpaWebApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpaWebApi.Test.Service
{
    [TestClass]
    public class FfmpegComplexOperationsTest
    {
        [TestMethod]
        public void BuildInputListColours()
        {
            var colour1 = "000000";
            var firstClip = new Clip
            {
                ClipId = 1,
                ClipName = "first",
                BackgroundColour = colour1
            };

            var video = new Video
            {
                BPM = 90,
                Format = Formats.mp4,
                VideoDelayMilliseconds = 100,
                VideoName = "test",
                Clips = new List<Clip>
                {
                    firstClip, firstClip
                }
            };

            var sb = new StringBuilder();

            var sut = new FfmpegComplexOperations();

            var output = sut.BuildInputList(sb, firstClip, video.BPM, null, Resolution.Free);

            Assert.AreEqual(colour1, output.Single().id);
            Assert.AreEqual("[0:v]", output.Single().ffmpegReference);
            Assert.AreEqual($"-f lavfi -i color=0x{colour1.ToUpper()}@1:s=384x216:r=2160/90 ", sb.ToString());
        }

        [TestMethod]
        public void BuildInputListLayer()
        {
            var layer1 = Guid.NewGuid();
            var firstClip = new Clip
            {
                ClipId = 1,
                ClipName = "first",
                Layers = new List<Layer>
                {
                    new Layer{LayerId = layer1 }
                }
            };

            var video = new Video
            {
                BPM = 90,
                Format = Formats.mp4,
                VideoDelayMilliseconds = 100,
                VideoName = "test",
                Clips = new List<Clip>
                {
                    firstClip, firstClip
                }
            };

            var sb = new StringBuilder();

            var sut = new FfmpegComplexOperations();

            var output = sut.BuildInputList(sb, firstClip, video.BPM, null, Resolution.Free);

            Assert.AreEqual(layer1.ToString(), output.Single().id);
            Assert.AreEqual("[0:v]", output.Single().ffmpegReference);
            Assert.AreEqual($"-framerate 2160/90 -i {layer1}/free/%d.png ", sb.ToString());
        }

        [TestMethod]
        public void BuildInputList()
        {
            var layer1 = Guid.NewGuid();
            var layer2 = Guid.NewGuid();
            var colour = "000000";
            var firstClip = new Clip
            {
                ClipId = 1,
                ClipName = "first",
                BackgroundColour = colour,
                Layers = new List<Layer>
                {
                    new Layer{LayerId = layer1 },
                    new Layer{LayerId = layer2 }
                }
            };

            var video = new Video
            {
                BPM = 90,
                Format = Formats.mp4,
                VideoDelayMilliseconds = 100,
                VideoName = "test",
                Clips = new List<Clip>
                {
                    firstClip, firstClip
                }
            };

            var sb = new StringBuilder();

            var sut = new FfmpegComplexOperations();

            var output = sut.BuildInputList(sb, firstClip, video.BPM, null, Resolution.Free);

            Assert.AreEqual(3, output.Count);

            Assert.AreEqual(layer1.ToString(), output[0].id);
            Assert.AreEqual("[0:v]", output[0].ffmpegReference);
            Assert.AreEqual(layer2.ToString(), output[1].id);
            Assert.AreEqual("[1:v]", output[1].ffmpegReference);
            Assert.AreEqual(colour, output[2].id);
            Assert.AreEqual("[2:v]", output[2].ffmpegReference);

            Assert.AreEqual($"-framerate 2160/90 -i {layer1}/free/%d.png -framerate 2160/90 -i {layer2}/free/%d.png -f lavfi -i color=0x{colour.ToUpper()}@1:s=384x216:r=2160/90 ", sb.ToString());
        }

        [TestMethod]
        public void SetBackgroundColourMaxFrames()
        {
            var black = "000000";
            var splitInput = new List<(string id, string ffmpegReference)>
            {
                new (black, "[v:0]" )
            };

            var sb = new StringBuilder("previous");
            var sut = new FfmpegComplexOperations();

            var output = sut.SetBackgroundColourMaxFrames(sb, black, splitInput, "x");


            Assert.AreEqual(black, output.Single().id);
            Assert.AreEqual("[x]", output.Single().ffmpegReference);

            Assert.AreEqual($"previous[v:0]trim=end_frame=64[x];", sb.ToString());
        }

        [TestMethod]
        public void SetBackgroundColourMaxFrames_NoOutputReference()
        {
            var black = "000000";
            var splitInput = new List<(string id, string ffmpegReference)>
            {
                new (black, "[v:0]" )
            };

            var sb = new StringBuilder("previous");
            var sut = new FfmpegComplexOperations();

            var output = sut.SetBackgroundColourMaxFrames(sb, black, splitInput, null);


            Assert.AreEqual(black, output.Single().id);
            Assert.AreEqual("[]", output.Single().ffmpegReference); // wouldn't use output here

            Assert.AreEqual($"previous[v:0]trim=end_frame=64,", sb.ToString());
        }

        [TestMethod]
        public void BuildClipOverlayAndTrimSingleLayerShort()
        {
            var layer1 = Guid.NewGuid();

            var zeroClip = new Clip
            {
                ClipId = 0,
                ClipName = "zero",
                Layers = new List<Layer>
            {
                new Layer{LayerId = layer1 }
            },
                BeatLength = 2,
                StartingBeat = 3
            };

            var splitClips = new List<(string id, string ffmpegReference)>
            {
                new (layer1.ToString(), "z1")
            };

            var sb = new StringBuilder("previous ");

            var sut = new FfmpegComplexOperations();

            sut.BuildClipByOverlayAndTrim(sb, zeroClip, splitClips);

            Assert.AreEqual("previous z1trim=start_frame=32:end_frame=64,setpts=PTS-STARTPTS,", sb.ToString());
        }

        [TestMethod]
        public void BuildClipOverlayAndTrimSingleLayer()
        {
            var layer1 = Guid.NewGuid();

            var zeroClip = new Clip
            {
                ClipId = 0,
                ClipName = "zero",
                Layers = new List<Layer>
            {
                new Layer{LayerId = layer1 }
            },
                BeatLength = 4,
                StartingBeat = 1
            };

            var splitClips = new List<(string id, string ffmpegReference)>
            {
                new (layer1.ToString(), "z1")
            };

            var sb = new StringBuilder("previous ");

            var sut = new FfmpegComplexOperations();

            sut.BuildClipByOverlayAndTrim(sb, zeroClip, splitClips);

            Assert.AreEqual("previous z1", sb.ToString());
        }

        [TestMethod]
        public void BuildClipOverlayAndTrimSingleColourNoChanges()
        {
            var colour = "000000";
            var zeroClip = new Clip
            {
                ClipId = 0,
                ClipName = "zero",
                BackgroundColour = colour,
                BeatLength = 4,
                StartingBeat = 1
            };

            var splitClips = new List<(string id, string ffmpegReference)>
            {
                new (colour, "z1")
            };

            var sb = new StringBuilder("previous ");

            var sut = new FfmpegComplexOperations();

            sut.BuildClipByOverlayAndTrim(sb, zeroClip, splitClips);

            Assert.AreEqual("previous z1", sb.ToString());
        }

        [TestMethod]
        public void BuildClipOverlayAndTrimSingleColour()
        {
            var colour = "000000";
            var zeroClip = new Clip
            {
                ClipId = 0,
                ClipName = "zero",
                BackgroundColour = colour,
                BeatLength = 1,
                StartingBeat = 3
            };

            var splitClips = new List<(string id, string ffmpegReference)> { new(colour, "z1") };

            var sb = new StringBuilder("previous ");

            var sut = new FfmpegComplexOperations();

            sut.BuildClipByOverlayAndTrim(sb, zeroClip, splitClips);

            Assert.AreEqual("previous z1trim=start_frame=32:end_frame=48,setpts=PTS-STARTPTS,", sb.ToString());
        }

        [TestMethod]
        public void BuildClipOverlayAndTrimAll()
        {
            var layer1 = Guid.NewGuid();
            var layer2 = Guid.NewGuid();
            var colour = "000000";
            var zeroClip = new Clip
            {
                ClipId = 0,
                ClipName = "zero",
                BackgroundColour = colour,
                BeatLength = 1,
                StartingBeat = 3,
                Layers = new List<Layer>
            {
                new Layer{LayerId = layer1 },
                new Layer{LayerId = layer2 }
            },
            };

            var splitClips = new List<(string id, string ffmpegReference)>
            {
                new (colour, "z1" ),
                new (layer1.ToString(), "z2" ),
                new (layer2.ToString(), "z3" )
            };

            var sb = new StringBuilder("previous ");

            var sut = new FfmpegComplexOperations();

            sut.BuildClipByOverlayAndTrim(sb, zeroClip, splitClips);

            Assert.AreEqual("previous z1z2overlay[o0];[o0]z3overlay,trim=start_frame=32:end_frame=48,setpts=PTS-STARTPTS,", sb.ToString());
        }
    }
}