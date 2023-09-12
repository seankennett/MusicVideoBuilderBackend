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
    public class CollectionService : ICollectionService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CollectionService(IHttpClientFactory httpClientFactory) 
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<Collection>> GetAllCollectionsAsync()
        {
            var httpClient = _httpClientFactory.CreateClient("PublicApi");
            var collections = await httpClient.GetFromJsonAsync<IEnumerable<Collection>>("Collections");
            return collections ?? new List<Collection>();
        }
    }
}
