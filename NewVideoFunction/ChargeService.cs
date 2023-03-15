using Microsoft.Extensions.Logging;
using NewVideoFunction.Interfaces;
using Stripe;
using System;
using System.Threading.Tasks;

namespace NewVideoFunction
{
    public class ChargeService : IChargeService
    {
        private readonly ILogger<ChargeService> _logger;

        public ChargeService(ILogger<ChargeService> logger) 
        {
            _logger = logger;
        }
        public async Task<bool> Charge(string paymentIntentId)
        {
            try
            {
                var paymentIntentService = new PaymentIntentService();
                await paymentIntentService.CaptureAsync(paymentIntentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Capture request failed, may need manual intervention");
                return false;
            }
        }
    }
}
