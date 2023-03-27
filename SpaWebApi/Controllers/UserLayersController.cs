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
    public class UserLayersController : ControllerBase
    {
        private readonly IUserLayerRepository _userLayerRepository;

        public UserLayersController(IUserLayerRepository userLayerRepository)
        {
            _userLayerRepository = userLayerRepository;
        }

        [HttpGet]
        public async Task<IEnumerable<UserLayer>> Get()
        {
            return await _userLayerRepository.GetAllAsync(User.GetUserObjectId());
        }
    }
}
