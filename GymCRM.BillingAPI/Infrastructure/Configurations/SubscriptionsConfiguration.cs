using GymCRM.BillingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.BillingAPI.Infrastructure.Configurations;

public class SubscriptionsConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> modelBuilder)
    {
        modelBuilder.ToTable("Subscriptions");

        modelBuilder.HasKey(x => x.Id);
        // Not unique - a member could hold more than one Subscription row over time (e.g. one
        // Cancelled, one Active), so lookups always need to also filter by Status.
        modelBuilder.HasIndex(x => x.MemberAccountGuid);

        modelBuilder.Property(x => x.MemberAccountGuid).IsRequired();
        modelBuilder.Property(x => x.PlanType).IsRequired();
        modelBuilder.Property(x => x.Status).IsRequired();
        modelBuilder.Property(x => x.StartDate).IsRequired();
        modelBuilder.Property(x => x.DateCreated).IsRequired();
        modelBuilder.Property(x => x.DateModified).IsRequired();
    }
}
