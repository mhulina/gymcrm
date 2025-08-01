using GymCRM.SchedulingAPI.Infrastructure.Configurations;
using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.SchedulingAPI.Infrastructure;

public class SchedulingDbContext : DbContext
{
    public virtual DbSet<TrainingSession> TrainingSessions { get; set; }
    public virtual DbSet<SessionType> SessionTypes { get; set; }

    public SchedulingDbContext(DbContextOptions<SchedulingDbContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
			
        modelBuilder.HasDefaultSchema("scheduling_db");

        modelBuilder.ApplyConfiguration(new TrainingSessionsConfiguration());
    }
}