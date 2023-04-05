using BuildDataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using SpaWebApi.Extensions;
using SpaWebApi.Services;
using VideoDataAccess.Entities;

namespace SpaWebApi.Controllers
{
    [Authorize]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes")]
    [Route("api/[controller]")]
    [ApiController]
    public class VideosController : ControllerBase
    {
        private readonly IVideoService _videoService;

        public VideosController(IVideoService videoService)
        {
            _videoService = videoService;
        }

        [HttpGet]
        public async Task<IEnumerable<Video>> Get()
        {
            return await _videoService.GetAllAsync(User.GetUserObjectId());
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
