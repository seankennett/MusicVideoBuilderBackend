using CollectionDataAccess.Services;
using CollectionEntities.Entities;
using Microsoft.Extensions.Options;
using SpaWebApi.Models;
using Stripe;
using UserSubscriptionAccess;
using UserSubscriptionAccess.Models;
using UserSubscriptionAccess.Repositories;

namespace SpaWebApi.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionProductService _subscriptionProductService;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly string _clientUrl;

        public SubscriptionService(ISubscriptionProductService subscriptionProductService, IUserSubscriptionRepository userSubscriptionRepository, IOptions<SpaWebApiConfig> config)
        {
            _subscriptionProductService = subscriptionProductService;
            _userSubscriptionRepository = userSubscriptionRepository;
            _clientUrl = config.Value.ClientUrl;
        }

        public async Task<string> CreateSubscriptionCheckoutSessionAsync(string? email, string priceId, Guid userObjectId)
        {
            var userSubscription = await _userSubscriptionRepository.GetAsync(userObjectId);
            string? customerId = null;
            if (userSubscription != null)
            {
                if (userSubscription.CanUserSubscriptionNotBeCreated())
                {
                    throw new Exception("User already has a subscription");
                }
                // case of existing canceled subscription and want a new one
                customerId = userSubscription.CustomerId;
                email = null;
            }

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                Mode = "subscription",
                CustomerEmail = email,
                Customer = customerId,
                SubscriptionData = new Stripe.Checkout.SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string> { { SubscriptionConstants.UserObjectIdMetaKey, userObjectId.ToString() } }
                },
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new Stripe.Checkout.SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    },
                },
                UiMode = "embedded",
                ReturnUrl = $"{_clientUrl}/subscriptionConfirmation?session_id={{CHECKOUT_SESSION_ID}}"             
            };
            var service = new Stripe.Checkout.SessionService();
            var session = await service.CreateAsync(options);
            return session.ClientSecret;
        }

        public async Task<SubscriptionProduct?> GetAsync(Guid userObjectId, bool isActive)
        {
            var userSubscription = await _userSubscriptionRepository.GetAsync(userObjectId);
            if (userSubscription != null && (isActive ? userSubscription.IsStatusActive() : userSubscription.CanUserSubscriptionNotBeCreated()))
            {
                var subscriptionProducts = await _subscriptionProductService.GetAllSubscriptionProductsAsync();
                return subscriptionProducts.FirstOrDefault(x => x.ProductId == userSubscription.ProductId);
            }

            return null;
        }

        public async Task<string> GetBillingPortalSessionUrl(Guid userObjectId)
        {
            var userSubscription = await _userSubscriptionRepository.GetAsync(userObjectId);
            if (userSubscription == null)
            {
                throw new Exception("User does not have subscription");
            }

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = userSubscription.CustomerId,
                ReturnUrl = _clientUrl,
            };
            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);
            return session.Url;
        }

        public async Task<string?> GetCheckoutSessionAsync(string sessionId, string email)
        {
            var service = new Stripe.Checkout.SessionService();
            var session = await service.GetAsync(sessionId);
            if (session == null || session.CustomerEmail != email)
            {
                throw new Exception("Invalid session Id");
            }

            return session.Status == "open" ? session.ClientSecret : null;
        }
    }
}
