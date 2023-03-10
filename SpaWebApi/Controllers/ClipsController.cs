using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using SharedEntities.Models;
using SpaWebApi.Extensions;
using SpaWebApi.Services;

namespace SpaWebApi.Controllers
{
    [Authorize]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes")]
    [Route("api/[controller]")]
    [ApiController]
    public class ClipsController : ControllerBase
    {
        private readonly IClipService _clipService;

        public ClipsController(IClipService clipService)
        {
            _clipService = clipService;
        }

        [HttpGet]
        public async Task<IEnumerable<Clip>> Get()
        {
            return await _clipService.GetAllAsync(User.GetUserObjectId());
        }

        [HttpPost]
        public async Task<Clip> Post(Clip clip)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception($"Clip model invalid: {string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}");
            }

            return await _clipService.SaveAsync(User.GetUserObjectId(), clip);
        }

        [HttpDelete]
        [Route("{clipId}")]
        public async Task Delete(int clipId)
        {
            await _clipService.DeleteAsync(User.GetUserObjectId(), clipId);
        }
    }
}
