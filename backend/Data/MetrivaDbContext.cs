using Microsoft.EntityFrameworkCore;
using Metriva.Features.Customers;
using Metriva.Features.FinancingApplications;

namespace Metriva.Data;

public class MetrivaDbContext : DbContext
{
    public MetrivaDbContext(
        DbContextOptions<MetrivaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<FinancingApplications> FinancingApplications =>
        Set<FinancingApplications>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FinancingApplications>(entity =>
        {
            entity.Property(f => f.PurchasePrice).HasPrecision(18, 2);
            entity.Property(f => f.DownPayment).HasPrecision(18, 2);
            entity.Property(f => f.AmountFinanced).HasPrecision(18, 2);

            entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(f => f.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}