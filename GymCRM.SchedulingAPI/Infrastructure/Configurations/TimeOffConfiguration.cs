using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.SchedulingAPI.Infrastructure.Configurations;

public class TimeOffConfiguration : IEntityTypeConfiguration<TimeOff>
{
    public void Configure(EntityTypeBuilder<TimeOff> modelBuilder)
    {
        modelBuilder.ToTable("TimeOff", "scheduling_db");

        modelBuilder.HasKey(x => x.Id);
        
        modelBuilder.Property(x => x.TrainerId).IsRequired();
        modelBuilder.Property(x => x.Date).IsRequired();
        modelBuilder.Property(x => x.Reason).IsRequired();
        modelBuilder.Property(x => x.DateCreated).IsRequired();
        modelBuilder.Property(x => x.DateModified).IsRequired();
    }
}