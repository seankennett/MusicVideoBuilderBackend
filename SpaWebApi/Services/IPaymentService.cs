using SharedEntities.Models;
using SpaWebApi.Models;
using Stripe;

namespace SpaWebApi.Services
{
    public interface IPaymentService
    {
        Task<string> CreatePaymentIntent(Video video, PaymentIntentRequest paymentIntent, Guid userObjectId);
        Task HandleStripeEvent(Event stripeEvent);
    }
}
