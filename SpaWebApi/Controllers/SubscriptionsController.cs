using CollectionEntities.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using SpaWebApi.Extensions;
using SpaWebApi.Models;
using SpaWebApi.Services;

namespace SpaWebApi.Controllers
{
    [Authorize]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes")]
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionsController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpPost]
        [Route("Checkout")]
        public async Task<string> Post(SubscriptionCheckoutRequest subscriptionCheckoutRequest)
        {
            return await _subscriptionService.CreateSubscriptionCheckoutSessionAsync(User.GetEmail(), subscriptionCheckoutRequest.PriceId, User.GetUserObjectId());
        }

        [HttpGet]
        [Route("BillingPortalSessionUrl")]
        public async Task<string> GetSessionUrl()
        {
            return await _subscriptionService.GetBillingPortalSessionUrl(User.GetUserObjectId());
        }

        [HttpGet]
        public async Task<SubscriptionProduct?> Get(bool isActive)
        {
            return await _subscriptionService.GetAsync(User.GetUserObjectId(), isActive);
        }

        [HttpGet]
        [Route("CheckoutSession")]
        public async Task<string?> Get(string sessionId)
        {
            return await _subscriptionService.GetCheckoutSessionAsync(sessionId, User.GetEmail());
        }
    }
}
