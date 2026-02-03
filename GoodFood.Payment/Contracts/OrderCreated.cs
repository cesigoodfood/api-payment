namespace GoodFood.Payment.Contracts;

public record OrderCreated
{
    public Guid OrderId { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
}