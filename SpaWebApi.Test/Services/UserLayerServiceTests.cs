//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Moq;
//using MusicVideoBuilderWebApi.Models;
//using MusicVideoBuilderWebApi.Repositories;
//using MusicVideoBuilderWebApi.Services;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace MusicVideoBuilderWebApi.Test.Service
//{
//    [TestClass]
//    public class UserLayerServiceTests
//    {
//        private Mock<IUserLayerRepository> _userLayerRepositoryMock;
//        private Mock<IClipRepository> _clipRepositoryMock;
//        private UserLayerService _sut;
//        private Guid _userId;
//        private int _userLayerId;
//        private UserLayerDTO _userLayer;
//        private Clip _firstClip;

//        [TestInitialize]
//        public void Init()
//        {
//            _userId = Guid.NewGuid();
//            _userLayerId = 1;

//            var layer1 = Guid.NewGuid();
//            var layer2 = Guid.NewGuid();
//            var layer3 = Guid.NewGuid();
//            _userLayer = new UserLayerDTO { LayerId = layer1, UserLayerId = _userLayerId };
//            _firstClip = new Clip
//            {
//                ClipId = 1,
//                ClipName = "first",
//                UserLayers = new List<UserLayer>
//            {
//                _userLayer
//            },
//                BeatLength = 4,
//                StartingBeat = 1
//            };

//            _userLayerRepositoryMock = new Mock<IUserLayerRepository>();
//            _clipRepositoryMock = new Mock<IClipRepository>();

//            _userLayerRepositoryMock.Setup(x => x.GetAsync(_userId, _userLayerId)).ReturnsAsync(_userLayer);

//            _sut = new UserLayerService(_userLayerRepositoryMock.Object, _clipRepositoryMock.Object);
//        }

//        [TestMethod]
//        public async Task Delete()
//        {
//            await _sut.DeleteAsync(_userId, _userLayerId);

//            _userLayerRepositoryMock.Verify(x => x.DeleteAsync(_userLayerId), Times.Once);
//        }

//        [TestMethod]
//        [ExpectedException(typeof(Exception))]
//        public async Task DeleteHasClips()
//        {
//            _clipRepositoryMock.Setup(x => x.GetAllAsync(_userId)).ReturnsAsync(new List<Clip> { _firstClip });

//            await _sut.DeleteAsync(_userId, _userLayerId);
//        }

//        [TestMethod]
//        [ExpectedException(typeof(Exception))]
//        public async Task DeleteNoVideo()
//        {
//            _userLayerRepositoryMock.Setup(x => x.GetAsync(_userId, _userLayerId)).ReturnsAsync((UserLayerDTO)null);

//            await _sut.DeleteAsync(_userId, _userLayerId);
//        }
//    }
//}