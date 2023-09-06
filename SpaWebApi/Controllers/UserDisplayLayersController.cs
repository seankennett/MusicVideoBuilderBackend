using BuildDataAccess.Entities;
using BuildDataAccess.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using SpaWebApi.Extensions;

namespace SpaWebApi.Controllers
{
    [Authorize]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserDisplayLayersController : ControllerBase
    {
        private readonly IUserDisplayLayerRepository _userDisplayLayerRepository;

        public UserDisplayLayersController(IUserDisplayLayerRepository userDisplayLayerRepository)
        {
            _userDisplayLayerRepository = userDisplayLayerRepository;
        }

        [HttpGet]
        public async Task<IEnumerable<UserDisplayLayer>> Get()
        {
            return await _userDisplayLayerRepository.GetAllAsync(User.GetUserObjectId());
        }
    }
}
