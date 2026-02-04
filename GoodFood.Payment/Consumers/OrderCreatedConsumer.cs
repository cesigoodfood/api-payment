using MassTransit;
using GoodFood.Payment.Data;
using GoodFood.Payment.Models;
using GoodFood.Payment.Contracts;
using Microsoft.EntityFrameworkCore; // Nécessaire pour mettre à jour la DB

namespace GoodFood.Payment.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly PaymentDbContext _dbContext;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(PaymentDbContext dbContext, ILogger<OrderCreatedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;

        _logger.LogInformation("[RabbitMQ] Traitement de la commande {OrderId}...", message.OrderId);

        // 1. Création initiale (Pending)
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Amount = message.TotalAmount,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        // 2. Simuler le traitement du paiement
        await Task.Delay(1000); 

        // 3. Valider ou refuser le paiement (ici, on valide si le montant est > 0)
        if (message.TotalAmount > 0)
        {
            payment.Status = "Success";
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Paiement validé en base pour {OrderId}", message.OrderId);

            await context.Publish(new PaymentSucceeded
            {
                OrderId = message.OrderId,
                PaymentId = payment.Id,
                ProcessedAt = DateTime.UtcNow
            });

            _logger.LogInformation("--> Événement PaymentSucceeded envoyé !");
        }
        else
        {
            payment.Status = "Failed";
            await _dbContext.SaveChangesAsync();
            _logger.LogWarning("Paiement refusé (montant invalide)");
        }
    }
}