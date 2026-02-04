namespace GoodFood.Payment.Contracts;

public record PaymentSucceeded
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public DateTime ProcessedAt { get; init; }
}