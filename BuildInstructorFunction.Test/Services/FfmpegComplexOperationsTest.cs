using BuildEntities;
using BuildInstructorFunction.Services;
using CollectionEntities.Entities;
using System.Text;
using VideoDataAccess.Entities;

namespace BuildInstructorFunction.Test.Services
{
    [TestClass]
    public class FfmpegComplexOperationsTest
    {
        [TestMethod]
        public void BuildInputListColours()
        {
            var colour1 = "000000";

            var sb = new StringBuilder();

            var sut = new FfmpegComplexOperations();

            var output = sut.BuildInputList(sb, colour1, 90, null, Resolution.Free, null, null);

            Assert.AreEqual(colour1, output.Single().id);
            Assert.AreEqual("[0:v]", output.Single().ffmpegReference);
            Assert.AreEqual($"-f lavfi -i color=0x{colour1.ToUpper()}@1:s=384x216:r=2160/90 ", sb.ToString());
        }

        [TestMethod]
        public void BuildInputListLayer()
        {
            var layer1 = Guid.NewGuid();
            var layers = new List<Layer>
                {
                    new Layer{LayerId = layer1 }
                };

            var sb = new StringBuilder();

            var sut = new FfmpegComplexOperations();

            var output = sut.BuildInputList(sb, null, 90, null, Resolution.Free, null, layers);

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
            var watermarkFilePath = "watermark.png";
            var audioFilePath = "audio.mp3";
            var layers = new List<Layer>
                    {
                        new Layer{LayerId = layer1 },
                        new Layer{LayerId = layer2 }
                    };

            var sb = new StringBuilder();

            var sut = new FfmpegComplexOperations();

            var output = sut.BuildInputList(sb, colour, 90, audioFilePath, Resolution.Free, watermarkFilePath, layers);

            Assert.AreEqual(4, output.Count);

            Assert.AreEqual(layer1.ToString(), output[0].id);
            Assert.AreEqual("[0:v]", output[0].ffmpegReference);
            Assert.AreEqual(layer2.ToString(), output[1].id);
            Assert.AreEqual("[1:v]", output[1].ffmpegReference);
            Assert.AreEqual(colour, output[2].id);
            Assert.AreEqual("[2:v]", output[2].ffmpegReference);
            Assert.AreEqual(watermarkFilePath, output[3].id);
            Assert.AreEqual("[3:v]", output[3].ffmpegReference);
            Assert.AreEqual(4, output.Count);

            Assert.AreEqual($"-framerate 2160/90 -i {layer1}/free/%d.png -framerate 2160/90 -i {layer2}/free/%d.png -f lavfi -i color=0x{colour.ToUpper()}@1:s=384x216:r=2160/90 -i \"{watermarkFilePath}\" -i \"{audioFilePath}\" ", sb.ToString());
        }

        [TestMethod]
        public void BuildLayerCommandColour()
        {
            var black = "000000";
            var splitInput = new List<(string id, string ffmpegReference)>
                {
                    new (black, "[v:0]" )
                };

            var sb = new StringBuilder("previous");
            var clip = new Clip { BackgroundColour = black };
            var sut = new FfmpegComplexOperations();

            var output = sut.BuildLayerCommand(sb, clip, splitInput, null, null);

            Assert.AreEqual("[v:0]", output.Single().ffmpegReference);

            Assert.AreEqual($"previous[v:0]trim=end_frame=64,format=gbrp", sb.ToString());
        }

        [TestMethod]
        public void BuildLayerCommandColourWatermark()
        {
            var watermark = "watermark";
            var black = "000000";
            var splitInput = new List<(string id, string ffmpegReference)>
                {
                    new (black, "[v:0]" ),
                    new (watermark, "[v:1]")
                };

            var sb = new StringBuilder("previous");
            var clip = new Clip { BackgroundColour = black };
            var sut = new FfmpegComplexOperations();

            var output = sut.BuildLayerCommand(sb, clip, splitInput, null, watermark);

            Assert.AreEqual("[l0]", output[0].ffmpegReference);
            Assert.AreEqual("[v:1]", output[1].ffmpegReference);

            Assert.AreEqual($"previous[v:0]trim=end_frame=64,format=gbrp[l0];", sb.ToString());
        }

        [TestMethod]
        public void BuildLayerCommandLayerDefault()
        {
            var layerId = Guid.NewGuid();
            var splitInput = new List<(string id, string ffmpegReference)>
                {
                    new (layerId.ToString(), "[v:0]" )
                };

            var sb = new StringBuilder("previous");
            var clip = new Clip();
            var layers = new List<Layer>
            {
                new Layer
                {
                    DefaultColour = "ffffff",
                    LayerId = layerId
                }
            };

            var sut = new FfmpegComplexOperations();

            var output = sut.BuildLayerCommand(sb, clip, splitInput, layers, null);

            Assert.AreEqual("[v:0]", output.Single().ffmpegReference);

            Assert.AreEqual($"previous[v:0]colorchannelmixer=rr=1:gg=1:bb=1,format=gbrp", sb.ToString());
        }

        [TestMethod]
        public void BuildLayerCommandLayerOverride()
        {
            var layerId = Guid.NewGuid();
            var splitInput = new List<(string id, string ffmpegReference)>
                {
                    new (layerId.ToString(), "[v:0]" )
                };

            var sb = new StringBuilder("previous");
            var clip = new Clip
            {
                ClipDisplayLayers = new List<ClipDisplayLayer>
                {
                    new ClipDisplayLayer
                    {
                        LayerClipDisplayLayers = new List<LayerClipDisplayLayer>
                        {
                            new LayerClipDisplayLayer
                            {
                                LayerId = layerId,
                                ColourOverride = "000000"
                            }
                        }
                    }
                }
            };
            var layers = new List<Layer>
            {
                new Layer
                {
                    DefaultColour = "ffffff",
                    LayerId = layerId
                }
            };

            var sut = new FfmpegComplexOperations();

            var output = sut.BuildLayerCommand(sb, clip, splitInput, layers, null);

            Assert.AreEqual("[v:0]", output.Single().ffmpegReference);

            Assert.AreEqual($"previous[v:0]colorchannelmixer=rr=0:gg=0:bb=0,format=gbrp", sb.ToString());
        }

        [TestMethod]
        public void BuildLayerCommandLayerDefaultWatermark()
        {
            var watermark = "watermark";
            var layerId = Guid.NewGuid();
            var splitInput = new List<(string id, string ffmpegReference)>
                {
                    new (layerId.ToString(), "[v:0]" ),
                    new (watermark, "[v:1]" )
                };

            var sb = new StringBuilder("previous");
            var clip = new Clip();
            var layers = new List<Layer>
            {
                new Layer
                {
                    DefaultColour = "ffffff",
                    LayerId = layerId
                }
            };

            var sut = new FfmpegComplexOperations();

            var output = sut.BuildLayerCommand(sb, clip, splitInput, layers, watermark);

            Assert.AreEqual("[l0]", output[0].ffmpegReference);
            Assert.AreEqual("[v:1]", output[1].ffmpegReference);

            Assert.AreEqual($"previous[v:0]colorchannelmixer=rr=1:gg=1:bb=1,format=gbrp[l0];", sb.ToString());
        }

        [TestMethod]
        public void BuildLayerCommandLayersOverrideWatermark()
        {
            var watermark = "watermark";
            var layerId = Guid.NewGuid();
            var layerId2 = Guid.NewGuid();
            var splitInput = new List<(string id, string ffmpegReference)>
                {
                    new (layerId.ToString(), "[v:0]" ),
                    new (layerId2.ToString(), "[v:1]" ),
                    new (watermark, "[v:3]" )
                };

            var sb = new StringBuilder("previous");
            var clip = new Clip
            {
                ClipDisplayLayers = new List<ClipDisplayLayer> 
                { 
                    new ClipDisplayLayer 
                    { 
                        LayerClipDisplayLayers = new List<LayerClipDisplayLayer> 
                        { 
                            new LayerClipDisplayLayer
                            {
                                LayerId = layerId2,
                                ColourOverride = "009911"
                            } 
                        } 
                    } 
                }
            };
            var layers = new List<Layer>
            {
                new Layer
                {
                    DefaultColour = "ffffff",
                    LayerId = layerId
                },
                new Layer
                {
                    DefaultColour = "ff0000",
                    LayerId = layerId2
                }
            };

            var sut = new FfmpegComplexOperations();

            var output = sut.BuildLayerCommand(sb, clip, splitInput, layers, null);

            Assert.AreEqual("[l0]", output[0].ffmpegReference);
            Assert.AreEqual("[l1]", output[1].ffmpegReference);
            Assert.AreEqual("[v:3]", output[2].ffmpegReference);

            Assert.AreEqual($"previous[v:0]colorchannelmixer=rr=1:gg=1:bb=1,format=gbrp[l0];[v:1]colorchannelmixer=rr=0:gg=0.6:bb=0.06666666666666667,format=gbrp[l1];", sb.ToString());
        }

        [TestMethod]
        public void BuildLayerCommandLayerDefaultBackground()
        {
            var black = "000000";
            var layerId = Guid.NewGuid();
            var splitInput = new List<(string id, string ffmpegReference)>
                {
                    new (black, "[v:1]"),
                    new (layerId.ToString(), "[v:0]" )
                };

            var sb = new StringBuilder("previous");
            var clip = new Clip { BackgroundColour = black };
            var layers = new List<Layer>
            {
                new Layer
                {
                    DefaultColour = "ffffff",
                    LayerId = layerId
                }
            };

            var sut = new FfmpegComplexOperations();

            var output = sut.BuildLayerCommand(sb, clip, splitInput, layers, null);

            Assert.AreEqual(black, output[0].id);
            Assert.AreEqual("[l0]", output[0].ffmpegReference);
            Assert.AreEqual(layerId.ToString(), output[1].id);
            Assert.AreEqual("[l1]", output[1].ffmpegReference);

            Assert.AreEqual($"previous[v:1]trim=end_frame=64,format=gbrp[l0];[v:0]colorchannelmixer=rr=1:gg=1:bb=1,format=gbrp[l1];", sb.ToString());
        }

        [TestMethod]
        public void BuildClipCommandLayer()
        {
            var layer1 = Guid.NewGuid();

            var layers = new List<Layer>
                {
                    new Layer{LayerId = layer1 }
                };

            var splitClips = new List<(string id, string ffmpegReference)>
                {
                    new (layer1.ToString(), "z1")
                };

            var sb = new StringBuilder("previous ");

            var sut = new FfmpegComplexOperations();

            sut.BuildClipCommand(sb, null, splitClips, null, layers);

            Assert.AreEqual("previous ", sb.ToString());
        }

        [TestMethod]
        public void BuildClipCommandBackground()
        {
            var colour = "000000";
            var splitClips = new List<(string id, string ffmpegReference)>
                {
                    new (colour, "[z1]")
                };

            var sb = new StringBuilder("previous ");

            var sut = new FfmpegComplexOperations();

            sut.BuildClipCommand(sb, colour, splitClips, null, null);

            Assert.AreEqual("previous ", sb.ToString());
        }

        [TestMethod]
        public void BuildClipCommandLayerWatermark()
        {
            string watermark = "watermark";
            var layer1 = Guid.NewGuid();

            var layers = new List<Layer>
                {
                    new Layer{LayerId = layer1 }
                };

            var splitClips = new List<(string id, string ffmpegReference)>
                {
                    new (layer1.ToString(), "[z1]"),
                    new (watermark, "[y1]")
                };

            var sb = new StringBuilder("previous ");

            var sut = new FfmpegComplexOperations();

            sut.BuildClipCommand(sb, null, splitClips, watermark, layers);

            Assert.AreEqual("previous [z1][y1]overlay=0:(main_h-overlay_h),format=gbrp", sb.ToString());
        }

        [TestMethod]
        public void BuildClipCommandBackgroundWatermark()
        {
            string watermark = "watermark";
            var colour = "000000";
            var splitClips = new List<(string id, string ffmpegReference)>
                {
                    new (colour, "[z1]"),
                    new (watermark, "[y1]")
                };

            var sb = new StringBuilder("previous ");

            var sut = new FfmpegComplexOperations();

            sut.BuildClipCommand(sb, colour, splitClips, watermark, null);

            Assert.AreEqual("previous [z1][y1]overlay=0:(main_h-overlay_h),format=gbrp", sb.ToString());
        }

        [TestMethod]
        public void BuildClipCommandLayersOverlay()
        {
            var layer1 = Guid.NewGuid();
            var layer2 = Guid.NewGuid();

            var layers = new List<Layer>
                {
                    new Layer{LayerId = layer1 },
                    new Layer{LayerId = layer2, IsOverlay = true }
                };

            var splitClips = new List<(string id, string ffmpegReference)>
                {
                    new (layer1.ToString(), "[z1]"),
                    new (layer2.ToString(), "[y1]")
                };

            var sb = new StringBuilder("previous ");

            var sut = new FfmpegComplexOperations();

            sut.BuildClipCommand(sb, null, splitClips, null, layers);

            Assert.AreEqual("previous [z1][y1]overlay,format=gbrp", sb.ToString());
        }

        [TestMethod]
        public void BuildClipCommandLayersBlend()
        {
            var layer1 = Guid.NewGuid();
            var layer2 = Guid.NewGuid();

            var layers = new List<Layer>
                {
                    new Layer{LayerId = layer1 },
                    new Layer{LayerId = layer2 }
                };

            var splitClips = new List<(string id, string ffmpegReference)>
                {
                    new (layer1.ToString(), "[z1]"),
                    new (layer2.ToString(), "[y1]")
                };

            var sb = new StringBuilder("previous ");

            var sut = new FfmpegComplexOperations();

            sut.BuildClipCommand(sb, null, splitClips, null, layers);

            Assert.AreEqual("previous [z1][y1]blend=all_mode=screen,format=gbrp", sb.ToString());
        }

        [TestMethod]
        public void BuildClipCommandLayersBlendWatermark()
        {
            string watermark = "watermark";
            var layer1 = Guid.NewGuid();
            var layer2 = Guid.NewGuid();

            var layers = new List<Layer>
                {
                    new Layer{LayerId = layer1 },
                    new Layer{LayerId = layer2 }
                };

            var splitClips = new List<(string id, string ffmpegReference)>
                {
                    new (watermark, "[x1]"),
                    new (layer1.ToString(), "[z1]"),
                    new (layer2.ToString(), "[y1]")
                };

            var sb = new StringBuilder("previous ");

            var sut = new FfmpegComplexOperations();

            sut.BuildClipCommand(sb, null, splitClips, watermark, layers);

            Assert.AreEqual("previous [z1][y1]blend=all_mode=screen,format=gbrp[o0];[o0][x1]overlay=0:(main_h-overlay_h),format=gbrp", sb.ToString());
        }

        [TestMethod]
        public void BuildClipCommandBackgroundLayers()
        {
            string colour = "000000";
            var layer1 = Guid.NewGuid();
            var layer2 = Guid.NewGuid();
            var layer3 = Guid.NewGuid();

            var layers = new List<Layer>
                {
                    new Layer{LayerId = layer1, IsOverlay = true },
                    new Layer{LayerId = layer2 },
                    new Layer{LayerId = layer3 },
                };

            var splitClips = new List<(string id, string ffmpegReference)>
                {
                    new (colour, "[x1]"),
                    new (layer1.ToString(), "[z1]"),
                    new (layer2.ToString(), "[y1]"),
                    new (layer3.ToString(), "[w1]")
                };

            var sb = new StringBuilder("previous ");

            var sut = new FfmpegComplexOperations();

            sut.BuildClipCommand(sb, colour, splitClips, null, layers);

            Assert.AreEqual("previous [x1][z1]overlay,format=gbrp[o0];[o0][y1]blend=all_mode=screen,format=gbrp[o1];[o1][w1]blend=all_mode=screen,format=gbrp", sb.ToString());
        }

        //[TestMethod]
        //public void BuildClipFilterCommandNormalLength()
        //{
        //    var clip = new Clip {
        //        BeatLength = 4,
        //        StartingBeat = 1
        //    };

        //    var sb = new StringBuilder("previous ");

        //    var sut = new FfmpegComplexOperations();

        //    sut.BuildClipFilterCommand(sb, clip);

        //    Assert.AreEqual("previous ", sb.ToString());
        //}

        //[TestMethod]
        //public void BuildClipFilterCommandShortLength()
        //{
        //    var clip = new Clip
        //    {
        //        BeatLength = 2,
        //        StartingBeat = 3
        //    };

        //    var sb = new StringBuilder("previous ");

        //    var sut = new FfmpegComplexOperations();

        //    sut.BuildClipFilterCommand(sb, clip);

        //    Assert.AreEqual("previous ,trim=start_frame=32:end_frame=64,setpts=PTS-STARTPTS", sb.ToString());
        //}

        //[TestMethod]
        //public void BuildClipFilterCommandShortLengthBackgroundException()
        //{
        //    var colour = "000000";
        //    var clip = new Clip
        //    {
        //        BackgroundColour = colour,
        //        BeatLength = 2,
        //        StartingBeat = 3
        //    };

        //    var splitClips = new List<(string id, string ffmpegReference)>
        //        {
        //            new (colour, "[z1]")
        //        };

        //    var sb = new StringBuilder("");

        //    var sut = new FfmpegComplexOperations();

        //    sut.BuildLayerCommand(sb, clip, splitClips, null, null);
        //    sut.BuildClipFilterCommand(sb, clip);

        //    Assert.AreEqual("[z1]trim=end_frame=64,format=gbrp[c0];[c0]trim=start_frame=32:end_frame=64,setpts=PTS-STARTPTS", sb.ToString());
        //}

        //[TestMethod]
        //public void BuildClipFilterCommandShortLengthNormal()
        //{
        //    var layerId = Guid.NewGuid();
        //    var clip = new Clip
        //    {
        //        BeatLength = 2,
        //        StartingBeat = 3
        //    };

        //    var layers = new List<Layer>
        //    {
        //        new Layer
        //        {
        //            LayerId = layerId,
        //            DefaultColour = "000000"
        //        }
        //    };

        //    var splitClips = new List<(string id, string ffmpegReference)>
        //        {
        //            new (layerId.ToString(), "[z1]")
        //        };

        //    var sb = new StringBuilder("");

        //    var sut = new FfmpegComplexOperations();

        //    sut.BuildLayerCommand(sb, clip, splitClips, layers, null);
        //    sut.BuildClipFilterCommand(sb, clip);

        //    Assert.AreEqual("[z1]colorchannelmixer=rr=0:gg=0:bb=0,format=gbrp,trim=start_frame=32:end_frame=64,setpts=PTS-STARTPTS", sb.ToString());
        //}
    }
}