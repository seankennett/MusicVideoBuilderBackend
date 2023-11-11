using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildInstructorFunction.Services
{
    public interface IUserSubscriptionService
    {
        Task DeleteUserSubscriptionAsync(string customerId);
        Task UpdateUserSubscriptionAsync(Subscription subscription);
    }
}
