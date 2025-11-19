using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.SchedulingAPI.Infrastructure.Configurations;

public class TrainerAvailabilitiesConfiguration : IEntityTypeConfiguration<TrainerAvailability>
{
    public void Configure(EntityTypeBuilder<TrainerAvailability> modelBuilder)
    {
        modelBuilder.ToTable("TrainerAvailabilities", "scheduling_db");
        
        modelBuilder.HasKey(x => x.Id);
        
        modelBuilder.Property(x => x.TrainerId).IsRequired();
        modelBuilder.Property(x => x.WorkingWeekends).HasDefaultValue(false);
        modelBuilder.Property(x => x.DateCreatedUtc).IsRequired();
        modelBuilder.Property(x => x.DateModifiedUtc).IsRequired();
    }
}