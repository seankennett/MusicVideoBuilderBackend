using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Stripe;
using Microsoft.Extensions.Options;
using BuildInstructorFunction.Services;
using Newtonsoft.Json;

namespace BuildInstructorFunction
{
    public class BuildInstructorFunction
    {
        private readonly string _stripeWebhookKey;
        private readonly IBuildService _buildService;
        private readonly IUserSubscriptionService _subscriptionService;

        public BuildInstructorFunction(IOptions<InstructorConfig> config, IBuildService buildService, IUserSubscriptionService subscriptionService)
        {
            _stripeWebhookKey = config.Value.StripeWebhookKey;
            _buildService = buildService;
            _subscriptionService = subscriptionService;
        }

        [FunctionName("BuildInstructorFunction")]
        public async Task RunQueue([QueueTrigger("%QueueName%", Connection = "ConnectionString")] UserBuild userBuild)
        {
            await _buildService.InstructBuildAsync(userBuild);
        }

        [FunctionName("BuildInstructorFunctionEndpoint")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest request, ILogger log)
        {
            var json = await new StreamReader(request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ParseEvent(json);
                var signatureHeader = request.Headers["Stripe-Signature"];

                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _stripeWebhookKey);
                if (stripeEvent.Type == Events.PaymentIntentSucceeded)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    log.LogInformation($"PaymentIntent {paymentIntent.Id} sucessful for amount {paymentIntent.Amount}");
                }
                else if (stripeEvent.Type == Events.PaymentIntentAmountCapturableUpdated) // card on hold so can crack on with building video
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    await _buildService.InstructBuildAsync(paymentIntent.Id);
                }
                else if (stripeEvent.Type == Events.PaymentIntentPaymentFailed)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    log.LogWarning($"PaymentIntent {paymentIntent.Id} failed");
                }
                else if (stripeEvent.Type == Events.CustomerSubscriptionCreated)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    await _subscriptionService.UpdateUserSubscriptionAsync(subscription);

                }
                else if (stripeEvent.Type == Events.CustomerSubscriptionUpdated)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    await _subscriptionService.UpdateUserSubscriptionAsync(subscription);
                }
                else if (stripeEvent.Type == Events.CustomerSubscriptionDeleted)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    await _subscriptionService.UpdateUserSubscriptionAsync(subscription);
                }
                else if (stripeEvent.Type == Events.CustomerDeleted)
                {
                    var customer = stripeEvent.Data.Object as Customer;
                    await _subscriptionService.DeleteUserSubscriptionAsync(customer.Id);
                }
                else
                {
                    log.LogInformation($"Unknown stripe event json: {stripeEvent.Type}");
                }

                return new OkResult();
            }
            catch (StripeException e)
            {
                log.LogError(e, e.Message);
                return new BadRequestResult();
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                throw;
            }
        }
    }
}
