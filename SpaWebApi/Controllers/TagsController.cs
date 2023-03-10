using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using SharedEntities.Models;

namespace SpaWebApi.Controllers
{
    [Authorize(Policy = "AuthorRolePolicy")]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes")]
    [Route("api/[controller]")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly ITagRepository _tagsRepository;

        public TagsController(ITagRepository tagsRepository)
        {
            _tagsRepository = tagsRepository;
        }
        [HttpGet]
        public IEnumerable<Tag> Get()
        {
            return _tagsRepository.GetAll();
        }
    }
}
