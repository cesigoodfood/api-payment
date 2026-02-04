using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoodFood.Payment.Data;

namespace GoodFood.Payment.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly PaymentDbContext _dbContext;

    public PaymentController(PaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("by-order/{orderId}")]
    public async Task<IActionResult> GetStatusByOrder(Guid orderId)
    {
        var payment = await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId);

        if (payment == null)
            return NotFound(new { Message = "Aucun paiement trouvé pour cette commande" });

        return Ok(new 
        { 
            payment.Id, 
            payment.Status, // Pending, Success, Failed
            payment.Amount,
            payment.CreatedAt 
        });
    }
}