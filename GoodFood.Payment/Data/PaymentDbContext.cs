using Microsoft.EntityFrameworkCore;
using GoodFood.Payment.Models;

namespace GoodFood.Payment.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<PaymentTransaction> Payments { get; set; }
}