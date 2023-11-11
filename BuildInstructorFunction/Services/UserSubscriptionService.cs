using CollectionDataAccess.Services;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserSubscriptionAccess;
using UserSubscriptionAccess.Models;
using UserSubscriptionAccess.Repositories;

namespace BuildInstructorFunction.Services
{
    public class UserSubscriptionService : IUserSubscriptionService
    {
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly ISubscriptionProductService _subscriptionProductService;

        public UserSubscriptionService(IUserSubscriptionRepository userSubscriptionRepository, ISubscriptionProductService subscriptionProductService) 
        {
            _userSubscriptionRepository = userSubscriptionRepository;
            _subscriptionProductService = subscriptionProductService;
        }

        public async Task DeleteUserSubscriptionAsync(string customerId)
        {
            await _userSubscriptionRepository.DeleteByCustomerIdAsync(customerId);
        }

        public async Task UpdateUserSubscriptionAsync(Subscription subscription)
        {
                var userObjectId = Guid.Parse(subscription.Metadata[SubscriptionConstants.UserObjectIdMetaKey]);
                var subscriptionProducts = await _subscriptionProductService.GetAllSubscriptionProductsAsync();
                var subscirptionProduct = subscriptionProducts.First(x => x.PriceId == subscription.Items.First().Price.Id);
                var userSubscription = new UserSubscription
                {
                    CustomerId = subscription.CustomerId,
                    SubscriptionId = subscription.Id,
                    SubscriptionStatus = subscription.Status,
                    ProductId = subscirptionProduct.ProductId
                };

                await _userSubscriptionRepository.SaveAsync(userSubscription, userObjectId);
        }
    }
}
