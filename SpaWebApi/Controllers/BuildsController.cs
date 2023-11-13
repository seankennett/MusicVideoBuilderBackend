using BuildDataAccess.Entities;
using BuildEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using SpaWebApi.Extensions;
using SpaWebApi.Models;
using SpaWebApi.Services;

namespace SpaWebApi.Controllers
{
    [Authorize]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes")]
    [Route("api/Videos/{videoId}/[controller]")]
    [ApiController]
    public class BuildsController : ControllerBase
    {
        private readonly IBuildService _videoAssetService;
        public BuildsController(IBuildService videoAssetService)
        {
            _videoAssetService = videoAssetService;
        }

        [HttpGet]
        [Route("~/api/Videos/[controller]")]
        public async Task<IEnumerable<BuildAsset>> GetAllAssets()
        {
            return await _videoAssetService.GetAllAsync(User.GetUserObjectId());
        }

        [HttpPost]
        public async Task Post(int videoId, VideoBuildRequest videoBuildRequest)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception($"Audio blob invalid: {string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}");
            }

            await _videoAssetService.BuildFreeVideoAsync(User.GetUserObjectId(), videoId, videoBuildRequest);
        }

        [HttpPost("CreateAudioBlobUri")]
        public async Task<Uri> CreateAudioBlobUri(int videoId, VideoBuildRequest videoBuildRequest)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception($"Create audio blob invalid: {string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}");
            }

            return await _videoAssetService.CreateUserAudioBlobUri(User.GetUserObjectId(), videoId, videoBuildRequest);
        }

        [HttpPost("ValidateAudioBlob")]
        public async Task ValidateAudioBlob(int videoId, VideoBuildRequest videoBuildRequest)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception($"Validate audio blob invalid: {string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}");
            }

            await _videoAssetService.ValidateAudioBlob(User.GetUserObjectId(), videoId, videoBuildRequest);
        }

        [HttpPost("Checkout")]
        public async Task<string> Post(int videoId, PaymentIntentRequest paymentIntentRequest)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception($"PaymentIntentRequest model invalid: {string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}");
            }

            return await _videoAssetService.CreatePaymentIntent(User.GetUserObjectId(), videoId, paymentIntentRequest, User.GetEmail());
        }
    }
}
