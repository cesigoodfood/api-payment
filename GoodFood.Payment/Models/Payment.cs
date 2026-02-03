namespace GoodFood.Payment.Models;

public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Status { get; set; } = "Pending";// "Pending", "Success", "Refused"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}