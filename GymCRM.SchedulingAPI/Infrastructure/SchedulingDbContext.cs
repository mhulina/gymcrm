using GymCRM.SchedulingAPI.Infrastructure.Configurations;
using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.SchedulingAPI.Infrastructure;

public class SchedulingDbContext : DbContext
{
    public virtual DbSet<TrainingSession> TrainingSessions { get; set; }
    public virtual DbSet<SessionType> SessionTypes { get; set; }
    public virtual DbSet<TrainerWorkingHours> WorkingHours { get; set; }
    public virtual DbSet<TrainerAvailability> Availabilities { get; set; }
    public virtual DbSet<TrainerDailyAvailability> DailyAvailabilities { get; set; }
    public virtual DbSet<TimeOff> TimeOff { get; set; }
    public virtual DbSet<Holiday> Holidays { get; set; }

    public SchedulingDbContext(DbContextOptions<SchedulingDbContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
			
        modelBuilder.HasDefaultSchema("scheduling_db");

        modelBuilder.ApplyConfiguration(new TrainingSessionsConfiguration());
        modelBuilder.ApplyConfiguration(new SessionTypesConfiguration());
        modelBuilder.ApplyConfiguration(new TrainerWorkingHoursConfiguration());
        modelBuilder.ApplyConfiguration(new TrainerAvailabilitiesConfiguration());
        modelBuilder.ApplyConfiguration(new TrainerDailyAvailabilityConfiguration());
        modelBuilder.ApplyConfiguration(new TimeOffConfiguration());
        modelBuilder.ApplyConfiguration(new HolidaysConfiguration());
    }
}