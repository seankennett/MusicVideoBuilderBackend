using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SharedEntities.Models;

namespace SpaWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LayerFindersController : ControllerBase
    {
        private readonly ILayerRepository _layerRepository;
        private readonly IMemoryCache _memoryCache;

        private const string GetAllCacheKey = "GetAllLayers";

        public LayerFindersController(ILayerRepository layerRepository, IMemoryCache memoryCache)
        {
            _layerRepository = layerRepository;
            _memoryCache = memoryCache;
        }

        //5 minutes = 5*60 seconds = 300 seconds for browser cache.  Can't use middleware as it'll mess up authentication
        [HttpGet]
        [ResponseCache(Duration = 300)]
        public async Task<IEnumerable<LayerFinder>> Get()
        {
            IEnumerable<LayerFinder> layers;
            if (_memoryCache.TryGetValue(GetAllCacheKey, out layers))
            {
                return layers;
            }

            layers = await _layerRepository.GetAllLayerFinderAsync();
            _memoryCache.Set(GetAllCacheKey, layers, TimeSpan.FromMinutes(5));

            return layers;
        }
    }
}
