using BuildDataAccess.Entities;
using BuildEntities;
using SpaWebApi.Models;

namespace SpaWebApi.Services
{
    public interface IBuildService
    {
        Task BuildFreeVideoAsync(Guid userObjectId, int videoId, Guid buildId);
        Task<string> CreatePaymentIntent(Guid userObjectId, int videoId, PaymentIntentRequest paymentIntent);
        Task<Uri> CreateUserAudioBlobUri(Guid userObjectId, int videoId, Guid buildId, Resolution resolution);
        Task<IEnumerable<BuildAsset>> GetAllAsync(Guid userObjectId);
        Task ValidateAudioBlob(Guid userObjectId, int videoId, Guid buildId, Resolution resolution);
    }
}