using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using PublicDataApiFunction.Repositories;
using CollectionEntities.Entities;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

namespace PublicDataApiFunction
{
    public class PublicDataApiFunction
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly IMemoryCache _memoryCache;
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1); // Create a semaphore for synchronization
        private const string CacheKey = "Collections";
        public PublicDataApiFunction(ICollectionRepository collectionRepository, IMemoryCache memoryCache)
        {
            _collectionRepository = collectionRepository;
            _memoryCache = memoryCache;
        }

        [FunctionName("PublicDataApiFunction")]
        public async Task<IEnumerable<Collection>> Run([HttpTrigger(authLevel:AuthorizationLevel.Anonymous, Route = "Collections")] HttpRequest req)
        {
            // Try to get the item from the cache
            if (!_memoryCache.TryGetValue(CacheKey, out IEnumerable<Collection> collections))
            {
                // Acquire a lock to ensure only one request computes or fetches the value
                await _cacheLock.WaitAsync();
                try
                {
                    // Check again if the item was added to the cache by another request while waiting
                    if (!_memoryCache.TryGetValue(CacheKey, out collections))
                    {
                        // If the item is not in the cache, calculate or fetch it
                        collections = await _collectionRepository.GetAllCollectionsAsync();

                        // Set the item in the cache with a specified expiration time
                        var cacheEntryOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) // Adjust the expiration time as needed
                        };

                        _memoryCache.Set(CacheKey, collections, cacheEntryOptions);
                    }
                }
                finally
                {
                    // Release the lock when done
                    _cacheLock.Release();
                }
            }

            return collections;
        }
    }
}
