using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.SchedulingAPI.Infrastructure.Configurations;

public class TrainerDailyAvailabilityConfiguration : IEntityTypeConfiguration<TrainerDailyAvailability>
{
    public void Configure(EntityTypeBuilder<TrainerDailyAvailability> modelBuilder)
    {
        modelBuilder.ToTable("TrainerDailyAvailabilities", "scheduling_db");
        
        modelBuilder.HasKey(x => x.Id);
        
        modelBuilder.Property(x => x.DayOfWeek).IsRequired();
        modelBuilder.Property(x => x.IsDayOff).HasDefaultValue(false);
        modelBuilder.Property(x => x.DateCreatedUtc).IsRequired();
        modelBuilder.Property(x => x.DateModifiedUtc).IsRequired();
        
        modelBuilder
            .HasOne(x => x.Availability)
            .WithMany(x => x.DailyAvailabilities)
            .HasForeignKey(x => x.AvailabilityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}