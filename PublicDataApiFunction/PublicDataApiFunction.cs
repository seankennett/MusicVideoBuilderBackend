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
using System.Linq;
using System.Text.Json;
using System.IO;

namespace PublicDataApiFunction
{
    public class PublicDataApiFunction
    {
        private readonly ICollectionRepository _collectionRepository;
        private readonly IMemoryCache _memoryCache;
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1); // Create a semaphore for synchronization
        private const string CollectionsCacheKey = "Collections";
        private const string SubscriptionsCacheKey = "Subscriptions";
        public PublicDataApiFunction(ICollectionRepository collectionRepository, IMemoryCache memoryCache)
        {
            _collectionRepository = collectionRepository;
            _memoryCache = memoryCache;
        }

        [FunctionName("CollectionsFunction")]
        public async Task<IEnumerable<Collection>> RunCollections([HttpTrigger(authLevel:AuthorizationLevel.Anonymous, Route = "Collections")] HttpRequest req, Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            //var collections = await _collectionRepository.GetAllCollectionsAsync();
            var collections = JsonSerializer.Deserialize<IEnumerable<Collection>>(File.ReadAllText(Path.GetFullPath(Path.Combine(context.FunctionDirectory, $"..{Path.DirectorySeparatorChar}collectionData.json"))));

            return collections;
        }        
    }
}
