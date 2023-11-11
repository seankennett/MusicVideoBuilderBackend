using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserSubscriptionAccess.Models;

namespace UserSubscriptionAccess.Repositories
{
    public interface IUserSubscriptionRepository
    {
        Task DeleteByCustomerIdAsync(string customerId);
        Task<UserSubscription?> GetAsync(Guid userObjectId);
        Task<UserSubscription> SaveAsync(UserSubscription userSubscription, Guid userObjectId);
    }
}
