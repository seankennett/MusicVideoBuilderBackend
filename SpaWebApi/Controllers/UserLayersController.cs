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
    public class UserLayersController : ControllerBase
    {
        private readonly IUserLayerService _userLayerService;

        public UserLayersController(IUserLayerService userLayerService)
        {
            _userLayerService = userLayerService;
        }

        [HttpGet]
        public async Task<IEnumerable<UserLayer>> Get()
        {
            return await _userLayerService.GetAllAsync(User.GetUserObjectId());
        }
    }
}
