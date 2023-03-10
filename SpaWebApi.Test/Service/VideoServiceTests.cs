using DataAccessLayer.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SharedEntities.Models;
using SpaWebApi.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpaWebApi.Test.Service
{
    [TestClass]
    public class VideoServiceTests
    {
        private Video _video;
        private Mock<IVideoRepository> _videoRepositoryMock;
        private Mock<IClipRepository> _clipRepositoryMock;
        private VideoService _sut;
        private Guid _userId;
        private Clip _firstClip;

        [TestInitialize]
        public void Init()
        {
            _userId = Guid.NewGuid();

            var layer1 = Guid.NewGuid();
            var layer2 = Guid.NewGuid();
            var layer3 = Guid.NewGuid();
            _firstClip = new Clip
            {
                ClipId = 1,
                ClipName = "first",
                Layers = new List<Layer>
            {
                new Layer{LayerId = layer1 }
            },
                BeatLength = 4,
                StartingBeat = 1
            };

            var secondClip = new Clip
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

            _video = new Video
            {
                VideoId = 1,
                BPM = 90,
                Format = Formats.mp4,
                VideoDelayMilliseconds = 100,
                VideoName = "test",
                Clips = new List<Clip>
                {
                    _firstClip, _firstClip, secondClip, _firstClip, secondClip
                }
            };

            _videoRepositoryMock = new Mock<IVideoRepository>();
            _clipRepositoryMock = new Mock<IClipRepository>();

            _clipRepositoryMock.Setup(x => x.GetAllAsync(_userId)).ReturnsAsync(_video.Clips);
            _videoRepositoryMock.Setup(x => x.SaveAsync(_userId, _video)).ReturnsAsync(_video);
            _videoRepositoryMock.Setup(x => x.GetAsync(_userId, _video.VideoId)).ReturnsAsync(new Video { Clips = new List<Clip> { _firstClip } });

            _sut = new VideoService(_videoRepositoryMock.Object, _clipRepositoryMock.Object);
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
        public async Task SaveVideoNotExist()
        {
            _videoRepositoryMock.Setup(x => x.GetAsync(_userId, _video.VideoId)).ReturnsAsync((Video)null);

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
    }
}