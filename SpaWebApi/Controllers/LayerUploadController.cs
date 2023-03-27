using LayerDataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using SpaWebApi.Extensions;
using SpaWebApi.Models;
using SpaWebApi.Services;

namespace SpaWebApi.Controllers
{
    [Authorize(Policy = "AuthorRolePolicy")]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes")]
    [Route("api/[controller]")]
    [ApiController]
    public class LayerUploadController : ControllerBase
    {
        private readonly ILayerUploadService _layerUploadService;

        public LayerUploadController(ILayerUploadService layerUploadService)
        {
            _layerUploadService = layerUploadService;
        }

        [HttpPost("CreateContainer")]
        public async Task<LayerUploadContainer> CreateContainer(LayerUpload layerUpload)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception($"Layer upload invalid: {string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}");
            }

            return await _layerUploadService.CreateLayerUploadContainer();
        }

        [HttpPost()]
        public async Task Post(LayerUploadMessage layerUpload)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception($"Layer upload message invalid: {string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}");
            }

            await _layerUploadService.SendToImageProcessingFunction(User.GetUserObjectId(), layerUpload);
        }
    }
}
