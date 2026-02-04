using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GoodFood.Payment.Data;
using GoodFood.Payment.Models;

namespace GoodFood.Payment.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly PaymentDbContext _dbContext;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(PaymentDbContext dbContext, ILogger<PaymentController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("by-order/{orderId}")]
    public async Task<IActionResult> GetStatusByOrder(Guid orderId)
    {
        var payment = await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId);

        if (payment == null)
            return NotFound(new { Message = "Paiement introuvable pour cette commande." });

        return Ok(new
        {
            payment.Id,
            payment.Status, // "Pending", "Success", "Failed"
            payment.Amount,
            payment.Currency,
            payment.CreatedAt
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetPaymentHistory([FromQuery] DateTime? dateInfo)
    {
        // Si aucune date n'est fournie, on prend celles du jour
        var targetDate = dateInfo?.Date ?? DateTime.UtcNow.Date;

        var payments = await _dbContext.Payments
            .Where(p => p.CreatedAt.Date == targetDate)
            .ToListAsync();

        return Ok(payments);
    }

    [HttpPost("{id}/refund")]
    public async Task<IActionResult> RefundPayment(Guid id)
    {
        var payment = await _dbContext.Payments.FindAsync(id);

        if (payment == null) 
            return NotFound();

        if (payment.Status != "Success") 
            return BadRequest("Seuls les paiements validés peuvent être remboursés.");

        payment.Status = "Refunded";
        
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Paiement {Id} remboursé manuellement par l'admin.", id);

        return Ok(new { Message = "Remboursement effectué avec succès." });
    }
}