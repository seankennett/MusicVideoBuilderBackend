using CollectionEntities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace CollectionDataAccess.Services
{
    public class SubscriptionProductService : ISubscriptionProductService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SubscriptionProductService(IHttpClientFactory httpClientFactory) 
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<SubscriptionProduct>> GetAllSubscriptionProductsAsync()
        {
            var httpClient = _httpClientFactory.CreateClient("PublicApi");
            var subscriptionProducts = await httpClient.GetFromJsonAsync<IEnumerable<SubscriptionProduct>>("api/Subscriptions");
            return subscriptionProducts ?? new List<SubscriptionProduct>();
        }
    }
}
