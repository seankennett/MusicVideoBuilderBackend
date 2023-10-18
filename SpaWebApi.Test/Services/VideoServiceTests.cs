using BuildDataAccess.Entities;
using BuildDataAccess.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SpaWebApi.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VideoDataAccess.Entities;
using VideoDataAccess.Repositories;
using VideoEntities.Entities;

namespace SpaWebApi.Test.Services
{
    [TestClass]
    public class VideoServiceTests
    {
        private Video _video;
        private Mock<IVideoRepository> _videoRepositoryMock;
        private Mock<IClipRepository> _clipRepositoryMock;
        private Mock<IBuildRepository> _buildRepositoryMock;
        private VideoService _sut;
        private Guid _userId;
        private Clip _firstClip;

        [TestInitialize]
        public void Init()
        {
            _userId = Guid.NewGuid();

            var displayLayer1 = Guid.NewGuid();
            var displayLayer2 = Guid.NewGuid();
            var displayLayer3 = Guid.NewGuid();
            _firstClip = new Clip
            {
                ClipId = 1,
                ClipName = "first",
                ClipDisplayLayers = new List<ClipDisplayLayer>
            {
                new ClipDisplayLayer{DisplayLayerId = displayLayer1 }
            },
                BeatLength = 4,
                StartingBeat = 1
            };

            var secondClip = new Clip
            {
                ClipId = 2,
                ClipName = "second",
                BackgroundColour = "000000",
                ClipDisplayLayers = new List<ClipDisplayLayer>
                {
                    new ClipDisplayLayer{DisplayLayerId = displayLayer1 },
                    new ClipDisplayLayer{DisplayLayerId = displayLayer2 },
                    new ClipDisplayLayer { DisplayLayerId = displayLayer3 }
                },
                BeatLength = 4,
                StartingBeat = 1
            };

            var clips = new[] { _firstClip, secondClip };

            _video = new Video
            {
                VideoId = 1,
                BPM = 90,
                Format = Formats.mp4,
                VideoDelayMilliseconds = 100,
                VideoName = "test",
                VideoClips = new List<VideoClip>
                {
                    new VideoClip{ ClipId = _firstClip.ClipId },
                    new VideoClip{ ClipId = _firstClip.ClipId },
                    new VideoClip{ ClipId = secondClip.ClipId },
                    new VideoClip{ ClipId = _firstClip.ClipId },
                    new VideoClip{ ClipId = secondClip.ClipId }
                }
            };

            _videoRepositoryMock = new Mock<IVideoRepository>();
            _clipRepositoryMock = new Mock<IClipRepository>();
            _buildRepositoryMock = new Mock<IBuildRepository>();

            _clipRepositoryMock.Setup(x => x.GetAllAsync(_userId)).ReturnsAsync(clips);
            _videoRepositoryMock.Setup(x => x.SaveAsync(_userId, _video)).ReturnsAsync(_video);
            _videoRepositoryMock.Setup(x => x.GetAsync(_userId, _video.VideoId)).ReturnsAsync(new Video { VideoClips = new List<VideoClip> { new VideoClip { ClipId = _firstClip.ClipId } } });

            _sut = new VideoService(_videoRepositoryMock.Object, _clipRepositoryMock.Object, _buildRepositoryMock.Object);
        }

        [TestMethod]
        public async Task Save()
        {
            var result = await _sut.SaveAsync(_userId, _video);

            Assert.AreEqual(_video, result);

            _videoRepositoryMock.Verify(x => x.SaveAsync(_userId, _video), Times.Once);
        }

        [TestMethod]
        public async Task SaveDuplicate()
        {
            _videoRepositoryMock.Setup(x => x.GetAsync(_userId, _video.VideoId)).ReturnsAsync(_video);

            var result = await _sut.SaveAsync(_userId, _video);

            Assert.AreEqual(_video, result);

            _videoRepositoryMock.Verify(x => x.SaveAsync(_userId, _video), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task SaveInvalidClips()
        {
            _clipRepositoryMock.Setup(x => x.GetAllAsync(_userId)).ReturnsAsync(new List<Clip> { _firstClip });

            await _sut.SaveAsync(_userId, _video);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task SaveInvalidVideoLength()
        {
            var videoClips = new List<VideoClip>();
            for (var i = 0; i < 1000; i++)
            {
                videoClips.Add(new VideoClip { ClipId = _firstClip.ClipId });
            }

            _video.VideoClips = videoClips;

            await _sut.SaveAsync(_userId, _video);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task SaveVideoNotExist()
        {
            _videoRepositoryMock.Setup(x => x.GetAsync(_userId, _video.VideoId)).ReturnsAsync((Video)null);

            await _sut.SaveAsync(_userId, _video);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task SaveVideoTooMany()
        {
            _video.VideoId = 0;
            _videoRepositoryMock.Setup(x => x.GetAllAsync(_userId))
                .ReturnsAsync(new List<Video> 
                {
                    new Video(),
                    new Video()
                });

            await _sut.SaveAsync(_userId, _video);
        }

        [TestMethod]
        public async Task Delete()
        {
            await _sut.DeleteAsync(_userId, _video.VideoId);

            _videoRepositoryMock.Verify(x => x.DeleteAsync(_video.VideoId), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task DeleteNoVideo()
        {
            _videoRepositoryMock.Setup(x => x.GetAsync(_userId, _video.VideoId)).ReturnsAsync((Video)null);

            await _sut.DeleteAsync(_userId, _video.VideoId);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task DeleteBuildingVideo()
        {
            var builds = new List<Build>
            {
                new Build
                {
                    BuildStatus = SharedEntities.Models.BuildStatus.BuildingPending,
                    VideoId = _video.VideoId                }
            };
            _buildRepositoryMock.Setup(x => x.GetAllAsync(_userId)).ReturnsAsync(builds);
            await _sut.DeleteAsync(_userId, _video.VideoId);
        }
    }
}