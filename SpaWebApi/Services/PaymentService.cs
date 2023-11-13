using BuildDataAccess.Entities;
using BuildDataAccess.Repositories;
using BuildEntities;
using CollectionDataAccess.Services;
using SharedEntities.Models;
using SpaWebApi.Models;
using SpaWebApi.Services;
using Stripe;
using UserSubscriptionAccess;
using UserSubscriptionAccess.Repositories;
using VideoDataAccess.Entities;
using VideoDataAccess.Repositories;

public class PaymentService : IPaymentService
{
    private readonly IUserCollectionRepository _userCollectionRepository;
    private readonly IBuildRepository _buildRepository;
    private readonly ICollectionService _collectionService;
    private readonly IClipRepository _clipRepository;
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;

    public PaymentService(IUserCollectionRepository userCollectionRepository, IBuildRepository buildRepository, ICollectionService collectionService, IClipRepository clipRepository, IUserSubscriptionRepository userSubscriptionRepository)
    {
        _userCollectionRepository = userCollectionRepository;
        _buildRepository = buildRepository;
        _collectionService = collectionService;
        _clipRepository = clipRepository;
        _userSubscriptionRepository = userSubscriptionRepository;
    }

    public async Task<string> CreatePaymentIntent(Video video, PaymentIntentRequest paymentIntentRequest, Guid userObjectId, string email)
    {
        int serverCalculatedCost = await GetVideoCost(video, paymentIntentRequest.Resolution, paymentIntentRequest.License, userObjectId);
        if (serverCalculatedCost != paymentIntentRequest.Cost)
        {
            throw new Exception($"Client cost {paymentIntentRequest.Cost} not the same as server cost {serverCalculatedCost} for video {video.VideoId}");
        }

        var paymentIntentService = new PaymentIntentService();
        var paymentIntentCreateOptions = new PaymentIntentCreateOptions
        {
            Amount = paymentIntentRequest.Cost * 100,
            Currency = "gbp",
            CaptureMethod = "manual",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
            ReceiptEmail = email
        };

        var paymentIntent = await paymentIntentService.CreateAsync(paymentIntentCreateOptions);
        await _buildRepository.SaveAsync(new Build
        {
            BuildId = paymentIntentRequest.BuildId,
            BuildStatus = BuildStatus.PaymentAuthorisationPending,
            HasAudio = false,
            License = paymentIntentRequest.License,
            Resolution = paymentIntentRequest.Resolution,
            PaymentIntentId = paymentIntent.Id,
            VideoId = video.VideoId,
            VideoName = video.VideoName,
            Format = video.Format
        }, userObjectId);

        return paymentIntent.ClientSecret;
    }

    public async Task<int> GetVideoCost(Video video, Resolution resolution, License license, Guid userObjectId)
    {
        var userCollections = await _userCollectionRepository.GetAllAsync(userObjectId);
        var collections = await _collectionService.GetAllCollectionsAsync();
        var clips = await _clipRepository.GetAllByVideoIdAsync(userObjectId, video.VideoId);
        var userSubscription = await _userSubscriptionRepository.GetAsync(userObjectId);

        var allDisplayLayers = clips.Where(c => c.ClipDisplayLayers != null).SelectMany(c => c.ClipDisplayLayers).Select(c => c.DisplayLayerId).Distinct();
        var uniqueVideoCollectionIds = collections.Where(c => c.DisplayLayers.Any(d => allDisplayLayers.Contains(d.DisplayLayerId))).Select(c => c.CollectionId);
        int serverCalculatedCost = GetVideoCost(uniqueVideoCollectionIds, userCollections, resolution, license, userSubscription);
        return serverCalculatedCost;
    }

    private int GetVideoCost(IEnumerable<Guid> uniqueCollections, IEnumerable<UserCollection> userCollections, Resolution resolution, License license, UserSubscriptionAccess.Models.UserSubscription? userSubscription)
    {
        var resolutionLicensedUserCollections = userCollections.Where(u => u.Resolution == resolution && u.License == license);
        return GetBuildCost(resolution, userSubscription) + GetLayerLicenseCost(uniqueCollections.Except(resolutionLicensedUserCollections.Select(x => x.CollectionId)).Count(), license, resolution, userSubscription);
    }

    private int GetLayerLicenseCost(int numberOfLicenses, License license, Resolution resolution, UserSubscriptionAccess.Models.UserSubscription? userSubscription)
    {
        if (userSubscription != null && userSubscription.IsStatusActive() && (userSubscription.ProductId == SubscriptionProducts.License || userSubscription.ProductId == SubscriptionProducts.LicenseBuilder))
        {
            return 0;
        }

        var licenseFactor = 0;
        switch (license)
        {
            case License.Standard:
                licenseFactor = 1;
                break;
            case License.Enhanced:
                licenseFactor = 3;
                break;
        }

        var layerResolutionCost = 0;
        switch (resolution)
        {
            case Resolution.Hd:
                layerResolutionCost = 25;
                break;
            case Resolution.FourK:
                layerResolutionCost = 50;
                break;
        }

        return licenseFactor * layerResolutionCost * numberOfLicenses;
    }

    private int GetBuildCost(Resolution resolution, UserSubscriptionAccess.Models.UserSubscription? userSubscription)
    {
        if (userSubscription != null && userSubscription.IsStatusActive() && (userSubscription.ProductId == SubscriptionProducts.Builder || userSubscription.ProductId == SubscriptionProducts.LicenseBuilder))
        {
            return 0;
        }

        return ((int)resolution - 1) * 5;
    }
}