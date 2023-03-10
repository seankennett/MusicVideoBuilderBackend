using SharedEntities.Models;
using SpaWebApi.Models;

namespace SpaWebApi.Services
{
    public interface IVideoAssetService
    {
        Task<VideoAsset> BuildFreeVideoAsync(Guid userObjectId, int videoId, Guid buildId);
        Task<string> CreatePaymentIntent(Guid userObjectId, int videoId, PaymentIntentRequest paymentIntent);
        Task<Uri> CreateUserAudioBlobUri(Guid userObjectId, int videoId, Guid buildId, Resolution resolution);
        Task<IEnumerable<VideoAsset>> GetAllAsync(Guid userObjectId);
    }
}