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
        // StartTime/EndTime are naive wall-clock values with no timezone concept,
        // consistent with the rest of this module (TrainerWorkingHours uses TimeOnly,
        // and IsTrainerWorkingOnDateAsync compares wall-clock time directly with no
        // conversion). Npgsql's default DateTime mapping is "timestamp with time zone",
        // which rejects DateTimeKind.Unspecified values - map to the timezone-free
        // Postgres type instead of forcing a UTC concept this domain doesn't have.
        modelBuilder.Property(x => x.StartTime).IsRequired().HasColumnType("timestamp without time zone");
        modelBuilder.Property(x => x.EndTime).IsRequired().HasColumnType("timestamp without time zone");
        modelBuilder.Property(x => x.DateCreated).IsRequired();
        modelBuilder.Property(x => x.DateModified).IsRequired();
        modelBuilder.Property(x => x.Status).IsRequired();
        modelBuilder.Property(x => x.Description).HasMaxLength(2000);
    }
}