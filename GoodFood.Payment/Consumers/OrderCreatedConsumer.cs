using MassTransit;
using GoodFood.Payment.Data;
using GoodFood.Payment.Models;
using GoodFood.Payment.Contracts;

namespace GoodFood.Payment.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly PaymentDbContext _dbContext;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    // Injection de dépendance pour la DB et le Logger
    public OrderCreatedConsumer(PaymentDbContext dbContext, ILogger<OrderCreatedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var message = context.Message;
        _logger.LogInformation($"[RabbitMQ] Commande reçue : {message.OrderId} pour {message.TotalAmount}€");

        // Création de la transaction de paiement
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Amount = message.TotalAmount,
            Status = "Pending", // En attente de traitement bancaire
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"[Base de Données] Paiement enregistré pour la commande {message.OrderId}");
    }
}