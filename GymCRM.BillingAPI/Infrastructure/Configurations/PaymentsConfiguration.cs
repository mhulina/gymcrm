using GymCRM.BillingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.BillingAPI.Infrastructure.Configurations;

public class PaymentsConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> modelBuilder)
    {
        modelBuilder.ToTable("Payments");

        modelBuilder.HasKey(x => x.Id);
        modelBuilder.HasIndex(x => x.SubscriptionId);

        modelBuilder.Property(x => x.Amount).IsRequired().HasPrecision(10, 2);
        modelBuilder.Property(x => x.Method).IsRequired();
        modelBuilder.Property(x => x.Status).IsRequired();
        modelBuilder.Property(x => x.PaidAt).IsRequired();
        modelBuilder.Property(x => x.ExternalReference).HasMaxLength(250);
        modelBuilder.Property(x => x.DateCreated).IsRequired();

        modelBuilder
            .HasOne(x => x.Subscription)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
