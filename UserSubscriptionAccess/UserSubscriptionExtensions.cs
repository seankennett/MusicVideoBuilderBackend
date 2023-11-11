using Stripe;
using UserSubscriptionAccess.Models;

namespace UserSubscriptionAccess
{
    public static class UserCollectionExtensions
    {
        public static bool CanUserSubscriptionNotBeCreated(this UserSubscription userSubscription)
        {
            return userSubscription.SubscriptionStatus != SubscriptionStatuses.Canceled && userSubscription.SubscriptionStatus != SubscriptionStatuses.IncompleteExpired;
        }

        public static bool IsStatusActive(this UserSubscription userSubscription)
        {
            return userSubscription.CanUserSubscriptionNotBeCreated() && userSubscription.SubscriptionStatus != SubscriptionStatuses.Incomplete && userSubscription.SubscriptionStatus != SubscriptionStatuses.Unpaid;
        }
    }
}
