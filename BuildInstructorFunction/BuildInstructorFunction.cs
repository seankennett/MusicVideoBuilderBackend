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

namespace BuildInstructorFunction
{
    public class BuildInstructorFunction
    {
        private readonly string _stripeWebhookKey;
        private readonly IBuildService _buildService;

        public BuildInstructorFunction(IOptions<InstructorConfig> config, IBuildService buildService)
        {
            _stripeWebhookKey = config.Value.StripeWebhookKey;
            _buildService = buildService;
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
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (stripeEvent.Type == Events.PaymentIntentSucceeded)
                {
                    log.LogInformation($"PaymentIntent {paymentIntent.Id} sucessful for amount {paymentIntent?.Amount}");
                }
                else if (stripeEvent.Type == Events.PaymentIntentAmountCapturableUpdated) // card on hold so can crack on with building video
                {
                    await _buildService.InstructBuildAsync(paymentIntent.Id);
                }
                else if (stripeEvent.Type == Events.PaymentIntentPaymentFailed)
                {
                    log.LogWarning($"PaymentIntent {paymentIntent.Id} failed");
                }
                else
                {
                    log.LogInformation($"Unknown stripe event json: {stripeEvent.ToJson()}");
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
