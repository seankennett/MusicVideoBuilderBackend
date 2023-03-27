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
    [Route("api/Videos/{videoId}")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        private readonly IVideoAssetService _videoAssetService;
        public VideoController(IVideoAssetService videoAssetService)
        {
            _videoAssetService = videoAssetService;
        }

        [HttpPost("Assets")]
        public async Task<VideoAsset> Post(int videoId, VideoBuildRequest videoAssetRequest)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception($"Audio blob invalid: {string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}");
            }

            if (videoAssetRequest.Resolution != Resolution.Free)
            {
                throw new Exception("Resolution must be free to call this route");
            }

            return await _videoAssetService.BuildFreeVideoAsync(User.GetUserObjectId(), videoId, videoAssetRequest.BuildId);
        }

        [HttpPost("Assets/CreateAudioBlobUri")]
        public async Task<Uri> CreateAudioBlobUri(int videoId, VideoBuildRequest videoAssetRequest)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception($"Create audio blob invalid: {string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}");
            }

            return await _videoAssetService.CreateUserAudioBlobUri(User.GetUserObjectId(), videoId, videoAssetRequest.BuildId, videoAssetRequest.Resolution);
        }

        [HttpPost("Assets/ValidateAudioBlob")]
        public async Task ValidateAudioBlob(int videoId, VideoBuildRequest videoAssetRequest)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception($"Validate audio blob invalid: {string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}");
            }

            await _videoAssetService.ValidateAudioBlob(User.GetUserObjectId(), videoId, videoAssetRequest.BuildId, videoAssetRequest.Resolution);
        }

        [HttpPost("Checkout")]
        public async Task<string> Post(int videoId, PaymentIntentRequest paymentIntentRequest)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception($"PaymentIntentRequest model invalid: {string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}");
            }

            return await _videoAssetService.CreatePaymentIntent(User.GetUserObjectId(), videoId, paymentIntentRequest);
        }
    }
}
