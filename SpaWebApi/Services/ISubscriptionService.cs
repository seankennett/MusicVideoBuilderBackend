using CollectionEntities.Entities;
using SpaWebApi.Models;

namespace SpaWebApi.Services
{
    public interface ISubscriptionService
    {
        Task<string> CreateSubscriptionCheckoutSessionAsync(string? email, string priceId, Guid userObjectId);
        Task<SubscriptionProduct?> GetAsync(Guid userObjectId, bool isActive);
        Task<string> GetBillingPortalSessionUrl(Guid userObjectId);
        Task<string?> GetCheckoutSessionAsync(string sessionId, string email);
    }
}