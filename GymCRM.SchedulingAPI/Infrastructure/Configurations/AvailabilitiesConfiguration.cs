using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.SchedulingAPI.Infrastructure.Configurations;

public class AvailabilitiesConfiguration : IEntityTypeConfiguration<Availability>
{
    public void Configure(EntityTypeBuilder<Availability> modelBuilder)
    {
        modelBuilder.ToTable("Availabilities", "scheduling_db");
        
        modelBuilder.HasKey(x => x.Id);
        
        modelBuilder.Property(x => x.MemberId).IsRequired();
        modelBuilder.Property(x => x.StartDate).IsRequired();
        modelBuilder.Property(x => x.EndDate).IsRequired();
        modelBuilder.Property(x => x.IsAvailable).IsRequired();
        modelBuilder.Property(x => x.DayOfWeek).IsRequired();
    }
}