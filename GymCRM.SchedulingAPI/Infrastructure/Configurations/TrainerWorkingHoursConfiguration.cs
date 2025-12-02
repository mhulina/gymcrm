using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.SchedulingAPI.Infrastructure.Configurations;

public class TrainerWorkingHoursConfiguration : IEntityTypeConfiguration<TrainerWorkingHours>
{
    public void Configure(EntityTypeBuilder<TrainerWorkingHours> modelBuilder)
    {
        modelBuilder.ToTable("TrainerWorkingHours", "scheduling_db");
        
        modelBuilder.HasKey(x => x.Id);
        
        modelBuilder.Property(x => x.StartTime).IsRequired();
        modelBuilder.Property(x => x.EndTime).IsRequired();
        modelBuilder.Property(x => x.DateCreatedUtc).IsRequired();
        modelBuilder.Property(x => x.DateModifiedUtc).IsRequired();
        
        modelBuilder
            .HasOne<TrainerDailyAvailability>()
            .WithMany()
            .HasForeignKey(x => x.DailyAvailabilityId)
            .OnDelete(DeleteBehavior.Cascade);
            
    }
}