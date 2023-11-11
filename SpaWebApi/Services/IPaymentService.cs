using BuildEntities;
using SpaWebApi.Models;
using VideoDataAccess.Entities;

namespace SpaWebApi.Services
{
    public interface IPaymentService
    {
        Task<string> CreatePaymentIntent(Video video, PaymentIntentRequest paymentIntent, Guid userObjectId);
        Task<int> GetVideoCost(Video video, Resolution resolution, License license, Guid userObjectId);
    }
}
