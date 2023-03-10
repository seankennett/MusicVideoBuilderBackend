using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SpaWebApi.Services;
using Stripe;

namespace SpaWebApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly string _stripeWebhookKey;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IOptions<Connections> connections, IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _stripeWebhookKey = connections.Value.StripeWebhookKey;
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ParseEvent(json);
                var signatureHeader = Request.Headers["Stripe-Signature"];

                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _stripeWebhookKey);
                await _paymentService.HandleStripeEvent(stripeEvent);

                return Ok();
            }
            catch (StripeException e)
            {
                _logger.LogError(e, e.Message);
                return BadRequest();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return StatusCode(500);
            }
        }
    }
}
