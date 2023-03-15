using DataAccessLayer.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SharedEntities.Models;
using SpaWebApi.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpaWebApi.Test.Services
{
    [TestClass]
    public class ClipServiceTests
    {
        private Mock<IVideoRepository> _videoRepositoryMock;
        private Mock<IClipRepository> _clipRepositoryMock;
        private ClipService _sut;
        private Guid _userId;
        private Clip _firstClip;

        [TestInitialize]
        public void Init()
        {
            _userId = Guid.NewGuid();

            var layerId1 = Guid.NewGuid();
            var layerId2 = Guid.NewGuid();
            var layerId3 = Guid.NewGuid();
            var layer1 = new Layer { LayerId = layerId1 };
            _firstClip = new Clip
            {
                ClipId = 1,
                ClipName = "first",
                Layers = new List<Layer>
            {
                layer1
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
                    layer1,
                    new Layer{LayerId = layerId2 },
                    new Layer{LayerId = layerId3 },
                },
                BeatLength = 4,
                StartingBeat = 1
            };

            _videoRepositoryMock = new Mock<IVideoRepository>();
            _clipRepositoryMock = new Mock<IClipRepository>();
            _clipRepositoryMock.Setup(x => x.GetAllAsync(_userId)).ReturnsAsync(new List<Clip> { _firstClip, secondClip });
            _clipRepositoryMock.Setup(x => x.SaveAsync(_userId, _firstClip)).ReturnsAsync(_firstClip);
            _clipRepositoryMock.Setup(x => x.GetAsync(_userId, _firstClip.ClipId)).ReturnsAsync(new Clip());

            _sut = new ClipService(_clipRepositoryMock.Object, _videoRepositoryMock.Object);
        }

        [TestMethod]
        public async Task Save()
        {
            var result = await _sut.SaveAsync(_userId, _firstClip);

            Assert.AreEqual(_firstClip, result);

            _clipRepositoryMock.Verify(x => x.SaveAsync(_userId, _firstClip), Times.Once);
        }

        [TestMethod]
        public async Task SaveDuplicate()
        {
            _clipRepositoryMock.Setup(x => x.GetAsync(_userId, _firstClip.ClipId)).ReturnsAsync(_firstClip);

            var result = await _sut.SaveAsync(_userId, _firstClip);

            Assert.AreEqual(_firstClip, result);

            _clipRepositoryMock.Verify(x => x.SaveAsync(_userId, _firstClip), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task SaveBadBeatLength()
        {
            _firstClip.BeatLength = 3;
            _firstClip.StartingBeat = 3;
            await _sut.SaveAsync(_userId, _firstClip);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task SaveNoBackground()
        {
            _firstClip.Layers = null;
            await _sut.SaveAsync(_userId, _firstClip);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task SaveNoDatabaseClip()
        {
            _clipRepositoryMock.Setup(x => x.GetAsync(_userId, _firstClip.ClipId)).ReturnsAsync((Clip)null);
            await _sut.SaveAsync(_userId, _firstClip);
        }

        [TestMethod]
        public async Task Delete()
        {
            await _sut.DeleteAsync(_userId, _firstClip.ClipId);

            _clipRepositoryMock.Verify(x => x.DeleteAsync(_firstClip.ClipId), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task DeleteDependentVideo()
        {
            _videoRepositoryMock.Setup(x => x.GetAllAsync(_userId)).ReturnsAsync(new List<Video> { new Video { Clips = new List<Clip> { _firstClip } } });
            await _sut.DeleteAsync(_userId, _firstClip.ClipId);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task DeleteNoClip()
        {
            _clipRepositoryMock.Setup(x => x.GetAsync(_userId, _firstClip.ClipId)).ReturnsAsync((Clip)null);
            await _sut.DeleteAsync(_userId, _firstClip.ClipId);
        }
    }
}