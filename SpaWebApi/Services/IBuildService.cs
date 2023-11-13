using BuildDataAccess.Entities;
using BuildEntities;
using SpaWebApi.Models;

namespace SpaWebApi.Services
{
    public interface IBuildService
    {
        Task BuildFreeVideoAsync(Guid userObjectId, int videoId, VideoBuildRequest videoBuildRequest);
        Task<string> CreatePaymentIntent(Guid userObjectId, int videoId, PaymentIntentRequest paymentIntent, string email);
        Task<Uri> CreateUserAudioBlobUri(Guid userObjectId, int videoId, VideoBuildRequest videoBuildRequest);
        Task<IEnumerable<BuildAsset>> GetAllAsync(Guid userObjectId);
        Task ValidateAudioBlob(Guid userObjectId, int videoId, VideoBuildRequest videoBuildRequest);
    }
}