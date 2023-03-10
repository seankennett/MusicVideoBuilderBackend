using DataAccessLayer.Repositories;
using SharedEntities.Models;
using SpaWebApi.Models;
using SpaWebApi.Services;
using Stripe;

public class PaymentService : IPaymentService
{
    private readonly IUserLayerService _userLayerService;
    private readonly IBuildRepository _buildRepository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IUserLayerService userLayerService, IBuildRepository buildRepository, ILogger<PaymentService> logger)
    {
        _userLayerService = userLayerService;
        _buildRepository = buildRepository;
        _logger = logger;
    }

    public async Task<string> CreatePaymentIntent(Video video, PaymentIntentRequest paymentIntentRequest, Guid userObjectId)
    {
        var userLayers = await _userLayerService.GetAllAsync(userObjectId);
        int serverCalculatedCost = GetVideoCost(video.Clips.Where(c => c.Layers != null).SelectMany(c => c.Layers).Select(l => l.LayerId).Distinct(), userLayers, paymentIntentRequest.Resolution, paymentIntentRequest.License);
        if (serverCalculatedCost != paymentIntentRequest.Cost) {
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
            }
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
            VideoId = video.VideoId 
        }, userObjectId);

        return paymentIntent.ClientSecret;
    }

    private int GetVideoCost(IEnumerable<Guid> uniqueVideoLayers, IEnumerable<UserLayer> userLayers, Resolution resolution, License license)
    {
        var resolutionLicensedUserLayers = userLayers.Where(u => u.Resolution == resolution && u.License == license);
        return GetBuildCost(resolution) + GetLayerLicenseCost(uniqueVideoLayers.Except(resolutionLicensedUserLayers.Select(x => x.LayerId)).Count(), license, resolution);
    }

    private int GetLayerLicenseCost(int numberOfLicenses, License license, Resolution resolution)
    {
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

    private int GetBuildCost(Resolution resolution)
    {
        return ((int)resolution - 1) * 5;
    }

    public async Task HandleStripeEvent(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (stripeEvent.Type == Events.PaymentIntentSucceeded)
        {
            _logger.LogInformation($"PaymentIntent {paymentIntent.Id} sucessful for amount {paymentIntent?.Amount}");
        }
        else if (stripeEvent.Type == Events.PaymentIntentAmountCapturableUpdated) // card on hold so can crack on with building video
        {
            UserBuild? build = await _buildRepository.GetByPaymentIntentId(paymentIntent.Id);
            if (build == null)
            {
                throw new Exception($"payment intent {paymentIntent.Id} not in build table");
            }
            
            build.BuildStatus = BuildStatus.BuildingPending;
            await _buildRepository.SaveAsync(build, build.UserObjectId);
            // save build status then send onto build conductor
        }
        else if (stripeEvent.Type == Events.PaymentIntentPaymentFailed)
        {
            _logger.LogWarning($"PaymentIntent {paymentIntent.Id} failed");
        }
        else
        {
            _logger.LogInformation($"Unknown stripe event json: {stripeEvent.ToJson()}");
        }
    }
}