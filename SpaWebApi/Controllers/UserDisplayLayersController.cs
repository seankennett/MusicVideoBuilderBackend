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
    public class UserCollectionsController : ControllerBase
    {
        private readonly IUserCollectionRepository _userCollectionRepository;

        public UserCollectionsController(IUserCollectionRepository userCollectionRepository)
        {
            _userCollectionRepository = userCollectionRepository;
        }

        [HttpGet]
        public async Task<IEnumerable<UserCollection>> Get()
        {
            return await _userCollectionRepository.GetAllAsync(User.GetUserObjectId());
        }
    }
}
