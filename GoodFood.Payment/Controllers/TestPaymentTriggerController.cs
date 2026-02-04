using Microsoft.AspNetCore.Mvc;
using MassTransit;
using GoodFood.Payment.Contracts;

namespace GoodFood.Payment.Controllers;

[ApiController]
[Route("api/test")]
public class TestPaymentTriggerController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;

    public TestPaymentTriggerController(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    // POST api/test/trigger-payment
    [HttpPost("trigger-payment")]
    public async Task<IActionResult> TriggerFakeOrder()
    {
        var fakeOrderId = Guid.NewGuid();
        
        // On simule ce que le service Commande ferait
        await _publishEndpoint.Publish(new OrderCreated
        {
            OrderId = fakeOrderId,
            TotalAmount = 42.50m, // Un montant au hasard
            CreatedAt = DateTime.UtcNow
        });

        return Ok(new { Message = $"Message envoyé pour la commande {fakeOrderId}" });
    }
}