using LayerDataAccess.Entities;
using LayerDataAccess.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace SpaWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CollectionsController : ControllerBase
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly IMemoryCache _memoryCache;

        private const string GetAllCacheKey = "GetAllCollections";

        public CollectionsController(ICollectionRepository collectionRepository, IMemoryCache memoryCache)
        {
            _collectionRepository = collectionRepository;
            _memoryCache = memoryCache;
        }

        //5 minutes = 5*60 seconds = 300 seconds for browser cache.  Can't use middleware as it'll mess up authentication
        [HttpGet]
        [ResponseCache(Duration = 300)]
        public async Task<IEnumerable<Collection>> Get()
        {
            IEnumerable<Collection> collections;
            if (_memoryCache.TryGetValue(GetAllCacheKey, out collections))
            {
                return collections;
            }

            collections = await _collectionRepository.GetAllCollectionsAsync();
            _memoryCache.Set(GetAllCacheKey, collections, TimeSpan.FromMinutes(5));

            return collections;
        }
    }
}
