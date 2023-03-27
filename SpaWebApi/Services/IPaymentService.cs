using SpaWebApi.Models;
using VideoDataAccess.Entities;

namespace SpaWebApi.Services
{
    public interface IPaymentService
    {
        Task<string> CreatePaymentIntent(Video video, PaymentIntentRequest paymentIntent, Guid userObjectId);
    }
}
