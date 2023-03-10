using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using SharedEntities.Models;
using SpaWebApi.Extensions;
using SpaWebApi.Models;
using SpaWebApi.Services;

namespace SpaWebApi.Controllers
{
    [Authorize]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes")]
    [Route("api/[controller]")]
    [ApiController]
    public class VideosController : ControllerBase
    {
        private readonly IVideoService _videoService;
        private readonly IVideoAssetService _videoAssetService;

        public VideosController(IVideoService videoService, IVideoAssetService videoAssetService)
        {
            _videoService = videoService;
            _videoAssetService = videoAssetService;
        }

        [HttpGet]
        public async Task<IEnumerable<Video>> Get()
        {
            return await _videoService.GetAllAsync(User.GetUserObjectId());
        }

        [HttpGet]
        [Route("Assets")]
        public async Task<IEnumerable<VideoAsset>> GetAllAssets()
        {
            return await _videoAssetService.GetAllAsync(User.GetUserObjectId());
        }

        [HttpPost]
        public async Task<Video> Post(Video video)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception($"Video model invalid: {string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}");
            }

            return await _videoService.SaveAsync(User.GetUserObjectId(), video);
        }

        [HttpDelete]
        [Route("{videoId}")]
        public async Task Delete(int videoId)
        {
            await _videoService.DeleteAsync(User.GetUserObjectId(), videoId);
        }
    }
}
