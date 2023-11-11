using CollectionEntities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectionDataAccess.Services
{
    public interface ISubscriptionProductService
    {
        Task<IEnumerable<SubscriptionProduct>> GetAllSubscriptionProductsAsync();
    }
}
