using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.SchedulingAPI.Infrastructure.Configurations;

public class TrainingSessionsConfiguration : IEntityTypeConfiguration<TrainingSession>
{
    public void Configure(EntityTypeBuilder<TrainingSession> modelBuilder)
    {
        modelBuilder.ToTable("TrainingSessions", "scheduling_db");
        
        modelBuilder.HasKey(x => x.Id);
        
        modelBuilder.Property(x => x.TrainerId).IsRequired();
        modelBuilder.Property(x => x.ClientId).IsRequired();
        modelBuilder.Property(x => x.StartTime).IsRequired();
        modelBuilder.Property(x => x.EndTime).IsRequired();
        modelBuilder.Property(x => x.DateCreated).IsRequired();
        modelBuilder.Property(x => x.DateModified).IsRequired();
        modelBuilder.Property(x => x.Status).IsRequired();
        modelBuilder.Property(x => x.Description).HasMaxLength(2000);
    }
}